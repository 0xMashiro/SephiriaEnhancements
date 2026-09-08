using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.DeveloperTools.Integration
{
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
    internal static class DeveloperToolsOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<InventoryReproductionOption>(true) == null)
            {
                GameObject row = CloneRow(template, section,
                    "Option_SephiriaEnhancements_InventoryReproduction",
                    Diagnostics.InventoryReproductionLocalization.Setting,
                    Diagnostics.InventoryReproductionLocalization.Help, 14,
                    out UI_HorizontalSelectionBox box, out UI_LocalizationStringText text);
                row.AddComponent<InventoryReproductionOption>().Configure(box, text);
                MarkCategory(row, OptionsCategory.General);
                row.SetActive(true);
            }

            if (panel.GetComponentInChildren<DeveloperPlayerDamageOption>(true) ==
                null)
            {
                CreateDeveloperPlayerDamageRow(template, section);
            }
        }

        private static void CreateDeveloperPlayerDamageRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DeveloperPlayerDamage",
                ModLocalization.SettingDeveloperPlayerDamage,
                ModLocalization.HelpDeveloperPlayerDamage, 13,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DeveloperPlayerDamageOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
    }

    internal sealed class DeveloperPlayerDamageOption : MonoBehaviour
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
            box.numberOfElements = DeveloperPlayerDamageSettings.MultiplierCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = DeveloperPlayerDamageSettings.MultiplierIndex;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.DeveloperPlayerDamageMultiplierKeys[
                value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            DeveloperPlayerDamageSettings.MultiplierIndex = value;
            DeveloperPlayerDamageSettings.Save();
            valueText?.UpdateKey(ModLocalization.DeveloperPlayerDamageMultiplierKeys[
                DeveloperPlayerDamageSettings.MultiplierIndex]);
        }
    }
#endif
}
