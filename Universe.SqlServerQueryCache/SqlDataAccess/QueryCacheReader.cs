using System.Data.Common;
using Dapper;

namespace Universe.SqlServerQueryCache.SqlDataAccess
{
    public class QueryCacheReader
    {
        public static IEnumerable<QueryCacheRow> Read(DbProviderFactory dbProvider, string connectionString)
        {
            var con = dbProvider.CreateConnection();
            con.ConnectionString = connectionString;
            return con.Query<QueryCacheRow>(TheQueryV3.SqlServerQueryCache, null);
        }
    }
}
