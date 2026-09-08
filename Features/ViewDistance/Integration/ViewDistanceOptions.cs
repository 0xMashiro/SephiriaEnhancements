using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.ViewDistance.Integration
{
    internal static class ViewDistanceOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<ViewDistanceOption>(true) == null)
            {
                CreateViewDistanceRow(template, section);
            }
        }

        private static void CreateViewDistanceRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_ViewDistance",
                ControlLocalization.SettingViewDistance, ControlLocalization.HelpViewDistance, 11,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<ViewDistanceOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.ControlsAndCamera);
            row.SetActive(true);
        }
    }

    internal sealed class ViewDistanceOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal UI_HorizontalSelectionBox Box => box;
        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        { box = selectionBox; valueText = text; }
        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = ViewDistanceSettings.ScaleCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = ViewDistanceSettings.ScaleIndex;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ControlLocalization.ViewDistanceKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            ViewDistanceSettings.ScaleIndex = value;
            ViewDistanceSettings.Save();
            valueText?.UpdateKey(ControlLocalization.ViewDistanceKeys[value]);
        }
    }
}
