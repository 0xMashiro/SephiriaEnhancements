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
        Console.WriteLine(
            "ReportDisplayWindow: interaction pause, resume and reset checks passed");
    }
}
