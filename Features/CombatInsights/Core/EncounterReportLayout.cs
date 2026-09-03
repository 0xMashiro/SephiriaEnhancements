using System;

namespace SephiriaEnhancements.Combat
{
    internal readonly struct EncounterReportLayout
    {
        internal const float Width = 304f;
        internal const float RowsTop = 36f;
        internal const float RowHeight = 20f;
        internal const float Margin = 12f;

        internal EncounterReportLayout(int playerCount, bool showFinalBlows)
        {
            DamageMixTop = RowsTop + playerCount * RowHeight + 3f;
            OutcomesTop = DamageMixTop + 15f;
            FinalBlowsTop = OutcomesTop + 28f;
            DismissHintTop = FinalBlowsTop + (showFinalBlows ? 14f : 0f);
            Height = DismissHintTop + 22f;
        }

        internal float DamageMixTop { get; }
        internal float OutcomesTop { get; }
        internal float FinalBlowsTop { get; }
        internal float DismissHintTop { get; }
        internal float Height { get; }

        internal static float FitScale(float canvasWidth, float canvasHeight,
            float height, float requestedScale) => Math.Max(0f,
                Math.Min(requestedScale, Math.Min(
                    (canvasWidth - Margin * 2f) / Width,
                    (canvasHeight - Margin * 2f) / height)));

        internal static float FitBrowserScale(float canvasWidth, float canvasHeight,
            float reportHeight, float requestedScale) => Math.Max(0f,
                Math.Min(Math.Min(canvasWidth / 640f, canvasHeight / 360f) * requestedScale,
                    Math.Min((canvasWidth - Margin * 2f) / (Width + 24f),
                        (canvasHeight - Margin * 2f) / (reportHeight + 76f))));
    }
}
