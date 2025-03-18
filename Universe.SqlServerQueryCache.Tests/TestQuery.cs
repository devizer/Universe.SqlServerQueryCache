using System.ComponentModel;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using Dapper;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.Exporter;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Tests;

public class TestQuery
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void A_GetSqlServers(SqlServerRef server)
    {
        Console.WriteLine($"SERVER [{server}]");
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void B_StartSqlServers(SqlServerRef server)
    {
        Console.WriteLine($"SERVER [{server}]");
        Console.WriteLine($"CONNECTION STRING [{server.ConnectionString}]");
        bool isLocal = CrossInfo.ThePlatform == CrossInfo.Platform.Windows && SqlServiceExtentions.IsLocalDbOrLocalServer(server.ConnectionString);
        if (!isLocal)
        {
            Console.WriteLine($"Server {server} is not local. Skipping Start Server");
            return;
        }
        bool ok = SqlServiceExtentions.StartService(server.DataSource, TimeSpan.FromSeconds(30));
        Console.WriteLine($"SERVER [{server}] is running=[{ok}]");
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void C_ShowQueryStatsSchema(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        SqlResultSetSchemaReader schemaReader = new SqlResultSetSchemaReader(SqlClientFactory.Instance, cs);
        var columns = schemaReader.GetSchema("Select * From sys.dm_exec_query_stats");
        SqlQueryStatsSchema schema = new SqlQueryStatsSchema(columns);
        Console.WriteLine(schema);

        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".QueryStatsSchema.txt");
        File.WriteAllText(dumpFile, schema.ToString());

    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void D_QueryQueryCache(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        QueryCacheReader reader = new QueryCacheReader(SqlClientFactory.Instance, cs);
        var rows = reader.Read().ToList();
        var columnsSchema = reader.ColumnsSchema;
        rows = rows.OrderByDescending(r => r.ExecutionCount).ToList();
        Console.WriteLine($"{rows.Count()} QUERIES ON SERVER [{server}]");
        Console.WriteLine(rows.ToJsonString());
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".Rows.json");
        var json = new { ColumnsSchema = columnsSchema, Rows = rows };
        File.WriteAllText(dumpFile, json.ToJsonString());
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void E_Produce_Html_Report(SqlServerRef server)
    {
        var cs = SqlServerReferenceExtensions.GetConnectionString(server);
        var mediumVersion = SqlServerReferenceExtensions.GetMediumVersion(cs);
        SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(SqlClientFactory.Instance, cs);
        var singleFileHtml = e.Export();
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".html");
        Console.WriteLine($"Store HTML Report to {dumpFile}");
        File.WriteAllText(dumpFile, singleFileHtml);

        var jsonExport = new { SqlServerVersion = mediumVersion, Summary = e.Summary, ColumnsSchema = e.ColumnsSchema, Queries = e.Rows };
        var jsonFileName = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".json");
        File.WriteAllText(jsonFileName, jsonExport.ToJsonString(false, JsonNaming.PascalCase));

        var hostPlatform = SqlClientFactory.Instance.CreateConnection(cs).Manage().HostPlatform;
        string summaryReport = SqlSummaryTextExporter.ExportAsText(e.Summary, $"SQL Server {mediumVersion} on {hostPlatform}");
        var dumpSummaryFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".QueryCacheSummary.txt");
        Console.WriteLine(summaryReport);
        File.WriteAllText(dumpSummaryFile, summaryReport);

        var sqlSysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, cs);
        Console.WriteLine(sqlSysInfo.Format("   "));
        File.AppendAllText(dumpSummaryFile, Environment.NewLine + Environment.NewLine + sqlSysInfo.Format("   "));

        var dumpXmlFolder = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".Xml-Plan");
        Directory.CreateDirectory(dumpXmlFolder);
        int indexPlan = 0;
        foreach (var queryCacheRow in e.Rows)
        {
            if (string.IsNullOrEmpty(queryCacheRow.QueryPlan)) continue;
            indexPlan++;
            File.WriteAllText(Path.Combine(dumpXmlFolder, $"{indexPlan}.sqlplan"), queryCacheRow.QueryPlan);
        }
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void E2_ShowMetricsRange(SqlServerRef server)
    {
        var cs = SqlServerReferenceExtensions.GetConnectionString(server);
        var mediumVersion = SqlServerReferenceExtensions.GetMediumVersion(cs);
        SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(SqlClientFactory.Instance, cs);
        var singleFileHtml = e.Export();
        IEnumerable<QueryCacheRow> rows = e.Rows;
        var properties = typeof(QueryCacheRow)
            .GetProperties()
            .Where(x => x.PropertyType == typeof(long))
            .ToList();

        StringBuilder report = new StringBuilder();
        report.AppendLine(mediumVersion);
        report.AppendLine($"Queries: {e.Rows.Count()}");
        var column1Length = properties.Select(x => x.Name.Length).Max();
        Func<long, string> longToString = l => l == 0 ? "-" : l.ToString("n0");
        foreach (var pi in properties)
        {
            var getProperty = PropertyAccessor.CreatePropertyGetter<QueryCacheRow, long>(pi);
            // var longs = rows.Select(x => (long)pi.GetValue(x)).ToArray();
            var longs = rows.Select(x => getProperty(x)).ToArray();
            if (longs.Length > 0)
            {
                var nonZeroValues = longs.Where(x => x != 0).Distinct().OrderBy(x => x).ToArray();
                var nonZeroValuesString = string.Join(",", nonZeroValues.Select(x => x.ToString()).ToArray());
                var nonZeroValueCount = nonZeroValues.Count();
                var nonZeroRowsCount = longs.Count(x => x != 0);
                string vals = longs.Max() != 0 || longs.Min() != 0 ? $", ({longToString(nonZeroValueCount)} values on {longToString(nonZeroRowsCount)} queries: {nonZeroValuesString})" : "";
                report.AppendLine($"{pi.Name.PadRight(column1Length)} | {longToString(longs.Min())} ... {longToString(longs.Max())}{vals}");
            }
        }
        Console.WriteLine(report);
        var rangesFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".Ranges.txt");
        File.WriteAllText(rangesFile, report.ToString());
    }


    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    // TODO: Remove, because it was used for debugging only
    public void F_Get_SQL_OS_Sys_Info(SqlServerRef server)
    {
        var cs = SqlServerReferenceExtensions.GetConnectionString(server);
        var sysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, cs);

        object osSysInfoRaw = SqlClientFactory.Instance.CreateConnection(cs).Query<object>("Select * from sys.dm_os_sys_info").FirstOrDefault();
        var props2 = TypeDescriptor.GetProperties(osSysInfoRaw);
        var properties = osSysInfoRaw.GetType().GetProperties();
        Console.WriteLine(osSysInfoRaw.ToJsonString());
        Console.WriteLine(Environment.NewLine + osSysInfoRaw.ToString());

        var sqlSysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, cs);
        var letsDebug = sqlSysInfo.ToString();
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    // TODO: Remove, because it was used for debugging only
    public void G_TryQueryStatsV4(SqlServerRef server)
    {
        var cs = SqlServerReferenceExtensions.GetConnectionString(server);
        var sysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, cs);

        SqlResultSetSchemaReader schemaReader = new SqlResultSetSchemaReader(SqlClientFactory.Instance, cs);
        var columns = schemaReader.GetSchema("Select * From sys.dm_exec_query_stats");
        SqlQueryStatsSchema schema = new SqlQueryStatsSchema(columns);
        TheQueryCacheQueryV4 queryCacheQueryV4 = new TheQueryCacheQueryV4(schema);
        Console.WriteLine(queryCacheQueryV4.GetSqlQuery());
    }


    [TearDown]
    public void TestTearDown()
    {
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}