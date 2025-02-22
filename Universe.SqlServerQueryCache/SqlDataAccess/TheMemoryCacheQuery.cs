namespace Universe.SqlServerQueryCache.SqlDataAccess;

public static class TheMemoryCacheQuery
{
    public const string SqlServerMemoryCache = @"IF ((@@MICROSOFTVERSION / 16777216) >= 11)
Exec('Select Name, Type, sum(pages_kb) kb
From sys.dm_os_memory_cache_entries
Group By Name, Type
Having Sum(pages_kb) > 0')
ELSE
Exec('Select Name, Type, 8*sum(pages_allocated_count) kb
From sys.dm_os_memory_cache_entries
Group By Name, Type
Having 8*sum(pages_allocated_count) > 0')
";
}