using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class EncounterReportLayoutChecks
{
    internal static void Run()
    {
        // Native HUD reference canvas: 640 x 360, Expand scaling.
        (int Width, int Height)[] resolutions =
        {
            (1280, 720), (1280, 800), (1920, 1080), (2560, 1440),
            (3440, 1440), (3840, 2160)
        };
        foreach (var resolution in resolutions)
        {
            float canvasScale = Math.Min(resolution.Width / 640f,
                resolution.Height / 360f);
            float canvasWidth = resolution.Width / canvasScale;
            float canvasHeight = resolution.Height / canvasScale;
            for (int count = 1; count <= 4; count++)
                foreach (bool finalBlows in new[] { false, true })
                    foreach (bool navigation in new[] { false, true })
                    {
                        var layout = new EncounterReportLayout(count, finalBlows, navigation);
                        float previousWidth = 0f;
                        foreach (float setting in new[] { 0.8f, 0.9f, 1f, 1.1f, 1.2f })
                        {
                            float scale = EncounterReportLayout.FitScale(canvasWidth,
                                canvasHeight, layout.Height, setting);
                            float width = EncounterReportLayout.Width * scale;
                            float height = layout.Height * scale;
                            Require(width > previousWidth, "scale steps must visibly change size");
                            Require(width + 24f <= canvasWidth + 0.001f &&
                                height + 24f <= canvasHeight + 0.001f,
                                "all report content must fit with margins");
                            previousWidth = width;
                            float browserScale = EncounterReportLayout.FitBrowserScale(
                                canvasWidth, canvasHeight, layout.Height, setting);
                            Require((EncounterReportLayout.Width + 24f) * browserScale <= canvasWidth - 24f + 0.001f &&
                                (layout.Height + 76f) * browserScale <= canvasHeight - 24f + 0.001f,
                                "browser tabs, report and close button fit together at every scale");
                            if (setting == 1f)
                            {
                                Require(scale == 1f, "100% uses the recommended layout directly");
                                Require(width / canvasWidth < 0.5f && height / canvasHeight <= (navigation ? 0.61f : 0.56f),
                                    "recommended report must stay within its screen-space budget");
                            }
                        }
                        Require(layout.OutcomesTop >= layout.DamageMixTop + 12f &&
                            layout.FinalBlowsTop >= layout.OutcomesTop + 25f &&
                            layout.NavigationTop >= layout.FinalBlowsTop + (finalBlows ? 14f : 0f) &&
                            layout.DismissHintTop >= layout.NavigationTop + (navigation ? 18f : 0f) &&
                            layout.Height >= layout.DismissHintTop + 16f,
                            "statistics and dismissal hint cannot overlap");
                    }
        }
        var fullReport = new EncounterReportLayout(6, true, true);
        foreach (var canvas in new[] { (220f, 360f), (640f, 160f), (220f, 160f) })
        {
            float scale = EncounterReportLayout.FitScale(canvas.Item1, canvas.Item2,
                fullReport.Height, 1.2f);
            Require(EncounterReportLayout.Width * scale <= canvas.Item1 - 24f + 0.001f &&
                fullReport.Height * scale <= canvas.Item2 - 24f + 0.001f,
                "narrow and short canvases must constrain both dimensions");
        }
        for (int encounterCount = 1; encounterCount <= 4; encounterCount++)
            for (int floorCount = encounterCount; floorCount <= 6; floorCount++)
                foreach (bool encounterFinalBlows in new[] { false, true })
                    foreach (bool floorFinalBlows in new[] { false, true })
                    {
                        var encounter = Snapshot(encounterCount, encounterFinalBlows);
                        var floor = Snapshot(floorCount, floorFinalBlows);
                        var shared = new EncounterReportLayout(
                            Math.Max(encounter.Players.Count, floor.Players.Count),
                            encounter.LocalFinalBlows > 0 || floor.LocalFinalBlows > 0, true);
                        var expected = new EncounterReportLayout(floorCount,
                            encounterFinalBlows || floorFinalBlows, true);
                        Require(shared.Height == expected.Height &&
                            shared.DamageMixTop == expected.DamageMixTop &&
                            shared.NavigationTop == expected.NavigationTop &&
                            shared.DismissHintTop == expected.DismissHintTop,
                            "both snapshots reserve the larger row count and any final-blow footer");
                    }
        Console.WriteLine("EncounterReportLayout: six resolutions, five scales, " +
            "shared page geometry, optional footer and small-canvas bounds passed");
    }

    private static CombatStatisticsSnapshot Snapshot(int count, bool finalBlows) => new(
        Enumerable.Range(0, count).Select(index => new CombatStatisticsPlayerSnapshot(
            index, "Player " + index, index == 0, 100f)).ToArray(),
        10f, 1, 0, 0, finalBlows ? 1 : 0, Array.Empty<CombatStatisticsDamageTypeSnapshot>());

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
