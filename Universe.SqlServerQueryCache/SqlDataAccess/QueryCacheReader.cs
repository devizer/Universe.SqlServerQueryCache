using System.Data.Common;
using System.Net.Sockets;
using Dapper;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class QueryCacheReader
{
    public readonly DbProviderFactory DbProvider;
    public readonly string ConnectionString;

    public List<QueryCacheRow> Rows { get; protected set; }
    public SqlQueryStatsSchema ColumnsSchema { get; protected set; }

    public QueryCacheReader(DbProviderFactory dbProvider, string connectionString)
    {
        DbProvider = dbProvider;
        ConnectionString = connectionString;
    }

    public IEnumerable<QueryCacheRow> Read()
    {
        SqlResultSetSchemaReader schemaReader = new SqlResultSetSchemaReader(DbProvider, ConnectionString);
        var statsColumns = schemaReader.GetSchema("Select * From sys.dm_exec_query_stats");
        ColumnsSchema = new SqlQueryStatsSchema(statsColumns);

        var con = DbProvider.CreateConnection();
        con.ConnectionString = ConnectionString;
        // var jit1 = con.Query<int>("Select 1 as Jit", null).ToList();
        // var jit2 = new QueryCacheRow().AvgElapsedTime;
        var now = DateTime.Now;
        var sqlServerQueryCache = TheQueryCacheQueryV3.SqlServerQueryCache;
        sqlServerQueryCache = new TheQueryCacheQueryV4(ColumnsSchema).GetSqlQuery();
        var ret = con.Query<QueryCacheRow>(sqlServerQueryCache, null).ToList();
        foreach (var row in ret)
            row.Lifetime = now - row.CreationTime;

        // Populate ObjectName and ObjectType
        SqlQueryObjectsReader objectMetaInfoReader = new SqlQueryObjectsReader(DbProvider, ConnectionString);
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
