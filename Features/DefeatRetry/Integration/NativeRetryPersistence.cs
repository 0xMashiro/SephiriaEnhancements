using System;
using System.IO;
using System.Linq;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryPersistence
    {
        internal static bool Matches(SaveData expected)
        {
            if (expected == null || !expected.enableSave || string.IsNullOrEmpty(expected.BindedPath)) return false;
            // Read the primary file directly: native Load may silently fall back
            // to a backup, which cannot establish that this restore was saved.
            var actual = new SaveData(expected.useEncryption, expected.extension, 0);
            try
            {
                if (!actual.LoadFromString(File.ReadAllText(expected.BindedPath))) return false;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            var values = actual.BakedData.ToDictionary(pair => pair.Key, pair => pair.Value);
            return values.Count == expected.BakedData.Count() && expected.BakedData.All(pair =>
                values.TryGetValue(pair.Key, out object value) && Equals(value, pair.Value));
        }
    }
}
