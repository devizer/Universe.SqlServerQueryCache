using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Exporter;

public class SqlServerSummaryBuilder
{
    public readonly DbProviderFactory DbProvider;
    public readonly string ConnectionString;
    public readonly List<QueryCacheRow> Rows;

    public SqlServerSummaryBuilder(DbProviderFactory dbProvider, string connectionString, List<QueryCacheRow> rows)
    {
        DbProvider = dbProvider;
        ConnectionString = connectionString;
        Rows = rows;
    }

    public List<SummaryRow> BuildTotalWholeSummary()
    {
        List<SummaryRow> summaryRows = SqlSummaryTextExporter.ExportStructured(Rows).ToList();

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

        // Wait Stats
        SqlOsWaitStatsReader waitStatsReader = new SqlOsWaitStatsReader(DbProvider, ConnectionString);
        var waitStatsSummary = waitStatsReader.Read().ToReportSummaryRows();
        summaryRows.AddRange(waitStatsSummary);

        // version
        var versionRow = new SummaryRow("Version", FormatKind.Unknown, $"{mediumVersion} on {hostPlatform}");
        // summaryRows.Add(versionRow); // Already on the HTML header

        var customSummaryRows = CustomSummaryRowReader.GetCustomSummary().ToList();
        foreach (var customSummaryRow in customSummaryRows)
        {
            var pos = Math.Max(0, customSummaryRow.Position);
            pos = Math.Min(summaryRows.Count, pos);
            if (pos > summaryRows.Count) summaryRows.Add(customSummaryRow);
            else summaryRows.Insert(pos, customSummaryRow);
        }

        return summaryRows;
    }

    // Refactor: Use SingleRowResultSet
    public IEnumerable<SummaryRow> BuildSysInfoSummary()
    {
        // Sys Info
        ICollection<SqlSysInfoReader.Info> sqlSysInfoList = SqlSysInfoReader.Query(DbProvider, ConnectionString);
        Dictionary<string, object> sqlSysInfo = sqlSysInfoList
            .ToLookup(x => x.Name)
            .Select(x => new { K = x.Key, V = x.FirstOrDefault()?.Value })
            .ToDictionary(x => x.K, x => x.V, StringComparer.OrdinalIgnoreCase);

        Func<string, long?> getLong = name => sqlSysInfo.TryGetValue(name, out var raw) ? Convert.ToInt64(raw) : null;
        var cpuCount = getLong("cpu_count");
        if (cpuCount.HasValue) yield return new SummaryRow("CPU Count", FormatKind.Natural, cpuCount.Value);
        var physical_memory_in_bytes = getLong("physical_memory_in_bytes");
        var physical_memory_kb = getLong("physical_memory_kb");
        long? memKb = physical_memory_kb ?? (physical_memory_in_bytes / 1024);
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



}