using System.Diagnostics;

namespace Universe.SqlServerQueryCache;

public static class TryAndForget
{
    public delegate int GetDelayOnRetry(int retryIndex);

    public static Exception Retry(string title, Action action, int retryCount = 100, GetDelayOnRetry getDelayOnRetry = null, bool throwException = true)
    {
        Exception ret = null;
        if (getDelayOnRetry == null) getDelayOnRetry = retryIndex => 1;
        int retryIndex = 0;
        while (retryIndex < retryCount)
        {
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                retryIndex++;
                ret = ex;
                if (retryIndex >= retryCount && throwException)
                {
                    var exceptionMessage = $"Action \"{title}\" failed {retryCount} times a row. {ex.GetExceptionDigest()}";
                    Trace.WriteLine(exceptionMessage);
                    throw new InvalidOperationException(exceptionMessage, ex);
                }

                Trace.WriteLine($"Warning! Retry {retryIndex} of {retryCount} for \"{title}\" failed. {ex.GetExceptionDigest()}");
                var delay = getDelayOnRetry(retryIndex);
                Thread.Sleep(Math.Max(0, delay));
            }
        }

        return ret;
    }
    public static T Evaluate<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch
        {
            return default;
        }
    }

    public static bool Execute(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static Exception ExecuteDetailed(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}