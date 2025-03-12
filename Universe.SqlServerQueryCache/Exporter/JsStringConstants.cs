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
        const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ"; // 1234567890

        // Returns true if added
        public bool GetOrAddThenReturnKey(string theString, out string key)
        {
            key = null;
            if (theString == null) return false;
            lock (Sync)
            {
                if (KeysByString.TryGetValue(theString, out key)) return false;
                var index = KeysByString.Count;
                int prefixCount = Alphabet.Length;
                key = $"{Alphabet[index % prefixCount]}{(index / prefixCount + 1):00}";
                KeysByString[theString] = key;
                return true;
            }
        }

        public string GetKey(string theString)
        {
            GetOrAddThenReturnKey(theString, out var ret);
            return ret;
        }
    }
}
