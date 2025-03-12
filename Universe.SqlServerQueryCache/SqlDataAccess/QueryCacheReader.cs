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

        // Populate ObjectName and ObjectType
        SqlQueryObjectsReader objectMetaInfoReader = new SqlQueryObjectsReader(dbProvider, connectionString);
        var dbIdList = ret.Select(x => x.DatabaseId).Distinct();
        var objectList = objectMetaInfoReader.Read(dbIdList);
        var objectsLookup = objectList.ToSpecializedLookup();
        foreach (var row in ret)
        {
            var meta = objectsLookup[row.DatabaseId, row.ObjectId];
            if (meta != null)
            {
                row.ObjectName = meta.ObjectName;
                row.ObjectType = meta.ObjectType;
                row.ObjectSchemaId = meta.SchemaId.GetValueOrDefault();
                row.ObjectSchemaName = meta.SchemaName;
            }
        }

        return ret;
    }
}
