using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Tests
{
    internal class TestEnvironment
    {
        private static Lazy<string> _DumpFolder = new Lazy<string>(() =>
        {
            var raw = Environment.GetEnvironmentVariable("SYSTEM_ARTIFACTSDIRECTORY");
            raw = raw ?? Environment.CurrentDirectory;
            var ret = Path.Combine(Path.GetFullPath(raw), "TestResults");
            TryAndForget.Execute(() => Directory.CreateDirectory(ret));
            return ret;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string DumpFolder => _DumpFolder.Value;
    }
}
