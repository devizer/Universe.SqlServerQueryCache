namespace Universe.SqlServerQueryCache.External;

public class ElapsedFormatter
{
    public static string FormatElapsed(TimeSpan elapsed)
    {
        var totalSeconds = elapsed.TotalSeconds;

        int hoursTotalInt = (int)Math.Floor(totalSeconds / 3600);
        int daysTotalInt = hoursTotalInt / 24;

        if (totalSeconds < 60)
            return elapsed.TotalSeconds.ToString("0.00") + "s";

        else if (totalSeconds < 60 * 60)
            // return elapsed.ToString("mm':'ss'.'f");
            return new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");

        else if (totalSeconds < 24 * 3600)
            return hoursTotalInt.ToString("00") + ":" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
        else
        {
            // return elapsed.ToString("d'.'hh':'mm':'ss'.'f");
            return daysTotalInt.ToString("0") + "d " + (hoursTotalInt % 24).ToString("00") + ":" + new DateTime(0).Add(elapsed).ToString("mm':'ss'.'f");
        }
    }

}