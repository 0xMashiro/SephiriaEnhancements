using SephiriaEnhancements.Runtime;
using System.Linq;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.ModInformation.Integration
{
    internal enum ModInformationRow { Version, GameVersion, Welcome, Automatic, Check, LastChecked, Nexus, GitHub }

    internal static class ModInformationOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<ModInformationOption>(true) != null) return;
            Add(template, section, ModInformationRow.Version, ModInformationLocalization.Version, ModInformationLocalization.VersionHelp);
            Add(template, section, ModInformationRow.GameVersion, ModInformationLocalization.GameVersion, ModInformationLocalization.GameVersionHelp);
            Add(template, section, ModInformationRow.Welcome, ModInformationLocalization.WelcomeSetting, ModInformationLocalization.WelcomeHelp);
            Add(template, section, ModInformationRow.Automatic, ModInformationLocalization.AutomaticSetting, ModInformationLocalization.AutomaticHelp);
            Add(template, section, ModInformationRow.Check, ModInformationLocalization.Check, ModInformationLocalization.CheckHelp);
            Add(template, section, ModInformationRow.LastChecked, ModInformationLocalization.LastChecked, ModInformationLocalization.LastCheckedHelp);
            Add(template, section, ModInformationRow.Nexus, ModInformationLocalization.Nexus, ModInformationLocalization.LinkHelp);
            Add(template, section, ModInformationRow.GitHub, ModInformationLocalization.GitHub, ModInformationLocalization.LinkHelp);
        }

        private static void Add(UI_OptionBox_PartyMemberDamage template, Transform section,
            ModInformationRow kind, string label, string help)
        {
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_ModInformation_" + kind,
                label, help, 3 + (int)kind, out UI_HorizontalSelectionBox box, out UI_LocalizationStringText text);
            // The native selection box remains the navigation owner; actions use submit/click, not left/right.
            UI_LocalizationStringText labelText = box.GetComponentsInChildren<UI_LocalizationStringText>(true)
                .FirstOrDefault(candidate => candidate != text);
            UI_LocalizationStringText labelReference = template.GetComponentsInChildren<UI_LocalizationStringText>(true)
                .FirstOrDefault(candidate => candidate != template.valueText);
            box.gameObject.AddComponent<ModInformationOption>().Configure(kind, box, text,
                template.valueText.text, labelText?.text, labelReference?.text);
            MarkCategory(row, OptionsCategory.AboutAndUpdates);
            row.SetActive(true);
        }
    }

    internal sealed class ModInformationOption : MonoBehaviour, ISubmitHandler, IPointerClickHandler
    {
        private ModInformationRow kind;
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        private TextMeshProUGUI fontReference;
        private TextMeshProUGUI labelText;
        private TextMeshProUGUI labelReference;
        private bool IsToggle => kind == ModInformationRow.Welcome || kind == ModInformationRow.Automatic;

        internal void Configure(ModInformationRow row, UI_HorizontalSelectionBox selection,
            UI_LocalizationStringText text, TextMeshProUGUI reference,
            TextMeshProUGUI label, TextMeshProUGUI nativeLabel)
        {
            kind = row; box = selection; valueText = text; fontReference = reference;
            labelText = label; labelReference = nativeLabel;
        }

        private void OnEnable()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModInformation))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModInformation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            if (box == null)
                return;
            box.numberOfElements = IsToggle ? 2 : 1;

            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            foreach (UI_HorizontalSelectionBox_Arrow arrow in box.GetComponentsInChildren<UI_HorizontalSelectionBox_Arrow>(true))
                arrow.gameObject.SetActive(IsToggle);
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void LateUpdate()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModInformation))
            {
                return;
            }

            try
            {
                LateUpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModInformation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void LateUpdateCore() => Refresh();

        private void Refresh()
        {
            if (box == null || valueText == null) return;
            box.interactable = kind != ModInformationRow.Version && kind != ModInformationRow.GameVersion &&
                kind != ModInformationRow.LastChecked &&
                (kind != ModInformationRow.Check || (NativeModInformation.Instance != null && NativeModInformation.Result.Status != ModUpdateStatus.Checking));
            bool selected = kind == ModInformationRow.Welcome ? ModInformationSettings.ShowWelcome :
                ModInformationSettings.AutomaticUpdateCheck;
            box.ChangeValueWithoutNotify(IsToggle && selected ? 1 : 0);
            string key = IsToggle ? (selected ? ModInformationLocalization.On : ModInformationLocalization.Off) :
                kind == ModInformationRow.Check ? ModInformationLocalization.StatusKey(NativeModInformation.Result.Status) :
                kind == ModInformationRow.Version ? ModInformationLocalization.Version :
                kind == ModInformationRow.GameVersion ? ModInformationLocalization.GameVersion :
                kind == ModInformationRow.LastChecked ? ModInformationLocalization.NotChecked : ModInformationLocalization.Open;
            if (valueText.valueString.key != key) valueText.UpdateKey(key);
            valueText.text.text = kind == ModInformationRow.Version ? NativeModInformation.InstalledVersion :
                kind == ModInformationRow.GameVersion ? Application.version :
                kind == ModInformationRow.LastChecked ? (NativeModInformation.LastCheckedUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? ModLocalization.Get(key)) :
                kind == ModInformationRow.Check && NativeModInformation.Result.Status == ModUpdateStatus.UpdateAvailable ?
                string.Format(ModLocalization.Get(key), NativeModInformation.Result.Version) : ModLocalization.Get(key);
            valueText.text.textWrappingMode = TextWrappingModes.NoWrap;
            NativeLocalizedText.MatchFontSize(valueText.text, fontReference);
            if (labelText != null && labelReference != null)
                NativeLocalizedText.MatchFontSize(labelText, labelReference);
        }

        private void Changed(int value)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModInformation))
            {
                return;
            }

            try
            {
                ChangedCore(value);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModInformation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void ChangedCore(int value)
        {
            if (!IsToggle)
                return;
            if (kind == ModInformationRow.Welcome)
                ModInformationSettings.ShowWelcome = value == 1;
            else
                ModInformationSettings.AutomaticUpdateCheck = value == 1;
            ModSettings.Save();
            Refresh();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (box == null || !box.IsInteractable()) return;
            if (IsToggle) Changed(box.CurrentSelection == 0 ? 1 : 0);
            else if (kind == ModInformationRow.Check)
            {
                NativeModInformation.Instance?.StartCheck();
                Refresh();
            }
            else if (kind == ModInformationRow.Nexus) Application.OpenURL(ModOfficialLinks.Nexus);
            else if (kind == ModInformationRow.GitHub) Application.OpenURL(ModOfficialLinks.GitHub);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && !IsToggle) OnSubmit(eventData);
        }
    }
}
