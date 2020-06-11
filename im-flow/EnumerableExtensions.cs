using System;
using System.Collections.Generic;
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
    }
}
