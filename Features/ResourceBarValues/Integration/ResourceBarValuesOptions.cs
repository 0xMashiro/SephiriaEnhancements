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
                GameObject action = CloneRow(template, section,
                    "Option_SephiriaEnhancements_DisableAllNumbers",
                    ResourceBarValueLocalization.DisableAllNumbers, ResourceBarValueLocalization.DisableAllNumbersHelp, 8,
                    out UI_HorizontalSelectionBox box, out UI_LocalizationStringText text);
                action.AddComponent<DisableAllResourceNumbersOption>().Configure(box, text);
                MarkCategory(action, OptionsCategory.CombatAndDisplay);
                action.SetActive(true);
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

    internal sealed class DisableAllResourceNumbersOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText value;

        internal void Configure(UI_HorizontalSelectionBox selection, UI_LocalizationStringText text)
        { box = selection; value = text; }

        private void OnEnable()
        {
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            ResourceBarValueSettings.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            box.OnValueChanged -= Changed;
            ResourceBarValueSettings.Changed -= Refresh;
        }

        private void Changed(int selection)
        {
            if (selection == 1) ResourceBarValueSettings.DisableAll();
            Refresh();
        }

        private void Refresh()
        {
            box.ChangeValueWithoutNotify(0);
            bool anyEnabled = ResourceBarValueSettings.AnyEnabled;
            value.UpdateKey(anyEnabled
                ? ResourceBarValueLocalization.DisableAll : ResourceBarValueLocalization.AllOff);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box, anyEnabled);
        }
    }


}
