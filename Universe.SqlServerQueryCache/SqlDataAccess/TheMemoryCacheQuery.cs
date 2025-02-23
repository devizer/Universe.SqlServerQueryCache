namespace Universe.SqlServerQueryCache.SqlDataAccess;

public static class TheMemoryCacheQuery
{
    public const string SqlServerMemoryClerks = @"-- select * from sys.dm_os_memory_clerks /* where type in ('MEMORYCLERK_SQLBUFFERPOOL') */ order by type;
IF ((@@MICROSOFTVERSION / 16777216) >= 11)
Exec('Select Type, Name, Sum(pages_kb) pages_kb, Sum(virtual_memory_committed_kb) memory_kb
From sys.dm_os_memory_clerks
Where memory_node_id <> 64
      And (pages_kb > 0 Or virtual_memory_committed_kb > 0)
Group By Name, Type
Order By Sum(pages_kb) + Sum(virtual_memory_committed_kb) desc')
ELSE
Exec('Select Type, Name, Sum(single_pages_kb + multi_pages_kb) pages_kb, Sum(virtual_memory_committed_kb) memory_kb
From sys.dm_os_memory_clerks
Where memory_node_id <> 64
      And (single_pages_kb > 0 Or multi_pages_kb > 0 Or virtual_memory_committed_kb > 0)
Group By Name, Type
Order By Sum(single_pages_kb + multi_pages_kb) + Sum(virtual_memory_committed_kb) desc')";


    public const string SqlServerMemoryCache = @"IF ((@@MICROSOFTVERSION / 16777216) >= 11)
Exec('Select Name, Type, sum(pages_kb) kb
From sys.dm_os_memory_cache_entries
Group By Name, Type
Having Sum(pages_kb) > 0')
ELSE
Exec('Select Name, Type, 8*sum(pages_allocated_count) kb
From sys.dm_os_memory_cache_entries
Group By Name, Type
Having 8*sum(pages_allocated_count) > 0')";
}

// MEMORYCLERK_SQLBUFFERPOOL:
// This memory clerk keeps track of commonly the largest memory consumer inside SQL Server - data and index pages.
// Buffer Pool or data cache keeps data and index pages loaded in memory to provide fast access to data.
// For more information, see Buffer Management.