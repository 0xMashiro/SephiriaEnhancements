using SephiriaEnhancements.Configuration;
using UnityEngine;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using Mirror;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesOptions
    {
        internal static void Inject(UI_OptionsPanel panel, UI_OptionBox_PartyMemberDamage template, Transform section, OptionsCategoryController categoryController)
        {
            if (panel.GetComponentInChildren<MultiplayerRulesPresetOption>(true) == null)
            {
                CreateMultiplayerRulesPresetRow(template,
                    section);
            }

            if (panel.GetComponentInChildren<
                    MultiplayerRulesExternalStackingOption>(true) == null)
            {
                CreateMultiplayerRulesExternalStackingRow(template,
                    section);
            }

            if (panel.GetComponentInChildren<
                    MultiplayerRulesParticipantCountOption>(true) == null)
            {
                CreateMultiplayerRulesParticipantCountRow(template,
                    section);
                CreateMultiplayerRulesCopyParticipantValuesRow(template,
                    section);
                CreateMultiplayerRulesHealthCombinationRow(template,
                    section);
            }

            if (panel.GetComponentInChildren<MultiplayerRuleGroupOption>(true) ==
                    null)
            {
                CreateMultiplayerRuleGroupRow(template,
                    section, categoryController);
            }

            if (panel.GetComponentInChildren<MultiplayerRuleOption>(true) == null)
            {
                int offset = 8;
                int groupIndex = 0;
                foreach (MultiplayerRulePresentationGroup group in
                    MultiplayerRulePresentationGroups.All)
                {
                    foreach (MultiplayerRuleId ruleId in group.RuleIds)
                    {
                        CreateMultiplayerRuleRow(template,
                            section,
                            MultiplayerRuleCatalog.Get(ruleId), groupIndex,
                            offset++);
                    }
                    groupIndex++;
                }
            }
        }

        private static void CreateMultiplayerRuleRow(
            UI_OptionBox_PartyMemberDamage template, Transform section,
            MultiplayerRuleDefinition definition, int groupIndex,
            int siblingOffset)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRule_" + definition.Id,
                MultiplayerRulesLocalization.RuleLabelKey(definition.Id),
                MultiplayerRulesLocalization.RuleHelpKey(definition.Id), siblingOffset,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRuleOption>()
                .Configure(box, valueText, definition);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.HostRule, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true, multiplayerRuleGroup: groupIndex);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRulesPresetRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesPreset",
                MultiplayerRulesLocalization.PresetSetting,
                MultiplayerRulesLocalization.PresetHelp, 2,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRulesPresetOption>().Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.HostRule, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRulesParticipantCountRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesParticipantCount",
                MultiplayerRulesLocalization.ParticipantCountSetting,
                MultiplayerRulesLocalization.ParticipantCountHelp, 4,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRulesParticipantCountOption>()
                .Configure(box, valueText);
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRuleGroupRow(
            UI_OptionBox_PartyMemberDamage template, Transform section,
            OptionsCategoryController controller)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRuleGroup",
                MultiplayerRulesLocalization.RuleGroupSetting,
                MultiplayerRulesLocalization.RuleGroupHelp, 7,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRuleGroupOption>().Configure(
                box, valueText, controller);
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRulesExternalStackingRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesExternalStacking",
                MultiplayerRulesLocalization.ExternalRuleStackingSetting,
                MultiplayerRulesLocalization.ExternalRuleStackingHelp, 3,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRulesExternalStackingOption>()
                .Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.HostRule, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRulesHealthCombinationRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesHealthCombination",
                MultiplayerRulesLocalization.HealthCombinationSetting,
                MultiplayerRulesLocalization.HealthCombinationHelp, 6,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRulesHealthCombinationOption>()
                .Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.HostRule, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true);
            row.SetActive(true);
        }

        private static void CreateMultiplayerRulesCopyParticipantValuesRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MultiplayerRulesCopyParticipantValues",
                MultiplayerRulesLocalization.CopyParticipantValuesSetting,
                MultiplayerRulesLocalization.CopyParticipantValuesHelp, 5,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MultiplayerRulesCopyParticipantValuesOption>()
                .Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.HostRule, template.valueText.text);
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true);
            row.SetActive(true);
        }
    }

    internal sealed class MultiplayerRulesPresetOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = MultiplayerRulesLocalization.PresetKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;

            bool explorationActive = MultiplayerRulesController.TryGetActivePreset(
                out MultiplayerRulesPreset preset);
            if (!explorationActive)
            {
                preset = PreferredMultiplayerRulesStore.Read().Preset;
            }

            int value = (int)preset;
            box.ChangeValueWithoutNotify(value);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                !explorationActive &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences());
            valueText?.UpdateKey(MultiplayerRulesLocalization.PresetKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            if (MultiplayerRulesController.TryGetActivePreset(out var activePreset) ||
                !MultiplayerRulesOptionsRefresh.CanEditHostPreferences())
            {
                int activeValue = MultiplayerRulesController.TryGetActivePreset(
                    out activePreset) ? (int)activePreset :
                    (int)PreferredMultiplayerRulesStore.Read().Preset;
                box.ChangeValueWithoutNotify(activeValue);
                valueText?.UpdateKey(
                    MultiplayerRulesLocalization.PresetKeys[activeValue]);
                return;
            }

            MultiplayerRulesPreset preset = value >= 0 && value <= 2
                ? (MultiplayerRulesPreset)value
                : MultiplayerRulesPreset.Original;
            PreferredMultiplayerRulesStore.WritePreset(preset);
            PreferredMultiplayerRulesStore.Save();
            valueText?.UpdateKey(MultiplayerRulesLocalization.PresetKeys[(int)preset]);
            MultiplayerRulesOptionsRefresh.Refresh(transform.parent);
        }
    }

    internal sealed class MultiplayerRulesExternalStackingOption : MonoBehaviour
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
            bool enabled = PreferredMultiplayerRulesStore.
                ReadAllowExternalRuleStacking();
            box.ChangeValueWithoutNotify(enabled ? 1 : 0);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences());
            valueText?.UpdateKey(enabled
                ? MultiplayerRulesLocalization.ToggleEnabled
                : MultiplayerRulesLocalization.ToggleDisabled);
        }

        private void Changed(int value)
        {
            if (!MultiplayerRulesOptionsRefresh.CanEditHostPreferences())
            {
                Refresh();
                return;
            }
            bool enabled = value != 0;
            PreferredMultiplayerRulesStore.WriteAllowExternalRuleStacking(enabled);
            PreferredMultiplayerRulesStore.Save();
            valueText?.UpdateKey(enabled
                ? MultiplayerRulesLocalization.ToggleEnabled
                : MultiplayerRulesLocalization.ToggleDisabled);
        }
    }

    internal sealed class MultiplayerRulesParticipantCountOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 4;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        internal void Refresh()
        {
            if (box == null) return;
            int participantCount =
                MultiplayerRulesOptionsRefresh.EditedParticipantCount;
            box.ChangeValueWithoutNotify(participantCount - 1);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                interactive: true);
            valueText?.UpdateKey(
                MultiplayerRulesLocalization.ParticipantCountValueKey(
                    participantCount));
        }

        private void Changed(int value)
        {
            MultiplayerRulesOptionsRefresh.EditedParticipantCount =
                Mathf.Clamp(value + 1, 1, 4);
            MultiplayerRulesOptionsRefresh.Refresh(transform.parent);
        }
    }

    internal sealed class MultiplayerRuleGroupOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        private OptionsCategoryController controller;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text, OptionsCategoryController owner)
        {
            box = selectionBox;
            valueText = text;
            controller = owner;
        }

        private void OnEnable()
        {
            if (box == null || controller == null) return;
            int count = MultiplayerRulePresentationGroups.All.Count;
            box.numberOfElements = count;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = count == 0 ? 0 : Mathf.Clamp(
                controller.SelectedMultiplayerRuleGroup, 0, count - 1);
            box.ChangeValueWithoutNotify(value);
            if (count > 0)
            {
                valueText?.UpdateKey(MultiplayerRulePresentationGroups.All[value].
                    LocalizationKey);
            }
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            int count = MultiplayerRulePresentationGroups.All.Count;
            if (count == 0) return;
            int groupIndex = Mathf.Clamp(value, 0, count - 1);
            valueText?.UpdateKey(MultiplayerRulePresentationGroups.All[groupIndex].
                LocalizationKey);
            controller?.SelectMultiplayerRuleGroup(groupIndex);
        }
    }

    internal sealed class MultiplayerRuleOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        private MultiplayerRuleDefinition definition;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text, MultiplayerRuleDefinition ruleDefinition)
        {
            box = selectionBox;
            valueText = text;
            definition = ruleDefinition;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements =
                MultiplayerRulesLocalization.NumericValueCount(definition) + 1;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Clamp;
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        internal void Refresh()
        {
            if (box == null) return;
            int participantCount =
                MultiplayerRulesOptionsRefresh.EditedParticipantCount;
            bool active = MultiplayerRulesController.TryGetDisplayedActiveRules(
                out ActiveExplorationMultiplayerRules activeRules);
            PreferredMultiplayerRules preferred = PreferredMultiplayerRulesStore.Read();
            ActiveExplorationMultiplayerRules displayedRules = active
                ? activeRules : preferred.Freeze();
            MultiplayerRulesPreset preset = displayedRules.Preset;
            MultiplayerRuleSnapshot rules = displayedRules.Rules;
            MultiplayerRuleValue<float> configured = rules.Get(definition.Id,
                participantCount);
            int selection = 0;
            if (configured.TryGetOverride(out float overrideValue))
            {
                selection = 1 + Mathf.RoundToInt(
                    (overrideValue - definition.Minimum) / definition.Step);
            }
            box.ChangeValueWithoutNotify(selection);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                !active &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences() &&
                preset == MultiplayerRulesPreset.Custom);
            valueText?.UpdateKey(selection == 0
                ? MultiplayerRulesLocalization.UseGameBehavior
                : MultiplayerRulesLocalization.NumericValueKey(definition,
                    selection - 1));
        }

        private void Changed(int value)
        {
            if (MultiplayerRulesController.TryGetActivePreset(out _) ||
                !MultiplayerRulesOptionsRefresh.CanEditHostPreferences() ||
                PreferredMultiplayerRulesStore.Read().Preset !=
                    MultiplayerRulesPreset.Custom)
            {
                Refresh();
                return;
            }
            MultiplayerRuleValue<float> configured = value <= 0
                ? MultiplayerRuleValue<float>.UseGameBehavior()
                : MultiplayerRuleValue<float>.Override(definition.Minimum +
                    definition.Step * (value - 1));
            PreferredMultiplayerRulesStore.WriteCustomValue(definition.Id,
                MultiplayerRulesOptionsRefresh.EditedParticipantCount, configured);
            PreferredMultiplayerRulesStore.Save();
            Refresh();
        }
    }

    internal sealed class MultiplayerRulesCopyParticipantValuesOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = 5;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Clamp;
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        internal void Refresh()
        {
            if (box == null) return;
            box.ChangeValueWithoutNotify(0);
            bool active = MultiplayerRulesController.TryGetActivePreset(out _);
            bool custom = PreferredMultiplayerRulesStore.Read().Preset ==
                MultiplayerRulesPreset.Custom;
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                !active && custom &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences());
            valueText?.UpdateKey(MultiplayerRulesLocalization.SelectCopyTarget);
        }

        private void Changed(int targetParticipantCount)
        {
            int sourceParticipantCount =
                MultiplayerRulesOptionsRefresh.EditedParticipantCount;
            bool canCopy = targetParticipantCount >= 1 &&
                targetParticipantCount <= 4 &&
                !MultiplayerRulesController.TryGetActivePreset(out _) &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences() &&
                PreferredMultiplayerRulesStore.Read().Preset ==
                    MultiplayerRulesPreset.Custom;
            if (canCopy && targetParticipantCount != sourceParticipantCount)
            {
                PreferredMultiplayerRulesStore.CopyCustomParticipantValues(
                    sourceParticipantCount, targetParticipantCount);
                PreferredMultiplayerRulesStore.Save();
            }
            MultiplayerRulesOptionsRefresh.Refresh(transform.parent);
        }
    }

    internal sealed class MultiplayerRulesHealthCombinationOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox,
            UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = MultiplayerRulesLocalization.HealthCombinationKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            Refresh();
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        internal void Refresh()
        {
            if (box == null) return;
            bool active = MultiplayerRulesController.TryGetDisplayedActiveRules(
                out ActiveExplorationMultiplayerRules activeRules);
            PreferredMultiplayerRules preferred = PreferredMultiplayerRulesStore.Read();
            ActiveExplorationMultiplayerRules displayedRules = active
                ? activeRules : preferred.Freeze();
            MultiplayerRulesPreset preset = displayedRules.Preset;
            EnemyHealthModifierCombination combination =
                displayedRules.HealthModifierCombination;
            int value = (int)combination;
            box.ChangeValueWithoutNotify(value);
            NativeHorizontalSelectionOptionState.Apply(gameObject, box,
                !active &&
                MultiplayerRulesOptionsRefresh.CanEditHostPreferences() &&
                preset == MultiplayerRulesPreset.Custom);
            valueText?.UpdateKey(
                MultiplayerRulesLocalization.HealthCombinationKeys[value]);
        }

        private void Changed(int value)
        {
            if (MultiplayerRulesController.TryGetActivePreset(out _) ||
                !MultiplayerRulesOptionsRefresh.CanEditHostPreferences() ||
                PreferredMultiplayerRulesStore.Read().Preset !=
                    MultiplayerRulesPreset.Custom)
            {
                Refresh();
                return;
            }
            EnemyHealthModifierCombination combination = value >= 0 && value <= 2
                ? (EnemyHealthModifierCombination)value
                : EnemyHealthModifierCombination.ParticipantRuleOnly;
            PreferredMultiplayerRulesStore.WriteCustomHealthCombination(combination);
            PreferredMultiplayerRulesStore.Save();
            Refresh();
        }
    }

    internal static class MultiplayerRulesOptionsRefresh
    {
        internal static int EditedParticipantCount { get; set; } = 1;

        internal static MultiplayerRulesPreset DisplayedPreset()
        {
            return MultiplayerRulesController.TryGetActivePreset(
                out MultiplayerRulesPreset preset)
                ? preset : PreferredMultiplayerRulesStore.Read().Preset;
        }

        internal static bool CanEditHostPreferences() =>
            NativeSettingsInteraction.CanEdit(SettingInteractionKind.HostRule);

        internal static void Refresh(Transform parent)
        {
            if (parent == null) return;
            foreach (MultiplayerRulesParticipantCountOption option in
                parent.GetComponentsInChildren<MultiplayerRulesParticipantCountOption>(true))
                option.Refresh();
            foreach (MultiplayerRulesCopyParticipantValuesOption option in
                parent.GetComponentsInChildren<MultiplayerRulesCopyParticipantValuesOption>(true))
                option.Refresh();
            foreach (MultiplayerRulesHealthCombinationOption option in
                parent.GetComponentsInChildren<MultiplayerRulesHealthCombinationOption>(true))
                option.Refresh();
            foreach (MultiplayerRuleOption option in
                parent.GetComponentsInChildren<MultiplayerRuleOption>(true))
                option.Refresh();
            parent.GetComponentInParent<OptionsCategoryController>()?.
                RefreshVisibility();
        }
    }
}
