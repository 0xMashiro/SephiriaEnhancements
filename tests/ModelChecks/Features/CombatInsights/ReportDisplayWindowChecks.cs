using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class ReportDisplayWindowChecks
{
    internal static void Run()
    {
        var reportWindow = new ReportDisplayWindow();
        reportWindow.Start(10f, 6f);
        reportWindow.SetPresentationAvailable(available: false, 12f);
        if (!reportWindow.IsOpen(30f) || reportWindow.IsVisible(30f))
            throw new InvalidOperationException("unavailable UI must pause a pending report");
        reportWindow.SetPresentationAvailable(available: true, 30f);
        if (!reportWindow.IsOpen(34f) || !reportWindow.IsVisible(34f) ||
            reportWindow.IsOpen(34.01f))
            throw new InvalidOperationException(
                "report must retain its unshown display duration");
        reportWindow.Start(40f, 6f);
        reportWindow.SetPresentationAvailable(available: false, 41f);
        reportWindow.Clear();
        reportWindow.SetPresentationAvailable(available: true, 50f);
        if (reportWindow.IsOpen(50f))
            throw new InvalidOperationException(
                "clearing a paused report must not revive it");
        foreach (float duration in new[] { 4.5f, 6f, 8f, 10f })
        {
            reportWindow.Start(100f, duration);
            Require(!reportWindow.ShowFloorStatistics, "each report starts on this battle");
            Require(reportWindow.TrySelectPage(true, 104f) && reportWindow.ShowFloorStatistics,
                "next tab selects current floor");
            float floorDuration = Math.Max(8f, duration);
            Require(reportWindow.IsVisible(104f + floorDuration) && !reportWindow.IsOpen(104.01f + floorDuration),
                "floor totals receive at least eight seconds without shortening longer reports");
            Require(!reportWindow.TrySelectPage(true, 105f), "same page does not restart the timer");
            Require(!reportWindow.IsOpen(104.01f + floorDuration), "reselecting floor cannot extend its deadline");
            reportWindow.SetPresentationAvailable(false, 105f);
            Require(!reportWindow.TrySelectPage(false, 106f) && reportWindow.ShowFloorStatistics,
                "a hidden report cannot change pages");
            reportWindow.SetPresentationAvailable(true, 110f);
            Require(reportWindow.IsVisible(109f + floorDuration) && !reportWindow.IsOpen(109.01f + floorDuration),
                "menu pause preserves the longer floor reading time");
            Require(reportWindow.TrySelectPage(false, 110f) && !reportWindow.ShowFloorStatistics,
                "previous tab returns to this battle after menu closes");
            Require(!reportWindow.TrySelectPage(true, 111f + duration),
                "expired reports cannot reopen through navigation");
            Require(reportWindow.IsVisible(110f + duration) && !reportWindow.IsOpen(110.01f + duration),
                "returning to this battle restores its own reading duration");
            reportWindow.Start(200f, duration);
            reportWindow.TrySelectPage(true, 201f);
            reportWindow.Start(202f, duration);
            Require(!reportWindow.ShowFloorStatistics, "new reports forget the previous page");
            reportWindow.TrySelectPage(true, 203f);
            reportWindow.CloseForEncounter(true, false, false);
            Require(!reportWindow.ShowFloorStatistics && !reportWindow.TrySelectPage(true, 204f),
                "new combat closes and resets a floor report");
            reportWindow.Start(300f, duration);
            reportWindow.TrySelectPage(true, 301f);
            reportWindow.TryDismiss(302f);
            Require(!reportWindow.ShowFloorStatistics && !reportWindow.TrySelectPage(true, 303f),
                "dismissal clears navigation without reopening");
        }
        Console.WriteLine(
            "ReportDisplayWindow: navigation, reading time, pause, resume and reset checks passed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
