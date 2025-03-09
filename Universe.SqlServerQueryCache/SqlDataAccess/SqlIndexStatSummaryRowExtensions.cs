﻿using System.Globalization;
using Universe.GenericTreeTable;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlIndexStatSummaryReport
{
    public IEnumerable<SqlIndexStatSummaryRow> RawArgs { get; internal set; }
    public bool ExcludeEmptyColumns { get; internal set; }
    public List<string> Metrics { get; internal set; } // Depends on SQL Server Version
    public HashSet<string> EmptyMetrics { get; internal set; }
    public List<string> NonEmptyMetrics { get; internal set; }
    public List<string> PlainColumns { get; internal set; }

    public ConsoleTable PlainTable { get; internal set; }
    public ConsoleTable TreeTable { get; internal set; }
}

public static class SqlIndexStatSummaryRowExtensions
{
    public static ConsoleTable BuildTreeConsoleTable(this IEnumerable<SqlIndexStatSummaryRow> arg)
    {
        SqlIndexStatTreeConfiguration treeConfiguration = new SqlIndexStatTreeConfiguration(/* depends on content */);
        var builder = new TreeTableBuilder<string, SqlIndexStatSummaryRow>(treeConfiguration);
        // builder.Build()
        throw new NotImplementedException();
    }
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
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(metricId.Replace("_", " "));
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

        var columns = new List<string>() { "DB", "Table/View", "Index" };
        columns.AddRange(reportMetrics.Select(GetMetricTitle));
        ConsoleTable plainConsoleTable = new ConsoleTable(columns.ToArray());
        foreach (var r in arg)
        {
            List<object> values = new List<object>() { r.Database, $"[{r.SchemaName}].{r.ObjectName}", r.IndexName };
            foreach (var metric in reportMetrics)
            {
                long? valNullable = r.GetMetricValue(metric);
                var valString = valNullable.GetValueOrDefault() == 0 ? null : (object)valNullable.Value.ToString("n0");
                values.Add(valString);
            }
            plainConsoleTable.AddRow(values.ToArray());
        }

        ret.PlainTable = plainConsoleTable;

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