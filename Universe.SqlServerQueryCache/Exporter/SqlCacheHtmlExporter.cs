using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;
using Universe.SqlServerQueryCache.TSqlSyntax;

namespace Universe.SqlServerQueryCache.Exporter;

public partial class SqlCacheHtmlExporter
{
    public readonly DbProviderFactory DbProvider;
    public readonly string ConnectionString;

    public List<QueryCacheRow> Rows { get; protected set; } // Available after Export
    public SqlQueryStatsSchema ColumnsSchema { get; protected set; }
    public List<SummaryRow> Summary { get; protected set; } // Available after Export
    public List<DatabaseTabRow> DatabaseTabRows { get; protected set; } // Available after Export
    private JsStringConstants Strings = new JsStringConstants();

    private TableHeaderDefinition[] _tableTopHeaders;


    public SqlCacheHtmlExporter(DbProviderFactory dbProvider, string connectionString) : this()
    {
        DbProvider = dbProvider;
        ConnectionString = connectionString;
    }

    public class DatabaseTabRow
    {
        public int DatabaseId { get; set; }
        public string DatabaseName { get; set; }
        public bool IsSystem { get; set; }
        public int QueriesCount { get; set; }
    }

    public void Export(TextWriter output)
    {
        TemplateEngine template = new TemplateEngine()
            .Substitute("ReportSubTitleHtml", textWriter => textWriter.WriteLine(ExportReportSubTitleAsHtml()))
            .Substitute("MainJS", textWriter => textWriter.WriteLine(ExportMainJs()))
            .Substitute("StylesCSS", textWriter => textWriter.WriteLine(ExplortMainCss()));

        using (this.LogStep("Query Final 'Query Stat'"))
        {
            QueryCacheReader reader = new QueryCacheReader(DbProvider, ConnectionString);
            Rows = reader.Read().ToList();
            ColumnsSchema = reader.ColumnsSchema;
        }

        CollectGarbage(
            $"got {Rows.Count} rows," +
            $" {Rows.Select(x => (x.QueryPlan?.Length).GetValueOrDefault() / 1024L).SmartySum()} Kb plans," +
            $" {Rows.Select(x => (x.SqlStatement?.Length).GetValueOrDefault() / 1024L).SmartySum()} Kb sql"
        );

        _tableTopHeaders = new AllSortingDefinitions(ColumnsSchema).GetHeaders().ToArray();
        _tableTopHeaders.First().Caption = Rows.Count == 0 ? "No Data" : Rows.Count() == 1 ? "Summary on 1 query" : $"Summary on {Rows.Count()} queries";

        using (this.LogStep("Build Summary Row List"))
        {
            SqlServerSummaryBuilder summaryBuilder = new SqlServerSummaryBuilder(DbProvider, ConnectionString, Rows);
            Summary = summaryBuilder.BuildTotalWholeSummary();
        }

        using (this.LogStep("Build Databases Row List")) 
            BuildDatabaseTabRows();

        void WriteHtmlBody(TextWriter htmlTables)
        {
            // Start: ExportAllHtmlTables()
            var selectedSortProperty = "Content_AvgElapsedTime";
            htmlTables.AppendLine($"<script>");
            // SCRIPT: Selected Sort Column
            htmlTables.AppendLine($"selectedSortProperty = '{selectedSortProperty}';");
            // SCRIPT: Databases Id and Names, including 0 (all the databases)
            using (this.LogStep("Render 'Databases' json"))
                htmlTables.AppendLine($"dbList={DatabaseTabRows.ToJsonString()};");

            // SCRIPT: Query Plan (optional) for each Query
            var queryPlans = Rows.Where(x => !string.IsNullOrEmpty(x.QueryPlan));
            var queryPlansCount = queryPlans.Count();
            var queryPlansTextSize = queryPlansCount > 0 ? queryPlans.Sum(x => x.QueryPlan.Length) : 0;
            using (this.LogStep($"Render {queryPlansCount} Query Plans, {queryPlansTextSize / 1024} Kb"))
            {
                htmlTables.AppendLine($"theFile={{}};");
                foreach (var queryCacheRow in Rows)
                {
                    // Download SQL STATEMENT IS NOT IMPLEMENTED
                    if (false && !string.IsNullOrEmpty(queryCacheRow.SqlStatement))
                        if (Strings.GetOrAddThenReturnKey(queryCacheRow.SqlStatement, out var keySqlStatement))
                            htmlTables.AppendLine($"\ttheFile[\"{keySqlStatement}\"] = {JsExtensions.EncodeJsString(queryCacheRow.SqlStatement)};");

                    if (!string.IsNullOrEmpty(queryCacheRow.QueryPlan))
                        if (Strings.GetOrAddThenReturnKey(queryCacheRow.QueryPlan, out var keyQueryPlan))
                            htmlTables
                                .Append($"\ttheFile[\"{keyQueryPlan}\"] = ")
                                .WriteEncodedJsString(queryCacheRow.QueryPlan)
                                .AppendLine(";");
                }
            }

            htmlTables.AppendLine($"</script>");

            using (LogStep("Render 'Modal Dialog' as html"))
            {
                string modalAsHtml = ExportModalAsHtml();
                htmlTables.AppendLine(modalAsHtml);
            }

            foreach (ColumnDefinition sortingDefinition in new AllSortingDefinitions(ColumnsSchema).Get())
            {
                using (this.LogStep($"Render Report Sorted by '{sortingDefinition.PropertyName}'"))
                {
                    bool isSelected = selectedSortProperty == sortingDefinition.GetHtmlId();
                    htmlTables.AppendLine($"<div id='{sortingDefinition.GetHtmlId()}' class='{(isSelected ? "" : "Hidden")}'>");
                    Export(output, sortingDefinition, isSelected);
                    htmlTables.AppendLine($"</div>");
                }

                CollectGarbage();
            }
            // Finish: ExportAllHtmlTables()
        }

        template.Substitute("Body", WriteHtmlBody);

        using(LogStep("Render Total HtmlTemplate.html"))
            template.Produce(output, ExporterResources.HtmlTemplate);

        CollectGarbage();
        
    }

