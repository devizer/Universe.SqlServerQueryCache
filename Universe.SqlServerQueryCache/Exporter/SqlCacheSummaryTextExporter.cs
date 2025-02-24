using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Exporter
{
    public class SqlCacheSummaryTextExporter
    {
        public static string Export(IEnumerable<QueryCacheRow> rows, string title)
        {
            Func<long, string> formatPagesAsString = pages =>
            {
                string ret = $"{pages:n0}";
                var kb = pages * 8192d / 1024;
                if (pages > 2048) ret += $"  (is {kb / 1024:n0} MB)";
                else if (pages > 512) ret += $"  (is {kb / 1024:n1} MB)";
                else if (pages > 0) ret += $"  (is {kb:n2} KB)";
                return ret;
            };

            StringBuilder ret = new StringBuilder();
            ret.AppendLine($"Summary on {title}");
            ret.AppendLine($"   Queries:             {rows.Count()}");
            ret.AppendLine($"   Execution Count:     {rows.Sum(x => x.ExecutionCount):n0}");
            ret.AppendLine($"   Duration:            {rows.Sum(x => x.TotalElapsedTime / 1000d):n2} milliseconds");
            ret.AppendLine($"   CPU Usage:           {rows.Sum(x => x.TotalWorkerTime / 1000d):n2}");
            ret.AppendLine($"   Total Pages Read:    {formatPagesAsString(rows.Sum(x => x.TotalLogicalReads))}");
            ret.AppendLine($"   Cached Pages Read:   {formatPagesAsString(rows.Sum(x => Math.Max(0, x.TotalLogicalReads - x.TotalPhysicalReads)))}");
            ret.AppendLine($"   Physical Pages Read: {formatPagesAsString(rows.Sum(x => x.TotalPhysicalReads))}");
            ret.AppendLine($"   Total Pages Writes:  {formatPagesAsString(rows.Sum(x => x.TotalLogicalWrites))}");
            ret.AppendLine($"   The Oldest Lifetime: {(rows.Any() ? rows.Max(x => x.Lifetime) : null)}");

            return ret.ToString();
        }
    }
}
