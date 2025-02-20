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
        double totalSeconds = elapsed.TotalSeconds;

        int hoursTotalInt = (int)Math.Floor(totalSeconds / 3600);
        int daysTotalInt = hoursTotalInt / 24;

        if (totalSeconds < 60)
            // return elapsed.TotalSeconds.ToString("0.00") + "s";
            return 
                Math.Floor(elapsed.TotalSeconds).ToString("0") 
                + "<span class='SecondsFractional'>." + GetFractional(elapsed, 2) + "</span>"
                + "<span class='SecondsSign'>s</span>";

        else if (totalSeconds < 60 * 60)
            // return elapsed.ToString("mm':'ss'.'f");
            return new DateTime(0).Add(elapsed).ToString("mm':'ss")
                   + "<span class='SecondsFractional'>." + GetFractional(elapsed, 1) + "</span>";

        else if (totalSeconds < 24 * 3600)
            // return hoursTotalInt.ToString("00") + ":" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
            return $"<span class='Hours'>{hoursTotalInt:00}:</span>" 
                   + new DateTime(0).Add(elapsed).ToString("mm':'ss");
        else
        {
            // return elapsed.ToString("d'.'hh':'mm':'ss'.'f");
            // return daysTotalInt.ToString("0") + "d " + (hoursTotalInt % 24).ToString("00") + ":" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
            return daysTotalInt.ToString("0") + "d " + $"<span class='Hours'>{hoursTotalInt % 24:00}:</span>" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
        }
    }

}