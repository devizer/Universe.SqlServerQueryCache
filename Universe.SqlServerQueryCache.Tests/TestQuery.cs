using System.ComponentModel;
using System.Data.SqlClient;
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
    public void C_QueryQueryCache(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        var rows = QueryCacheReader.Read(SqlClientFactory.Instance, cs);
        rows = rows.OrderByDescending(r => r.ExecutionCount).ToArray();
        Console.WriteLine($"{rows.Count()} QUERIES ON SERVER [{server}]");
        Console.WriteLine(rows.ToJsonString());
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".Rows.json");
        File.WriteAllText(dumpFile, rows.ToJsonString());
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void D_Produce_Html_Report(SqlServerRef server)
    {
        var cs = SqlServerReferenceExtensions.GetConnectionString(server);
        var rows = QueryCacheReader.Read(SqlClientFactory.Instance, cs);
        var mediumVersion = SqlServerReferenceExtensions.GetMediumVersion(cs);
        SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(SqlClientFactory.Instance, cs);
        var singleFileHtml = e.Export();
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, server.GetSafeFileOnlyName() + ".html");
        Console.WriteLine($"Store HTML Report to {dumpFile}");
        File.WriteAllText(dumpFile, singleFileHtml);

        var jsonExport = new { Summary = e.Summary, Queries = e.Rows };
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
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    // TODO: Remove, because it was used for debugging only
    public void E_Get_SQL_OS_Sys_Info(SqlServerRef server)
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
}