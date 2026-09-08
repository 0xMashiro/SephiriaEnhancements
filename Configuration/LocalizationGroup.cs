using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Configuration
{
    // A language is selected for the whole group, never for individual keys.
    internal static class LocalizationGroup
    {
        internal const string FallbackLanguage = "en-US";

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages, string key, Dictionary<string, string> translations)
        {
            string english = translations[FallbackLanguage];
            if (string.IsNullOrWhiteSpace(english))
                throw new InvalidOperationException("Invalid English localization group: " + key);
            foreach (string language in languages)
                addText(language, key, translations.TryGetValue(language, out var value) &&
                    !string.IsNullOrWhiteSpace(value) ? value : english);
        }

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages, string[] keys, Dictionary<string, string[]> translations)
        {
            string[] english = translations[FallbackLanguage];
            if (english.Length != keys.Length || !Complete(english) || keys.Distinct().Count() != keys.Length)
                throw new InvalidOperationException("Invalid English localization group: " + keys.FirstOrDefault());
            foreach (string language in languages)
            {
                string[] values = translations.TryGetValue(language, out var translated) &&
                    translated.Length == keys.Length && Complete(translated) ? translated : english;
                for (int index = 0; index < keys.Length; index++)
                    addText(language, keys[index], values[index]);
            }
        }

        internal static void Register(Action<string, string, string> addText,
            IEnumerable<string> languages, Dictionary<string, Dictionary<string, string>> translations)
        {
            Dictionary<string, string> english = translations[FallbackLanguage];
            if (!Complete(english.Values))
                throw new InvalidOperationException("Invalid English localization group: " + english.Keys.FirstOrDefault());
            foreach (string language in languages)
            {
                Dictionary<string, string> values = translations.TryGetValue(language, out var translated) &&
                    translated.Count == english.Count && english.Keys.All(translated.ContainsKey) &&
                    Complete(translated.Values) ? translated : english;
                foreach (var text in values) addText(language, text.Key, text.Value);
            }
        }

        private static bool Complete(IEnumerable<string> values) =>
            values.All(value => !string.IsNullOrWhiteSpace(value));
    }
}
