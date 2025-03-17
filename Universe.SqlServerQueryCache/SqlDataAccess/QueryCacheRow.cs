namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class QueryCacheRow
{
    public int DatabaseId { get; set; }
    public string DatabaseName { get; set; }
    public int ObjectId { get; set; }
    public int ObjectSchemaId { get; set; } // Need Separate Query per DatabaseId
    public string ObjectSchemaName { get; set; } // Need Separate Query per DatabaseId
    public string ObjectName { get; set; } // Need Separate Query per DatabaseId
    public string ObjectType { get; set; } // Need Separate Query per DatabaseId
    public byte[] SqlHandle { get; set; }
    public DateTime CreationTime { get; set; }
    public string SqlStatement { get; set; }
    public string QueryPlan { get; set; }
    public long ExecutionCount { get; set; }

    public long PlanGenerationNum { get; set; }
    public DateTime LastExecutionTime { get; set; }
    public TimeSpan Lifetime { get; set; }

    double GetAverage(long total)
    {
        return ExecutionCount == 0 ? 0 : (double)total / ExecutionCount;
    }

    public long TotalElapsedTime { get; set; }
    public long LastElapsedTime { get; set; }
    public long MinElapsedTime { get; set; }
    public long MaxElapsedTime { get; set; }
    public double AvgElapsedTime => GetAverage(TotalElapsedTime);

    public long TotalWorkerTime { get; set; }
    public long LastWorkerTime { get; set; }
    public long MinWorkerTime { get; set; }
    public long MaxWorkerTime { get; set; }
    public double AvgWorkerTime => GetAverage(TotalWorkerTime);

    public long TotalPhysicalReads { get; set; }
    public long LastPhysicalReads { get; set; }
    public long MinPhysicalReads { get; set; }
    public long MaxPhysicalReads { get; set; }
    public double AvgPhysicalReads => GetAverage(TotalPhysicalReads);

    public long TotalLogicalReads { get; set; }
    public long LastLogicalReads { get; set; }
    public long MinLogicalReads { get; set; }
    public long MaxLogicalReads { get; set; }
    public double AvgLogicalReads => GetAverage(TotalLogicalReads);

    public long TotalLogicalWrites { get; set; }
    public long LastLogicalWrites { get; set; }
    public long MinLogicalWrites { get; set; }
    public long MaxLogicalWrites { get; set; }
    public double AvgLogicalWrites => GetAverage(TotalLogicalWrites);

    public long TotalRows { get; set; }
    public long LastRows { get; set; }
    public long MinRows { get; set; }
    public long MaxRows { get; set; }
    public double AvgRows => GetAverage(TotalRows);

    public long TotalDop { get; set; }
    public long LastDop { get; set; }
    public long MinDop { get; set; }
    public long MaxDop { get; set; }
    public double AvgDop => GetAverage(TotalDop);

    public long TotalGrantKb { get; set; }
    public long LastGrantKb { get; set; }
    public long MinGrantKb { get; set; }
    public long MaxGrantKb { get; set; }
    public double AvgGrantKb => GetAverage(TotalGrantKb);

    public long TotalUsedGrantKb { get; set; }
    public long LastUsedGrantKb { get; set; }
    public long MinUsedGrantKb { get; set; }
    public long MaxUsedGrantKb { get; set; }
    public double AvgUsedGrantKb => GetAverage(TotalUsedGrantKb);

    public long TotalIdealGrantKb { get; set; }
    public long LastIdealGrantKb { get; set; }
    public long MinIdealGrantKb { get; set; }
    public long MaxIdealGrantKb { get; set; }
    public double AvgIdealGrantKb => GetAverage(TotalIdealGrantKb);

    public long TotalReservedThreads { get; set; }
    public long LastReservedThreads { get; set; }
    public long MinReservedThreads { get; set; }
    public long MaxReservedThreads { get; set; }
    public double AvgReservedThreads => GetAverage(TotalReservedThreads);

    public long TotalUsedThreads { get; set; }
    public long LastUsedThreads { get; set; }
    public long MinUsedThreads { get; set; }
    public long MaxUsedThreads { get; set; }
    public double AvgUsedThreads => GetAverage(TotalUsedThreads);

    public long TotalColumnStoreSegmentReads { get; set; }
    public long LastColumnStoreSegmentReads { get; set; }
    public long MinColumnStoreSegmentReads { get; set; }
    public long MaxColumnStoreSegmentReads { get; set; }
    public double AvgColumnStoreSegmentReads => GetAverage(TotalColumnStoreSegmentReads);

    public long TotalColumnStoreSegmentSkips { get; set; }
    public long LastColumnStoreSegmentSkips { get; set; }
    public long MinColumnStoreSegmentSkips { get; set; }
    public long MaxColumnStoreSegmentSkips { get; set; }
    public double AvgColumnStoreSegmentSkips => GetAverage(TotalColumnStoreSegmentSkips);

    public long TotalSpills { get; set; }
    public long LastSpills { get; set; }
    public long MinSpills { get; set; }
    public long MaxSpills { get; set; }
    public double AvgSpills => GetAverage(TotalSpills);

    public long TotalNumPhysicalReads { get; set; }
    public long LastNumPhysicalReads { get; set; }
    public long MinNumPhysicalReads { get; set; }
    public long MaxNumPhysicalReads { get; set; }
    public double AvgNumPhysicalReads => GetAverage(TotalNumPhysicalReads);

    public long TotalPageServerReads { get; set; }
    public long LastPageServerReads { get; set; }
    public long MinPageServerReads { get; set; }
    public long MaxPageServerReads { get; set; }
    public double AvgPageServerReads => GetAverage(TotalPageServerReads);

    public long TotalNumPageServerReads { get; set; }
    public long LastNumPageServerReads { get; set; }
    public long MinNumPageServerReads { get; set; }
    public long MaxNumPageServerReads { get; set; }
    public double AvgNumPageServerReads => GetAverage(TotalNumPageServerReads);




}