using System.Drawing;

namespace Universe.SqlServerQueryCache.TSqlSyntax
{
    public class SqlSyntaxColors
    {
        // Default values for light theme
        public Color Text { get; set; } = Color.Black;
        public Color Comment { get; set; } = Color.Green;
        public Color String { get; set; } = Color.Red;
        public Color Keyword { get; set; } = Color.Blue;
        public Color DataType { get; set; } = Color.Fuchsia;

        public static SqlSyntaxColors LightTheme => new SqlSyntaxColors()
        {
            Text = Color.Black,
            Comment = Color.Green,
            String = Color.Red,
            Keyword = Color.Blue,
            DataType = Color.Fuchsia
        };

        public static SqlSyntaxColors DarkTheme => new SqlSyntaxColors()
        {
            Text = Color.FromArgb(unchecked((int)0xFFF0F0F0)),
            Comment = Color.FromArgb(unchecked((int)0xFFA5FF7F)),
            String = Color.FromArgb(unchecked((int)0xFFFFA0A0)),
            Keyword = Color.FromArgb(unchecked((int)0xFF93B0FF)),
            DataType = Color.FromArgb(unchecked((int)0xFFFF7AFF))
        };

    }
}