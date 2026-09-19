using System;

namespace SephiriaEnhancements.Integration
{
    internal static class NumberInputEditing
    {
        internal static string Apply(string text, string key, int limit,
            Func<string, int, char, char> validate)
        {
            if (key == "⌫") return text.Length == 0 ? text : text.Substring(0, text.Length - 1);
            if (key == "±")
            {
                if (text.StartsWith("-", StringComparison.Ordinal)) return text.Substring(1);
                return text.Length < limit && validate(text, 0, '-') == '-' ? "-" + text : text;
            }
            if (text.Length >= limit) return text;
            char character = validate(text, text.Length, key[0]);
            return character == '\0' ? text : text + character;
        }
    }
}
