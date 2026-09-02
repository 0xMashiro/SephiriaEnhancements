using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Integration
{
    internal static class CombatInsightsNotifications
    {
        internal static void ShowDisplayVisibility(bool hiddenByUser)
        {
            string key = hiddenByUser
                ? ModLocalization.DamageStatisticsDisplayHidden
                : ModLocalization.DamageStatisticsDisplayRestored;
            Show(key);
        }

        internal static void ShowReportVisibility(bool visible) =>
            Show(visible ? ModLocalization.EncounterReportOpened
                : ModLocalization.EncounterReportClosed);

        internal static void ShowReportUnavailable() =>
            Show(ModLocalization.EncounterReportUnavailable);

        private static void Show(string key)
        {
            UI_SystemMessage message =
                UIManager.Instance?.GetElement<UI_SystemMessage>();
            message?.Open(ModLocalization.Get(key), 2f);
        }
    }
}
