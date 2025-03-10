using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.GenericTreeTable;

namespace Universe.SqlServerQueryCache.SqlDataAccess
{
    internal class SqlIndexStatTreeConfiguration : ITreeTableConfiguration<string, SqlIndexStatSummaryRow>
    {
        public IEqualityComparer<string> EqualityComparer { get; } = StringComparer.Ordinal;
        public string Separator => TheSeparator;
        public static readonly string TheSeparator = " \x2192 ";
        public string KeyPartToText(string keyPart) => keyPart;


        public readonly List<List<string>> TreeColumns;
        public readonly Func<SqlIndexStatSummaryRow, List<object>> WriteMetricsCell;

        public SqlIndexStatTreeConfiguration(List<List<string>> treeColumns, Func<SqlIndexStatSummaryRow, List<object>> writeMetricsCell)
        {
            TreeColumns = treeColumns;
            WriteMetricsCell = writeMetricsCell;
        }

        public ConsoleTable CreateColumns()
        {
            return new ConsoleTable(TreeColumns) { NeedUnicode = true };
        }

        public void WriteColumns(ConsoleTable table, string renderedKey, SqlIndexStatSummaryRow nodeData)
        {
            List<object> row = new List<object>();
            row.Add(renderedKey);
            row.AddRange(WriteMetricsCell(nodeData));
            table.AddRow(row.ToArray());
        }
    }
}
