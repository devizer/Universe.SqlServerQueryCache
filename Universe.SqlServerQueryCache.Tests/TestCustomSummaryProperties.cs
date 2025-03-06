using Universe.SqlServerQueryCache.Exporter;
using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Tests;

public class TestCustomSummaryProperties
{
    [Test]
    public void ShowCustomProperties()
    {
        var customSummary = CustomSummaryRowReader.GetCustomSummary();
        Console.WriteLine($"CUSTOM SUMMARY:{customSummary.ToJsonString()}");
        /*
        var allEnvVars = Environment.GetEnvironmentVariables().Keys
            .OfType<object>()
            .Select(x => Convert.ToString(x))
            .OrderBy(x => x)
            .Select(x => new { Var = x, Val = Environment.GetEnvironmentVariable(x) })
            .ToArray();

        Console.WriteLine($"All Env Vars:{allEnvVars.ToJsonString()}");
        */

    }
}