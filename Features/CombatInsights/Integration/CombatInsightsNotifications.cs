using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.Integration
{
    internal static class CombatInsightsNotifications
    {
        internal static string BlockedMessage(ReportPresentationBlock block)
        {
            switch (block)
            {
                case ReportPresentationBlock.Loading:
                    return CombatInsightsLocalization.EncounterReportLoading;
                case ReportPresentationBlock.ScreenTransition:
                    return CombatInsightsLocalization.EncounterReportScreenTransition;
                case ReportPresentationBlock.Cutscene:
                    return CombatInsightsLocalization.EncounterReportCutscene;
                case ReportPresentationBlock.Menu:
                    return CombatInsightsLocalization.EncounterReportMenu;
                default:
                    return null;
            }
        }

        internal static void Show(string key)
        {
            if (key == null) return;
            NativeModNotifications.Short(key);
        }
    }
}
