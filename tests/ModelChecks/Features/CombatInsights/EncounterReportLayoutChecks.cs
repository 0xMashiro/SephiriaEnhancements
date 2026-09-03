using SephiriaEnhancements.Combat;

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
                {
                    var layout = new EncounterReportLayout(count, finalBlows);
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
                            Require(width / canvasWidth < 0.5f && height / canvasHeight <= 0.56f,
                                "recommended report must stay within its screen-space budget");
                        }
                    }
                    Require(layout.OutcomesTop >= layout.DamageMixTop + 12f &&
                        layout.FinalBlowsTop >= layout.OutcomesTop + 25f &&
                        layout.DismissHintTop >= layout.FinalBlowsTop + (finalBlows ? 14f : 0f) &&
                        layout.Height >= layout.DismissHintTop + 16f,
                        "statistics and dismissal hint cannot overlap");
                }
        }
        var fullReport = new EncounterReportLayout(4, true);
        foreach (var canvas in new[] { (220f, 360f), (640f, 160f), (220f, 160f) })
        {
            float scale = EncounterReportLayout.FitScale(canvas.Item1, canvas.Item2,
                fullReport.Height, 1.2f);
            Require(EncounterReportLayout.Width * scale <= canvas.Item1 - 24f + 0.001f &&
                fullReport.Height * scale <= canvas.Item2 - 24f + 0.001f,
                "narrow and short canvases must constrain both dimensions");
        }
        Console.WriteLine("EncounterReportLayout: six resolutions, five scales, " +
            "one to four players, optional footer and small-canvas bounds passed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
