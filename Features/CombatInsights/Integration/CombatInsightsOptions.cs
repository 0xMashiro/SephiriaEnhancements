using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.CombatInsights.Integration
{
    internal static class CombatInsightsOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<DisplayPolicyOption>(true) == null)
            {
                CreateDisplayPolicyRow(template, section);
            }

            if (panel.GetComponentInChildren<HitStreakFeedbackOption>(true) == null)
            {
                CreateHitStreakFeedbackRow(template, section);
            }

            if (panel.GetComponentInChildren<DamageStatisticsScaleOption>(true) == null)
            {
                CreateDamageStatisticsScaleRow(template, section);
            }
        }

        private static void CreateDisplayPolicyRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_DisplayPolicy",
                ModLocalization.SettingDisplayPolicy, ModLocalization.HelpDisplayPolicy, 6,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<DisplayPolicyOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }

        private static void CreateHitStreakFeedbackRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_HitStreakFeedback",
                ModLocalization.SettingHitStreakFeedback,
                ModLocalization.HelpHitStreakFeedback, 7,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<HitStreakFeedbackOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }

        private static void CreateDamageStatisticsScaleRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DamageStatisticsScale",
                ModLocalization.SettingDamageStatisticsScale,
                ModLocalization.HelpDamageStatisticsScale, 8,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DamageStatisticsScaleOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }
    }

    internal sealed class DisplayPolicyOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal UI_HorizontalSelectionBox Box => box;
        internal void Configure(UI_HorizontalSelectionBox selectionBox, UI_LocalizationStringText text)
        { box = selectionBox; valueText = text; }
        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = ModLocalization.DisplayPolicyKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)ModSettings.DisplayPolicy;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.DisplayPolicyKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            ModSettings.DisplayPolicy = (CombatInsightsDisplayPolicy)value;
            ModSettings.Save();
            valueText?.UpdateKey(ModLocalization.DisplayPolicyKeys[value]);
        }
    }

    internal sealed class HitStreakFeedbackOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = ModSettings.HitStreakFeedback ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            ModSettings.HitStreakFeedback = value == 1;
            ModSettings.Save();
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }
    }

    internal sealed class DamageStatisticsScaleOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox, UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = ModSettings.DamageStatisticsScaleCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = ModSettings.DamageStatisticsScaleIndex;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.ScaleKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            ModSettings.DamageStatisticsScaleIndex = value;
            ModSettings.Save();
            valueText?.UpdateKey(ModLocalization.ScaleKeys[value]);
        }
    }
}
