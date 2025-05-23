﻿using System.Data;
using System.Data.SqlClient;
using NDesk.Options;
using Universe.GenericTreeTable;
using Universe.SqlServerJam;
using Universe.SqlServerQueryCache.Exporter;
using Universe.SqlServerQueryCache.External;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.CLI;

internal class MainProgram
{
    private static string[] MeaningfulSysInfoKeys = new string[]
    {
        "Cpu_Count",
        "bpool_committed", "physical_memory_in_bytes", // up to 2008 r2
        "physical_memory_kb", // on azure use process_memory_limit_mb column in sys.dm_os_job_object
        "committed_kb", // 2012+
        // sqlserver_start_time_ms_ticks - Ms_Ticks - sql server uptime
        "sqlserver_start_time_ms_ticks", "Ms_Ticks",
        // Used and Available memory on 2005...2008R2
        "Bpool_Committed",
        "Bpool_Commit_Target",
        "Bpool_Visible",
        // Used and Available memory on 2012+
        "Committed_Kb",
        "Committed_Target_Kb",
        "Visible_Target_Kb",
        // Total CPU Usage on 2012+
        "Process_Kernel_Time_Ms",
        "Process_User_Time_Ms",
    };

    public static int Run(string[] args)
    {
        List<string> ConnectionStrings = new List<string>();
        // string connectionString = null;
        // string sqlServer = null; // SSPI
        bool appendSqlServerVersion = false;
        bool justPrintHelp = false;
        string outputFile = null;
        bool allLocalServers = false;
        string csFormat = "Data Source={0}; Integrated Security=SSPI; TrustServerCertificate=true; Encrypt=false";
        OptionSet p = new OptionSet()
            .Add("o=|output=", v => outputFile = v)
            .Add("s=|server=", v => ConnectionStrings.Add(string.Format(csFormat, v)))
            .Add("cs=|ConnectionString=", v => ConnectionStrings.Add(v))
            .Add("av|append-version", v => appendSqlServerVersion = true)
            .Add("all|all-local-servers", v => allLocalServers = true)
            .Add("h|?|help", v => justPrintHelp = true);

        
        List<string> extra = p.Parse(args);
        if (justPrintHelp || args.Length == 0)
        {
            p.WriteOptionDescriptions(Console.Out);
            return 0;
        }

        if (allLocalServers)
        {
            var servers = SqlDiscovery.GetLocalDbAndServerList();
            var localServers = servers
                .Where(x => x.ServiceStartup != LocalServiceStartup.Disabled)
                .ToArray();

            
            var onlineServers = localServers
                // Service should be running
                .Where(x => x.Kind == SqlServerDiscoverySource.WellKnown || x.ToSqlServerDataSource().IsLocalDb || x.ToSqlServerDataSource().CheckLocalServiceStatus()?.State == SqlServiceStatus.ServiceState.Running)
                .ToArray();

            foreach (var s in localServers)
            {
                // Console.WriteLine($"{s} [Service={SqlServiceExtentions.GetServiceName(s.DataSource)}]: {SqlServiceExtentions.CheckServiceStatus(s.DataSource)}");
            }

            Console.WriteLine($"Found online {onlineServers.Length} local SQL Servers: [{string.Join(", ", onlineServers.Select(x => x.DataSource).ToArray())}]");
            // ConnectionStrings.AddRange(onlineServers.Select(x => string.Format(csFormat, x.DataSource)));
            ConnectionStrings.AddRange(onlineServers.Select(x => x.ConnectionString));
        }
        var argPadding = "    ";
        Console.WriteLine($@"SQL Server Query Cache CLI Arguments:");
        foreach (var connectionString in ConnectionStrings)
            Console.WriteLine($@"{argPadding}Connection String: {connectionString}");

        if (string.IsNullOrEmpty(outputFile))
            Console.WriteLine($@"{argPadding}Output File argument is missing. Results will not be stored");
        else
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
            try
            {
                var instanceName = GetInstanceName(connectionString);
                var hostPlatform = SqlClientFactory.Instance.CreateConnection(connectionString).Manage().HostPlatform;
                SqlCacheHtmlExporter e = new SqlCacheHtmlExporter(SqlClientFactory.Instance, connectionString);

                if (!string.IsNullOrEmpty(outputFile))
                {
                    // Does not supported by net framework
                    // var realOutputFile = outputFile.Replace("{InstanceName}", SafeFileName.Get(instanceName), StringComparison.OrdinalIgnoreCase);
                    var realOutputFile = outputFile.ReplaceCore("{InstanceName}", SafeFileName.Get(instanceName), StringComparison.OrdinalIgnoreCase);
                    if (appendSqlServerVersion) realOutputFile += $" {mediumVersion} on {hostPlatform}";
                    CreateDirectoryForFile(realOutputFile);

                    e.ExportToFile(realOutputFile + ".html");

                    // rows = QueryCacheReader.Read(SqlClientFactory.Instance, connectionString).ToArray();
                    Console.WriteLine(" OK");
                    // Medium Version already got, so HostPlatform error is not visualized explicitly
                    var summary = e.Summary;
                    string summaryReport = SqlSummaryTextExporter.ExportAsText(summary, $"SQL Server {mediumVersion}");
                    Console.WriteLine(summaryReport);

                    // Sys Info
                    ICollection<SqlSysInfoReader.Info> sqlSysInfo = SqlSysInfoReader.Query(SqlClientFactory.Instance, connectionString);
                    var summaryReportFull = summaryReport + Environment.NewLine + sqlSysInfo.Format("   ");
                    File.WriteAllText(realOutputFile + ".txt", summaryReportFull);

                    var jsonExport = new { SqlServerVersion = mediumVersion, Summary = e.Summary, ColumnsSchema = e.ColumnsSchema, Queries = e.Rows };
                    JsonExtensions.ToJsonFile(realOutputFile + ".json", jsonExport, false, JsonNaming.CamelCase);


                    // Indexes: json
                    SqlIndexStatsReader reader = new SqlIndexStatsReader(SqlClientFactory.Instance, connectionString);
                    var structuredIndexStats = reader.ReadStructured();
                    // File.WriteAllText(realOutputFile + ".Indexes.json", structuredIndexStats.ToJsonString());
                    JsonExtensions.ToJsonFile(realOutputFile + ".Indexes.json", structuredIndexStats, false, JsonNaming.CamelCase);


                    // Indexes: full plain
                    SqlIndexStatSummaryReport reportFull = structuredIndexStats.GetRidOfUnnamedIndexes().GetRidOfMicrosoftShippedObjects().BuildPlainConsoleTable();
                    File.WriteAllText(realOutputFile + ".Indexes-Full.txt", reportFull.PlainTable.ToString());
                    SqlIndexStatSummaryReport reportShrunk = structuredIndexStats.GetRidOfUnnamedIndexes().GetRidOfMicrosoftShippedObjects().BuildPlainConsoleTable(true);

                    // Indexes: plain
                    var reportShrunkContent = reportShrunk.PlainTable + Environment.NewLine + Environment.NewLine + reportShrunk.EmptyMetricsFormatted;
                    File.WriteAllText(realOutputFile + ".Indexes-Plain.txt", reportShrunkContent);

                    // tree (shrunk)
                    var reportTreeContent = reportShrunk.TreeTable + Environment.NewLine + Environment.NewLine + reportShrunk.EmptyMetricsFormatted;
                    File.WriteAllText(realOutputFile + ".Indexes-Tree.txt", reportTreeContent);

                    File.WriteAllText(realOutputFile + ".log", e.GetLogsAsString());
                }
                else
                {
                    e.Export(TextWriter.Null);

                    // rows = QueryCacheReader.Read(SqlClientFactory.Instance, connectionString).ToArray();
                    Console.WriteLine(" OK");
                    // Medium Version already got, so HostPlatform error is not visualized explicitly
                    var summary = e.Summary;
                    string summaryReport = SqlSummaryTextExporter.ExportAsText(summary, $"SQL Server {mediumVersion}");
                    Console.WriteLine(summaryReport);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" {ex.GetExceptionDigest()}");
                errorReturn++;
            }

        }

        return errorReturn;
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