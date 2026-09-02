using System;
using System.Globalization;

namespace SephiriaEnhancements.Core
{
    internal static class DpsFormatter
    {
        internal static string Compact(float value)
        {
            float safe = Math.Max(0f, value);
            if (safe < 999.5f)
            {
                return Math.Round(safe).ToString("0", CultureInfo.InvariantCulture);
            }

            if (safe < 999500f)
            {
                return Scaled(safe / 1000f) + "K";
            }

            if (safe < 999500000f)
            {
                return Scaled(safe / 1000000f) + "M";
            }

            return Scaled(safe / 1000000000f) + "B";
        }

        internal static string Rate(float damage, float seconds) =>
            Compact(seconds > 0f ? damage / seconds : 0f);

        internal static string Percent(float value, float total)
        {
            float percent = total > 0f ? Math.Max(0f, value) / total * 100f : 0f;
            return Math.Round(percent).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        internal static string Seconds(float seconds)
        {
            float safe = Math.Max(0f, seconds);
            return safe < 100f
                ? safe.ToString("0.0", CultureInfo.InvariantCulture) + "s"
                : safe.ToString("0", CultureInfo.InvariantCulture) + "s";
        }

        private static string Scaled(float value) => value < 10f
            ? value.ToString("0.#", CultureInfo.InvariantCulture)
            : value.ToString("0", CultureInfo.InvariantCulture);
    }
}
