namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class QueryCacheRow
{
    public byte[] SqlHandle { get; set; }
    public DateTime CreationTime { get; set; }
    public string SqlStatement { get; set; }
    public long ExecutionCount { get; set; }

    public long PlanGenerationNum { get; set; }
    public long LastExecutionTime { get; set; }

    public long TotalElapsedTime { get; set; }
    public long LastElapsedTime { get; set; }
    public long MinElapsedTime { get; set; }
    public long MaxElapsedTime { get; set; }
    public long TotalWorkerTime { get; set; }
    public long LastWorkerTime { get; set; }
    public long MinWorkerTime { get; set; }
    public long MaxWorkerTime { get; set; }

    public long TotalPhysicalReads { get; set; }
    public long LastPhysicalReads { get; set; }
    public long MinPhysicalReads { get; set; }
    public long MaxPhysicalReads { get; set; }
    public long TotalLogicalWrites { get; set; }
    public long LastLogicalWrites { get; set; }
    public long MinLogicalWrites { get; set; }
    public long MaxLogicalWrites { get; set; }

}