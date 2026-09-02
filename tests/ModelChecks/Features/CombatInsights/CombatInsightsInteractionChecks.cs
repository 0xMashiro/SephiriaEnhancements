using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Integration;
using System.Text.Json;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class CombatInsightsInteractionChecks
{
    internal static void Run()
    {
        var shortcut = new CombatInsightsShortcut();
        Check(shortcut.Update(true, true, true, false, 0f),
            CombatInsightsShortcutAction.None, "press waits for tap or hold");
        Check(shortcut.Update(true, false, false, true, 0.2f),
            CombatInsightsShortcutAction.ToggleReport, "tap recalls the report");
        Check(shortcut.Update(true, false, false, false, 0.3f),
            CombatInsightsShortcutAction.None, "tap fires once");

        shortcut.Update(true, true, true, false, 1f);
        Check(shortcut.Update(true, false, true, false, 1.49f),
            CombatInsightsShortcutAction.None, "hold threshold not reached");
        Check(shortcut.Update(true, false, true, false, 1.5f),
            CombatInsightsShortcutAction.ToggleDisplay, "hold changes visibility");
        Check(shortcut.Update(true, false, true, false, 4f),
            CombatInsightsShortcutAction.None, "continued hold cannot repeat");
        Check(shortcut.Update(true, false, false, true, 4.1f),
            CombatInsightsShortcutAction.None, "hold release cannot recall report");

        shortcut.Update(true, true, true, false, 5f);
        shortcut.Update(false, false, true, false, 5.1f);
        Check(shortcut.Update(true, false, false, true, 5.2f),
            CombatInsightsShortcutAction.None, "menu or modifier cancels gesture");
        shortcut.Update(true, true, true, false, 6f);
        shortcut.Reset();
        Check(shortcut.Update(true, false, false, true, 6.1f),
            CombatInsightsShortcutAction.None, "context reset cancels gesture");
        shortcut.Update(true, true, true, false, 7f);
        Check(shortcut.Update(true, false, false, true, 7.6f),
            CombatInsightsShortcutAction.ToggleDisplay, "slow frame preserves long press");
        Check(shortcut.Update(true, true, false, true, 8f),
            CombatInsightsShortcutAction.ToggleReport, "same-frame tap is recognized");

        var report = new ReportDisplayWindow();
        report.Start(10f, 6f);
        for (float now = 10f; now <= 16f; now += 0.25f)
        {
            // Movement and attacks without a new damage/defeat contribution
            // may change the candidate area; they must not dismiss the report.
            report.CloseForEncounter(false, true, false);
            report.SetPresentationAvailable(true, now);
            Require(report.IsVisible(now), "ordinary input preserves automatic duration");
        }
        Require(!report.IsOpen(16.1f), "automatic report still expires normally");
        Require(report.State(16.1f) == ReportDisplayState.Expired,
            "timeout is distinct from explicit dismissal");
        report.OpenUntilDismissed();
        Require(report.IsVisible(1000f), "expired report can reopen without a timeout");
        report.SetPresentationAvailable(false, 1001f);
        Require(report.IsOpen(2000f) && !report.IsVisible(2000f),
            "menu pauses a manually opened report");
        report.SetPresentationAvailable(true, 2001f);
        Require(report.IsVisible(3000f), "manual report resumes after menu");
        report.Clear(ReportDisplayState.Dismissed);
        Require(!report.IsOpen(3000f), "explicit dismissal closes report");
        Require(report.State(3000f) == ReportDisplayState.Dismissed,
            "explicit dismissal has its own diagnostic state");
        report.OpenUntilDismissed();
        report.CloseForEncounter(false, false, true);
        Require(report.IsVisible(3001f), "completed encounter totals do not dismiss report");
        report.CloseForEncounter(false, true, true);
        Require(!report.IsOpen(3001f), "new ordinary contribution closes old report");
        Require(report.State(3001f) == ReportDisplayState.CombatStarted,
            "next encounter is distinct from dismissal and timeout");
        report.OpenUntilDismissed();
        report.CloseForEncounter(true, false, false);
        Require(!report.IsOpen(3002f), "boss start closes old report immediately");
        report.Start(4000f, 6f);
        Require(!report.IsOpen(4007f), "new automatic report replaces manual lifetime");

        report.Start(5000f, 6f);
        report.SetPresentationAvailable(false, 5000f);
        Require(!report.IsVisible(5000f) && report.IsOpen(5000f),
            "a newly published report can be deferred in the same frame");
        Require(report.State(5000f) == ReportDisplayState.Paused,
            "presentation blocking pauses rather than dismisses the report");
        report.SetPresentationAvailable(true, 5020f);
        Require(report.IsVisible(5025.9f) && !report.IsOpen(5026.1f),
            "a report deferred at publication keeps its full reading duration");
        report.SetPresentationAvailable(false, 5027f);
        report.SetPresentationAvailable(true, 5030f);
        Require(report.State(5030f) == ReportDisplayState.Expired,
            "later blocking does not reopen an already expired report");
        report.Clear();
        Require(report.State(10000f) == ReportDisplayState.Closed,
            "context reset clears the previous display reason");

        using JsonDocument document = JsonDocument.Parse(ModShortcuts.ActionMapJson);
        JsonElement binding = document.RootElement.GetProperty("maps")[0]
            .GetProperty("bindings").EnumerateArray().Single(item =>
                item.GetProperty("action").GetString() == ModShortcuts.ToggleDamageStatistics &&
                item.GetProperty("groups").GetString() == ModShortcuts.KeyboardScheme &&
                item.GetProperty("path").GetString() != string.Empty);
        Require(binding.GetProperty("path").GetString() == "<Keyboard>/f7",
            "statistics uses its dedicated default key");
        Console.WriteLine("CombatInsightsInteraction: tap/hold, report recall, input " +
            "persistence, combat transitions and default binding passed");
    }

    private static void Check(CombatInsightsShortcutAction actual,
        CombatInsightsShortcutAction expected, string label) =>
        Require(actual == expected, label);

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
}
