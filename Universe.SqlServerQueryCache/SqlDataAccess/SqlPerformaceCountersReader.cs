using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;

namespace Universe.SqlServerQueryCache.SqlDataAccess
{
    public static class SqlPerformanceCountersReaderExtensions
    {
        public static SqlBasicPerformanceCounters ReadBasicCounters(this SqlPerformanceCountersReader reader)
        {
            var rows = reader.Select("WHERE counter_name in ('Database pages', 'Page reads/sec', 'Page writes/sec') And object_name like '%Buffer Manager%'");
            var grouped = rows.GroupByCounterName().ToList();
            return new SqlBasicPerformanceCounters()
            {
                BufferPages = grouped.FirstOrDefault(x => x.CounterName == "Database pages")?.Value ?? 0L,
                PageReadsPerSecond = grouped.FirstOrDefault(x => x.CounterName == "Page reads/sec")?.Value ?? 0L,
                PageWritesPerSecond = grouped.FirstOrDefault(x => x.CounterName == "Page writes/sec")?.Value ?? 0L,
            };
        }


    }

    public class SqlBasicPerformanceCounters
    {
        public long BufferPages { get; set; }
        public long PageReadsPerSecond { get; set; }
        public long PageWritesPerSecond { get; set; }
    }

    // Supported Versions: 2005 ... 2022+
    public class SqlPerformanceCountersReader
    {
        private DbProviderFactory _dbProvider;
        private string _connectionString;

        public SqlPerformanceCountersReader(DbProviderFactory dbProvider, string connectionString)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public IEnumerable<SqlPerformanceCounterDataRow> Select(string whereClause)
        {
            var con = _dbProvider.CreateConnection();
            con.ConnectionString = _connectionString;
            var ret = con.Query<SqlPerformanceCounterDataRow>(
                "Select * From sys.dm_os_performance_counters"
                + (string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}"), null)
                .ToList();

            return ret;
        }

        public IEnumerable<SqlPerformanceCounterDataRow> Select()
        {
            return Select(string.Empty);
        }
    }

    /*
select * from sys.dm_os_performance_counters where object_name like '%Buffer Node%'
-- https://learn.microsoft.com/en-us/sql/relational-databases/performance-monitor/use-sql-server-objects?view=sql-server-ver16#SQLServerPOs

SELECT * FROM sys.dm_os_performance_counters WHERE object_name LIKE '%Buffer Manager%';
-- https://learn.microsoft.com/en-us/sql/relational-databases/performance-monitor/sql-server-buffer-manager-object?view=sql-server-ver16

SELECT * FROM sys.dm_os_performance_counters WHERE counter_name = 'Maximum Workspace Memory (KB)'

Select *
From sys.dm_os_performance_counters
Where
counter_name in ('Database pages', 'Page reads/sec', 'Page writes/sec')
And object_name like '%Buffer Manager%'
    */

    public class SqlPerformanceCounterDataRow
    {
        public string ObjectName => Object_Name.TrimEnd();
        public string CounterName => Counter_Name.TrimEnd();
        public string InstanceName => Instance_Name.TrimEnd();
        public long Value => Cntr_Value;
        public long Type => Cntr_Type;

        // Data Access
        [JsonIgnore]
        public string Object_Name { get; set; }
        [JsonIgnore]
        public string Counter_Name { get; set; }
        [JsonIgnore]
        public string Instance_Name { get; set; }
        [JsonIgnore]
        public long Cntr_Value { get; set; }
        [JsonIgnore]
        public long Cntr_Type { get; set; }
    }

    public static class SqlPerformanceCounterDataRowExtensions
    {
        public static List<SqlPerformanceCounterDataRow> GroupByCounterName(this IEnumerable<SqlPerformanceCounterDataRow> list)
        {
            var groups = list.ToLookup(x => $"{x.ObjectName}-->{x.CounterName}");
            List<SqlPerformanceCounterDataRow> ret = new List<SqlPerformanceCounterDataRow>();
            foreach (var group in groups)
            {
                SqlPerformanceCounterDataRow item = new SqlPerformanceCounterDataRow();
                foreach (var subItem in group)
                {
                    item.Object_Name = subItem.Object_Name;
                    item.Counter_Name = subItem.Counter_Name;
                    item.Cntr_Value += subItem.Cntr_Value;
                    item.Cntr_Type = subItem.Cntr_Type;
                }
                ret.Add(item);
            }

            return ret;
        }
    }
}
