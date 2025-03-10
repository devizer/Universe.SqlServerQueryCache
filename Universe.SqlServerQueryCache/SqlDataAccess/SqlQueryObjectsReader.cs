using System.Collections.Concurrent;
using System.Data.Common;
using Dapper;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlQueryObjectsReader
{
    private DbProviderFactory _dbProvider;
    private string _connectionString;

    private static readonly string SqlQuery = @"Select 
  s.schema_id SchemaId,
  s.name SchemaName,
  o.type_desc ObjectType,
  o.object_id ObjectId,
  o.name ObjectName,
  o.is_ms_shipped IsMsShipped
From 
  sys.schemas s
  Inner Join sys.objects o On (s.schema_id = o.schema_id)
";

    public SqlQueryObjectsReader(DbProviderFactory dbProvider, string connectionString)
    {
        _dbProvider = dbProvider;
        _connectionString = connectionString;
    }

    public List<SqlObjectMetaInfo> ReadForAllDatabases()
    {
        var dbList = this.GetDatabases(null);
        return ReadImplementation(dbList);
    }
    public List<SqlObjectMetaInfo> Read(IEnumerable<int> databaseIdList)
    {
        var dbList = this.GetDatabases(databaseIdList);
        return ReadImplementation(dbList);
    }

    private List<SqlObjectMetaInfo> ReadImplementation(List<DB> databases)
    {
        List<SqlObjectMetaInfo> ret = new List<SqlObjectMetaInfo>();
        foreach (var db in databases)
        {
            var sql = $"Use [{db.DatabaseName}]; {SqlQuery}";
            var con = _dbProvider.CreateConnection();
            con.ConnectionString = _connectionString;
            List<SqlObjectMetaInfo> portion = con.Query<SqlObjectMetaInfo>(sql, null).ToList();
            foreach (var o in portion)
            {
                o.DatabaseId = db.DatabaseId;
                o.DatabaseName = db.DatabaseName;
            }
            ret.AddRange(portion);
        }

        return ret;
    }

    // null databaseIdList means all the databases
    public List<DB> GetDatabases(IEnumerable<int> databaseIdList)
    {
        var idListCopy = databaseIdList.ToList();
        var sql = "Select database_id DatabaseId, name DatabaseName From sys.databases";
        if (idListCopy?.Count > 0) sql += $" Where database_id In ({string.Join(",", idListCopy.Select(x => $"{x:0}").ToArray())})";
        var con = _dbProvider.CreateConnection();
        con.ConnectionString = _connectionString;
        return con.Query<DB>(sql, null).ToList();
    }

    public class DB
    {
        public int DatabaseId { get; internal set; }
        public string DatabaseName { get; internal set; }
    }


}

public class SqlObjectMetaInfo
{
    public int DatabaseId { get; internal set; }
    public string DatabaseName { get; internal set; }
    public int? SchemaId { get; internal set; }
    public string SchemaName { get; internal set; }
    public int ObjectId { get; internal set; }
    public string ObjectName { get; internal set; }
    public string ObjectType { get; internal set; }
    public bool IsMsShipped { get; internal set; }

    protected bool Equals(SqlObjectMetaInfo other)
    {
        return DatabaseId == other.DatabaseId && ObjectId == other.ObjectId;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SqlObjectMetaInfo)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (DatabaseId * 397) ^ ObjectId;
        }
    }

    public static bool operator ==(SqlObjectMetaInfo left, SqlObjectMetaInfo right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SqlObjectMetaInfo left, SqlObjectMetaInfo right)
    {
        return !Equals(left, right);
    }
}

public class SqlObjectMetaInfoLookup
{
    public readonly IDictionary<SqlObjectMetaInfo, SqlObjectMetaInfo> AsDictionary = new ConcurrentDictionary<SqlObjectMetaInfo, SqlObjectMetaInfo>();

    public SqlObjectMetaInfoLookup(IEnumerable<SqlObjectMetaInfo> sqlObjects)
    {
        foreach (var so in sqlObjects ?? throw new ArgumentNullException(nameof(sqlObjects)))
            AsDictionary[so] = so;
    }

    // Returns null if not found
    public SqlObjectMetaInfo this[int databaseId, int objectId] => AsDictionary.TryGetValue(new SqlObjectMetaInfo() { DatabaseId = databaseId, ObjectId = objectId }, out var ret) ? ret : null;
}

public static class SqlObjectMetaInfoExtensions
{
    public static SqlObjectMetaInfoLookup ToSpecializedLookup(this IEnumerable<SqlObjectMetaInfo> sqlObjects) => new SqlObjectMetaInfoLookup(sqlObjects);
}