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
                string ret = !needHtml ? $"{pages:n0}" : HtmlNumberFormatter.Format(pages, 0, ""); 
                var kb = pages * 8192d / 1024;
                Func<string, string> toSmall = arg => $"&nbsp;<span class='Units'>{arg}</span>";
                var mbFormatted = needHtml ? $"{toSmall("MB")}" : " MB";
                var kbFormatted = needHtml ? $"{toSmall("KB")}" : " KB";
                Func<string, string> toNotImportant = arg => needHtml ? $"&nbsp;&nbsp;<span class='NotImportant'>{arg}</span>" : arg;
                if (pages > 2048) ret += toNotImportant($"  (is {kb / 1024:n0}{mbFormatted})");
                else if (pages > 512) ret += toNotImportant($"  (is {kb / 1024:n1}{mbFormatted})");
                else if (pages > 0) ret += toNotImportant($"  (is {kb:n0}{kbFormatted})");
                return ret;
            };

            List<SummaryRow> ret = new List<SummaryRow>();

            void Add(string title, object value, string description)
            {
                ret.Add(new SummaryRow(title, value, description));
            }

            var rowsCountFormatted = needHtml ? HtmlNumberFormatter.Format(rows.Count(), 0) : rows.Count().ToString("n0");
            Add("Queries", rows.Count(), rowsCountFormatted);
            
            var executionCount = rows.Sum(x => x.ExecutionCount);
            var executionCountFormatted = !needHtml ? $"{executionCount:n0}" : HtmlNumberFormatter.Format(executionCount, 0);
            Add($"Execution Count", executionCount, executionCountFormatted);

            var duration = rows.Sum(x => x.TotalElapsedTime / 1000d);
            var durationFormatted = !needHtml ? $"{duration:n2}" : HtmlNumberFormatter.Format(duration, 2); 
            Add($"Duration (milliseconds)", duration, durationFormatted);
            
            var cpuUsage = rows.Sum(x => x.TotalWorkerTime / 1000d);
            var cpuUsageFormatted = !needHtml ? $"{cpuUsage:n2}" : HtmlNumberFormatter.Format(cpuUsage, 2);
            Add($"CPU Usage", cpuUsage, cpuUsageFormatted);

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
