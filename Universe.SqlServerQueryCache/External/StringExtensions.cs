using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.External
{
    public static class StringExtensions
    {
        public static string ReplaceCore(this string argument, string oldValue, string newValue, StringComparison comparison)
        {
            if (oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));
            if (oldValue.Length == 0)
                throw new ArgumentException("Argument old value can not be empty string", nameof(oldValue));

            // If they asked to replace oldValue with a null, replace all occurrences
            // with the empty string.
            if (newValue == null)
                newValue = string.Empty;

            StringBuilder result = new StringBuilder();

            int startIndex = 0;
            int index = 0;

            int matchLength = 0;

            bool hasDoneAnyReplacements = false;

            var argumentLength = argument.Length;
            do
            {
                // index =  ci.IndexOf(argument, oldValue, startIndex, argumentLength - startIndex, options);
                index = argument.IndexOf(oldValue, startIndex, comparison);
                if (index >= 0)
                {
                    // append the unmodified portion of string
                    if (index > startIndex) result.Append(argument, startIndex, index - startIndex);

                    // append the replacement
                    result.Append(newValue);

                    matchLength = oldValue.Length;
                    startIndex = index + matchLength;
                    hasDoneAnyReplacements = true;
                }
                else if (!hasDoneAnyReplacements)
                {
                    return argument;
                }
                else
                {
                    if (argumentLength > startIndex) result.Append(argument, startIndex, argumentLength - startIndex);
                }
            } while (index >= 0);

            return result.ToString();
        }

        // public static string ReplaceCore(this string argument, string oldValue, string newValue, CultureInfo culture, CompareOptions options)
        public static string ReplaceCore(this string argument, string oldValue, string newValue, CultureInfo culture, CompareOptions options)
        {
            if (oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));
            if (oldValue.Length == 0)
                throw new ArgumentException("Argument old value can not be empty string", nameof(oldValue));

            // If they asked to replace oldValue with a null, replace all occurrences
            // with the empty string.
            if (newValue == null)
                newValue = string.Empty;

            CultureInfo referenceCulture = culture ?? CultureInfo.CurrentCulture;
            StringBuilder result = new StringBuilder();

            int startIndex = 0;
            int index = 0;

            int matchLength = 0;

            bool hasDoneAnyReplacements = false;
            CompareInfo ci = referenceCulture.CompareInfo;

            var argumentLength = argument.Length;
            do
            {
                index = ci.IndexOf(argument, oldValue, startIndex, argumentLength - startIndex, options);
                if (index >= 0)
                {
                    // append the unmodified portion of string
                    if (index > startIndex) result.Append(argument, startIndex, index - startIndex);

                    // append the replacement
                    result.Append(newValue);

                    matchLength = oldValue.Length;
                    startIndex = index + matchLength;
                    hasDoneAnyReplacements = true;
                }
                else if (!hasDoneAnyReplacements)
                {
                    return argument;
                }
                else
                {
                    if (argumentLength > startIndex) result.Append(argument, startIndex, argumentLength - startIndex);
                }
            } while (index >= 0);

            return result.ToString();
        }

    }
}
