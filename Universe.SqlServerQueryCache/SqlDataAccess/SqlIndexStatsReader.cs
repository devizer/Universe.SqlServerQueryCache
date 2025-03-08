using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlIndexStatsReader
{
    private DbProviderFactory _dbProvider;
    private string _connectionString;

    public SqlIndexStatsReader(DbProviderFactory dbProvider, string connectionString)
    {
        _dbProvider = dbProvider;
        _connectionString = connectionString;
    }

    public IEnumerable<IndexStatSummaryRow> ReadStructured(IEnumerable<IDictionary<string, long>> argRawList)
    {
        var ret = argRawList.Select(x => new IndexStatSummaryRow() { Metrics = x }).ToList();
        foreach (var retRow in ret)
        {
            retRow.DatabaseId = (int)GetLong(retRow.Metrics, "database_id");
            retRow.ObjectId = (int)GetLong(retRow.Metrics, "object_id");
            retRow.IndexId= (int)GetLong(retRow.Metrics, "index_id");
        }

        // Populate DatabaseName
        var idDatabaseList = ret.Select(x => x.DatabaseId).Distinct();
        var con = _dbProvider.CreateConnection();
        con.ConnectionString = _connectionString;
        var rawDatabases = con.Query<SysDatabasesRow>("select database_id, name from sys.databases", null).ToList();
        Dictionary<int, string> dbNameById = rawDatabases
            .Select(x => new { Id = x.database_id, Name = x.name })
            .ToLookup(x => x.Id, x => x.Name)
            .ToDictionary(x => x.Key, x => x.FirstOrDefault());

        foreach (var retRow in ret)
            retRow.Database = dbNameById.TryGetValue(retRow.DatabaseId, out var dbName) ? dbName : $"#{retRow.DatabaseId}";

        // Populate ObjectName and IndexName
        var allDatabasesIndexes = new List<SysIndexDetailsRow>();
        foreach (var idDatabase in idDatabaseList)
        {
            if (dbNameById.TryGetValue(idDatabase, out var dbName))
            {
                string sqlQueryIndexDetails = $"Use [{dbName}]; " + SqlSelectIndexes;
                List<SysIndexDetailsRow> indexDetailList = con.Query<SysIndexDetailsRow>(sqlQueryIndexDetails, null).ToList();
                foreach (var sysIndexDetailsRow in indexDetailList) sysIndexDetailsRow.DatabaseId = idDatabase;
                allDatabasesIndexes.AddRange(indexDetailList);
            }
        }
        ;

        return ret;
    }


    public IEnumerable<IndexStatSummaryRow> ReadStructured()
    {
        return ReadStructured(ReadAsRaw());
    }

    public IEnumerable<IDictionary<string, long>> ReadAsRaw()
    {
        var con = _dbProvider.CreateConnection();
        con.ConnectionString = _connectionString;
        var rawCollection = con.Query<object>("Select * From sys.dm_db_index_operational_stats(0,0,-1,0)", null);
        List<IDictionary<string, long>> ret = new List<IDictionary<string, long>>();
        foreach (object dapperRow in rawCollection)
        {
            var dRow = (IDictionary<string, object>)dapperRow;
            Dictionary<string, long> retRow = new Dictionary<string, long>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var pair in dRow) retRow[pair.Key] = Convert.ToInt64(pair.Value);
            ret.Add(retRow);
        }

        return ret;
    }

    static long GetLong(IDictionary<string, long> rawRow, string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && rawRow.TryGetValue(propertyName, out var rawRet) ? rawRet : -1;
    }

    // IO
    public class SysDatabasesRow
    {
        public int database_id;
        public string name;
    }

    public static readonly string SqlSelectIndexes = @"
Select 
  s.schema_id SchemaId,
  s.name SchemaName,
  o.object_id ObjectId,
  o.name ObjectName,
  i.index_id IndexId,
  i.name IndexName,
  i.type IndexTypeId,
  i.type_desc IndexType,
  i.is_unique IsUnique,
  i.is_unique_constraint IsUniqueConstraint,
  i.is_primary_key IsPrimaryKey
From 
  sys.schemas s
  Inner Join sys.objects o On (s.schema_id = o.schema_id)
  Inner Join sys.indexes i On (o.object_id = i.object_id)
";
    public class SysIndexDetailsRow
    {
        public int DatabaseId { get; set; }
        public int SchemaId { get; set; }
        public string SchemaName { get; set; }
        public int ObjectId { get; set; }
        public string ObjectName { get; set; }
        public int IndexId { get; set; }
        public string IndexName { get; set; }
        public short IndexTypeId { get; set; }
        public string IndexType { get; set; }
        public bool IsUnique { get; set; }
        public bool IsUniqueConstraint { get; set; }
        public bool IsPrimaryKey { get; set; }

        protected bool Equals(SysIndexDetailsRow other)
        {
            return DatabaseId == other.DatabaseId && ObjectId == other.ObjectId && IndexId == other.IndexId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SysIndexDetailsRow)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = DatabaseId;
                hashCode = (hashCode * 397) ^ ObjectId;
                hashCode = (hashCode * 397) ^ IndexId;
                return hashCode;
            }
        }

        public static bool operator ==(SysIndexDetailsRow left, SysIndexDetailsRow right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(SysIndexDetailsRow left, SysIndexDetailsRow right)
        {
            return !Equals(left, right);
        }
    }

}