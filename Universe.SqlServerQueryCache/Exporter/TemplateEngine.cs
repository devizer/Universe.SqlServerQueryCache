using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlServerQueryCache.Exporter
{
    public class TemplateEngine
    {
        private List<Substitution> Substitutions = new List<Substitution>();
        public TemplateEngine Substitute(string placeholder, Action<TextWriter> writeImplementation)
        {
            Substitutions.Add(new Substitution() { Placeholder = placeholder, WriteImplementation = writeImplementation});
            return this;
        }

        public void Produce(TextWriter output, string template)
        {
            int pos = 0;

            void TryWriteText(int next)
            {
                int len = next - pos - 1;
                if (len > 0 && pos + len < template.Length)
                    output.Write(template.Substring(pos, len));
            }

            void TryWritePlaceholder(string placeholder)
            {
                var subst = Substitutions.FirstOrDefault(x => x.Placeholder.Equals(placeholder.Trim(), StringComparison.InvariantCultureIgnoreCase));
                if (subst == null)
                    throw new ArgumentException($"Unknown template's placeholder '{placeholder}'", nameof(template));

                subst.WriteImplementation(output);
            }

            while (pos < template.Length)
            {
                int nextStart = template.IndexOf("{{", pos);
                TryWriteText(nextStart);
                if (nextStart < 0) return;

                // TODO: Validate '.IndexOf' arguments (if invalid template)
                int nextEnd = template.IndexOf("}}", nextStart + 2);

                // TODO: Validate '.Substring' arguments (if invalid template)
                string nextPlaceholder = template.Substring(nextStart + 2, nextEnd - nextStart - 3);
                pos = nextEnd + 2;

                TryWritePlaceholder(nextPlaceholder);
            }

        }

        public class Substitution
        {
            public string Placeholder;
            public Action<TextWriter> WriteImplementation;
        }



    }
}
