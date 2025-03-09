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

        public ConsoleTable CreateColumns()
        {
            throw new NotImplementedException();
        }

        public void WriteColumns(ConsoleTable table, string renderedKey, SqlIndexStatSummaryRow nodeData)
        {
            throw new NotImplementedException();
        }

    }
}
