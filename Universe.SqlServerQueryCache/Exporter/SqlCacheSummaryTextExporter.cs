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
        public static string Export(IEnumerable<QueryCacheRow> rows, string mediumVersion)
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendLine($"Summary on SQL Server {mediumVersion}");
            ret.AppendLine($"   Queries:             {rows.Count()}");
            ret.AppendLine($"   Execution Count:     {rows.Sum(x => x.ExecutionCount):n0}");
            ret.AppendLine($"   Duration:            {rows.Sum(x => x.TotalElapsedTime / 1000d):n2} milliseconds");
            ret.AppendLine($"   CPU Usage:           {rows.Sum(x => x.TotalWorkerTime / 1000d):n2}");
            ret.AppendLine($"   Total Pages Read:    {rows.Sum(x => x.TotalLogicalReads):n0}");
            ret.AppendLine($"   Cached Pages Read:   {rows.Sum(x => Math.Max(0, x.TotalLogicalReads - x.TotalPhysicalReads)):n0}");
            ret.AppendLine($"   Physical Pages Read: {rows.Sum(x => x.TotalPhysicalReads):n0}");
            ret.AppendLine($"   Total Pages Writes:  {rows.Sum(x => x.TotalLogicalWrites):n0}");
            ret.AppendLine($"   The Oldest Lifetime: {rows.Max(x => x.Lifetime)}");

            return ret.ToString();
        }
    }
}
