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

        StringBuilder htmlTables = new StringBuilder();
        foreach (var sortingDefinition in AllSortingDefinitions.Get())
        {
            string htmlForSortedProperty = $"<div id='{sortingDefinition.GetHtmlId()}'>" +
                          "<!-- Not imaplemented -->" +
                          $"</div>";

            htmlTables.AppendLine(htmlForSortedProperty);
        }

        return ExporterResources.HtmlTemplate
            .Replace("{{ Body }}", htmlTables.ToString())
            .Replace("{{ MainJS }}", ExporterResources.MainJS)
            .Replace("{{ StylesCSS }}", ExporterResources.StyleCSS);
    }

    public string Export<TOrderProperty>(Func<QueryCacheRow, TOrderProperty> descendingSort)
    {
        var sortedRows = Rows.OrderByDescending(descendingSort).ToArray();
        throw new NotImplementedException();
    }
}

public static class SortingDefinitionExtensions
{
    public static string GetHtmlId(this ColumnDefinition arg)
    {
        return $"Content_{arg.PropertyName}";
    }
}
