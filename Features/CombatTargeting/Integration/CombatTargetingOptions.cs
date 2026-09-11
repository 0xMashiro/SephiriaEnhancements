using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.CombatTargeting.Integration
{
    internal static class CombatTargetingOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<TargetingModeOption>(true) == null)
            {
                CreateTargetingModeRow(template, section);
            }

            if (panel.GetComponentInChildren<MouseAimAssistOption>(true) == null)
            {
                CreateMouseAimAssistRow(template, section);
            }
        }

        private static void CreateTargetingModeRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_TargetingMode",
                ControlLocalization.SettingTargetingMode,
                ControlLocalization.HelpTargetingMode, 9,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<TargetingModeOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.ControlsAndCamera);
            row.SetActive(true);
        }

        private static void CreateMouseAimAssistRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MouseAimAssist",
                ControlLocalization.SettingMouseAimAssist,
                ControlLocalization.HelpMouseAimAssist, 10,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<MouseAimAssistOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.ControlsAndCamera);
            row.SetActive(true);
        }
    }

    internal sealed class TargetingModeOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal UI_HorizontalSelectionBox Box => box;
        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        { box = selectionBox; valueText = text; }
        private void OnEnable()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            if (box == null)
                return;
            box.numberOfElements = CombatTargetingSettings.TargetingModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)CombatTargetingSettings.TargetingMode;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ControlLocalization.TargetingModeKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                ChangedCore(value);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ChangedCore(int value)
        {
            CombatTargetingSettings.TargetingMode = (TargetingMode)value;
            CombatTargetingSettings.Save();
            NativeControlCoordinator.OnTargetingSettingChanged(value != (int)TargetingMode.Disabled);
            valueText?.UpdateKey(ControlLocalization.TargetingModeKeys[value]);
        }
    }

    internal sealed class MouseAimAssistOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal UI_HorizontalSelectionBox Box => box;
        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        { box = selectionBox; valueText = text; }
        private void OnEnable()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            if (box == null)
                return;
            box.numberOfElements = CombatTargetingSettings.MouseAimAssistModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = CombatTargetingSettings.MouseAimAssistEnabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ControlLocalization.MouseAimAssistKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatTargeting))
            {
                return;
            }

            try
            {
                ChangedCore(value);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatTargeting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ChangedCore(int value)
        {
            CombatTargetingSettings.MouseAimAssistEnabled = value == 1;
            CombatTargetingSettings.Save();
            valueText?.UpdateKey(ControlLocalization.MouseAimAssistKeys[value]);
        }
    }
}
