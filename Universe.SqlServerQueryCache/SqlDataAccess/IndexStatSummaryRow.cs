using System.Globalization;
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

    public long? GetMetricValue(string propertyName)
    {
        return !string.IsNullOrEmpty(propertyName) && Metrics.TryGetValue(propertyName, out var rawRet) ? rawRet : null;
    }


}

public static class IndexStatSummaryRowExtensions
{
    public static ConsoleTable BuildPlainConsoleTable(this IEnumerable<IndexStatSummaryRow> arg)
    {
        string GetMetricTitle(string metricId)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metricId.Replace("_", " "));
        }
        List<string> metrics = new List<string>();
        bool hasToInclude = false;
        foreach (var metricName in arg.FirstOrDefault()?.Metrics.Keys.ToList() ?? new List<string>())
        {
            hasToInclude = hasToInclude || metricName.ToLower() == "leaf_insert_count";
            if (hasToInclude) metrics.Add(metricName);
        }

        var columns = new List<string>() { "DB", "Table", "Index" };
        columns.AddRange(metrics.Select(GetMetricTitle));
        ConsoleTable ret = new ConsoleTable(columns.ToArray());
        foreach (var r in arg)
        {
            List<object> values = new List<object>() { r.Database, $"[{r.SchemaName}].{r.ObjectName}", r.IndexName };
            foreach (var metric in metrics)
            {
                long? valNullable = r.GetMetricValue(metric);
                values.Add(valNullable.HasValue ? (object)valNullable.Value : null);
            }
            ret.AddRow(values.ToArray());
        }

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