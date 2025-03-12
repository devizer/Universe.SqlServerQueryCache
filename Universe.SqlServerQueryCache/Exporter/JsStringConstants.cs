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
        
        // Returns true if added
        public bool GetOrAddThenReturnKey(string theString, out string key)
        {
            key = null;
            if (theString == null) return false;
            lock (Sync)
            {
                if (KeysByString.TryGetValue(theString, out key)) return true;
                var index = KeysByString.Count;
                key = $"{(char)((index % 26) + 65)}{(index / 26):0}";
                KeysByString[theString] = key;
                return true;
            }
        }

        public string GetKey(string theString)
        {
            return GetOrAddThenReturnKey(theString, out var ret) ? ret : null;
        }
    }
}
