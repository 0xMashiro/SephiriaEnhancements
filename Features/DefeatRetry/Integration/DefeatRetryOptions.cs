using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.DefeatRetry.Integration
{
    internal static class DefeatRetryOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<DefeatRetryOption>(true) == null)
            {
                CreateDefeatRetryRow(template, section);
            }
        }

        private static void CreateDefeatRetryRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DefeatRetry",
                ModLocalization.SettingDefeatRetry,
                ModLocalization.HelpDefeatRetry, 5,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DefeatRetryOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
    }

    internal sealed class DefeatRetryOption : MonoBehaviour
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
            int value = DefeatRetrySettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DefeatRetryOn
                : ModLocalization.DefeatRetryOff);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            DefeatRetrySettings.Enabled = value == 1;
            DefeatRetrySettings.Save();
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DefeatRetryOn
                : ModLocalization.DefeatRetryOff);
        }
    }
}
