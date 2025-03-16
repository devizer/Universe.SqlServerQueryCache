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

public class SqlCacheHtmlExporter
{
    public readonly DbProviderFactory DbProvider;
    public readonly string ConnectionString;

    public IEnumerable<QueryCacheRow> Rows { get; protected set; } // Available after Export
    public List<SummaryRow> Summary { get; protected set; } // Available after Export
    public List<DatabaseTabRow> DatabaseTabRows { get; protected set; } // Available after Export
    private JsStringConstants Strings = new JsStringConstants();

    private IEnumerable<TableHeaderDefinition> _tableTopHeaders;


    private const string DownloadIconSvg =
        @"<svg id='svgDownloadIcon' xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='400' height='400' viewBox='0, 0, 400,400'>
<style id='style42'>
  .dark-light { fill: #111 }
  @media (prefers-color-scheme: dark) { .dark-light { fill: #DDD } }
</style>
<g id='svgIconGraphics'><path id='path0' d='M187.880 19.069 C 178.822 22.159,172.261 29.430,169.994 38.889 C 169.345 41.594,169.175 56.705,169.158 113.243 L 169.136 184.202 142.130 157.306 C 116.453 131.734,114.896 130.302,110.494 128.218 C 87.625 117.390,64.023 140.817,74.433 164.009 C 76.535 168.691,179.142 271.666,183.951 273.919 C 192.444 277.897,200.149 277.897,208.642 273.919 C 213.280 271.746,315.893 168.761,318.115 164.049 C 328.874 141.230,304.865 117.333,282.099 128.200 C 277.711 130.295,276.059 131.814,250.463 157.306 L 223.457 184.202 223.435 113.243 C 223.411 36.793,223.495 38.864,220.153 32.276 C 214.366 20.867,199.929 14.959,187.880 19.069 M82.099 321.552 C 55.748 326.818,54.577 366.187,80.547 373.736 C 85.540 375.188,307.052 375.188,312.046 373.736 C 335.627 366.881,337.975 334.615,315.706 323.446 L 311.420 321.296 197.840 321.208 C 135.370 321.160,83.287 321.314,82.099 321.552 ' stroke='none' class='SvgIcon dark-light' fill-rule='evenodd'></path></g>
</svg>";

    static readonly string DownloadIconSrcEmbedded = $"data:image/svg+xml;base64,{Convert.ToBase64String(new ASCIIEncoding().GetBytes(DownloadIconSvg))}";

    public SqlCacheHtmlExporter(DbProviderFactory dbProvider, string connectionString)
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

    public string Export()
    {
        Rows = QueryCacheReader.Read(DbProvider, ConnectionString).ToList();
        _tableTopHeaders = AllSortingDefinitions.GetHeaders().ToArray();
        _tableTopHeaders.First().Caption = Rows.Count() == 0 ? "No Data" : Rows.Count() == 1 ? "Summary on 1 query" : $"Summary on {Rows.Count()} queries";


        var selectedSortProperty = "Content_AvgElapsedTime";
        StringBuilder htmlTables = new StringBuilder();
        htmlTables.AppendLine($"<script>");
        // SCRIPT: Selected Sort Column
        htmlTables.AppendLine($"selectedSortProperty = '{selectedSortProperty}';theFile={{}};");
        // SCRIPT: Databases Id and Names
        BuildDatabaseTabRows();
        htmlTables.AppendLine($"dbList={DatabaseTabRows.ToJsonString()};");
        // SCRIPT: Query Plan (optional) for each Query
        foreach (var queryCacheRow in Rows)
        {
            // Download SQL STATEMENT IS NOT IMPLEMENTED
            if (false && !string.IsNullOrEmpty(queryCacheRow.SqlStatement))
                if (Strings.GetOrAddThenReturnKey(queryCacheRow.SqlStatement, out var keySqlStatement))
                    htmlTables.AppendLine($"\ttheFile[\"{keySqlStatement}\"] = {JsExtensions.EncodeJsString(queryCacheRow.SqlStatement)};");

            if (!string.IsNullOrEmpty(queryCacheRow.QueryPlan))
                if (Strings.GetOrAddThenReturnKey(queryCacheRow.QueryPlan, out var keyQueryPlan))
                    htmlTables.AppendLine($"\ttheFile[\"{keyQueryPlan}\"] = {JsExtensions.EncodeJsString(queryCacheRow.QueryPlan)};");
        }
        htmlTables.AppendLine($"</script>");

        void CollectGarbage()
        {
            if (ReportBuilderConfiguration.NeedGarbageCollection)
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        void IterateSortingColumn(ColumnDefinition columnDefinition)
        {
            bool isSelected = selectedSortProperty == columnDefinition.GetHtmlId();
            /*
            string htmlForSortedProperty = @$"<div id='{columnDefinition.GetHtmlId()}' class='{(isSelected ? "" : "Hidden")}'>
{Export(columnDefinition, isSelected)}
</div>";

            htmlTables.AppendLine(htmlForSortedProperty);
            */
            htmlTables.AppendLine($"<div id='{columnDefinition.GetHtmlId()}' class='{(isSelected ? "" : "Hidden")}'>");
            htmlTables.AppendLine(Export(columnDefinition, isSelected));
            htmlTables.AppendLine($"</div>");

            CollectGarbage();
        }

        foreach (ColumnDefinition sortingDefinition in AllSortingDefinitions.Get())
        {
            IterateSortingColumn(sortingDefinition);
        }

        var css = ExporterResources.StyleCSS
                  + Environment.NewLine + ExporterResources.SqlSyntaxHighlighterCss
                  + Environment.NewLine + ExporterResources.FloatButtonCss
                  + Environment.NewLine + ExporterResources.FlexedListCss
                  + Environment.NewLine + ExporterResources.ModalSummaryCss
                  + Environment.NewLine + ExporterResources.DownloadIconCss
                  + Environment.NewLine + ExporterResources.TabsStylesCss
                  + Environment.NewLine + ExporterResources.DatabasesStylesCss
            ;

        var htmlSummary = ExportModalAsHtml();

        var finalHtml = htmlSummary + Environment.NewLine + htmlTables;
        var finalJs = ExporterResources.MainJS
                      + Environment.NewLine + ExporterResources.ModalSummaryJS
                      + Environment.NewLine + ExporterResources.TabsSummaryJs
                      + Environment.NewLine + ExporterResources.DatabasesJs
            ;

        var ret = ExporterResources.HtmlTemplate
            .Replace("{{ Body }}", finalHtml)
            .Replace("{{ MainJS }}", finalJs)
            .Replace("{{ StylesCSS }}", css);

        CollectGarbage();
        return ret;
    }

    private void BuildDatabaseTabRows()
    {
        this.DatabaseTabRows = Rows
            .ToLookup(x => new { dbId = x.DatabaseId, dbName = x.DatabaseName })
            .Select(x => new { x.Key.dbId, x.Key.dbName, isSystem = SqlDatabaseInfo.IsSystemDatabase(x.Key.dbName), queriesCount = x.Count() })
            .Where(x => !string.IsNullOrEmpty(x.dbName))
            .Distinct()
            .OrderBy(x => !x.isSystem)
            .ThenBy(x => x.dbName)
            .Select(x => new DatabaseTabRow() { DatabaseId = x.dbId, DatabaseName = x.dbName, IsSystem = x.isSystem, QueriesCount = x.queriesCount })
            .ToList();
    }

    public string Export(ColumnDefinition sortByColumn, bool isFieldSelected)
    {
        var headers = _tableTopHeaders.ToArray();
        var sortedRows = sortByColumn.SortAction(Rows).ToArray();
        StringBuilder htmlTable = new StringBuilder();
        htmlTable.AppendLine("  <table class='Metrics'><thead>");

        // Table Header: 1st Row (metrics categories)
        htmlTable.AppendLine("  <tr>");
        foreach (var header in headers)
        {
            htmlTable.AppendLine($"    <th colspan='{header.Columns.Count}' class='TableHeaderGroupCell'>{header.Caption}</th>");
        }
        htmlTable.AppendLine("  </tr>");

        // Table Header: 2nd row (concrete metrics)
        htmlTable.AppendLine("  <tr>");
        var columnDefinitions = headers.SelectMany(h => h.Columns).ToArray();
        foreach (var column in columnDefinitions)
        {
            bool isThisSorting = column.PropertyName == sortByColumn.PropertyName;
            const string arrows = " ⇓ ⇩ ↓ ↡";
            // var attrs = "";
            // var onClick = $"onclick='SelectContent(\"{column.GetHtmlId()}\"); alert('HAHA'); return false;'";
            // if (!isFieldSelected && column.AllowSort) attrs = $"style=\"cursor: pointer; display: inline-block;\" class='SortButton' data-sorting='{column.GetHtmlId()}'";
            // var spanSortingParameter = $"<span id='SortingParameter' class='Hidden'>{column.GetHtmlId()}</span>";
            htmlTable.AppendLine($"    <th class='TableHeaderCell {(isThisSorting ? "Selected" : "")}' data-sorting='{column.GetHtmlId()}'><button>{column.TheCaption}</button></th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  </thead>");

        htmlTable.AppendLine("  <tbody>");
        foreach (QueryCacheRow row in sortedRows)
        {
            // Metrics Row
            var hasDatabase = row.DatabaseId != null && !string.IsNullOrEmpty(row.DatabaseName);
            var trClass = hasDatabase ? $"DB-Id-{row.DatabaseId}" : null;
            var trDataDbId = hasDatabase ? $" data-db-id='{row.DatabaseId}'" : null;
            // TODO: Choose either DB-Id-? class or data-db-id attribute
            htmlTable.AppendLine($"  <tr class='MetricsRow {trClass}'{trDataDbId}>");
            foreach (ColumnDefinition column in columnDefinitions)
            {
                var value = column.PropertyAccessor(row);
                var valueString = GetValueAsHtml(value, row, column);
                htmlTable.AppendLine($"\t\t<td>{valueString}</td>");
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
                var htmlDownloadIcon = "⇓";
                htmlDownloadIcon = $"<img style='width: 20px; height: 20px' src='{DownloadIconSrcEmbedded}' />";
                htmlDownloadIcon = $"<div class='Icon'><img style='width: 16px; height: 16px' src='{DownloadIconSrcEmbedded}' /></div>";
                htmlDownloadIcon = "&nbsp;";
                htmlDownloadIcon = "<div class='SvgIcon' />";
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
        var con = DbProvider.CreateConnection();
        con.ConnectionString = ConnectionString;
        var man = con.Manage();
        var hostPlatform = man.HostPlatform;
        var mediumVersion = man.MediumServerVersion;
        if (man.ShortServerVersion.Major >= 14) mediumVersion += $" on {hostPlatform}";

        // Custom Headers
        var customSummaryRows = CustomSummaryRowReader.GetCustomSummary();
        var customHeaders = customSummaryRows.Where(x => x.IsHeader);
        var sep = Environment.NewLine + "\t\t\t\t\t";
        string GetSummaryHeaderRowHtml(CustomSummaryRowReader.CustomSummaryRow customSummaryRow)
        {
            return
                ((string.IsNullOrEmpty(customSummaryRow.Title) ? "" : $"{customSummaryRow.Title} ")
                + customSummaryRow.DescriptionAsHtml).Trim();
        }
        var customHeadersHtml = string.Join(sep, customHeaders.Select(x => $"<br/>{GetSummaryHeaderRowHtml(x)}").ToArray());
        // Done: Custom Header

        return $@"
    <div id='modal-summary-root' class='Modal-Summary'>
         <div class='Modal-Summary-body Capped'>
             <center>SQL Server Summary<br/>v{mediumVersion}{customHeadersHtml}<br/><br/></center>

<div class='tabs'>
  <div class='tabs__pills'>
    <button class='TabLink active' data-id='SummaryModalContent'>Summary</button>
    <button class='TabLink' data-id='DatabasesModalContent'>Filter DB</button>
  </div>

  <div class='tabs__panels'>
    <!-- Content panels for each tab -->
    <!-- Summer tab -->
    <div id='SummaryModalContent' class='active'>
        {ExportSummaryAsHtml()}
    </div>
    <div id='DatabasesModalContent'>
        {ExportDatabasesTab()}
    </div>
  </div>
</div>

        </div> <!-- Modal -->
     </div> <!-- Modal -->
";
    }

    private string ExportDatabasesTab()
    {
        StringBuilder ret = new StringBuilder();
        ret.AppendLine("<div id='DbListContainer'>");
        foreach (var databaseTabRow in DatabaseTabRows)
        {
            ret.AppendLine($@"
<div class='DbListItem'>
  <div class='DbListColumn DbListColumnCheckbox'>
    <input type='radio' data-for-db-id='{databaseTabRow.DatabaseId}' class='InputChooseDb'/>
  </div>
   <div class='DbListColumn DbListColumnTitle'>
    {HtmlExtensions.EncodeHtml(databaseTabRow.DatabaseName)}, ({databaseTabRow.QueriesCount}&nbsp;queries)
  </div>
</div>
");
        }

        ret.AppendLine("</div>");

        return ret.ToString();
    }

    private string ExportSummaryAsHtml()
    {
        SqlServerSummaryBuilder summaryBuilder = new SqlServerSummaryBuilder(DbProvider, ConnectionString, Rows.ToList());
        Summary = summaryBuilder.BuildTotalWholeSummary();
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
}

public static class SortingDefinitionExtensions
{
    public static string GetHtmlId(this ColumnDefinition arg)
    {
        return $"Content_{arg.PropertyName}";
    }
}
