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
    }
}