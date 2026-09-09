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
                GameObject row = CloneRow(template, section,
                    "Option_SephiriaEnhancements_DefeatRetryCutscenes",
                    DefeatRetryCutsceneLocalization.Name, DefeatRetryCutsceneLocalization.Help, 5,
                    out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
                row.AddComponent<DefeatRetryOption>().Configure(box, valueText, cutscenes: true);
                MarkCategory(row, OptionsCategory.General);
                row.SetActive(true);
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
        private bool cutscenes;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text, bool cutscenes = false)
        {
            box = selectionBox;
            valueText = text;
            this.cutscenes = cutscenes;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (cutscenes ? DefeatRetrySettings.SkipCutscenes : DefeatRetrySettings.Enabled) ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            UpdateText(value);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            if (cutscenes) DefeatRetrySettings.SkipCutscenes = value == 1;
            else DefeatRetrySettings.Enabled = value == 1;
            DefeatRetrySettings.Save();
            UpdateText(value);
        }

        private void UpdateText(int value)
        {
            if (cutscenes)
            {
                valueText?.UpdateKey(value == 1 ? DefeatRetryCutsceneLocalization.On : DefeatRetryCutsceneLocalization.Off);
                return;
            }
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DefeatRetryOn
                : ModLocalization.DefeatRetryOff);
        }
    }
}
