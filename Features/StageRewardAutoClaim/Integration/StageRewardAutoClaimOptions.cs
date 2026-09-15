using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.StageRewardAutoClaim.Integration
{
    internal static class StageRewardAutoClaimOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<StageRewardAutoClaimOption>(true) != null) return;
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_StageRewardAutoClaim",
                StageRewardAutoClaimLocalization.Name, StageRewardAutoClaimLocalization.Help, 6,
                out var box, out var valueText);
            row.AddComponent<StageRewardAutoClaimOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
    }

    internal sealed class StageRewardAutoClaimOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal void Configure(UI_HorizontalSelectionBox selection, UI_LocalizationStringText text)
        { box = selection; valueText = text; }

        private void OnEnable() => FeatureFailure.Run(FeatureId.StageRewardAutoClaim, () =>
        {
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            box.ChangeValueWithoutNotify(StageRewardAutoClaimSettings.Enabled ? 1 : 0);
            RefreshText();
        });

        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value) => FeatureFailure.Run(FeatureId.StageRewardAutoClaim, () =>
        {
            StageRewardAutoClaimSettings.Enabled = value == 1;
            StageRewardAutoClaimSettings.Save();
            RefreshText();
        });
        private void RefreshText() => valueText?.UpdateKey(StageRewardAutoClaimSettings.Enabled
            ? StageRewardAutoClaimLocalization.On : StageRewardAutoClaimLocalization.Off);
    }
}
