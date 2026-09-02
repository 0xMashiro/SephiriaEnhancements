using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.Integration
{
    internal static class NativeReportPresentation
    {
        internal static ReportPresentationBlock ReadBlock(UIManager manager,
            PlayerAvatar player)
        {
            if (player != null && player.loadingScreenType != -1)
                return ReportPresentationBlock.Loading;
            if (ScreenFader.Instance?.IsFading == true)
                return ReportPresentationBlock.ScreenTransition;
            if (CutScenePlayer.Current != null)
                return ReportPresentationBlock.Cutscene;
            if (manager?.CurrentControlStack != null)
                return ReportPresentationBlock.Menu;
            // Passive HUD reminders and brief flashes do not own interaction.
            return ReportPresentationBlock.None;
        }
    }
}
