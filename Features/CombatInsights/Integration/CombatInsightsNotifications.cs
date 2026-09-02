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
                    return ModLocalization.EncounterReportLoading;
                case ReportPresentationBlock.ScreenTransition:
                    return ModLocalization.EncounterReportScreenTransition;
                case ReportPresentationBlock.Cutscene:
                    return ModLocalization.EncounterReportCutscene;
                case ReportPresentationBlock.Menu:
                    return ModLocalization.EncounterReportMenu;
                default:
                    return null;
            }
        }

        internal static void Show(string key)
        {
            if (key == null) return;
            UI_SystemMessage message =
                UIManager.Instance?.GetElement<UI_SystemMessage>();
            message?.Open(ModLocalization.Get(key), 2f);
        }
    }
}
