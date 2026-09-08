using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.CombatRelationOutlines.Integration
{
    internal static class CombatRelationOutlinesOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<CombatRelationOutlinesOption>(true) == null)
            {
                CreateCombatRelationOutlinesRow(template, section);
            }
        }

        private static void CreateCombatRelationOutlinesRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_CombatRelationOutlines",
                ModLocalization.SettingCombatRelationOutlines,
                ModLocalization.HelpCombatRelationOutlines, 3,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<CombatRelationOutlinesOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }
    }

    internal sealed class CombatRelationOutlinesOption : MonoBehaviour
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
            int value = CombatRelationOutlinesSettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            CombatRelationOutlinesSettings.Enabled = value == 1;
            CombatRelationOutlinesSettings.Save();
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }
    }
}
