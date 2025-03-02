using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Exporter;

public class SqlSummaryTextExporter
{
    public static IEnumerable<SummaryRow> ExportStructured(IEnumerable<QueryCacheRow> rows)
    {
        List<SummaryRow> ret = new List<SummaryRow>();

        void Add(string title, FormatKind kind, object value)
        {
            ret.Add(new SummaryRow(title, kind, value));
        }

        Add("Queries", FormatKind.Natural, rows.Count());
            
        var executionCount = rows.Sum(x => x.ExecutionCount);
        Add($"Execution Count", FormatKind.Natural, executionCount);

        var duration = rows.Sum(x => x.TotalElapsedTime / 1000d);
        Add($"Duration (milliseconds)", FormatKind.Numeric2, duration);
            
        var cpuUsage = rows.Sum(x => x.TotalWorkerTime / 1000d);
        Add($"CPU Usage", FormatKind.Numeric2, cpuUsage);

        var totalLogicalReads = rows.Sum(x => x.TotalLogicalReads);
        Add($"Total Pages Read", FormatKind.Pages, totalLogicalReads);
        var cachedReads = rows.Sum(x => Math.Max(0, x.TotalLogicalReads - x.TotalPhysicalReads));
        Add($"Cached Pages Read", FormatKind.Pages, cachedReads);
        var physicalReads = rows.Sum(x => x.TotalPhysicalReads);
        Add($"Physical Pages Read", FormatKind.Pages, physicalReads);
        var writes = rows.Sum(x => x.TotalLogicalWrites);
        Add($"Total Pages Writes", FormatKind.Pages, writes);

        TimeSpan? oldestLifetime = rows.Any() ? rows.Max(x => x.Lifetime) : (TimeSpan?)null;
        Add($"The Oldest Lifetime", FormatKind.Timespan, oldestLifetime);

        return ret;
    }
    public static string ExportAsText(IEnumerable<QueryCacheRow> rows, string title)
    {
        StringBuilder ret = new StringBuilder();
        ret.AppendLine($"Summary on {title}");
        var summaryRows = ExportStructured(rows);
        var maxTitleLength = summaryRows.Max(x => x.Title.Length);
        foreach (var summaryRow in summaryRows)
            ret.AppendLine("   " + (summaryRow.Title + ":").PadRight(maxTitleLength + 2) + summaryRow.GetFormatted(false));

        return ret.ToString();
    }
}

public enum FormatKind
{
    Unknown,
    Natural,
    Numeric1,
    Numeric2,
    Pages,
    Timespan,
}