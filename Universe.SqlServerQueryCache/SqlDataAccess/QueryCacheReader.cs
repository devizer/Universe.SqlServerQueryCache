using System.Data.Common;
using System.Net.Sockets;
using Dapper;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class QueryCacheReader
{
    public static IEnumerable<QueryCacheRow> Read(DbProviderFactory dbProvider, string connectionString)
    {
        var con = dbProvider.CreateConnection();
        con.ConnectionString = connectionString;
        var jit1 = con.Query<int>("Select 1 as Jit", null).ToList();
        var jit2 = new QueryCacheRow().AvgElapsedTime;
        var now = DateTime.Now;
        var ret = con.Query<QueryCacheRow>(TheQueryCacheQueryV3.SqlServerQueryCache, null).ToList();
        foreach (var row in ret)
            row.Lifetime = now - row.CreationTime;

        return ret;
    }
}
