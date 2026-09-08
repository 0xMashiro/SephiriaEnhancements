using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Configuration
{
    internal static partial class ModLocalization
    {
        private static readonly Lazy<Dictionary<string, string>> EnglishFallback =
            new Lazy<Dictionary<string, string>>(() =>
            {
                var texts = new Dictionary<string, string>(StringComparer.Ordinal);
                Register((language, key, value) =>
                {
                    if (language == LocalizationGroup.FallbackLanguage) texts.Add(key, value);
                });
                return texts;
            });

        internal static string GetEnglish(string key) =>
            EnglishFallback.Value.TryGetValue(key, out var text) ? text : key;
    }
}
