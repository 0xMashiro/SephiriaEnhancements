using UnityEngine;

namespace SephiriaEnhancements.CombatVisuals
{
    internal static class CombatVisualSettings
    {
        internal const string PresetKey =
            "SephiriaEnhancements.CombatVisuals.Preset";
        internal const string CompanionBodyKey =
            "SephiriaEnhancements.CombatVisuals.CompanionBody";
        internal const string CompanionEffectsKey =
            "SephiriaEnhancements.CombatVisuals.CompanionEffects";
        internal const string OutlineScopeKey =
            "SephiriaEnhancements.CombatVisuals.OutlineScope";

        internal const int PresetCount = 4;
        internal const int TransparencyLevelCount = 4;
        internal const int OutlineScopeCount = 3;

        internal static CombatVisualPreset Preset
        {
            get => (CombatVisualPreset)GetInt(PresetKey,
                (int)CombatVisualPolicy.DefaultPreset, PresetCount);
            set => SetInt(PresetKey, (int)value, PresetCount);
        }

        internal static EffectTransparencyLevel CompanionBody
        {
            get => (EffectTransparencyLevel)GetInt(CompanionBodyKey,
                (int)EffectTransparencyLevel.SlightlyTransparent,
                TransparencyLevelCount);
            set => SetInt(CompanionBodyKey, (int)value, TransparencyLevelCount);
        }

        internal static EffectTransparencyLevel CompanionEffects
        {
            get => (EffectTransparencyLevel)GetInt(CompanionEffectsKey,
                (int)EffectTransparencyLevel.VeryTransparent,
                TransparencyLevelCount);
            set => SetInt(CompanionEffectsKey, (int)value, TransparencyLevelCount);
        }

        internal static CombatOutlineScope OutlineScope
        {
            get => (CombatOutlineScope)GetInt(OutlineScopeKey,
                (int)CombatOutlineScope.HostileAndFriendly, OutlineScopeCount);
            set => SetInt(OutlineScopeKey, (int)value, OutlineScopeCount);
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }

        private static int GetInt(string key, int fallback, int count)
        {
            return Mathf.Clamp(OptionsBinding.Instance?.DeviceOptions?.GetInt(
                key, fallback) ?? fallback, 0, count - 1);
        }

        private static void SetInt(string key, int value, int count)
        {
            OptionsBinding.Instance?.DeviceOptions?.SetInt(key,
                Mathf.Clamp(value, 0, count - 1));
        }
    }
}
