using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.NativeCompanion.Integration
{
    internal static class NativeCompanionOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<NativeCompanionOption>(true) == null)
            {
                CreateNativeCompanionRow(template, section);
            }
        }

        private static void CreateNativeCompanionRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_NativeCompanion",
                ModLocalization.SettingNativeCompanion,
                ModLocalization.HelpNativeCompanion, 4,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<NativeCompanionOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
    }

    internal sealed class NativeCompanionOption : MonoBehaviour
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
            box.numberOfElements = NativeCompanionSettings.ModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)NativeCompanionSettings.Mode;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.NativeCompanionModeKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            NativeCompanionSettings.Mode = (NativeCompanionMode)value;
            NativeCompanionSettings.Save();
            valueText?.UpdateKey(ModLocalization.NativeCompanionModeKeys[value]);
        }
    }
}
