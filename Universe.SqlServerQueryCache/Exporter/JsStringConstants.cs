using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Exporter
{
    public class JsStringConstants
    {
        private Dictionary<string, string> KeysByString = new Dictionary<string, string>();
        private readonly object Sync = new object();

        public string GetOrAddThenReturnKey(string theString)
        {
            if (theString == null) return null;
            lock (Sync)
            {
                if (KeysByString.TryGetValue(theString, out var key)) return key;
                var index = KeysByString.Count;
                key = $"{(char)((index % 26) + 65)}{(index/26):0}";
                KeysByString[theString] = key;
                return key;
            }
        }
    }
}
