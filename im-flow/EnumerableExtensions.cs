using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace im_flow
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> ToSingleton<T>(this T item)
        {
            yield return item;
        }

        public static string RemoveTrailingData(this string text)
        {
            if (text == null)
                return null;

            if (text.EndsWith("data", StringComparison.OrdinalIgnoreCase))
                return text.Remove(text.Length - 4);
            else
                return text;
        }

        public static string FormatPhoneNumber(this string text)
        {
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;

            if (text.All(x => Char.IsDigit(x)) && text.Length == 10)
                return $"{text.Substring(0, 3)}-{text.Substring(3, 3)}-{text.Substring(6)}";
            else
                return text;
        }

        public static string FormatWithHeader(this string text, string header)
        {
            if (String.IsNullOrWhiteSpace(text))
                return String.Empty;
            else
                return $"{header}{text}";
        }

        public static bool ContainsWildcards(this string text)
        {
            return (text.Contains("*") || text.Contains("?"));
        }
    }
}
