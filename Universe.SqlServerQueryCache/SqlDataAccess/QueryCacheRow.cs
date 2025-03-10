namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class QueryCacheRow
{
    public int DatabaseId { get; set; }
    public string DatabaseName { get; set; }
    public int ObjectId { get; set; }
    public string ObjectName { get; set; } // TODO: Need Separate Query
    public string ObjectType { get; set; } // TODO: Need Separate Query
    public byte[] SqlHandle { get; set; }
    public DateTime CreationTime { get; set; }
    public string SqlStatement { get; set; }
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

}