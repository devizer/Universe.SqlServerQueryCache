using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Universe.SqlServerQueryCache.Exporter;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlOsWaitStatsReader
{
    private DbProviderFactory _dbProvider;
    private string _connectionString;

    public SqlOsWaitStatsReader(DbProviderFactory dbProvider, string connectionString)
    {
        _dbProvider = dbProvider;
        _connectionString = connectionString;
    }

    public List<SqlWaitStatsSummaryRow> Read()
    {
        var con = _dbProvider.CreateConnection();
        con.ConnectionString = _connectionString;
        return con.Query<SqlWaitStatsSummaryRow>(@"Select 
  wait_type [WaitType],
  waiting_tasks_count [WaitCount],
  wait_time_ms [DurationMilliseconds]
From 
  sys.dm_os_wait_stats 
Where wait_type Like 'PAGEIOLATCH_%' Or wait_type = 'WRITELOG'; -- UP, SH, EX", null).ToList();
    }
}

public class SqlWaitStatsSummaryRow
{
    public string WaitType { get; set; }
    public long WaitCount { get; set; }
    public long DurationMilliseconds { get; set; }
}

public static class SqlWaitStatsSummaryRowExtensions
{
    public static List<SummaryRow> ToReportSummaryRows(this IEnumerable<SqlWaitStatsSummaryRow> waitStatsRows)
    {
        var copy = waitStatsRows.ToList();
        List<SummaryRow> ret = new List<SummaryRow>();

        void ToResultRow(string waitType, string summaryTitle)
        {
            var found = copy.FirstOrDefault(x => x.WaitType == waitType);
            if (found != null)
                ret.Add(new SummaryRow(summaryTitle, FormatKind.CounterWithDuration, new SummaryRowCounterValueWithDuration() { Counter = found.WaitCount, Milliseconds = found.DurationMilliseconds}));
        }

        ToResultRow("PAGEIOLATCH_SH", "IO Wait Page Read");
        ToResultRow("PAGEIOLATCH_UP", "IO Wait Page Update");
        ToResultRow("PAGEIOLATCH_EX", "IO Wait Page Write");
        ToResultRow("WRITELOG", "IO Wait Log Write");

        return ret;
    }
}