    private static string ExplortMainCss()
    {
        return ExporterResources.StyleCSS
               + Environment.NewLine + ExporterResources.SqlSyntaxHighlighterCss
               + Environment.NewLine + ExporterResources.FloatButtonCss
               + Environment.NewLine + ExporterResources.FlexedListCss
               + Environment.NewLine + ExporterResources.ModalSummaryCss
               + Environment.NewLine + ExporterResources.DownloadIconCss
               + Environment.NewLine + ExporterResources.TabsStylesCss
               + Environment.NewLine + ExporterResources.DatabasesStylesCss
            ;
    }

    private static string ExportMainJs()
    {
        return ExporterResources.MainJS
               + Environment.NewLine + ExporterResources.ModalSummaryJS
               + Environment.NewLine + ExporterResources.TabsSummaryJs
               + Environment.NewLine + ExporterResources.DatabasesJs
               + Environment.NewLine + ExporterResources.ColumnsChooserJs
            ;
    }

    private void BuildDatabaseTabRows()
    {
        DatabaseTabRow anyDbRow = new DatabaseTabRow()
        {
            DatabaseId = 0,
            DatabaseName = "All the Databases",
            IsSystem = false,
            QueriesCount = Rows.Count
        };

        var actualDbList = Rows
                .ToLookup(x => new { dbId = x.DatabaseId, dbName = x.DatabaseName })
                .Select(x => new { x.Key.dbId, x.Key.dbName, isSystem = SqlDatabaseInfo.IsSystemDatabase(x.Key.dbName), queriesCount = x.Count() })
                .Where(x => !string.IsNullOrEmpty(x.dbName))
                .Distinct()
                .OrderBy(x => !x.isSystem)
                .ThenBy(x => x.dbName)
                .Select(x => new DatabaseTabRow() { DatabaseId = x.dbId, DatabaseName = x.dbName, IsSystem = x.isSystem, QueriesCount = x.queriesCount })
            ;

        var dbRows = new[] { anyDbRow }.Concat(actualDbList).ToList();
        DatabaseTabRows = dbRows;
    }

