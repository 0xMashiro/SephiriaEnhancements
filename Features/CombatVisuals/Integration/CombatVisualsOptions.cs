using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.CombatVisuals.Integration
{
    internal static class CombatVisualsOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            if (panel.GetComponentInChildren<CombatVisualOption>(true) == null)
            {
                CreateCombatVisualRows(template, section);
            }
        }

        private static void CreateCombatVisualRows(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            CreateCombatVisualRow(template, section,
                "Option_SephiriaEnhancements_CombatVisualPreset",
                CombatVisualLocalization.SettingPreset,
                CombatVisualLocalization.HelpPreset,
                CombatVisualOptionKind.Preset);
            CreateCombatVisualRow(template, section,
                "Option_SephiriaEnhancements_CompanionBody",
                CombatVisualLocalization.SettingCompanionBody,
                CombatVisualLocalization.HelpCompanionBody,
                CombatVisualOptionKind.CompanionBody);
            CreateCombatVisualRow(template, section,
                "Option_SephiriaEnhancements_CompanionEffects",
                CombatVisualLocalization.SettingCompanionEffects,
                CombatVisualLocalization.HelpCompanionEffects,
                CombatVisualOptionKind.CompanionEffects);
            CreateCombatVisualRow(template, section,
                "Option_SephiriaEnhancements_CombatOutlineScope",
                CombatVisualLocalization.SettingOutlineScope,
                CombatVisualLocalization.HelpOutlineScope,
                CombatVisualOptionKind.OutlineScope);
        }

        private static void CreateCombatVisualRow(
            UI_OptionBox_PartyMemberDamage template, Transform section,
            string objectName, string labelKey, string helpKey,
            CombatVisualOptionKind kind)
        {
            GameObject row = CloneRow(template, section, objectName, labelKey,
                helpKey, section.childCount, out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<CombatVisualOption>().Configure(kind, box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }
    }

    internal sealed class CombatVisualOption : MonoBehaviour
    {
        private static event System.Action RefreshRequested;

        private CombatVisualOptionKind kind;
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(CombatVisualOptionKind optionKind,
            UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            kind = optionKind;
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            RefreshRequested += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
            RefreshRequested -= Refresh;
        }

        private void Changed(int value)
        {
            switch (kind)
            {
                case CombatVisualOptionKind.Preset:
                    CombatVisualPreset preset = (CombatVisualPreset)value;
                    CombatVisualSettings.Preset = preset;
                    if (preset == CombatVisualPreset.Balanced)
                    {
                        CombatVisualSettings.CompanionBody =
                            EffectTransparencyLevel.SlightlyTransparent;
                        CombatVisualSettings.CompanionEffects =
                            EffectTransparencyLevel.VeryTransparent;
                        CombatVisualSettings.OutlineScope =
                            CombatOutlineScope.HostileAndFriendly;
                    }
                    else if (preset == CombatVisualPreset.Minimal)
                    {
                        CombatVisualSettings.CompanionBody =
                            EffectTransparencyLevel.VeryTransparent;
                        CombatVisualSettings.CompanionEffects =
                            EffectTransparencyLevel.CompletelyTransparent;
                        CombatVisualSettings.OutlineScope =
                            CombatOutlineScope.HostileAndFriendly;
                    }
                    break;
                case CombatVisualOptionKind.CompanionBody:
                    CombatVisualSettings.CompanionBody =
                        (EffectTransparencyLevel)value;
                    CombatVisualSettings.Preset = CombatVisualPreset.Custom;
                    break;
                case CombatVisualOptionKind.CompanionEffects:
                    CombatVisualSettings.CompanionEffects =
                        (EffectTransparencyLevel)value;
                    CombatVisualSettings.Preset = CombatVisualPreset.Custom;
                    break;
                case CombatVisualOptionKind.OutlineScope:
                    CombatVisualSettings.OutlineScope = (CombatOutlineScope)value;
                    CombatVisualSettings.Preset = CombatVisualPreset.Custom;
                    break;
            }

            CombatVisualSettings.Save();
            CombatVisualRuntime.RefreshCompanionBodies();
            RefreshRequested?.Invoke();
        }

        private void Refresh()
        {
            if (box == null) return;
            int value;
            string key;
            switch (kind)
            {
                case CombatVisualOptionKind.Preset:
                    box.numberOfElements = CombatVisualSettings.PresetCount;
                    value = (int)CombatVisualSettings.Preset;
                    key = CombatVisualLocalization.PresetKeys[value];
                    break;
                case CombatVisualOptionKind.CompanionBody:
                    box.numberOfElements =
                        CombatVisualSettings.TransparencyLevelCount;
                    value = (int)CombatVisualSettings.CompanionBody;
                    key = CombatVisualLocalization.TransparencyKeys[value];
                    break;
                case CombatVisualOptionKind.CompanionEffects:
                    box.numberOfElements =
                        CombatVisualSettings.TransparencyLevelCount;
                    value = (int)CombatVisualSettings.CompanionEffects;
                    key = CombatVisualLocalization.TransparencyKeys[value];
                    break;
                default:
                    box.numberOfElements = CombatVisualSettings.OutlineScopeCount;
                    value = (int)CombatVisualSettings.OutlineScope;
                    key = CombatVisualLocalization.OutlineScopeKeys[value];
                    break;
            }

            box.ChangeValueWithoutNotify(value);
            box.interactable = kind == CombatVisualOptionKind.Preset ||
                CombatVisualSettings.Preset == CombatVisualPreset.Custom;
            valueText?.UpdateKey(key);
        }
    }

    internal enum CombatVisualOptionKind
    {
        Preset,
        CompanionBody,
        CompanionEffects,
        OutlineScope
    }
}
