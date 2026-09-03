using UnityEngine;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.Configuration
{
    internal enum CombatInsightsDisplayPolicy
    {
        Smart,
        BossOnly,
        AllCombat,
        Disabled
    }

    internal static class ModSettings
    {
        private static readonly float[] DamageStatisticsScales =
            { 0.8f, 0.9f, 1f, 1.1f, 1.2f };

        internal const string HitStreakFeedbackKey =
            "SephiriaEnhancements.HitStreakFeedback.Enabled";
        internal const string DamageStatisticsScaleIndexKey =
            "SephiriaEnhancements.CombatInsights.DamageStatisticsScaleIndex";
        internal const string DisplayPolicyKey = "SephiriaEnhancements.CombatInsights.DisplayPolicy";
        internal const string InventoryOptimizationTendencyKey =
            "SephiriaEnhancements.Inventory.OptimizationTendency";
        internal const string InventoryTargetPreferencesKey =
            "SephiriaEnhancements.Inventory.TargetPreferences";

        internal static bool HitStreakFeedback
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(HitStreakFeedbackKey, true) ??
                true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(HitStreakFeedbackKey, value);
        }

        internal static CombatInsightsDisplayPolicy DisplayPolicy
        {
            get => (CombatInsightsDisplayPolicy)Mathf.Clamp(
                OptionsBinding.Instance?.DeviceOptions?.GetInt(DisplayPolicyKey, 0) ?? 0, 0, 3);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(DisplayPolicyKey,
                Mathf.Clamp((int)value, 0, 3));
        }

        internal static InventoryOptimizationTendency InventoryOptimizationTendency
        {
            get => (InventoryOptimizationTendency)Mathf.Clamp(
                OptionsBinding.Instance?.DeviceOptions?.GetInt(
                    InventoryOptimizationTendencyKey,
                    (int)Inventory.InventoryOptimizationTendency.Automatic) ??
                (int)Inventory.InventoryOptimizationTendency.Automatic,
                (int)Inventory.InventoryOptimizationTendency.Automatic,
                (int)Inventory.InventoryOptimizationTendency.Aggressive);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(
                InventoryOptimizationTendencyKey, Mathf.Clamp((int)value,
                    (int)Inventory.InventoryOptimizationTendency.Automatic,
                    (int)Inventory.InventoryOptimizationTendency.Aggressive));
        }

        internal static int DamageStatisticsScaleIndex
        {
            get => Mathf.Clamp(OptionsBinding.Instance?.DeviceOptions?.GetInt(
                    DamageStatisticsScaleIndexKey, 2) ?? 2,
                0, DamageStatisticsScales.Length - 1);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(
                DamageStatisticsScaleIndexKey,
                Mathf.Clamp(value, 0, DamageStatisticsScales.Length - 1));
        }

        internal static float DamageStatisticsScale =>
            DamageStatisticsScales[DamageStatisticsScaleIndex];

        internal static int DamageStatisticsScalePercent =>
            Mathf.RoundToInt(DamageStatisticsScale * 100f);

        internal static int DamageStatisticsScaleCount =>
            DamageStatisticsScales.Length;

        internal static void Save()
        {
            OptionsBinding binding = OptionsBinding.Instance;
            if (binding != null)
            {
                binding.DeviceOptions?.Save();
            }
        }

    }
}
