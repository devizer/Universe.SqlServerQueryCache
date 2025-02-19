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
        var now = DateTime.Now;
        var jit = con.Query<QueryCacheRow>("Select 1 as Jit", null).ToList();
        var ret = con.Query<QueryCacheRow>(TheQueryV3.SqlServerQueryCache, null).ToList();
        foreach (var row in ret)
            row.Lifetime = now - row.CreationTime;

        return ret;
    }
}
