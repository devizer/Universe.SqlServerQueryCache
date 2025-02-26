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
        List<string> ConnectionStrings = new List<string>();
        // string connectionString = null;
        // string sqlServer = null; // SSPI
        bool appendSqlServerVersion = false;
        bool justPrintHelp = false;
        string outputFile = null;
        int verbose = 0;
        string csFormat = "Data Source={0}; Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false";
        OptionSet p = new OptionSet()
            .Add("o=|output=", v => outputFile = v)
            .Add("s=|server=", v => ConnectionStrings.Add(string.Format(csFormat, v)))
            .Add("cs=|ConnectionString=", v => ConnectionStrings.Add(v))
            .Add("a|AppendVersion", v => appendSqlServerVersion = true)
            .Add("h|?|help", v => justPrintHelp = true);

        
        List<string> extra = p.Parse(args);
        if (justPrintHelp)
        {
            p.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        var argPadding = "    ";
        Console.WriteLine($@"SQL Server Query Cache CLI Arguments:");
        foreach (var connectionString in ConnectionStrings)
            Console.WriteLine($@"{argPadding}Connection String: {connectionString}");

        Console.WriteLine($@"{argPadding}Output File: {outputFile}");
        if (appendSqlServerVersion) Console.WriteLine($@"{argPadding}Append version to file name: true");

        int errorReturn = 0;
        foreach (var connectionString in ConnectionStrings)
        {
            var mediumVersion = GetMediumVersion(connectionString);
            if (mediumVersion == null)
            {
                errorReturn++;
                continue;
            };

            Console.Write($"Analyzing Query Cache for {GetInstanceName(connectionString)}:");
            IEnumerable<QueryCacheRow>? rows;
            try
            {
                rows = QueryCacheReader.Read(SqlClientFactory.Instance, connectionString);
                Console.WriteLine(" OK");
                // Medium Version already got, so HostPlatform error is not visualized explicitly
                var hostPlatform = SqlClientFactory.Instance.CreateConnection(connectionString).Manage().HostPlatform;
                string? summaryReport = SqlCacheSummaryTextExporter.Export(rows, $"SQL Server {mediumVersion} on {hostPlatform}");

                // 
                SqlPerformanceCountersReader perfReader = new SqlPerformanceCountersReader(SqlClientFactory.Instance, connectionString);
                var summaryCounters = perfReader.ReadBasicCounters();
                var padding = "   ";
                Func<long, string, string> formatPagesAsString = (pages, units) => $"  (is {(pages * 8196 / 1024d / 1024):n1} {units})";
                string summaryCountersAsString = $@"{padding}Database Pages:      {summaryCounters.BufferPages:n0} {formatPagesAsString(summaryCounters.BufferPages, "MB")}
{padding}Page Reads/sec:      {summaryCounters.PageReadsPerSecond:n0} {formatPagesAsString(summaryCounters.PageReadsPerSecond, "MB/s")}
{padding}Page Writes/sec:     {summaryCounters.PageWritesPerSecond:n0}{formatPagesAsString(summaryCounters.PageWritesPerSecond, "MB/s")} ";
                summaryReport += summaryCountersAsString;

                // Sys Info
                var sqlSysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, connectionString);
                summaryReport += Environment.NewLine + sqlSysInfo.Format("   ");


                Console.WriteLine(summaryReport);
                if (!string.IsNullOrEmpty(outputFile))
                {
                    var instanceName = GetInstanceName(connectionString);
                    // var realFolder = Path.GetFullPath(Path.GetDirectoryName(outputFile));
                    // var realFile = Path.GetFileName(outputFile);
                    var realOutputFile = outputFile.Replace("{InstanceName}", SafeFileName.Get(instanceName), StringComparison.OrdinalIgnoreCase);
                    if (appendSqlServerVersion) realOutputFile += $" {mediumVersion} on {hostPlatform}";

                    CreateDirectoryForFile(realOutputFile);
                    File.WriteAllText(realOutputFile + ".txt", summaryReport);
                    File.WriteAllText(realOutputFile + ".json", rows.ToJsonString(false, JsonNaming.PascalCase));
                    SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(rows);
                    var singleFileHtml = e.Export();
                    File.WriteAllText(realOutputFile + ".html", singleFileHtml);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" {ex.GetExceptionDigest()}");
                errorReturn++;
            }

        }


        return 0;
    }

    static void CreateDirectoryForFile(string fileName)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        }
        catch
        {
        }
    }

    static string GetInstanceName(string connectionString)
    {
        var b = SqlClientFactory.Instance.CreateConnectionStringBuilder();
        b.ConnectionString = connectionString;
        object ret = b["Data Source"];
        return ret == null ? null : Convert.ToString(ret);
    }

    static IDbConnection CreateConnection(string connectionString)
    {
        var con = SqlClientFactory.Instance.CreateConnection();
        con.ConnectionString = connectionString;
        return con;
    }

    static string GetMediumVersion(string connectionString)
    {
        Console.Write($"Validation connection string for {GetInstanceName(connectionString)}:");
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