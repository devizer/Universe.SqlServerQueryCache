namespace Universe.SqlServerQueryCache.External;

public class ElapsedFormatter
{
    static string GetFractional(TimeSpan elapsed, int fractionalCount)
    {
        double totalSeconds = elapsed.TotalSeconds;
        var fracSeconds = totalSeconds - Math.Floor(totalSeconds);
        // TODO: Optimize
        return fracSeconds.ToString($"0.{new string('0', fractionalCount)}").Substring(2);
    }

    public static string FormatElapsedAsHtml(TimeSpan elapsed)
    {
        return FormatElapsed(elapsed, true);
    }
    public static string FormatElapsedAsText(TimeSpan elapsed)
    {
        return FormatElapsed(elapsed, false);
    }

    public static string FormatElapsed(TimeSpan elapsed, bool needHtml)
    {
        double totalSeconds = elapsed.TotalSeconds;

        int hoursTotalInt = (int)Math.Floor(totalSeconds / 3600);
        int daysTotalInt = hoursTotalInt / 24;

        bool h = needHtml;
        if (totalSeconds < 60)
            // return elapsed.TotalSeconds.ToString("0.00") + "s";
            return 
                Math.Floor(elapsed.TotalSeconds).ToString("0") 
                + (h ? "<span class='SecondsFractional'>." : ".") 
                + GetFractional(elapsed, 2) 
                + (h ? "</span>" : "")
                + (h ? "<span class='SecondsSign'>s</span>" : "s");

        else if (totalSeconds < 60 * 60)
            // return elapsed.ToString("mm':'ss'.'f");
            return
                new DateTime(0).Add(elapsed).ToString("mm':'ss")
                + (h ? "<span class='SecondsFractional'>." : ".")
                + GetFractional(elapsed, 1)
                + (h ? "</span>" : "");

        else if (totalSeconds < 24 * 3600)
            return
                (h ? $"<span class='Hours'>{hoursTotalInt:00}:</span>" : $"{hoursTotalInt:00}:")
                + new DateTime(0).Add(elapsed).ToString("mm':'ss");
        else
        {
            // return elapsed.ToString("d'.'hh':'mm':'ss'.'f");
            // return daysTotalInt.ToString("0") + "d " + (hoursTotalInt % 24).ToString("00") + ":" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
            return
                (h ? "<span class='Hours'>" : "")
                + daysTotalInt.ToString("0")
                + (h ? "d&nbsp;" : "d ")
                + $"{hoursTotalInt % 24:00}:"
                + (h ? "</span>" : "")
                + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
        }
    }

}