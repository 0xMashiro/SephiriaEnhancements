using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.FixedExplorationSeed.Integration
{
    internal static class ExplorationSeedOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<ExplorationSeedOption>(true) != null) return;
            var row = NativeOptionsRows.CloneRow(template, section,
                "Option_SephiriaEnhancements_ExplorationSeed", ExplorationSeedLocalization.Setting,
                ExplorationSeedLocalization.Help, 2, out var box, out var value);
            row.AddComponent<ExplorationSeedOption>().Configure(box, value);
            NativeOptionsRows.MarkCategory(row, OptionsCategory.GameRules);
            row.SetActive(true);
        }
    }

    internal sealed class ExplorationSeedOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText value;
        private UI_MessageBox_InputYesNo dialog;
        private bool CanEdit => EnhancementsSettings.Enabled &&
            FeatureFailure.IsAvailable(FeatureId.FixedExplorationSeed) &&
            OptionsBinding.Instance?.DeviceOptions != null && (!NetworkClient.active || NetworkServer.active);

        internal void Configure(UI_HorizontalSelectionBox selection, UI_LocalizationStringText text)
        {
            box = selection;
            value = text;
            box.numberOfElements = 1;
            foreach (var arrow in box.GetComponentsInChildren<UI_HorizontalSelectionBox_Arrow>(true))
                arrow.gameObject.SetActive(false);
            box.gameObject.AddComponent<NativeOptionActivation>().Configure(Open);
        }

        private void Update()
        {
            NativeHorizontalSelectionOptionState.Apply(gameObject, box, CanEdit);
            if (NetworkClient.active && !NetworkServer.active)
                value.UpdateKey(ExplorationSeedLocalization.HostOnly);
            else if (string.IsNullOrEmpty(ExplorationSeedSettings.Text))
                value.UpdateKey(ExplorationSeedLocalization.Random);
            else
            {
                value.UpdateKey(ExplorationSeedLocalization.Setting);
                value.text.text = ExplorationSeedSettings.Text;
            }
        }

        private void Open()
        {
            if (!CanEdit) return;
            dialog = NativeNumberInputDialog.Open(new NumberInputOptions
            {
                Prompt = ModLocalization.Get(ExplorationSeedLocalization.Prompt),
                Initial = ExplorationSeedSettings.Text,
                Placeholder = ModLocalization.Get(ExplorationSeedLocalization.Random),
                CharacterLimit = 11, Signed = true,
                ConfirmKey = ExplorationSeedLocalization.Confirm,
                CancelKey = ExplorationSeedLocalization.Cancel,
                ClearKey = ExplorationSeedLocalization.Clear,
                CanEdit = () => CanEdit,
                IsValid = text => ExplorationSeedInput.TryParse(text, out _),
                ValidateCharacter = ExplorationSeedInput.ValidateCharacter,
                Feedback = text => ModLocalization.Get(!CanEdit ? ExplorationSeedLocalization.Unavailable :
                    ExplorationSeedInput.TryParse(text, out _) ? ExplorationSeedLocalization.InputHelp : ExplorationSeedLocalization.Invalid),
                Save = ExplorationSeedSettings.Save
            }, box.gameObject);
            if (dialog != null) dialog.onCloseMessageBox += Closed;
        }

        private void Closed(UI_MessageBox closed)
        {
            closed.onCloseMessageBox -= Closed;
            dialog = null;
        }

        private void OnDisable()
        {
            if (dialog != null && dialog.IsOpened) dialog.Close();
            dialog = null;
        }
    }
}
