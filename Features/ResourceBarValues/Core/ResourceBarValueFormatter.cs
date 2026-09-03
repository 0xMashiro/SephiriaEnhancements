using System;
using System.Globalization;

namespace SephiriaEnhancements.ResourceBarValues
{
    internal static class ResourceBarValueFormatter
    {
        internal static string Amount(float value) =>
            Math.Ceiling(Math.Max(0d, value)).ToString("N0", CultureInfo.InvariantCulture);

        internal static string Ratio(float current, float maximum) => maximum > 0f
            ? Amount(current) + " / " + Amount(maximum) : string.Empty;

        internal static string HealthWithShield(float current, float maximum, float shield)
        {
            string health = Ratio(current, maximum);
            return health.Length == 0 || shield <= 0f ? health
                : health + " (+" + Amount(shield) + ")";
        }

        internal static string Mana(int current, int maximum, int reserved, string reserveLabel)
        {
            string mana = Amount(current) + " / " + Amount(Math.Max(0, maximum - reserved));
            return reserved > 0 ? mana + " (" + reserveLabel + " " + Amount(reserved) + ")" : mana;
        }

        // A life segment has the same weight as the current HP segment in this bar.
        internal static string RemainingLivesHealth(float current, float maximum,
            int remainingLives, int totalLives) => Ratio(
                remainingLives * maximum + Math.Max(0f, current), (totalLives + 1f) * maximum);
    }
}
