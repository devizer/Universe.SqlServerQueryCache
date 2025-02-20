using System.Data.SqlClient;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.Exporter;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Tests;

public class TestElapsedFormatter
{
    [Test]
    public void TestFormat()
    {
        var testCases = new[] { 0.0001, 
            0.01, 
            0.1, 
            9.9, 
            59.9, 
            60.1, 
            3599, 
            3599.9, 
            3600, 
            3600.1,
            24*3600-0.1,
            24*3600,
            24*3600+0.1,
        };
        foreach (object sec in testCases)
        {
            decimal seconds = Convert.ToDecimal(sec);
            string columnArgument = $"{seconds} Seconds:";
            Console.WriteLine($"{columnArgument,-17} {ElapsedFormatter.FormatElapsedAsHtml(TimeSpan.FromSeconds((double)seconds))}");
        }
    }
}

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
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, SafeFileName.Get(server.DataSource) + ".json");
        File.WriteAllText(dumpFile, rows.ToJsonString());
    }

    [Test]
    [TestCaseSource(typeof(SqlServersTestCaseSource), nameof(SqlServersTestCaseSource.SqlServers))]
    public void D_Produce_Html_Report(SqlServerRef server)
    {
        SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(server.ConnectionString);
        b.Encrypt = false;
        var cs = b.ConnectionString;
        var rows = QueryCacheReader.Read(SqlClientFactory.Instance, cs);
        SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(rows);
        var singleFileHtml = e.Export();
        var dumpFile = Path.Combine(TestEnvironment.DumpFolder, SafeFileName.Get(server.DataSource) + ".html");
        Console.WriteLine($"Store HTML Report to {dumpFile}");
        File.WriteAllText(dumpFile, singleFileHtml);
    }

}