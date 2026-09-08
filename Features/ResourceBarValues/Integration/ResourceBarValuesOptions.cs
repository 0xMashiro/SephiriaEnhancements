using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    internal static class ResourceBarValuesOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<ResourceBarValueOption>(true) == null)
            {
                foreach (ResourceBarValueSetting setting in System.Enum.GetValues(typeof(ResourceBarValueSetting)))
                    CreateResourceBarValueRow(template, section, setting);
            }
        }

        private static void CreateResourceBarValueRow(UI_OptionBox_PartyMemberDamage template,
            Transform section, ResourceBarValueSetting setting)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_" + setting,
                ResourceBarValueLocalization.Setting(setting),
                ResourceBarValueLocalization.Help(setting), 8,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<ResourceBarValueOption>().Configure(box, valueText, setting);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }
    }


}
