using System.Data;
using System.Data.Common;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlResultSetColumn
{
    public int Index { get; set; }
    public string Name { get; set; }
    public Type NetType { get; set; }
    public Type ProviderSpecificNetType { get; set; }
    public string SqlType { get; set; }

    public override string ToString()
    {
        // « a text »
        return $"{$"#{Index}",3} [{Name}] «{SqlType}» is {NetType}{(ProviderSpecificNetType != null && ProviderSpecificNetType != NetType ? $", {ProviderSpecificNetType}" : "")}";
    }
}

public class SqlResultSetSchemaReader
{
    private DbProviderFactory _dbProvider;
    private string _connectionString;

    public SqlResultSetSchemaReader(DbProviderFactory dbProvider, string connectionString)
    {
        _dbProvider = dbProvider;
        _connectionString = connectionString;
    }

    public List<SqlResultSetColumn> GetSchemaByFullProcessing(string sqlQuery, CommandType commandType = CommandType.Text)
    {
        var con = _dbProvider.CreateConnection();
        con.ConnectionString = _connectionString;
        var cmd = con.CreateCommand();
        cmd.CommandText = sqlQuery;
        cmd.CommandType = commandType;
        using (cmd)
        {
            con.Open();
            using (var dbDataReader = cmd.ExecuteReader())
            {
                return GetSchema(dbDataReader);
            }
        }
    }

    public List<SqlResultSetColumn> GetSchema(string sqlQuery)
    {
        sqlQuery = "Set FMTONLY On; " + sqlQuery;
        return GetSchemaByFullProcessing(sqlQuery);
    }

    public static List<SqlResultSetColumn> GetSchema(DbDataReader dbDataReader)
    {
        var n = dbDataReader.FieldCount;
        List<SqlResultSetColumn> ret = new List<SqlResultSetColumn>();
        for (int i = 0; i < n; i++)
        {
            SqlResultSetColumn row = new SqlResultSetColumn()
            {
                Index = i,
                Name = dbDataReader.GetName(i),
                SqlType = dbDataReader.GetDataTypeName(i),
                NetType = dbDataReader.GetFieldType(i),
                ProviderSpecificNetType = dbDataReader.GetProviderSpecificFieldType(i),
            };
            ret.Add(row);
        }
        return ret;
    }
}