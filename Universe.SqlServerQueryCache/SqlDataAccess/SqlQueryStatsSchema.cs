using System.Text;

namespace Universe.SqlServerQueryCache.SqlDataAccess;

public class SqlQueryStatsSchema
{
    public readonly List<SqlResultSetColumn> Columns;
    public IDictionary<string, SqlResultSetColumn> ColumnsAsDictionary { get; protected set; }

    public SqlQueryStatsSchema(IEnumerable<SqlResultSetColumn> columns)
    {
        Columns = columns.ToList();
        ColumnsAsDictionary = new Dictionary<string, SqlResultSetColumn>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var sqlResultSetColumn in Columns)
            ColumnsAsDictionary[sqlResultSetColumn.Name] = sqlResultSetColumn;
    }

    public SqlResultSetColumn GetColumn(string name)
    {
        ColumnsAsDictionary.TryGetValue(name, out var ret);
        return ret;
    }

    public override string ToString()
    {
        StringBuilder ret = new StringBuilder();
        int index = 0;
        int indexTotal = -1;
        foreach (var column in Columns)
        {
            bool isTotal = column.Name.StartsWith("total_");
            if (isTotal)
            {
                ret.AppendLine();
                indexTotal = index;
            }
            ret.AppendLine(column.ToString());
            if (index == indexTotal + 3 && indexTotal > 0)
            {
                indexTotal = -1;
                bool isNextTotal = index + 1 < Columns.Count && Columns[index + 1].Name.StartsWith("total_");
                if (!isNextTotal) ret.AppendLine();
            }
            index++;
        }

        return ret.ToString();
    }

    public bool HasRows => GetHasFourColumns("rows");
    public bool HasDop => GetHasFourColumns("dop");
    public bool HasGrantKb => GetHasFourColumns("grant_kb");
    public bool HasUsedGrantKb => GetHasFourColumns("used_grant_kb");
    public bool HasIdealGrantKb => GetHasFourColumns("ideal_grant_kb");
    public bool HasReservedThreads => GetHasFourColumns("reserved_threads");
    public bool HasUsedThreads => GetHasFourColumns("used_threads");
    public bool HasColumnStoreSegmentReads => GetHasFourColumns("columnstore_segment_reads");
    public bool HasColumnStoreSegmentSkips => GetHasFourColumns("columnstore_segment_skips");
    public bool HasSpills => GetHasFourColumns("spills");
    public bool HasNumPhysicalReads => GetHasFourColumns("num_physical_reads");
    public bool HasPageServerReads => GetHasFourColumns("page_server_reads");
    public bool HasNumPageServerReads => GetHasFourColumns("num_page_server_reads");



    private static readonly string[] _Prefixes = new[] { "total", "last", "min", "max" };
    private bool GetHasFourColumns(string suffix)
    {
        return _Prefixes.All(x => GetColumn($"{x}_{suffix}") != null);
    }
}