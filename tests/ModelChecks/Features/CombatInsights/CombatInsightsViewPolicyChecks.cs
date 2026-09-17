using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class CombatInsightsViewPolicyChecks
{
    internal static void Run()
    {
        Require(Select(CombatInsightsDisplayPolicy.Disabled, boss: true, report: true).Mode == CombatInsightsViewMode.Hidden,
            "disabled statistics take precedence over boss and report views");
        Require(Select(boss: true, report: true).Mode == CombatInsightsViewMode.Boss, "boss precedes report");
        Require(Select(CombatInsightsDisplayPolicy.BossOnly, report: true).Mode == CombatInsightsViewMode.Report,
            "an already-open report precedes the ordinary-combat filter");
        Require(Select(CombatInsightsDisplayPolicy.BossOnly).Reason == CombatInsightsVisibilityReason.BossOnlyOutsideBoss,
            "boss-only hides ordinary combat with its specific reason");
        Require(Select(waiting: true, elapsed: 10f).Reason == CombatInsightsVisibilityReason.SmartAwaitingContribution,
            "elapsed time alone cannot reveal smart combat statistics");
        Require(Select(elapsed: 1.499f).Reason == CombatInsightsVisibilityReason.SmartInitialDelay &&
            Select(elapsed: 1.5f).Mode == CombatInsightsViewMode.Pulse, "smart delay ends at exactly 1.5 seconds");
        Require(Select(major: true, elapsed: 0f).Mode == CombatInsightsViewMode.Pulse &&
            Select(major: true, elapsed: 0f, solo: false).Mode == CombatInsightsViewMode.Party,
            "major encounters skip delay and expand only for a party");
        Require(Select(solo: false, elapsed: 5.999f).Mode == CombatInsightsViewMode.Pulse &&
            Select(solo: false, elapsed: 6f).Mode == CombatInsightsViewMode.Party, "ordinary party view expands at six seconds");
        Require(Select(CombatInsightsDisplayPolicy.AllCombat, waiting: true, elapsed: 0f).Mode == CombatInsightsViewMode.Pulse,
            "all-combat mode has no contribution or initial-delay gate");
        Require(Select(active: false).Reason == CombatInsightsVisibilityReason.NoActiveCombatOrReport,
            "inactive ordinary encounters do not receive smart-delay reasons");

        var window = new ReportDisplayWindow();
        var view = Select();
        CombatInsightsVisibilityReason Reason(bool enabled = true, bool local = true, bool menu = false,
            bool hidden = false, bool attached = true, bool hudActive = true,
            bool hasReport = false, ReportPresentationBlock block = ReportPresentationBlock.None) =>
            CombatInsightsViewPolicy.VisibilityReason(view, window, hasReport, block, enabled, local,
                menu, hidden, attached, hudActive);
        Require(Reason(enabled: false, local: false, menu: true) == CombatInsightsVisibilityReason.StatisticsDisabled,
            "suite disable precedes local identity and menus");
        Require(Reason(local: false, menu: true) == CombatInsightsVisibilityReason.LocalPlayerUnavailable,
            "missing local player precedes native controls");
        Require(Reason(menu: true, hidden: true) == CombatInsightsVisibilityReason.NativeControlOpen,
            "native menu ownership precedes user hide");
        Require(Reason(hidden: true, attached: false) == CombatInsightsVisibilityReason.HiddenByUser,
            "user hide precedes HUD attachment");
        Require(Reason(attached: false, hudActive: false) == CombatInsightsVisibilityReason.HudUnavailable &&
            Reason(hudActive: false) == CombatInsightsVisibilityReason.HudSuppressedByHierarchy,
            "missing HUD and hidden native hierarchy remain distinct");

        window.Start(10f, 6f);
        window.SetPresentationAvailable(false, 12f);
        view = Select(active: false, report: window.IsVisible(20f));
        Require(Reason(hasReport: true, block: ReportPresentationBlock.Loading) == CombatInsightsVisibilityReason.PresentationBlocked,
            "paused report identifies the native presentation block");
        Require(Reason(hasReport: true) == CombatInsightsVisibilityReason.ReportDeferred,
            "paused report without a native blocker remains deferred");
        view = Select(waiting: true);
        Require(Reason(hasReport: true) == CombatInsightsVisibilityReason.SmartAwaitingContribution,
            "smart contribution gate precedes deferred-report fallback");
        window.SetPresentationAvailable(true, 20f);
        view = Select(active: false, report: window.IsVisible(24f));
        Require(view.Mode == CombatInsightsViewMode.Report && Reason(hasReport: true) == CombatInsightsVisibilityReason.Visible,
            "resumed report keeps its unspent display time including the deadline");
        view = Select(active: false, report: window.IsVisible(24.01f));
        Require(Reason(hasReport: true) == CombatInsightsVisibilityReason.ReportExpired, "expired report is not deferred");
        window.Clear(ReportDisplayState.Dismissed);
        Require(Reason(hasReport: true) == CombatInsightsVisibilityReason.NoActiveCombatOrReport,
            "dismissal is not reported as expiration even while its snapshot remains");
        Require(!window.HasStarted && !window.IsPaused, "reading display decisions must not restart or pause a report");
        Console.WriteLine("CombatInsightsViewPolicy: view precedence, timing boundaries, visibility reasons and report lifecycle passed");
    }

    private static (CombatInsightsViewMode Mode, CombatInsightsVisibilityReason Reason) Select(
        CombatInsightsDisplayPolicy policy = CombatInsightsDisplayPolicy.Smart, bool boss = false, bool report = false,
        bool active = true, bool waiting = false, bool major = false, float elapsed = 2f, bool solo = true) =>
        CombatInsightsViewPolicy.Select(policy, boss, report, active, waiting, major, elapsed, solo);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
