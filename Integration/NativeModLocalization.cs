using HarmonyLib;

namespace SephiriaEnhancements.Configuration
{
    // Keep game localization APIs at the integration boundary.
    internal static partial class ModLocalization
    {
        internal static void Register(HorayModLocalizationContext context)
        {
            Register((language, key, value) => context.AddText(language, key, value));
        }

        internal static bool RegisterCurrent()
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null || manager.Languages == null || manager.Languages.Count == 0)
            {
                return false;
            }

            Register(manager.AddModText);
            return true;
        }

        internal static string Get(string key)
        {
            LocalizationManager manager = LocalizationManager.Instance;
            if (manager == null || string.IsNullOrEmpty(manager.CurrentLanguage))
            {
                return GetEnglish(key);
            }

            return manager.GetText(manager.CurrentLanguage, key);
        }

    }

    // Initialize also calls LoadLanguage. Register before its notification so
    // existing native labels never refresh against a table without Mod text.
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.LoadLanguage))]
    internal static class ModLanguageLoadPatch
    {
        private static void Prefix(LocalizationManager __instance)
        {
            ModLocalization.Register(__instance.AddModText);
        }
    }
}
