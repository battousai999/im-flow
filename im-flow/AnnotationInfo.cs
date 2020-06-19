using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace im_flow
{
    public class AnnotationInfo
    {
        public Regex Regex { get; private set; }
        public Func<Match, string> Projection { get; private set; }

        public AnnotationInfo(Regex regex, Func<Match, string> projection)
        {
            this.Regex = regex;
            this.Projection = projection;
        }

        public string GetAnnotation(string text)
        {
            var match = Regex.Match(text);

            if (!match.Success)
                return null;

            return Projection(match);
        }

        public string GetAnnotation(List<string> lines)
        {
            return GetAnnotation(String.Join(Environment.NewLine, lines));
        }
    }
}
