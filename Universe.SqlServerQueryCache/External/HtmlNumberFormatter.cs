using System.Text;

namespace Universe.SqlServerQueryCache.External;

public class HtmlNumberFormatter
{
    public static volatile bool DisableFormat = false;

    public static string Format(object numericArg, int fractionalCount, string classPrefix)
    {
        if (numericArg == null) return "";
        double value = Convert.ToDouble(numericArg);
        if (DisableFormat)
        {
            return value.ToString($"n{fractionalCount}");
        }

        if (Math.Abs(value) <= Double.Epsilon) return "";

        string stringFractional;
        if (fractionalCount > 0)
        {
            long pow = 1;
            if (fractionalCount == 1) pow = 10;
            else if (fractionalCount == 2) pow = 100;
            else if (fractionalCount == 3) pow = 1000;
            else if (fractionalCount == 4) pow = 10000;
            else
                for (int i = 0; i < fractionalCount; i++)
                    pow *= 10;

            long longFractional = ((long)Math.Floor(value * pow)) % pow;
            stringFractional = longFractional.ToString(new string('0', fractionalCount));
        }
        else
            stringFractional = null;

        string stringThousand = ((long)Math.Floor(value / 1000)).ToString("n0");
        if (stringThousand == "0") stringThousand = null;

        long longNatural = (long)Math.Floor(value) % 1000;
        string stringNatural =
            stringThousand == null
                ? longNatural.ToString("0")
                : longNatural.ToString("000");

        StringBuilder ret = new StringBuilder();
        if (stringThousand != null)
            ret.Append($"<span class='Thousand'>{stringThousand.Replace(",", "<small>,</small>")}<small>,</small></span>");


        ret.Append(stringNatural);
        if (stringFractional != null)
            ret.Append($"<span class='Fractional'>.{stringFractional}</span>");

        return ret.ToString();
    }
}