using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.Exporter;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.CLI;

internal class MainProgram
{
    public static int Run(string[] args)
    {
        string connectionString = null;
        string sqlServer = null; // SSPI
        bool appendSqlServerVersion = false;
        bool justPrintHelp = false;
        string outputFile = null;
        int verbose = 0;
        OptionSet p = new OptionSet()
            .Add("o=|output=", v => outputFile = v)
            .Add("s=|server=", v => sqlServer = v)
            .Add("cs=|ConnectionString=", v => connectionString = v)
            .Add("a|AppendVersion", v => appendSqlServerVersion = true)
            .Add("h|?|help", v => justPrintHelp = true);

        
        List<string> extra = p.Parse(args);
        if (justPrintHelp)
        {
            p.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        if (!string.IsNullOrEmpty(sqlServer))
        {
            connectionString = $"Data Source={sqlServer}; Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false";
        }

        var argPadding = "    ";
        Console.WriteLine($@"SQL Server Query Cache CLI Arguments:");
        if (sqlServer != null) Console.WriteLine($@"{argPadding}SQL Server (SSPI): {sqlServer}");
        Console.WriteLine($@"{argPadding}Connection String: {connectionString}");
        Console.WriteLine($@"{argPadding}Output File: {outputFile}");
        if (appendSqlServerVersion) Console.WriteLine($@"{argPadding}Append version to file name: true");

        var mediumVersion = GetMediumVersion(connectionString);
        if (mediumVersion == null) return 2;

        Console.Write("Analyzing Query Cache:");
        IEnumerable<QueryCacheRow>? rows;
        try
        {
            rows = QueryCacheReader.Read(SqlClientFactory.Instance, connectionString);
            Console.WriteLine(" OK");
            // Medium Version already got, so HostPlatform error is not visualized explicitly
            var hostPlatform = SqlClientFactory.Instance.CreateConnection(connectionString).Manage().HostPlatform;
            var summaryReport = SqlCacheSummaryTextExporter.Export(rows, $"SQL Server {mediumVersion} on {hostPlatform}");
            Console.WriteLine(summaryReport);
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {ex.GetExceptionDigest()}");
            return 3;
        }

        return 0;
    }

    static IDbConnection CreateConnection(string connectionString)
    {
        var con = SqlClientFactory.Instance.CreateConnection();
        con.ConnectionString = connectionString;
        return con;
    }

    static string GetMediumVersion(string connectionString)
    {
        Console.Write("Validation connection string:");
        try
        {
            var man = CreateConnection(connectionString).Manage();
            var ret = "v" + man.MediumServerVersion + " on " + man.HostPlatform;
            Console.WriteLine($" OK, {ret}");
            return ret;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" {ex.GetExceptionDigest()}");
            return null;
        }
    }
}