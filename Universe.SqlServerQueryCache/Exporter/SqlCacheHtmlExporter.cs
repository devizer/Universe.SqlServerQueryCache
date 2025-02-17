using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

    public string Export<TOrderProperty>(Func<QueryCacheRow, TOrderProperty> descendingSort)
    {
        var sortedRows = Rows.OrderByDescending(descendingSort).ToArray();
        throw new NotImplementedException();
    }
}