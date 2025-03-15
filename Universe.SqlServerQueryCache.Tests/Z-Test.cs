using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Tests
{
    public class Z_Test
    {
        [Test]
        public void ShowMemory()
        {
            var p = Process.GetCurrentProcess();
            Console.WriteLine($"Memory Usage");
            Console.WriteLine($"────────────");
            Console.WriteLine($"Working Set            │ {p.WorkingSet64:n0}");
            Console.WriteLine($"PagedMemory Size       │ {p.PagedMemorySize64:n0}");
            Console.WriteLine($"Peak Paged Memory Size │ {p.PeakPagedMemorySize64:n0}");
        }
    }
}
