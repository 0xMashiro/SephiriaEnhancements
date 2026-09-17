using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.Combat
{
    internal enum CombatInsightsViewMode { Hidden, Pulse, Party, Boss, Report }
    internal enum CombatInsightsVisibilityReason
    {
        Visible,
        StatisticsDisabled,
        LocalPlayerUnavailable,
        NativeControlOpen,
        PresentationBlocked,
        HiddenByUser,
        HudUnavailable,
        HudSuppressedByHierarchy,
        BossOnlyOutsideBoss,
        SmartAwaitingContribution,
        SmartInitialDelay,
        ReportDeferred,
        ReportExpired,
        NoActiveCombatOrReport,
        RuntimeIncompatible,
        ControllerDisabled
    }

    internal static class CombatInsightsViewPolicy
    {
        private const float SmartPulseDelaySeconds = 1.5f;
        private const float PartyExpansionSeconds = 6f;

        internal static (CombatInsightsViewMode Mode, CombatInsightsVisibilityReason Reason) Select(
            CombatInsightsDisplayPolicy policy, bool bossActive, bool reportVisible,
            bool encounterActive, bool awaitingContribution, bool majorEncounter, float elapsed, bool solo)
        {
            if (policy == CombatInsightsDisplayPolicy.Disabled)
                return (CombatInsightsViewMode.Hidden, CombatInsightsVisibilityReason.StatisticsDisabled);
            if (bossActive) return (CombatInsightsViewMode.Boss, CombatInsightsVisibilityReason.Visible);
            if (reportVisible) return (CombatInsightsViewMode.Report, CombatInsightsVisibilityReason.Visible);
            if (policy == CombatInsightsDisplayPolicy.BossOnly)
                return (CombatInsightsViewMode.Hidden, CombatInsightsVisibilityReason.BossOnlyOutsideBoss);
            if (!encounterActive)
                return (CombatInsightsViewMode.Hidden, CombatInsightsVisibilityReason.NoActiveCombatOrReport);
            if (policy == CombatInsightsDisplayPolicy.Smart && awaitingContribution)
                return (CombatInsightsViewMode.Hidden, CombatInsightsVisibilityReason.SmartAwaitingContribution);
            if (policy == CombatInsightsDisplayPolicy.Smart && !majorEncounter && elapsed < SmartPulseDelaySeconds)
                return (CombatInsightsViewMode.Hidden, CombatInsightsVisibilityReason.SmartInitialDelay);
            return (!solo && (majorEncounter || elapsed >= PartyExpansionSeconds)
                ? CombatInsightsViewMode.Party : CombatInsightsViewMode.Pulse, CombatInsightsVisibilityReason.Visible);
        }

        internal static CombatInsightsVisibilityReason VisibilityReason(
            (CombatInsightsViewMode Mode, CombatInsightsVisibilityReason Reason) selection,
            ReportDisplayWindow reportWindow, bool hasReport, ReportPresentationBlock block,
            bool statisticsEnabled, bool localPlayerReady, bool menuOpen, bool hiddenByUser,
            bool hudAttached, bool hudActive)
        {
            if (!statisticsEnabled) return CombatInsightsVisibilityReason.StatisticsDisabled;
            if (!localPlayerReady) return CombatInsightsVisibilityReason.LocalPlayerUnavailable;
            if (menuOpen) return CombatInsightsVisibilityReason.NativeControlOpen;
            if (hiddenByUser) return CombatInsightsVisibilityReason.HiddenByUser;
            if (hasReport && reportWindow.IsPaused && block != ReportPresentationBlock.None)
                return CombatInsightsVisibilityReason.PresentationBlocked;
            if (selection.Mode != CombatInsightsViewMode.Hidden)
            {
                if (!hudAttached) return CombatInsightsVisibilityReason.HudUnavailable;
                if (!hudActive) return CombatInsightsVisibilityReason.HudSuppressedByHierarchy;
                return CombatInsightsVisibilityReason.Visible;
            }
            if (selection.Reason != CombatInsightsVisibilityReason.NoActiveCombatOrReport)
                return selection.Reason;
            if (hasReport && reportWindow.IsPaused) return CombatInsightsVisibilityReason.ReportDeferred;
            if (reportWindow.HasStarted) return CombatInsightsVisibilityReason.ReportExpired;
            return CombatInsightsVisibilityReason.NoActiveCombatOrReport;
        }
    }
}
