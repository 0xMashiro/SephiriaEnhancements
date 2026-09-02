using UnityEngine;

namespace SephiriaEnhancements.ViewDistance
{
    internal static class ViewDistanceSettings
    {
        private static readonly float[] Multipliers = { 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2f };

        internal const string ScaleIndexKey =
            "SephiriaEnhancements.ViewDistance.ScaleIndex";
        internal const int ScaleCount = 6;

        internal static int ScaleIndex
        {
            get => Mathf.Clamp(OptionsBinding.Instance?.DeviceOptions?.GetInt(
                    ScaleIndexKey, 1) ?? 1,
                0, Multipliers.Length - 1);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(ScaleIndexKey,
                Mathf.Clamp(value, 0, Multipliers.Length - 1));
        }

        internal static float Multiplier => Multipliers[ScaleIndex];

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
