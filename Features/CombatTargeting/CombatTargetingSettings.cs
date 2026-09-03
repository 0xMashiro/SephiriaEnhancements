using SephiriaEnhancements.Configuration;
using UnityEngine;

namespace SephiriaEnhancements.CombatTargeting
{
    internal enum TargetingMode
    {
        Disabled,
        Automatic
    }

    internal static class CombatTargetingSettings
    {
        internal const string TargetingModeKey =
            "SephiriaEnhancements.CombatTargeting.Targeting.Mode";
        internal const string MouseAimAssistEnabledKey =
            "SephiriaEnhancements.CombatTargeting.MouseAimAssist.Enabled";

        internal const int TargetingModeCount = 2;
        internal const int MouseAimAssistModeCount = 2;

        internal static TargetingMode TargetingMode
        {
            get => (TargetingMode)Mathf.Clamp(
                OptionsBinding.Instance?.DeviceOptions?.GetInt(
                    TargetingModeKey, (int)TargetingMode.Automatic) ??
                (int)TargetingMode.Automatic,
                0, TargetingModeCount - 1);
            set => OptionsBinding.Instance?.DeviceOptions?.SetInt(TargetingModeKey,
                Mathf.Clamp((int)value, 0, TargetingModeCount - 1));
        }

        internal static bool MouseAimAssistEnabled
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(
                MouseAimAssistEnabledKey, false) ?? false;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(
                MouseAimAssistEnabledKey, value);
        }

        internal static void Save()
        {
            OptionsBinding.Instance?.DeviceOptions?.Save();
        }
    }
}
