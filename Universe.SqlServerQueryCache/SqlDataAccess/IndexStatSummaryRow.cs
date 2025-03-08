namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class IndexStatSummaryRow
{
    public int DatabaseId { get; set; }
    public string Database { get; set; }
    public int SchemaId { get; set; }
    public string SchemaName { get; set; }
    public int ObjectId { get; set; }
    public string ObjectName { get; set; }
    public int IndexId { get; set; }
    public string IndexName { get; set; }
    public int PartitionNumber { get; set; }
    public long PageLatchWaitCount { get; set; }
    public long PageLatchWaitInMs { get; set; }
    public long PageIoLatchWaitCount { get; set; }
    public long PageIoLatchWaitInMs { get; set; }
    public IDictionary<string, long> Metrics { get; set; }


}