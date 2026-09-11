using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;
using SephiriaEnhancements.MultiplayerAccess.Presentation;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class MultiplayerAccessOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<MultiplayerAccessOption>(true) == null)
            {
                CreateMidRunAdmissionRow(template, section);
                GameObject reconnectRow = CloneRow(template, section,
                    "Option_SephiriaEnhancements_ReconnectSupport",
                    MultiplayerAccessLocalization.ReconnectSetting, MultiplayerAccessLocalization.ReconnectHelp, 2,
                    out UI_HorizontalSelectionBox reconnectBox, out UI_LocalizationStringText reconnectText);
                reconnectRow.AddComponent<MultiplayerAccessOption>().Configure(reconnectBox, reconnectText, true);
                NativeSettingsInteraction.Bind(reconnectRow, reconnectBox, reconnectText, SettingInteractionKind.Reconnect, template.valueText.text);
                MarkCategory(reconnectRow, OptionsCategory.Multiplayer);
                reconnectRow.SetActive(true);
            }
        }

        private static void CreateMidRunAdmissionRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MidRunAdmission",
                MultiplayerAccessLocalization.AllowMidRunJoinSetting,
                MultiplayerAccessLocalization.AllowMidRunJoinHelp, 1,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerAccessOption>().Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.Admission, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
        }
    }

    internal sealed class MultiplayerAccessOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        private bool reconnect;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text, bool reconnectSupport = false)
        {
            box = selectionBox;
            valueText = text;
            reconnect = reconnectSupport;
        }

        private void OnEnable()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            if (box == null)
                return;
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
            bool enabled = reconnect ? MidRunAdmissionSettings.ReconnectSupport : MidRunAdmissionSettings.AllowMidRunJoin;
            box.ChangeValueWithoutNotify(enabled ? 1 : 0);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                NativeSettingsInteraction.CanEdit(reconnect ? SettingInteractionKind.Reconnect : SettingInteractionKind.Admission));
            valueText?.UpdateKey(enabled
                ? MultiplayerAccessLocalization.On
                : MultiplayerAccessLocalization.Off);
        }

        private void Changed(int value)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                ChangedCore(value);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ChangedCore(int value)
        {
            if (!NativeSettingsInteraction.CanEdit(reconnect ? SettingInteractionKind.Reconnect : SettingInteractionKind.Admission))
            {
                Refresh();
                return;
            }

            bool enabled = value != 0;
            if (reconnect)
                MidRunAdmissionSettings.ReconnectSupport = enabled;
            else
                MidRunAdmissionSettings.AllowMidRunJoin = enabled;
            MidRunAdmissionSettings.Save();
            valueText?.UpdateKey(enabled ? MultiplayerAccessLocalization.On : MultiplayerAccessLocalization.Off);
        }
    }
}
