using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace im_flow
{
    public class AnnotationInfo
    {
        public List<Regex> Regexes { get; private set; }
        public Func<List<string>, string> Projection { get; private set; }

        public AnnotationInfo(Func<List<string>, string> projection, params Regex[] regexes)
        {
            this.Regexes = regexes.ToList();
            this.Projection = projection;
        }

        public string GetAnnotation(string text)
        {
            var matches = Regexes.Select(x => x.Match(text)).ToList();

            if (!matches.Any(x => x.Success))
                return null;

            var values = matches.Select(x => x.Success ? x.Groups[1].Value : null).ToList();

            return Projection(values);
        }

        public string GetAnnotation(List<string> lines)
        {
            return GetAnnotation(String.Join(Environment.NewLine, lines));
        }
    }
}
