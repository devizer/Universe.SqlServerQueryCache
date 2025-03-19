using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

// TODO: V4 - columns depends on Columns Schema
public static class TheQueryCacheQueryV3
{
    public const string SqlServerQueryCache = @"SELECT -- Query Cache Report https://github.com/devizer/Universe.SqlServerQueryCache
    p.dbid DatabaseId,
    d.name DatabaseName,
    p.objectid ObjectId, /* stored proc, function, or trigger */
    t.dbid dbid2,
    t.objectid objectid2,
    qs.sql_handle,
    qs.creation_time [CreationTime],
    (SELECT TOP 1 SUBSTRING(t.[text],(qs.statement_start_offset/2) + 1, ( (CASE WHEN qs.statement_end_offset = -1 THEN (LEN(CONVERT(nvarchar(max),t.[text])) * 2) ELSE qs.statement_end_offset END) - qs.statement_start_offset)/2 + 1)) AS [SqlStatement],
    p.query_plan [QueryPlan],
    qs.execution_count [ExecutionCount],
    qs.plan_generation_num [PlanGenerationNum],
    qs.last_execution_time [LastExecutionTime],

    qs.total_elapsed_time [TotalElapsedTime],
    qs.last_elapsed_time [LastElapsedTime],
    qs.min_elapsed_time [MinElapsedTime],
    qs.max_elapsed_time [MaxElapsedTime],
    
    qs.total_worker_time [TotalWorkerTime],
    qs.last_worker_time [LastWorkerTime],
    qs.min_worker_time [MinWorkerTime],
    qs.max_worker_time [MaxWorkerTime],

    qs.total_physical_reads [TotalPhysicalReads],
    qs.last_physical_reads [LastPhysicalReads],
    qs.min_physical_reads [MinPhysicalReads],
    qs.max_physical_reads [MaxPhysicalReads],

    qs.total_logical_reads [TotalLogicalReads],
    qs.last_logical_reads [LastLogicalReads],
    qs.min_logical_reads [MinLogicalReads],
    qs.max_logical_reads [MaxLogicalReads],

    qs.total_logical_writes [TotalLogicalWrites],
    qs.last_logical_writes [LastLogicalWrites],
    qs.min_logical_writes [MinLogicalWrites],
    qs.max_logical_writes [MaxLogicalWrites]
    /* Optional Columns */
FROM sys.dm_exec_query_stats AS qs
CROSS APPLY sys.dm_exec_sql_text(sql_handle) AS t      -- join sql text
OUTER APPLY sys.dm_exec_query_plan(qs.plan_handle) p   -- join database id and query plan
LEFT JOIN sys.databases d ON p.dbid = d.database_id    -- join database name
-- Order By p.objectid Desc, qs.total_elapsed_time Desc;
/* If creation_time changed then add else replace */
";
}