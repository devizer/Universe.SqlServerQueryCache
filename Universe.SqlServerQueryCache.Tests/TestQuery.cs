using System.Data.SqlClient;
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
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, GetSafeFileOnlyName(server) + ".json");
        File.WriteAllText(dumpFile, rows.ToJsonString());
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void D_Produce_Html_Report(SqlServerRef server)
    {
        var cs = GetConnectionString(server);
        var rows = QueryCacheReader.Read(SqlClientFactory.Instance, cs);
        var mediumVersion = GetMediumVersion(cs);
        SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(rows);
        var singleFileHtml = e.Export();
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, GetSafeFileOnlyName(server) + ".html");
        Console.WriteLine($"Store HTML Report to {dumpFile}");
        File.WriteAllText(dumpFile, singleFileHtml);

        var hostPlatform = SqlClientFactory.Instance.CreateConnection(cs).Manage().HostPlatform;
        var summaryReport = SqlCacheSummaryTextExporter.Export(rows, $"SQL Server {mediumVersion} on {hostPlatform}");
        var dumpSummaryFile = Path.Combine(TestEnvironment.DumpFolder, GetSafeFileOnlyName(server) + ".QueryCacheSummary.txt");
        Console.WriteLine(summaryReport);
        File.WriteAllText(dumpSummaryFile, summaryReport);


    }

    private static string GetSafeFileOnlyName(SqlServerRef server)
    {
        var cs = GetConnectionString(server);
        var mediumVersion = GetMediumVersion(cs);
        var platform = SqlClientFactory.Instance.CreateConnection(cs).Manage().HostPlatform;
        return SafeFileName.Get($"{server.DataSource}: v{mediumVersion} on {platform}");
    }

    private static string GetMediumVersion(string cs)
    {
        var mediumVersion = SqlClientFactory.Instance.CreateConnection(cs).Manage().MediumServerVersion;
        return mediumVersion;
    }

    private static string GetConnectionString(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        return cs;
    }
}