using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Exporter
{
    public static class CustomSummaryRowReader
    {
        private const string ENV_NAME_BASE = "SQL_QUERY_CACHE_SUMMARY_";

        public class CustomSummaryRow : SummaryRow
        {
            // Default is 2000000000, at the end
            public int Position { get; set; } = 2000000000;
        }

        public static IEnumerable<CustomSummaryRow> GetCustomSummary()
        {
            var allKeys = Environment.GetEnvironmentVariables().Keys
                .OfType<object>()
                .Select(x => Convert.ToString(x)?.Trim()?.ToUpper())
                .Where(x => !string.IsNullOrEmpty(x))
                .Where(x => x.StartsWith(ENV_NAME_BASE))
                .OrderBy(x => x)
                .ToList();

            var titleKeys = allKeys.Where(x => x.ToUpper().EndsWith("_TITLE")).ToList();
            Console.WriteLine($"[Debug] TITLES: {titleKeys.ToJsonString()}");
            foreach (var titleKey in titleKeys)
            {
                Func<string, string> getProperty = property =>
                {
                    int len = titleKey.Length - "_TITLE".Length - ENV_NAME_BASE.Length;
                    string ret = null;
                    if (titleKey.Length > ENV_NAME_BASE.Length && len > 0)
                    {
                        var prefix = titleKey.Substring(ENV_NAME_BASE.Length, len);
                        var varName = $"{ENV_NAME_BASE}{prefix}_{property}".ToUpper();
                        var realVarName = allKeys.FirstOrDefault(x => x == varName);
                        Console.WriteLine(@$"[DEBUG property '{property}' for '{titleKey}'] 
   realVarName=[{realVarName}]
   prefix=[{prefix}]
   varName=[{varName}]");
                        if (realVarName != null)
                        {
                            ret = Environment.GetEnvironmentVariable(varName);
                        }
                    }
                    Console.WriteLine($"[DEBUG property '{property}' for '{titleKey}'] ret=[{ret}]");
                    return string.IsNullOrEmpty(ret) ? null : ret;
                };
                string title = Environment.GetEnvironmentVariable(titleKey);
                if (string.IsNullOrEmpty(title)) continue;
                string rawKind = getProperty("KIND") ?? "Unknown";
                string rawValue = getProperty("VALUE") ?? null;
                string rawPosition = getProperty("POSITION") ?? "2000000000";
                Console.WriteLine(@$"[DEBUG for '{titleKey}'] 
   rawKind=[{rawKind}]
   rawValue=[{rawValue}]
   rawPosition=[{rawPosition}]");

                FormatKind? kind = TryParseKind(rawKind);
                int position = Int32.TryParse(rawPosition, out var tempPosition) ? tempPosition : 2000000000;
                object value = kind.GetValueOrDefault() == FormatKind.Unknown ? Convert.ToString(rawValue) :
                    double.TryParse(rawValue, out var tempValue) ? tempPosition : null;

                yield return new CustomSummaryRow()
                {
                    Title = title,
                    Value = value,
                    Kind = kind.GetValueOrDefault(),
                    Position = position
                };
            }
        }
        // Title
        // Kind
        // Value
        // Position
/*
Natural,
Numeric1,
Numeric2,
Pages,
PagesPerSecond,
Timespan,
*/

        static FormatKind? TryParseKind(string rawKind)
        {
            try
            {
                return (FormatKind)Enum.Parse(typeof(FormatKind), rawKind);
            }
            catch
            {
                return null;
            }
        }
    }
}
