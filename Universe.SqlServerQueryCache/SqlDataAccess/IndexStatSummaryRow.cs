using Universe.GenericTreeTable;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class IndexStatSummaryRow
{
    public int DatabaseId { get; set; }
    public string Database { get; set; }
    public int SchemaId { get; set; }
    public string SchemaName { get; set; }
    public int ObjectId { get; set; }
    public string ObjectName { get; set; }
    public bool IsMsShipped { get; set; }
    public int IndexId { get; set; }
    public string IndexName { get; set; }
    public string IndexType { get; set; }
    public int PartitionNumber { get; set; }
    public long PageLatchWaitCount { get; set; }
    public long PageLatchWaitInMs { get; set; }
    public long PageIoLatchWaitCount { get; set; }
    public long PageIoLatchWaitInMs { get; set; }
    public IDictionary<string, long> Metrics { get; set; }


}

public static class IndexStatSummaryRowExtensions
{
    public static ConsoleTable BuildPlainConsoleTable(this IEnumerable<IndexStatSummaryRow> arg)
    {
        ConsoleTable ret = new ConsoleTable("DB", "Table", "Index");
        foreach (var r in arg)
            ret.AddRow(r.Database, $"[{r.SchemaName}].{r.ObjectName}", r.IndexName);

        return ret;
    }

    public static IEnumerable<IndexStatSummaryRow> GetRidOfUnnamedIndexes(this IEnumerable<IndexStatSummaryRow> arg)
    {
        foreach (var r in arg)
            if (!string.IsNullOrEmpty(r.IndexName)) yield return r;
    }

    public static IEnumerable<IndexStatSummaryRow> GetRidOfMicrosoftShippedObjects(this IEnumerable<IndexStatSummaryRow> arg)
    {
        foreach (var r in arg)
            if (!r.IsMsShipped) yield return r;
    }

}