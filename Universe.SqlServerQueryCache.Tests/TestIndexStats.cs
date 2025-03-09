using System.Data;
using System.Data.SqlClient;
using Universe.GenericTreeTable;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Tests;

public class TestIndexStats
{
    public static readonly string SQL_QUERY_INDEX_STATS = @"-- SET FMTONLY ON;
Declare @dbid mallint; Set @dbid = db_id();
print @dbid
Select 
  s.name schema_name,
  o.name table_name,
  i.name index_name,
  stat.*
From 
  sys.schemas s
  Inner Join sys.objects o On (s.schema_id = o.schema_id)
  Inner Join sys.indexes i On (o.object_id = i.object_id)
  Cross Apply sys.dm_db_index_operational_stats(@dbid, i.object_id, i.index_id, default) stat
Where
  o.is_ms_shipped = 0;
";

    // Has everywhere
    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void A_HasIndexStats(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        bool hasIndexStats = server.HasSystemObject("dm_db_index_operational_stats");
        Console.WriteLine($"HAS dm_db_index_operational_stats: {hasIndexStats}");
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void B_GetIndexStatSchema(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        var con = SqlClientFactory.Instance.CreateConnection(cs);
        var cmd = con.CreateCommand();
        cmd.CommandText = "select * from sys.dm_db_index_operational_stats(0,0,-1,0)";
        var adapter = SqlClientFactory.Instance.CreateDataAdapter();
        adapter.SelectCommand = cmd;
        DataSet dataSet = new DataSet();
        adapter.Fill(dataSet);
        var dataTable = dataSet.Tables[0];
        var index = 0;
        var columns = dataTable.Columns.OfType<DataColumn>().Select(x => new { Index = index++, ColumnName = x.ColumnName, DataType = x.DataType, AllowDBNull = x.AllowDBNull }).ToArray();
        var columnsText = string.Join(Environment.NewLine, columns.Select(x => $"{x.Index:00} {x.DataType.Name} {x.ColumnName}").ToArray());
        Console.WriteLine(columnsText);

        var dumpFileJson = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".IndexStatsSchema.json");
        File.WriteAllText(dumpFileJson, columns.ToJsonString());

        var dumpFileText = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".IndexStatsSchema.txt");
        File.WriteAllText(dumpFileText, columnsText);
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void C_ReadAsRaw(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        SqlIndexStatsReader reader = new SqlIndexStatsReader(SqlClientFactory.Instance, cs);
        var rawIndexStats = reader.ReadRaw();

        var dumpFileJson = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".IndexStats.json");
        File.WriteAllText(dumpFileJson, rawIndexStats.ToJsonString());
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void D_ReadStructured(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        SqlIndexStatsReader reader = new SqlIndexStatsReader(SqlClientFactory.Instance, cs);
        var structuredIndexStats = reader.ReadStructured();

        var dumpFileJson = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".IndexStatsStructured.json");
        File.WriteAllText(dumpFileJson, structuredIndexStats.ToJsonString());

        var dumpFileTableFull = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".IndexesFull.txt");
        SqlIndexStatSummaryReport reportFull = structuredIndexStats.GetRidOfUnnamedIndexes().GetRidOfMicrosoftShippedObjects().BuildPlainConsoleTable(false);
        File.WriteAllText(dumpFileTableFull, reportFull.PlainTable.ToString());

        var dumpFileTable = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".Indexes.txt");
        SqlIndexStatSummaryReport reportShrunk = structuredIndexStats.GetRidOfUnnamedIndexes().GetRidOfMicrosoftShippedObjects().BuildPlainConsoleTable(true);
        var reportShrunkContent = reportShrunk.PlainTable + Environment.NewLine + Environment.NewLine + reportShrunk.EmptyMetricsFormatted;
        File.WriteAllText(dumpFileTable, reportShrunkContent);

    }



}