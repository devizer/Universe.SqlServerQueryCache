using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerJam;

namespace Universe.SqlServerQueryCache.Tests
{
    internal class SqlServersTestCaseSource
    {
        static Lazy<List<SqlServerRef>> _SqlServers = new Lazy<List<SqlServerRef>>(SqlDiscovery.GetLocalDbAndServerList);

        public static List<SqlServerRef> SqlServers => _SqlServers.Value;
    }
}
