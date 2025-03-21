using System.Data.Common;
using System.Net.Sockets;
using Dapper;
using Universe.SqlServerQueryCache.External;

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
        using (StepsLogger.Instance?.LogStep("Query 'Query Stats' Schema"))
        {
            SqlResultSetSchemaReader schemaReader = new SqlResultSetSchemaReader(DbProvider, ConnectionString);
            var statsColumns = schemaReader.GetSchema("Select * From sys.dm_exec_query_stats");
            ColumnsSchema = new SqlQueryStatsSchema(statsColumns);
        }

        var stepsLogger = StepsLogger.Instance?.LogStep("Query Raw 'Query Stats'");
        var con = DbProvider.CreateConnection();
        con.ConnectionString = ConnectionString;
        var now = DateTime.Now;
        var sqlServerQueryCache = TheQueryCacheQueryV3.SqlServerQueryCache;
        sqlServerQueryCache = new TheQueryCacheQueryV4(ColumnsSchema).GetSqlQuery();
        var ret = con.Query<QueryCacheRow>(sqlServerQueryCache, null).ToList();
        foreach (var row in ret)
            row.Lifetime = now - row.CreationTime;

        stepsLogger?.Restart($"Populate Object Name and Object Type for {ret.Count} queries");
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

        stepsLogger?.Dispose();
        return ret;
    }
}
