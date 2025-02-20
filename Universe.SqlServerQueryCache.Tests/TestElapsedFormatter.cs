using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Tests;

public class TestElapsedFormatter
{
    [Test]
    public void TestFormat()
    {
        var testCases = new[] { 0.0001, 
            0.01, 
            0.1, 
            9.9, 
            59.9, 
            60.1, 
            3599, 
            3599.9, 
            3600, 
            3600.1,
            24*3600-0.1,
            24*3600,
            24*3600+0.1,
        };
        foreach (object sec in testCases)
        {
            decimal seconds = Convert.ToDecimal(sec);
            string columnArgument = $"{seconds} Seconds:";
            Console.WriteLine($"{columnArgument,-17} {ElapsedFormatter.FormatElapsedAsHtml(TimeSpan.FromSeconds((double)seconds))}");
        }
    }
}