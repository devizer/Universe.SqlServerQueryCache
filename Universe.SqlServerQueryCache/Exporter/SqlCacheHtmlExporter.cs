using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
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
    public IEnumerable<SummaryRow> Summary { get; protected set; } // Available after Export

    private IEnumerable<TableHeaderDefinition> _tableTopHeaders;

    public SqlCacheHtmlExporter(DbProviderFactory dbProvider, string connectionString)
    {
        DbProvider = dbProvider;
        ConnectionString = connectionString;
    }

    public string Export()
    {
        Rows = QueryCacheReader.Read(DbProvider, ConnectionString).ToList();
        _tableTopHeaders = AllSortingDefinitions.GetHeaders().ToArray();
        _tableTopHeaders.First().Caption = Rows.Count() == 0 ? "No Data" : Rows.Count() == 1 ? "Summary on 1 query" : $"Summary on {Rows.Count()} queries";

        var selectedSortProperty = "Content_AvgElapsedTime";
        StringBuilder htmlTables = new StringBuilder();
        htmlTables.AppendLine($"<script>selectedSortProperty = '{selectedSortProperty}';</script>");
        foreach (ColumnDefinition sortingDefinition in AllSortingDefinitions.Get())
        {
            bool isSelected = selectedSortProperty == sortingDefinition.GetHtmlId();
            string htmlForSortedProperty = @$"<div id='{sortingDefinition.GetHtmlId()}' class='{(isSelected ? "" : "Hidden")}'>
{Export(sortingDefinition, isSelected)}
</div>";

            htmlTables.AppendLine(htmlForSortedProperty);
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var css = ExporterResources.StyleCSS
                  + Environment.NewLine + ExporterResources.SqlSyntaxHighlighterCss
                  + Environment.NewLine + ExporterResources.FloatButtonCss
                  + Environment.NewLine + ExporterResources.FlexedListCss
                  + Environment.NewLine + ExporterResources.ModalSummaryCss;

        var htmlSummary = ExportModalSummaryAsHtml();

        var finalHtml = htmlSummary + Environment.NewLine + htmlTables;
        var finalJs = ExporterResources.MainJS + Environment.NewLine + ExporterResources.ModalSummaryJS;

        return ExporterResources.HtmlTemplate
            .Replace("{{ Body }}", finalHtml)
            .Replace("{{ MainJS }}", finalJs)
            .Replace("{{ StylesCSS }}", css);
    }

    string ExportModalSummaryAsHtml()
    {
        return $@"
    <div id=""modal-summary-root"" class=""Modal-Summary"">
         <div class=""Modal-Summary-body Capped"">
<center>SQL Server Summary</center><br/>
{ExportSummaryAsHtml()}
        </div>
     </div>
";
    }
    
    private string ExportSummaryAsHtml()
    {
        List<SummaryRow> summaryRows = SqlSummaryTextExporter.ExportStructured(Rows).ToList();
        Summary = summaryRows;

        var con = DbProvider.CreateConnection();
        con.ConnectionString = ConnectionString;
        var man = con.Manage();
        var hostPlatform = man.HostPlatform;
        var mediumVersion = man.MediumServerVersion;
        // string summaryReportAsText = SqlSummaryTextExporter.ExportAsText(Rows, $"SQL Server {mediumVersion} on {hostPlatform}");

        // SQL Performance Counters
        SqlPerformanceCountersReader perfReader = new SqlPerformanceCountersReader(DbProvider, ConnectionString);
        var summaryCounters = perfReader.ReadBasicCounters();
        summaryRows.Add(new SummaryRow("Database Pages", FormatKind.Pages, summaryCounters.BufferPages));
        summaryRows.Add(new SummaryRow("Page Reads/sec", FormatKind.Pages, summaryCounters.PageReadsPerSecond));
        summaryRows.Add(new SummaryRow("Page Writes/sec", FormatKind.Pages, summaryCounters.PageWritesPerSecond));

        // Sys Info
        var summarySysInfo = BuildSysInfoSummary().ToArray();
        summaryRows.AddRange(summarySysInfo);

        // version
        var versionRow = new SummaryRow("Version", FormatKind.Unknown, $"{mediumVersion} on {hostPlatform}");
        summaryRows.Add(versionRow);


        StringBuilder ret = new StringBuilder();
        ret.AppendLine("<div class='SqlSummaryContainer'>");
        foreach (var summaryRow in summaryRows)
        {
            char padding = '\t';
            ret.AppendLine($@"{padding}<dl class=""flexed-list"">
{padding}{padding}<dt><span>{summaryRow.Title}:</span></dt>
{padding}{padding}<dd>{summaryRow.GetFormatted(true)}</dd>
{padding}</dl>");
        }
        ret.AppendLine("</div>");

        return ret.ToString();
    }

    private IEnumerable<SummaryRow> BuildSysInfoSummary()
    {
        // Sys Info
        ICollection<SqlSysInfoReader.Info> sqlSysInfoList = SqlSysInfoReader.Query(DbProvider, ConnectionString);
        var sqlSysInfo = sqlSysInfoList.ToLookup(x => x.Name).Select(x => new { K = x.Key, V = x.FirstOrDefault()?.Value }).ToDictionary(x => x.K, x => x.V, StringComparer.OrdinalIgnoreCase);
        Func<string, long?> getLong = name => sqlSysInfo.TryGetValue(name, out var raw) ? Convert.ToInt64(raw) : null;
        var cpuCount = getLong("cpu_count");
        if (cpuCount.HasValue) yield return new SummaryRow("CPU Count", FormatKind.Natural, cpuCount.Value);
        var physical_memory_in_bytes = getLong("physical_memory_in_bytes");
        var physical_memory_kb = getLong("physical_memory_kb");
        long? memKb = physical_memory_kb ?? physical_memory_in_bytes / 1024;
        if (memKb.HasValue) yield return new SummaryRow("Physical Memory (MB)", FormatKind.Natural, memKb.Value / 1024);

        var bpool_Committed = getLong("Bpool_Committed");
        if (bpool_Committed.HasValue) yield return new SummaryRow("Buffer Pages", FormatKind.Pages, bpool_Committed.Value);
        var bpool_Commit_Target = getLong("Bpool_Commit_Target");
        var bpool_Visible = getLong("Bpool_Visible");
        var visiblePages = GetMin(bpool_Commit_Target, bpool_Visible);
        if (visiblePages.HasValue) yield return new SummaryRow("Visible Buffer Pages", FormatKind.Pages, visiblePages.Value);

        var Committed_Kb = getLong("Committed_Kb");
        var Committed_Target_Kb = getLong("Committed_Target_Kb");
        var Visible_Target_Kb = getLong("Visible_Target_Kb");

        var sysInfoKeys = new string[]
        {
            "Cpu_Count",
            "bpool_committed", "physical_memory_in_bytes", // up to 2008 r2
            "physical_memory_kb", // on azure use process_memory_limit_mb column in sys.dm_os_job_object
            "committed_kb", // 2012+
            // sqlserver_start_time_ms_ticks - Ms_Ticks - sql server uptime
            "sqlserver_start_time_ms_ticks", "Ms_Ticks",
            // Used and Available memory on 2005...2008R2
            "Bpool_Committed",
            "Bpool_Commit_Target",
            "Bpool_Visible",
            // Used and Available memory on 2012+
            "Committed_Kb",
            "Committed_Target_Kb",
            "Visible_Target_Kb",
            // Total CPU Usage on 2012+
            "Process_Kernel_Time_Ms",
            "Process_User_Time_Ms",
        };

    }

    static long? GetMin(params long?[] values)
    {
        var notNull = values.Where(x => x.HasValue).ToArray();
        return notNull.Length == 0 ? null : notNull.Min();
    }

    public string Export(ColumnDefinition sortByColumn, bool isFieldSelected)
    {
        var headers = _tableTopHeaders.ToArray();
        var sortedRows = sortByColumn.SortAction(Rows).ToArray();
        StringBuilder htmlTable = new StringBuilder();
        htmlTable.AppendLine("  <table class='Metrics'><thead>");
        htmlTable.AppendLine("  <tr>");
        foreach (var header in headers)
        {
            htmlTable.AppendLine($"    <th colspan='{header.Columns.Count}' class='TableHeaderGroupCell'>{header.Caption}</th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  <tr>");
        var columnDefinitions = headers.SelectMany(h => h.Columns).ToArray();
        foreach (var column in columnDefinitions)
        {
            bool isThisSorting = column.PropertyName == sortByColumn.PropertyName;
            const string arrows = " ⇓ ⇩ ↓ ↡";
            var attrs = "";
            var onClick = $"onclick='SelectContent(\"{column.GetHtmlId()}\"); alert('HAHA'); return false;'";
            if (!isFieldSelected && column.AllowSort) attrs = $"style=\"cursor: pointer; display: inline-block;\" class='SortButton' data-sorting='{column.GetHtmlId()}'";
            var spanSortingParameter = $"<span id='SortingParameter' class='Hidden'>{column.GetHtmlId()}</span>";
            htmlTable.AppendLine($"    <th class='TableHeaderCell {(isThisSorting ? "Selected" : "")}' data-sorting='{column.GetHtmlId()}'><button {attrs}>{column.TheCaption}</button></th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  </thead>");

        htmlTable.AppendLine("  <tbody>");
        foreach (QueryCacheRow row in sortedRows)
        {
            htmlTable.AppendLine("  <tr class='MetricsRow'>");
            foreach (ColumnDefinition column in columnDefinitions)
            {
                var value = column.PropertyAccessor(row);
                var valueString = GetValueAsHtml(value, row, column);
                htmlTable.AppendLine($"\t\t<td>{valueString}</td>");
            }
            htmlTable.AppendLine("\t</tr>");
            htmlTable.AppendLine("\t<tr class='SqlRow'>");
            var tsqlHtmlString = TSqlToVanillaHtmlConverter.ConvertTSqlToHtml(row.SqlStatement, SqlSyntaxColors.DarkTheme);
            htmlTable.AppendLine("\t\t<td colspan='2'></td>");
            htmlTable.AppendLine($"\t\t<td colspan='{columnDefinitions.Length - 2}'><pre>{tsqlHtmlString}</pre></td>");
            htmlTable.AppendLine("\t</tr>");
        }
        htmlTable.AppendLine("  </tbody>");

        htmlTable.AppendLine("  </table>");
        return htmlTable.ToString();
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
