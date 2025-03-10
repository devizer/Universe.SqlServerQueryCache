﻿using System;
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
    <div id=""modal-summary-root"" class=""Modal-Summary"">
         <div class=""Modal-Summary-body Capped"">
             <center>SQL Server Summary<br/>v{mediumVersion}{customHeadersHtml}<br/><br/></center>
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
        if (summaryCounters.BufferPages > 0)
            summaryRows.Add(new SummaryRow("Database Pages", FormatKind.Pages, summaryCounters.BufferPages));
        if (summaryCounters.PageReadsPerSecond > 0)
            summaryRows.Add(new SummaryRow("Page Reads/sec", FormatKind.PagesPerSecond, summaryCounters.PageReadsPerSecond));
        if (summaryCounters.PageWritesPerSecond > 0)
            summaryRows.Add(new SummaryRow("Page Writes/sec", FormatKind.PagesPerSecond, summaryCounters.PageWritesPerSecond));

        // Sys Info
        var summarySysInfo = BuildSysInfoSummary().ToArray();
        summaryRows.AddRange(summarySysInfo);

        // version
        var versionRow = new SummaryRow("Version", FormatKind.Unknown, $"{mediumVersion} on {hostPlatform}");
        // summaryRows.Add(versionRow); // Already on the HTML header

        var customSummaryRows = CustomSummaryRowReader.GetCustomSummary().ToList();
        foreach (var customSummaryRow in customSummaryRows)
        {
            var pos = Math.Max(0, customSummaryRow.Position);
            pos = Math.Min(summaryRows.Count, pos);
            if (pos > summaryRows.Count) summaryRows.Add(customSummaryRow); else summaryRows.Insert(pos, customSummaryRow);
        }

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

        // 2005...2008R2
        var bpool_Committed = getLong("Bpool_Committed");
        if (bpool_Committed.HasValue) yield return new SummaryRow("Buffer Pages", FormatKind.Pages, bpool_Committed.Value);
        var bpool_Commit_Target = getLong("Bpool_Commit_Target");
        var bpool_Visible = getLong("Bpool_Visible");
        var visiblePages = GetMin(bpool_Commit_Target, bpool_Visible);
        if (visiblePages.HasValue) yield return new SummaryRow("Visible Buffer Pages", FormatKind.Pages, visiblePages.Value);

        // 2012+
        var Committed_Kb = getLong("Committed_Kb");
        if (Committed_Kb.HasValue) yield return new SummaryRow("Committed Memory (MB)", FormatKind.Natural, Committed_Kb.Value / 1024);
        var Committed_Target_Kb = getLong("Committed_Target_Kb");
        var Visible_Target_Kb = getLong("Visible_Target_Kb");
        var visibleKb = GetMin(Committed_Target_Kb, Visible_Target_Kb);
        if (visibleKb.HasValue) yield return new SummaryRow("Visible Memory (MB)", FormatKind.Natural, visibleKb.Value / 1024);

        // uptime
        var sqlserver_start_time_ms_ticks = getLong("sqlserver_start_time_ms_ticks");
        var Ms_Ticks = getLong("Ms_Ticks");
        if (sqlserver_start_time_ms_ticks.HasValue && Ms_Ticks.HasValue)
            yield return new SummaryRow("Uptime", FormatKind.Timespan, TimeSpan.FromMilliseconds(Math.Abs(Ms_Ticks.Value - sqlserver_start_time_ms_ticks.Value)));

        // Total Cpu Usage
        var process_User_Time_Ms = getLong("Process_User_Time_Ms");
        if (process_User_Time_Ms.HasValue) yield return new SummaryRow("CPU User Time (seconds)", FormatKind.Numeric2, process_User_Time_Ms.Value / 1000d);
        var process_Kernel_Time_Ms = getLong("Process_Kernel_Time_Ms");
        if (process_Kernel_Time_Ms.HasValue) yield return new SummaryRow("CPU Kernel Time (seconds)", FormatKind.Numeric2, process_Kernel_Time_Ms.Value / 1000d);




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
            htmlTable.AppendLine("  <tr class='MetricsRow'>");
            foreach (ColumnDefinition column in columnDefinitions)
            {
                var value = column.PropertyAccessor(row);
                var valueString = GetValueAsHtml(value, row, column);
                htmlTable.AppendLine($"\t\t<td>{valueString}</td>");
            }
            htmlTable.AppendLine("\t</tr>");
            htmlTable.AppendLine("\t<tr class='SqlRow'>");
            var rowSqlStatement = string.IsNullOrEmpty(row.DatabaseName) ? row.SqlStatement : ($"Use [{row.DatabaseName}];{Environment.NewLine}{row.SqlStatement}");
            var tsqlHtmlString = TSqlToVanillaHtmlConverter.ConvertTSqlToHtml(rowSqlStatement, SqlSyntaxColors.DarkTheme);
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
