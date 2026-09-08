using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal static class InventoryOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<InventorySearchModeOption>(
                    true) == null)
            {
                CreateInventorySearchModeRow(template,
                    section);
            }
        }

        private static void CreateInventorySearchModeRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_InventorySearchMode",
                InventoryOptimizationLocalization.SettingSearchMode,
                InventoryOptimizationLocalization.HelpSearchMode, 14,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<InventorySearchModeOption>().Configure(
                box, valueText);
            MarkCategory(row, OptionsCategory.InventoryArrangement);
            row.SetActive(true);
        }
    }

    internal sealed class InventorySearchModeOption : MonoBehaviour
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
            if (box == null)
            {
                return;
            }

            box.numberOfElements = InventoryOptimizationLocalization.
                SearchModeKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)ModSettings.InventorySearchMode;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(InventoryOptimizationLocalization.
                SearchModeKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null)
            {
                box.OnValueChanged -= Changed;
            }
        }

        private void Changed(int value)
        {
            ModSettings.InventorySearchMode =
                (InventorySearchMode)value;
            ModSettings.Save();
            valueText?.UpdateKey(InventoryOptimizationLocalization.
                SearchModeKeys[value]);
        }
    }
}
