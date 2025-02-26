select * from sys.dm_os_performance_counters where object_name like '%Buffer Node%'
-- https://learn.microsoft.com/ru-ru/sql/relational-databases/performance-monitor/use-sql-server-objects?view=sql-server-ver16#SQLServerPOs

SELECT * FROM sys.dm_os_performance_counters WHERE object_name LIKE '%Buffer Manager%';
-- https://learn.microsoft.com/en-us/sql/relational-databases/performance-monitor/sql-server-buffer-manager-object?view=sql-server-ver16

SELECT * FROM sys.dm_os_performance_counters WHERE counter_name = 'Maximum Workspace Memory (KB)'