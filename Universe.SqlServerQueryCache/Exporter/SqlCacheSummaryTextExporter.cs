using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Exporter
{
    public class SqlSummaryTextExporter
    {
        public static IEnumerable<SummaryRow> Export(IEnumerable<QueryCacheRow> rows, bool needHtml)
        {
            Func<long, string> formatPagesAsString = pages =>
            {
                string ret = $"{pages:n0}";
                var kb = pages * 8192d / 1024;
                if (pages > 2048) ret += $"  (is {kb / 1024:n0} MB)";
                else if (pages > 512) ret += $"  (is {kb / 1024:n1} MB)";
                else if (pages > 0) ret += $"  (is {kb:n0} KB)";
                return ret;
            };

            List<SummaryRow> ret = new List<SummaryRow>();

            void Add(string title, object value, string description)
            {
                ret.Add(new SummaryRow(title, value, description));
            }

            var rowsCountFormatted = needHtml ? HtmlNumberFormatter.Format(rows.Count(), 0, "") : rows.Count().ToString("n0");
            Add("Queries", rows.Count(), rowsCountFormatted);
            
            var executionCount = rows.Sum(x => x.ExecutionCount);
            var executionCountFormatted = !needHtml ? $"{executionCount:n0}" : HtmlNumberFormatter.Format(executionCount, 0, "");
            Add($"Execution Count", executionCount, executionCountFormatted);

            var duration = rows.Sum(x => x.TotalElapsedTime / 1000d);
            Add($"Duration (milliseconds)", duration, $"{duration:n2}");
            var cpuUsage = rows.Sum(x => x.TotalWorkerTime / 1000d);
            Add($"CPU Usage", cpuUsage, $"{cpuUsage:n2}");
            var totalLogicalReads = rows.Sum(x => x.TotalLogicalReads);
            Add($"Total Pages Read", totalLogicalReads, $"{formatPagesAsString(totalLogicalReads)}");
            var cachedReads = rows.Sum(x => Math.Max(0, x.TotalLogicalReads - x.TotalPhysicalReads));
            Add($"Cached Pages Read", cachedReads, $"{formatPagesAsString(cachedReads)}");
            var physicalReads = rows.Sum(x => x.TotalPhysicalReads);
            Add($"Physical Pages Read", physicalReads, $"{formatPagesAsString(physicalReads)}");
            var writes = rows.Sum(x => x.TotalLogicalWrites);
            Add($"Total Pages Writes", writes, $"{formatPagesAsString(writes)}");
            TimeSpan? oldestLifetime = rows.Any() ? rows.Max(x => x.Lifetime) : (TimeSpan?)null;
            var oldestLifetimeFormatted = oldestLifetime == null ? "" : 
                needHtml ? ElapsedFormatter.FormatElapsedAsHtml(oldestLifetime.Value) : oldestLifetime.Value.ToString();
            Add($"The Oldest Lifetime", oldestLifetime,  oldestLifetimeFormatted);

            return ret;
        }
        public static string ExportAsText(IEnumerable<QueryCacheRow> rows, string title)
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendLine($"Summary on {title}");
            var summaryRows = Export(rows, false);
            var maxTitleLength = summaryRows.Max(x => x.Title.Length);
            foreach (var summaryRow in summaryRows)
                ret.AppendLine("   " + (summaryRow.Title + ":").PadRight(maxTitleLength + 2) + summaryRow.Description);

            return ret.ToString();
        }
    }

    public class SummaryRow
    {
        public string Title { get; set; }
        public object Value { get; set; }
        public string Description { get; set; }

        public SummaryRow()
        {
        }

        public SummaryRow(string title, object value, string description)
        {
            Title = title;
            Value = value;
            Description = description;
        }
    }
}
