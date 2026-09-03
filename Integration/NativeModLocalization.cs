using System;
using System.Collections.Generic;

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
                if (Texts["en-US"].TryGetValue(key, out string primary))
                {
                    return primary;
                }

                if (key == RetryFloor)
                {
                    return RetryFloorTexts["en-US"];
                }

                if (key == RetryBossEncounter)
                {
                    return RetryBossEncounterTexts["en-US"];
                }

                return AdditionalTexts["en-US"].TryGetValue(key, out string additional)
                    ? additional
                    : HelpTexts["en-US"].TryGetValue(key, out string help) ? help
                    : HitStreakFeedbackTexts["en-US"].TryGetValue(key, out string feedback)
                        ? feedback
                    : OutlineTexts["en-US"].TryGetValue(key, out string outline)
                        ? outline
                    : SuiteTexts["en-US"].TryGetValue(key, out string suite)
                        ? suite
                    : InsightsTexts["en-US"].TryGetValue(key, out string insights)
                        ? insights
                    : DeveloperConsoleTexts["en-US"].TryGetValue(key,
                        out string developerConsole) ? developerConsole
                    : DeveloperPlayerDamageTexts["en-US"].TryGetValue(key,
                        out string developerPlayerDamage) ? developerPlayerDamage
                    : DefeatRetryTexts["en-US"].TryGetValue(key,
                        out string defeatRetry) ? defeatRetry : key;
            }

            return manager.GetText(manager.CurrentLanguage, key);
        }

    }
}
