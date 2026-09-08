using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using Mirror;
using SephiriaEnhancements.MultiplayerAccess.Presentation;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class MultiplayerAccessOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<MidRunAdmissionOption>(true) == null)
            {
                CreateMidRunAdmissionRow(template, section);
            }
        }

        private static void CreateMidRunAdmissionRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MidRunAdmission",
                MultiplayerAccessLocalization.AllowJoinAndReconnectSetting,
                MultiplayerAccessLocalization.AllowJoinAndReconnectHelp, 1,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MidRunAdmissionOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
        }
    }

    internal sealed class MidRunAdmissionOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

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
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Refresh()
        {
            bool enabled = MidRunAdmissionSettings.AllowJoinAndReconnect;
            box.ChangeValueWithoutNotify(enabled ? 1 : 0);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                MidRunAdmissionRuntime.IsAvailable &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences());
            valueText?.UpdateKey(enabled
                ? MultiplayerRulesLocalization.ToggleEnabled
                : MultiplayerRulesLocalization.ToggleDisabled);
        }

        private void Changed(int value)
        {
            if (!MidRunAdmissionRuntime.IsAvailable ||
                !MultiplayerRulesOptionsRefresh.CanEditHostPreferences())
            {
                Refresh();
                return;
            }
            bool enabled = value != 0;
            MidRunAdmissionSettings.AllowJoinAndReconnect = enabled;
            MidRunAdmissionSettings.Save();
            valueText?.UpdateKey(enabled
                ? MultiplayerRulesLocalization.ToggleEnabled
                : MultiplayerRulesLocalization.ToggleDisabled);
        }
    }
}
