using System;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    // This namespace in the owner's native profile contains choices, never earned progress.
    // Write the default value to clear a choice; keys are not deleted independently.
    internal static class NativeProfilePreferences
    {
        private const string Prefix = "SephiriaEnhancements.ProfilePreferences.";

        internal static string GetString(string name, string fallback = "") =>
            SaveManager.Current?.GetString(Prefix + name, fallback) ?? fallback;

        internal static void SetString(string name, string value) =>
            SaveManager.Current.SetString(Prefix + name, value);

        internal static void Preserve(SaveData latest, SaveData restored)
        {
            foreach (var field in latest.BakedData)
                if (field.Key.StartsWith(Prefix, StringComparison.Ordinal))
                    restored.SetString(field.Key, (string)field.Value);
        }
    }
}
