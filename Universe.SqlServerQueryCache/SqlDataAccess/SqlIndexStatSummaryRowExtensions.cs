using System.Globalization;
using Universe.GenericTreeTable;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlIndexStatSummaryReport
{
    public IEnumerable<SqlIndexStatSummaryRow> RawArgs { get; internal set; }
    public bool ExcludeEmptyColumns { get; internal set; }
    public List<string> Metrics { get; internal set; } // Depends on SQL Server Version
    public HashSet<string> EmptyMetrics { get; internal set; }
    public List<string> NonEmptyMetrics { get; internal set; }
    // public List<string> PlainColumns { get; internal set; }

    public ConsoleTable PlainTable { get; internal set; }
    public ConsoleTable TreeTable { get; internal set; }

    public string EmptyMetricsFormatted
    {
        get
        {
            if (EmptyMetrics.Count == 0) return string.Empty;
            string ret = ExcludeEmptyColumns ? "Empty Metrics below are excluded:" : "Below are empty metrics:";
            ret += Environment.NewLine + string.Join(Environment.NewLine, EmptyMetrics.Select(x => $"\t{x}").ToArray()) + Environment.NewLine;
            return ret;
        }
    }
}

public static class SqlIndexStatSummaryRowExtensions
{
    public static SqlIndexStatSummaryReport BuildPlainConsoleTable(this IEnumerable<SqlIndexStatSummaryRow> arg)
    {
        return BuildPlainConsoleTable(arg, false);
    }

    public static SqlIndexStatSummaryReport BuildPlainConsoleTable(this IEnumerable<SqlIndexStatSummaryRow> arg, bool excludeEmptyColumns)
    {
        SqlIndexStatSummaryReport ret = new SqlIndexStatSummaryReport()
        {
            RawArgs = arg.ToList(),
            ExcludeEmptyColumns = excludeEmptyColumns
        };

        string GetMetricTitle(string metricId)
        {
            var metricTitle = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metricId.Replace("_", " "));
            return metricTitle
                    .Replace(" Io ", " IO ")
                    .Replace(" Ms", " ms")
                    .Replace(" In ms", $" in{(char)160}ms")
                    .Replace(" Bytes", " bytes")
                    .Replace(" Pages", " pages")
                    .Replace(" Count", " count")
                    .Replace(" In ", " in ")
                    .Replace("Nonleaf ", "Non Leaf ")
                    .Replace(" Promotion ", " Promo ")
                    .Replace(" Allocation ", " Alloc ")
                ;

        }
        List<string> metrics = new List<string>();
        bool hasToInclude = false;
        foreach (var metricName in arg.FirstOrDefault()?.Metrics.Keys.ToList() ?? new List<string>())
        {
            hasToInclude = hasToInclude || metricName.ToLower() == "leaf_insert_count";
            if (hasToInclude) metrics.Add(metricName);
        }
        ret.Metrics = metrics;

        HashSet<string> emptyMetrics = new HashSet<string>();
        foreach (var metric in metrics)
        {
            bool isNonEmpty = arg.Any(r => r.GetMetricValue(metric).GetValueOrDefault() != 0);
            if (!isNonEmpty) emptyMetrics.Add(metric);
        }
        ret.EmptyMetrics = emptyMetrics;

        List<string> nonEmptyMetrics = metrics.Where(m => !emptyMetrics.Contains(m)).ToList();
        ret.NonEmptyMetrics = nonEmptyMetrics;
        List<string> reportMetrics = excludeEmptyColumns ? nonEmptyMetrics : metrics;

        List<List<string>> metricsColumns = reportMetrics.Select(h => $"-{GetMetricTitle(h)}".Split(' ').ToList()).ToList();

        List<List<string>> plainColumns = new List<string>() { " DB ", " Table / View ", " Index " }.Select(x => new List<string>() { x }).ToList();
        List<List<string>> treeColumns = new List<List<string>>() { new List<string>() { "DB / Table + Views / Index" } };
        plainColumns.AddRange(metricsColumns);
        treeColumns.AddRange(metricsColumns);

        List<object> WriteMetricsCells(SqlIndexStatSummaryRow row)
        {
            // metricCells for both Plain and Tree table
            List<object> metricCells = new List<object>();
            foreach (var metric in reportMetrics)
            {
                long? valNullable = row.GetMetricValue(metric);
                var valString = valNullable.GetValueOrDefault() == 0 ? null : (object)valNullable.Value.ToString("n0");
                metricCells.Add(valString);
            }
            return metricCells;
        }

        SqlIndexStatTreeConfiguration treeConfiguration = new SqlIndexStatTreeConfiguration(treeColumns, WriteMetricsCells);
        var treeBuilder = new TreeTableBuilder<string, SqlIndexStatSummaryRow>(treeConfiguration);
        ConsoleTable plainConsoleTable = new ConsoleTable(plainColumns.ToArray());
        plainConsoleTable.NeedUnicode = true;
        foreach (SqlIndexStatSummaryRow r in arg)
        {
            // metricCells for both Plain and Tree table
            List<object> metricCells = WriteMetricsCells(r);
            // Row for Plain table
            List<object> plainCells = new List<object>() { r.Database, $"[{r.SchemaName}].{r.ObjectName}", r.IndexName };
            plainCells.AddRange(metricCells);
            plainConsoleTable.AddRow(plainCells.ToArray());
            // Row for Tree table
        }
        ret.PlainTable = plainConsoleTable;

        List<KeyValuePair<IEnumerable<string>, SqlIndexStatSummaryRow>> treeSource = new List<KeyValuePair<IEnumerable<string>, SqlIndexStatSummaryRow>>();
        Func<SqlIndexStatSummaryRow,IEnumerable<string>> createKey = row => new List<string>() { row.Database, $"[{row.SchemaName}].{row.ObjectName}", row.IndexName };
        treeSource = arg.OrderBy(x => x.Database).ThenBy(x => x.ObjectName).ThenBy(x => x.IndexName).Select(x => new KeyValuePair<IEnumerable<string>, SqlIndexStatSummaryRow>(createKey(x), x)).ToList();
        ConsoleTable treeConsoleTable = treeBuilder.Build(treeSource);
        ret.TreeTable = treeConsoleTable;

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