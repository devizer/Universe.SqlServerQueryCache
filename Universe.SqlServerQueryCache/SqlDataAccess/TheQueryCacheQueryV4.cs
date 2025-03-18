using System.Text;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class TheQueryCacheQueryV4
{
    public readonly SqlQueryStatsSchema ColumnsSchema;

    public TheQueryCacheQueryV4(SqlQueryStatsSchema columnsSchema)
    {
        ColumnsSchema = columnsSchema;
    }

    public string GetSqlQuery()
    {
        var parts = new[]
        {
            new { NetSuffix = "Rows", SqlSuffix = "rows" },
            new { NetSuffix = "Dop", SqlSuffix = "dop" },
            new { NetSuffix = "GrantKb", SqlSuffix = "grant_kb" },
            new { NetSuffix = "UsedGrantKb", SqlSuffix = "used_grant_kb" },
            new { NetSuffix = "IdealGrantKb", SqlSuffix = "ideal_grant_kb" },
            new { NetSuffix = "ReservedThreads", SqlSuffix = "reserved_threads" },
            new { NetSuffix = "UsedThreads", SqlSuffix = "used_threads" },
            new { NetSuffix = "ColumnStoreSegmentReads", SqlSuffix = "columnstore_segment_reads" },
            new { NetSuffix = "ColumnStoreSegmentSkips", SqlSuffix = "columnstore_segment_skips" },
            new { NetSuffix = "Spills", SqlSuffix = "spills" },
            new { NetSuffix = "NumPhysicalReads", SqlSuffix = "num_physical_reads" },
            new { NetSuffix = "PageServerReads", SqlSuffix = "page_server_reads" },
            new { NetSuffix = "NumPageServerReads", SqlSuffix = "num_page_server_reads" },
        };

        StringBuilder optionalColumns = new StringBuilder();
        string[] sqlPrefixes = new[] { "total", "last", "min", "max" };
        string[] netPrefixes = new[] { "Total", "Last", "Min", "Max" };
        var hasFourColumns = (string sqlSuffix) => sqlPrefixes.All(x => this.ColumnsSchema.GetColumn($"{x}_{sqlSuffix}") != null);
        int totalIndex = 0;
        int totalCount = 4 * parts.Count(x => hasFourColumns(x.SqlSuffix));
        if (totalCount > 0) optionalColumns.AppendLine().AppendLine("    /* Columns below depends on SQL Server version */, ");

        foreach (var part in parts)
        {
            if (hasFourColumns(part.SqlSuffix))
            {
                optionalColumns.AppendLine();
                for (int partIndex = 0; partIndex < sqlPrefixes.Length; partIndex++)
                {
                    optionalColumns
                        .Append($"    qs.{sqlPrefixes[partIndex]}_{part.SqlSuffix} [{netPrefixes[partIndex]}{part.NetSuffix}]")
                        .Append(totalIndex + 1 < totalCount ? "," : "")
                        .AppendLine();

                    totalIndex++;
                }
            }
        }

        var ret = TheQueryCacheQueryV3.SqlServerQueryCache.Replace("/* Optional Columns */", optionalColumns.ToString());
        return ret;



    }
}