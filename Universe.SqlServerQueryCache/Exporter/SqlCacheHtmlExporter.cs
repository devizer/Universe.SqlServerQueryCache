using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.SqlDataAccess;

namespace Universe.SqlServerQueryCache.Exporter;

public class SqlCacheHtmlExporter
{
    private IEnumerable<QueryCacheRow> Rows;

    public SqlCacheHtmlExporter(IEnumerable<QueryCacheRow> rows)
    {
        Rows = rows;
    }



    public string Export()
    {
        var selectedSortProperty = "Content_AvgElapsedTime";
        StringBuilder htmlTables = new StringBuilder();
        htmlTables.AppendLine($"<script>selectedSortProperty = '{selectedSortProperty}';</script>");
        foreach (var sortingDefinition in AllSortingDefinitions.Get())
        {
            bool isSelected = selectedSortProperty == sortingDefinition.GetHtmlId();
            string htmlForSortedProperty = @$"<div id='{sortingDefinition.GetHtmlId()}' class='{(isSelected ? "" : "Hidden")}'>
{Export(sortingDefinition, isSelected)}
</div>";

            htmlTables.AppendLine(htmlForSortedProperty);
        }

        return ExporterResources.HtmlTemplate
            .Replace("{{ Body }}", htmlTables.ToString())
            .Replace("{{ MainJS }}", ExporterResources.MainJS)
            .Replace("{{ StylesCSS }}", ExporterResources.StyleCSS);
    }

    public string Export(ColumnDefinition sortByColumn, bool isFieldSelected)
    {
        var headers = AllSortingDefinitions.GetHeaders().ToArray();
        var sortedRows = sortByColumn.SortAction(Rows).ToArray();
        StringBuilder htmlTable = new StringBuilder();
        htmlTable.AppendLine("  <table class='Metrics'><thead>");
        htmlTable.AppendLine("  <tr>");
        foreach (var header in headers)
        {
            htmlTable.AppendLine($"    <th colspan='{header.Columns.Count}' class='TableHeaderGroupCell'>{header.Caption}</th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  <tr>");
        var columnDefinitions = headers.SelectMany(h => h.Columns).ToArray();
        foreach (var column in columnDefinitions)
        {
            bool isThisSorting = column.PropertyName == sortByColumn.PropertyName;
            const string arrows = " ⇓ ⇩ ↓ ↡";
            var attrs = "";
            var onClick = $"onclick='SelectContent(\"{column.GetHtmlId()}\"); alert('HAHA'); return false;'";
            if (!isFieldSelected && column.AllowSort) attrs = $"style=\"cursor: pointer; display: inline-block;\" class='SortButton' data-sorting='{column.GetHtmlId()}'";
            var spanSortingParameter = $"<span id='SortingParameter' class='Hidden'>{column.GetHtmlId()}</span>";
            htmlTable.AppendLine($"    <th class='TableHeaderCell {(isThisSorting ? "Selected" : "")}' data-sorting='{column.GetHtmlId()}'><button {attrs}>{column.TheCaption}</button></th>");
        }
        htmlTable.AppendLine("  </tr>");
        htmlTable.AppendLine("  </thead>");

        htmlTable.AppendLine("  <tbody>");
        foreach (QueryCacheRow row in sortedRows)
        {
            htmlTable.AppendLine("  <tr class='MetricsRow'>");
            foreach (ColumnDefinition column in columnDefinitions)
            {
                var value = column.PropertyAccessor(row);
                var valueString = GetValueAsHtml(value, row, column);
                htmlTable.AppendLine($"\t\t<td>{valueString}</td>");
            }
            htmlTable.AppendLine("\t</tr>");
            htmlTable.AppendLine("\t<tr class='SqlRow'>");
            htmlTable.AppendLine($"\t\t<td colspan='{columnDefinitions.Length}'><pre>{row.SqlStatement}</pre></td>");
            htmlTable.AppendLine("\t</tr>");
        }
        htmlTable.AppendLine("  </tbody>");

        htmlTable.AppendLine("  </table>");
        return htmlTable.ToString();
    }

    private static string GetValueAsHtml(object value, QueryCacheRow row, ColumnDefinition column)
    {
        var valueString = Convert.ToString(value);
        if (value is long l)
            valueString = l == 0 ? "" : l.ToString("n0");
        if (value is double d)
            valueString = Math.Abs(d) <= Double.Epsilon ? "" : d.ToString("n2");
        return valueString;
    }
}

public static class SortingDefinitionExtensions
{
    public static string GetHtmlId(this ColumnDefinition arg)
    {
        return $"Content_{arg.PropertyName}";
    }
}
