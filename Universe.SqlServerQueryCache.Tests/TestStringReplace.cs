using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Tests;

public class TestStringReplace
{
    [Test]
    [TestCase("a", "a", "x", "x")]
    [TestCase("a", "Z", "x", "a")]
    [TestCase("abc", "a", "x", "xbc")]
    [TestCase("Abc", "a", "x", "xbc")]
    [TestCase("œ", "oe", "", "")]
    [TestCase("-œ", "oe", "", "-")]
    [TestCase("œ-", "oe", "", "œ-", StringComparison.OrdinalIgnoreCase)] // FAIL BY DESIGN if Ignore Case
    public void TestOnComparision(string arg, string old, string @new, string expected, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
    {
        Assert.AreEqual(expected, arg.ReplaceCore(old, @new, comparison));
    }

}