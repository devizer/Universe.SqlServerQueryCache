using System.Drawing;
using System.Text;

namespace Universe.SqlServerQueryCache.TSqlSyntax;

public static class TSqlToVanillaHtmlConverter
{
    public static string ConvertTSqlToHtml(string tsqlCode, SqlSyntaxColors colors)
    {
        TSqlParser parser = new TSqlParser(tsqlCode);
        List<TSqlFragment> fragments = parser.Parse();
        StringBuilder ret = new StringBuilder();
        foreach (var sqlFragment in fragments)
        {
            if (sqlFragment.Start >= 0 && sqlFragment.Start + sqlFragment.Length <= tsqlCode.Length)
            {
                // To DevExpress
                // ret.Append($"<color={GetColorByKind(sqlFragment.Kind, colors)}>{tsqlCode.Substring(sqlFragment.Start, sqlFragment.Length)}</color>");
                // To Html
                ret.Append($"<span class='SqlFragment{sqlFragment.Kind}'>{tsqlCode.Substring(sqlFragment.Start, sqlFragment.Length)}</span>");
            }
        }

        return ret.ToString();
    }

    private static string GetColorByKind(TSqlFragmentKind argKind, SqlSyntaxColors colors)
    {
        switch (argKind)
        {
            case TSqlFragmentKind.Comment: return ColorAsString(colors.Comment);
            case TSqlFragmentKind.DataType: return ColorAsString(colors.DataType);
            case TSqlFragmentKind.Keyword: return ColorAsString(colors.Keyword);
            case TSqlFragmentKind.String: return ColorAsString(colors.String);
            case TSqlFragmentKind.Text: return ColorAsString(colors.Text);
            default:
                throw new ArgumentException($"Unknown T-Sql Fragment's Kind \"{argKind}\"");
        }
    }

    static string ColorAsString(Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

}