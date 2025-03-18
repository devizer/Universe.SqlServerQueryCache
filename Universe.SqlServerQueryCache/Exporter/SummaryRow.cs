using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Universe.SqlServerQueryCache.External;

namespace Universe.SqlServerQueryCache.Exporter;

public class SummaryRow
{
    public string Title { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public FormatKind Kind { get; set; }
    public object Value { get; set; }
    public bool IsHeader { get; set; }


    public SummaryRow()
    {
    }

    public SummaryRow(string title, FormatKind kind, object value)
    {
        Title = title;
        Kind = kind;
        Value = value;
    }

    public string DescriptionAsText => GetFormatted(false);
    public string DescriptionAsHtml => GetFormatted(true);

    public string GetFormatted(bool needHtml)
    {
        if (Kind == FormatKind.Natural)
        {
            return !needHtml ? $"{Value:n0}" : HtmlNumberFormatter.Format(Value, 0);
        }
        else if (Kind == FormatKind.Numeric1)
        {
            return !needHtml ? $"{Value:n1}" : HtmlNumberFormatter.Format(Value, 1);
        }
        else if (Kind == FormatKind.Numeric2)
        {
            return !needHtml ? $"{Value:n2}" : HtmlNumberFormatter.Format(Value, 2);
        }
        else if (Kind == FormatKind.Timespan)
        {
            TimeSpan? ts = null;
            if (Value is TimeSpan t1) ts = t1;
            else if (Value is TimeSpan?) ts = (TimeSpan?) Value;
            var formatted = ts == null ? "" : needHtml ? ElapsedFormatter.FormatElapsedAsHtml(ts.Value) : ts.Value.ToString();
            return formatted;
        }
        else if (Kind == FormatKind.Pages || Kind == FormatKind.PagesPerSecond)
        {
            string suffix = Kind == FormatKind.PagesPerSecond ? "/s" : "";
            long pages = Convert.ToInt64(Value);
            string ret = !needHtml ? $"{pages:n0}" : HtmlNumberFormatter.Format(pages, 0, "");
            var kb = pages * 8192d / 1024;
            Func<string, string> toSmall = arg => $"&nbsp;<span class='Units'>{arg}</span>";
            var mbFormatted = needHtml ? $"{toSmall($"MB{suffix}")}" : $" MB{suffix}";
            var kbFormatted = needHtml ? $"{toSmall($"KB{suffix}")}" : $" KB{suffix}";
            Func<string, string> toNotImportant = arg => needHtml ? $"&nbsp;&nbsp;<span class='NotImportant'>{arg}</span>" : arg;
            if (pages > 2048) ret += toNotImportant($"  (is {kb / 1024:n0}{mbFormatted})");
            else if (pages > 512) ret += toNotImportant($"  (is {kb / 1024:n1}{mbFormatted})");
            else if (pages > 0) ret += toNotImportant($"  (is {kb:n0}{kbFormatted})");
            return ret;
        }
        else
        {
            return Convert.ToString(Value);
        }

    }

    public string GetSummaryHeaderRowHtml()
    {
        return
            ((string.IsNullOrEmpty(this.Title) ? "" : $"{this.Title} ")
             + this.DescriptionAsHtml).Trim();
    }


}