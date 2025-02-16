using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.SqlDataAccess
{
    public static class TheQueryV3
    {
        public const string SqlServerQueryCache = @"-- Query the query cache. https://github.com/devizer/Universe.SqlServerQueryCache
SELECT
    s2.dbid,
	s2.objectid,
    s1.sql_handle,
	s1.creation_time [CreationTime],
    (SELECT TOP 1 SUBSTRING(s2.text,(statement_start_offset / 2) + 1, ( (CASE WHEN statement_end_offset = -1 THEN (LEN(CONVERT(nvarchar(max),s2.text)) * 2) ELSE statement_end_offset END)  - statement_start_offset) / 2+1)) AS [SqlStatement],
    s1.execution_count [ExecutionCount],
    plan_generation_num [PlanGenerationNum],
    last_execution_time [LastExecutionTime],

    total_elapsed_time [TotalElapsedTime],
    last_elapsed_time [LastElapsedTime],
    min_elapsed_time [MinElapsedTime],
    max_elapsed_time [MaxElapsedTime],
    
    total_worker_time [TotalWorkerTime],
    last_worker_time [LastWorkerTime],
    min_worker_time [MinWorkerTime],
    max_worker_time [MaxWorkerTime],

    total_physical_reads [TotalPhysicalReads],
    last_physical_reads [LastPhysicalReads],
    min_physical_reads [MinPhysicalReads],
    max_physical_reads [MaxPhysicalReads],

    total_logical_writes [TotalLogicalWrites],
    last_logical_writes [LastLogicalWrites],
    min_logical_writes [MinLogicalWrites],
    max_logical_writes [MaxLogicalWrites]
FROM sys.dm_exec_query_stats AS s1
CROSS APPLY sys.dm_exec_sql_text(sql_handle) AS s2
-- WHERE s2.objectid is null
-- ORDER BY s1.sql_handle, s1.statement_start_offset, s1.statement_end_offset;
-- Order By total_elapsed_time Desc;

/* If creation_time changed then add else replace */
";
    }
}
