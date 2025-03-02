using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Exporter
{
    public class ExporterResources
    {
        public static string HtmlTemplate => ReadResource("HtmlTemplate.html");
        public static string MainJS => ReadResource("Main.js");
        public static string StyleCSS => ReadResource("Style.css");
        public static string SqlSyntaxHighlighterCss => ReadResource("SqlSyntaxHighlighter.css");
        public static string FloatButtonCss => ReadResource("FloatButton.css");
        public static string FlexedListCss => ReadResource("Flexed-List.css");

        // Modal-Summary
        public static string ModalSummaryCss => ReadResource("Modal-Summary.css");
        public static string ModalSummaryJS => ReadResource("Modal-Summary.js");





        static string ReadResource(string name)
        {
            var resourceName = typeof(ExporterResources).Namespace + "." + name;
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null) throw new ArgumentException($"Embedded resource '{resourceName}' not found", name);
            using (StreamReader rdr = new StreamReader(stream, Encoding.UTF8))
            {
                return rdr.ReadToEnd();
            }
        }
    }
}
