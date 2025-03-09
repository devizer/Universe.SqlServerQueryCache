using System.Data;
using System.Globalization;
using Universe.GenericTreeTable;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlIndexStatSummaryRow
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
    public static ConsoleTable BuildPlainConsoleTable(this IEnumerable<SqlIndexStatSummaryRow> arg)
    {
        return BuildPlainConsoleTable(arg, false);
    }

    public static ConsoleTable BuildPlainConsoleTable(this IEnumerable<SqlIndexStatSummaryRow> arg, bool excludeEmptyColumns)
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

        HashSet<string> emptyMetrics = new HashSet<string>();
        foreach (var metric in metrics)
        {
            bool isNonEmpty = arg.Any(r => r.GetMetricValue(metric).GetValueOrDefault() != 0);
            if (!isNonEmpty) emptyMetrics.Add(metric);
        }

        List<string> nonEmptyMetrics = metrics.Where(m => !emptyMetrics.Contains(m)).ToList();
        var reportMetrics = excludeEmptyColumns ? nonEmptyMetrics : metrics;

        var columns = new List<string>() { "DB", "Table/View", "Index" };
        columns.AddRange(reportMetrics.Select(GetMetricTitle));
        ConsoleTable ret = new ConsoleTable(columns.ToArray());
        foreach (var r in arg)
        {
            List<object> values = new List<object>() { r.Database, $"[{r.SchemaName}].{r.ObjectName}", r.IndexName };
            foreach (var metric in reportMetrics)
            {
                long? valNullable = r.GetMetricValue(metric);
                var valString = valNullable.GetValueOrDefault() == 0 ? null : (object)valNullable.Value.ToString("n0");
                values.Add(valString);
            }
            ret.AddRow(values.ToArray());
        }

        return ret;
    }

    public static IEnumerable<SqlIndexStatSummaryRow> GetRidOfUnnamedIndexes(this IEnumerable<SqlIndexStatSummaryRow> arg)
    {
        foreach (var r in arg)
            if (!string.IsNullOrEmpty(r.IndexName)) yield return r;
    }

    public static IEnumerable<SqlIndexStatSummaryRow> GetRidOfMicrosoftShippedObjects(this IEnumerable<SqlIndexStatSummaryRow> arg)
    {
        foreach (var r in arg)
            if (!r.IsMsShipped) yield return r;
    }

}