using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using Universe.SqlServerJam;
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
            Console.WriteLine($"Summary on {mediumVersion}");
            Console.WriteLine($"   Queries:             {rows.Count()}");
            Console.WriteLine($"   Execution Count:     {rows.Sum(x => x.ExecutionCount):n0}");
            Console.WriteLine($"   Duration:            {rows.Sum(x => x.TotalElapsedTime / 1000d):n2} milliseconds");
            Console.WriteLine($"   CPU Usage:           {rows.Sum(x => x.TotalWorkerTime / 1000d):n2}");
            Console.WriteLine($"   Total Pages Read:    {rows.Sum(x => x.TotalLogicalReads):n0}");
            Console.WriteLine($"   Cached Pages Read:   {rows.Sum(x => Math.Max(0, x.TotalLogicalReads - x.TotalPhysicalReads)):n0}");
            Console.WriteLine($"   Physical Pages Read: {rows.Sum(x => x.TotalPhysicalReads):n0}");
            Console.WriteLine($"   Total Pages Writes:  {rows.Sum(x => x.TotalLogicalWrites):n0}");
            Console.WriteLine($"   The Oldest Lifetime: {rows.Max(x => x.Lifetime)}");
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