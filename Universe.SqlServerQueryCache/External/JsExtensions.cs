using System.Text;
using Universe.SqlServerQueryCache.Exporter;

namespace Universe.SqlServerQueryCache.External;

public static class JsExtensions
{
    public static TextWriter WriteEncodedJsString(this TextWriter output, string s)
    {
        var sb = output;
        sb.Append("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < 32 || c > 127)
                    {
                        // sb.AppendFormat("\\u{0:X04}", (int)c);
                        sb.Append($"\\u{(int)c:X04}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append("\"");
        return sb;
    }


    public static string EncodeJsString(string s)
    {
        StringBuilder sb = new StringBuilder();
        StringWriter wr = new StringWriter(sb);
        WriteEncodedJsString(wr, s);
        wr.Flush();
        return sb.ToString();
    }
}