    // TODO: Replace StringBuilder by TextWriter
    public string Export(TextWriter output, ColumnDefinition sortByColumn, bool isFieldSelected)
    {
        var htmlTable = output;
        var headers = _tableTopHeaders;
        var sortedRows = sortByColumn.SortAction(Rows).ToArray();
        htmlTable.AppendLine("  <table class='Metrics'><thead>");

        // Table Header: 1st Row (metrics categories)
        htmlTable.AppendLine("  <tr>");
        foreach (var header in headers)
        {
            bool isFirst = header == headers.FirstOrDefault();
            var class1 = (isFirst ? "MetricsSummaryHeaderCell" : "");
            var class2 = header.Visible ? "" : "Hidden";
            htmlTable.AppendLine($"    <th data-columns-header-id='{header.Caption}' colspan='{header.Columns.Count}' class='TableHeaderGroupCell {class1} {class2}'>{header.Caption}</th>");
        }
        htmlTable.AppendLine("  </tr>");

        // Table Header: 2nd row (concrete metrics)
        htmlTable.AppendLine("  <tr>");
        var columnDefinitions = headers.SelectMany(h => h.Columns).ToArray();
        foreach (var header in headers)
            foreach (var column in header.Columns)
        {
            bool isThisSorting = column.PropertyName == sortByColumn.PropertyName;
            const string arrows = " ⇓ ⇩ ↓ ↡";
                // var attrs = "";
                // var onClick = $"onclick='SelectContent(\"{column.GetHtmlId()}\"); alert('HAHA'); return false;'";
                // if (!isFieldSelected && column.AllowSort) attrs = $"style=\"cursor: pointer; display: inline-block;\" class='SortButton' data-sorting='{column.GetHtmlId()}'";
                // var spanSortingParameter = $"<span id='SortingParameter' class='Hidden'>{column.GetHtmlId()}</span>";
                var class1 = (isThisSorting ? "Selected" : "");
                var class2 = header.Visible ? "" : "Hidden";
                htmlTable.AppendLine($"    <th data-columns-header-id='{header.Caption}' class='TableHeaderCell {class1} {class2}' data-sorting='{column.GetHtmlId()}'><button>{column.TheCaption}</button></th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  </thead>");

        htmlTable.AppendLine("  <tbody>");
        foreach (QueryCacheRow row in sortedRows)
        {
            // Metrics Row
            var hasDatabase = row.DatabaseId != null && !string.IsNullOrEmpty(row.DatabaseName);
            var trClass = hasDatabase ? $"DB-Id-{row.DatabaseId}" : null;
            // var trDataDbId = hasDatabase ? $" data-db-id='{row.DatabaseId}'" : null;
            // data-db-id required for javascript show/hide db
            var trDataDbId = hasDatabase ? $" data-db-id='{row.DatabaseId}'" : " data-db-id='-1'";
            // TODO: Choose either DB-Id-? class or data-db-id attribute
            htmlTable.AppendLine($"  <tr class='MetricsRow {trClass} hidden'{trDataDbId}>");
            foreach (var header in headers)
                foreach (ColumnDefinition column in header.Columns)
            {
                var value = column.PropertyAccessor(row);
                var valueString = GetValueAsHtml(value, row, column);
                var class2 = header.Visible ? "" : " class='Hidden'";
                htmlTable.AppendLine($"\t\t<td data-columns-header-id='{header.Caption}'{class2}>{valueString}</td>");
            }
            htmlTable.AppendLine("\t</tr>");
            htmlTable.AppendLine($"\t<tr class='SqlRow  {trClass}'{trDataDbId}>");

            // BUILD META LINE as SQL: Database, ObjectType and ObjectName
            StringBuilder sqlMeta = new StringBuilder(); // Use [DB-Stress]; -- For SQL STORED PROCEDURE [Stress By Select]
            if (!string.IsNullOrEmpty(row.DatabaseName)) sqlMeta.Append($"Use [{row.DatabaseName}];");
            var objectType = row.ObjectType?.Replace("_", " ");
            if (objectType != null) objectType = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(objectType);
            string htmlObjectSchemaName = string.IsNullOrEmpty(row.ObjectSchemaName) ? "" : $"[{row.ObjectSchemaName}].";
            if (!string.IsNullOrEmpty(row.ObjectName)) sqlMeta.Append(sqlMeta.Length == 0 ? "" : " ").Append($"-- For {objectType} {htmlObjectSchemaName}[{row.ObjectName}]".Replace("  [", " ["));
            // Done: sqlMeta

            var rowSqlStatement = sqlMeta.Length == 0 ? row.SqlStatement : $"{sqlMeta}{Environment.NewLine}{row.SqlStatement}";
            var tsqlHtmlString = TSqlToVanillaHtmlConverter.ConvertTSqlToHtml(rowSqlStatement, SqlSyntaxColors.DarkTheme);
            var htmlSqlPlanButton = "";
            if (!string.IsNullOrEmpty(row.QueryPlan))
            {
                var keyQueryPlan = Strings.GetKey(row.QueryPlan);
                var jsDownloadPlan = $"dynamicDownloading(theFile['{keyQueryPlan}'], 'text/xml', 'SQL Execution Plan {keyQueryPlan}.sqlplan');";
                var htmlDownloadIcon = "<div class='SvgIconDownload' />";
                htmlSqlPlanButton = $"<div class='SqlPlanDownload' Title='Open Execution Plan' onclick=\"{jsDownloadPlan}; return false;\">{htmlDownloadIcon}</div>";
            }

            // Query Plan Cell (2 columns)
            htmlTable.AppendLine($"\t\t<td colspan='2' class='SqlPadding'>{htmlSqlPlanButton}</td>");

            // T-SQL Cell (rest of columns)
            htmlTable.AppendLine($"\t\t<td colspan='{columnDefinitions.Length - 2}'><pre>{tsqlHtmlString}</pre></td>");
            htmlTable.AppendLine("\t</tr>");
        }
        htmlTable.AppendLine("  </tbody>");

        htmlTable.AppendLine("  </table>");
        return htmlTable.ToString();
    }


    string ExportModalAsHtml()
    {
        var mediumVersion = GetMediumVersionAndPlatform();

        // Custom Headers
        var customSummaryRows = CustomSummaryRowReader.GetCustomSummary();
        var customHeaders = customSummaryRows.Where(x => x.IsHeader);
        var sep = Environment.NewLine + "\t\t\t\t\t";
        var customHeadersHtml = string.Join(sep, customHeaders.Select(x => $"<br/>{x.GetSummaryHeaderRowHtml()}").ToArray());
        // Done: Custom Header

        return $@"
    <div id='modal-summary-root' class='Modal-Summary'>
         <div class='Modal-Summary-body Capped'>
             <center>SQL Server Summary<br/>v{mediumVersion}{customHeadersHtml}<br/></center>

<div class='tabs'>
  <div class='tabs__pills'>
    <button class='TabLink active' data-id='SummaryModalContent'>Summary</button>
    <button class='TabLink' data-id='DatabasesModalContent'>Databases</button>
    <button class='TabLink' data-id='ColumnsChooserModalContent'>Columns</button>
  </div>

  <div class='tabs__panels'>
    <!-- Content panels for each tab -->
    <div id='SummaryModalContent' class='active'>
        {ExportSummaryAsHtml()}
    </div>
    <div id='DatabasesModalContent'>
        {ExportDatabasesTab()}
    </div>
    <div id='ColumnsChooserModalContent'>
        {ExportColumnsChooserTab()}
    </div>
  </div>
</div>

        </div> <!-- Modal -->
     </div> <!-- Modal -->
";
    }

    private string ExportColumnsChooserTab()
    {
        using var _ = LogStep("Export 'Columns' tab as html");

        StringBuilder ret = new StringBuilder();
        ret.AppendLine("<div id='DbListContainer'>");
        // data-for-columns-header-id - checkboxes
        // data-columns-header-id - cells

        var headers = new AllSortingDefinitions(ColumnsSchema).GetHeaders();
        foreach (var header in headers.Where(x => x.AllowHide))
        {
            ret.AppendLine($@"
<div class='DbListItem'>
  <div class='DbListColumn DbListColumnCheckbox'>
    <input type='checkbox' data-for-columns-header-id='{header.Caption}' class='InputChooseDb' {(header.Visible ? "checked" : "")}/>
  </div>
   <div class='DbListColumn DbListColumnTitle'>
    {HtmlExtensions.EncodeHtml(header.Caption)}
  </div>
</div>
");
        }

        ret.AppendLine("</div>");

        return ret.ToString();
    }
    private string ExportDatabasesTab()
    {
        using var _ = LogStep("Export 'Databases' tab as html");

        StringBuilder ret = new StringBuilder();
        ret.AppendLine("<div id='DbListContainer'>");

        var dbRows = DatabaseTabRows;
        var idSelectedDb = 0;
        foreach (var databaseTabRow in dbRows)
        {
            ret.AppendLine($@"
<div class='DbListItem'>
  <div class='DbListColumn DbListColumnCheckbox'>
    <input type='radio' data-for-db-id='{databaseTabRow.DatabaseId}' class='InputChooseDb' {(databaseTabRow.DatabaseId == idSelectedDb ? "checked" : "")}/>
  </div>
   <div class='DbListColumn DbListColumnTitle'>
    {HtmlExtensions.EncodeHtml(databaseTabRow.DatabaseName)} ({databaseTabRow.QueriesCount}&nbsp;{(databaseTabRow.QueriesCount > 1 ? "queries" : "query")})
  </div>
</div>
");
        }

        ret.AppendLine("</div>");

        return ret.ToString();
    }

    string ExportReportSubTitleAsHtml()
    {
        var mediumVersion = GetMediumVersionAndPlatform();

        List<string> pieces = new List<string>();
        pieces.Add($"v{mediumVersion}");
        pieces.AddRange(Summary.Where(x => x.IsHeader).Select(x => x.GetSummaryHeaderRowHtml()));

        StringBuilder ret = new StringBuilder();
        foreach (var piece in pieces)
        {
            ret.AppendLine($"\t\t\t\t<div class='ReportSubTitle'>{piece}</div>");
        }
        return ret.ToString();
    }

    public string GetMediumVersionAndPlatform()
    {
        using var _ = LogStep("Fetch SQL Server Version and Platform");
        var con = DbProvider.CreateConnection();
        con.ConnectionString = ConnectionString;
        var man = con.Manage();
        var hostPlatform = man.HostPlatform;
        var mediumVersion = man.MediumServerVersion;
        if (man.ShortServerVersion.Major >= 14) mediumVersion += $" on {hostPlatform}";
        return mediumVersion;
    }

    private string ExportSummaryAsHtml()
    {
        using var _ = LogStep("Export 'Summary' tab as html");
        List<SummaryRow> summaryRows = Summary;

        StringBuilder ret = new StringBuilder();
        string padding = "\t\t";
        ret.AppendLine($"{padding}{padding}<div class='SqlSummaryContainer'>");
        foreach (SummaryRow summaryRow in summaryRows.Where(x => !x.IsHeader))
        {
            ret.AppendLine($@"
{padding}{padding}<dl class=""flexed-list"">
{padding}{padding}{padding}<dt><span>{summaryRow.Title}:</span></dt>
{padding}{padding}{padding}<dd>{summaryRow.GetFormatted(true)}</dd>
{padding}{padding}</dl>");
        }
        ret.AppendLine($"{padding}{padding}</div>");

        return ret.ToString();
    }



    private static string GetValueAsHtml(object value, QueryCacheRow row, ColumnDefinition column)
    {
        string valueString;
        if (value is long l)
            // valueString = l == 0 ? "" : l.ToString("n0");
            valueString = HtmlNumberFormatter.Format(l, 0, "");
        else if (value is double d)
            // valueString = Math.Abs(d) <= Double.Epsilon ? "" : d.ToString("n2");
            valueString = HtmlNumberFormatter.Format(d, 2, "");
        else if (value is TimeSpan t)
            valueString = ElapsedFormatter.FormatElapsedAsHtml(t);
        else
            valueString = Convert.ToString(value);

        return valueString;
    }

    void CollectGarbage() => CollectGarbage(null);
    void CollectGarbage(string details)
    {
        if (ReportBuilderConfiguration.NeedGarbageCollection)
        {
            var title = $"Collect Garbage{(string.IsNullOrEmpty(details) ? "" : $", {details}")}";
            using (this.LogStep(title))
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }
    }
}

public static class SqlCacheHtmlExporterExtensions
{
    public static void ExportToFile(this SqlCacheHtmlExporter exporter, string fileName)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        }
        catch
        {
        }

        using(FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 32768))
        using (StreamWriter wr = new StreamWriter(fs, new UTF8Encoding(false)))
        {
            exporter.Export(wr);
        }
    }
}

public static class TextWriterExtensions
{
    public static TextWriter AppendLine(this TextWriter output, string line)
    {
        output.WriteLine(line);
        return output;
    }
    public static TextWriter Append(this TextWriter output, string text)
    {
        output.Write(text);
        return output;
    }
    public static TextWriter Append(this TextWriter output, char ch)
    {
        output.Write(ch);
        return output;
    }
}
public static class SortingDefinitionExtensions
{
    public static string GetHtmlId(this ColumnDefinition arg)
    {
        return $"Content_{arg.PropertyName}";
    }
}
public static class SumExtensions
{
    public static long SmartySum(this IEnumerable<long> list)
    {
        return list?.Any() == true ? list.Sum() : 0;
    }
}
