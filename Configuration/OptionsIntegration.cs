using SephiriaEnhancements.Diagnostics;
using HarmonyLib;
using SephiriaEnhancements.CombatRelationOutlines;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.CombatTargeting;
using SephiriaEnhancements.ViewDistance;
using SephiriaEnhancements.NativeCompanion;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.DeveloperConsole;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using SephiriaEnhancements.DeveloperTools;
#endif
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.MapEnhancements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Mirror;

namespace SephiriaEnhancements.Configuration
{
    [HarmonyPatch(typeof(UI_OptionsPanel), "OnOpened")]
    internal static class OptionsPanelPatch
    {
        private static void Postfix(UI_OptionsPanel __instance)
        {
            try
            {
                Inject(__instance);
            }
            catch (System.Exception ex)
            {
                SupportLogger.Warning("settings_integration_failed", "[SephiriaEnhancements] Native settings integration disabled: " +
                    ex.Message);
            }
        }

        private static void Inject(UI_OptionsPanel __instance)
        {
            try
            {
                NativeControlOptionsIntegration.Inject(__instance);
            }
            catch (System.Exception ex)
            {
                SupportLogger.Warning("control_rows_failed", "[SephiriaEnhancements] Native control rows " +
                    "could not be attached: " + ex.Message);
            }

            UI_OptionBox_PartyMemberDamage template =
                __instance.GetComponentInChildren<UI_OptionBox_PartyMemberDamage>(true);
            if (template == null)
            {
                SupportLogger.Warning("settings_template_unavailable", "[SephiriaEnhancements] Native options template not found; " +
                    "default settings remain active.");
                return;
            }

            OptionsSectionMarker section =
                __instance.GetComponentInChildren<OptionsSectionMarker>(true);
            if (section == null)
            {
                section = CreateSectionHeader(template);
            }
            if (section == null)
            {
                SupportLogger.Warning("settings_header_unavailable", "[SephiriaEnhancements] Native section-header template not found; " +
                    "settings rows were not injected; default settings remain active.");
                return;
            }

            OptionsCategoryController categoryController =
                __instance.GetComponent<OptionsCategoryController>();
            if (categoryController == null)
            {
                categoryController = __instance.gameObject.AddComponent<
                    OptionsCategoryController>();
            }
            categoryController.Configure(__instance, template, section.transform.parent);

            if (__instance.GetComponentInChildren<OptionsCategoryOption>(true) == null)
            {
                CreateOptionsCategoryRow(template, section.transform,
                    categoryController);
            }

            if (__instance.GetComponentInChildren<MasterEnabledOption>(true) == null)
            {
                CreateMasterEnabledRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<DisplayPolicyOption>(true) == null)
            {
                CreateDisplayPolicyRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<CombatRelationOutlinesOption>(true) == null)
            {
                CreateCombatRelationOutlinesRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<CombatVisualOption>(true) == null)
            {
                CreateCombatVisualRows(template, section.transform);
            }

            if (__instance.GetComponentInChildren<NativeCompanionOption>(true) == null)
            {
                CreateNativeCompanionRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<DefeatRetryOption>(true) == null)
            {
                CreateDefeatRetryRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<ShowHiddenRoomsOption>(true) == null)
            {
                CreateShowHiddenRoomsRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<HitStreakFeedbackOption>(true) == null)
            {
                CreateHitStreakFeedbackRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<DamageStatisticsScaleOption>(true) == null)
            {
                CreateDamageStatisticsScaleRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<TargetingModeOption>(true) == null)
            {
                CreateTargetingModeRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<MouseAimAssistOption>(true) == null)
            {
                CreateMouseAimAssistRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<ViewDistanceOption>(true) == null)
            {
                CreateViewDistanceRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<DeveloperConsoleOption>(true) == null)
            {
                CreateDeveloperConsoleRow(template, section.transform);
            }

            if (__instance.GetComponentInChildren<InventoryOptimizationTendencyOption>(
                    true) == null)
            {
                CreateInventoryOptimizationTendencyRow(template,
                    section.transform);
            }

#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            if (__instance.GetComponentInChildren<InventoryReproductionOption>(true) == null)
            {
                GameObject row = CloneRow(template, section.transform,
                    "Option_SephiriaEnhancements_InventoryReproduction",
                    Diagnostics.InventoryReproductionLocalization.Setting,
                    Diagnostics.InventoryReproductionLocalization.Help, 14,
                    out UI_HorizontalSelectionBox box, out UI_LocalizationStringText text);
                row.AddComponent<InventoryReproductionOption>().Configure(box, text);
                MarkCategory(row, OptionsCategory.General);
                row.SetActive(true);
            }
            if (__instance.GetComponentInChildren<DeveloperPlayerDamageOption>(true) ==
                null)
            {
                CreateDeveloperPlayerDamageRow(template, section.transform);
            }
#endif

            MultiplayerSectionMarker multiplayerSection =
                __instance.GetComponentInChildren<MultiplayerSectionMarker>(true);
            if (multiplayerSection == null)
            {
                multiplayerSection = CreateMultiplayerSectionHeader(section);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<MidRunAdmissionOption>(true) == null)
            {
                CreateMidRunAdmissionRow(template, multiplayerSection.transform);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<MultiplayerRulesPresetOption>(true) == null)
            {
                CreateMultiplayerRulesPresetRow(template,
                    multiplayerSection.transform);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<
                    MultiplayerRulesExternalStackingOption>(true) == null)
            {
                CreateMultiplayerRulesExternalStackingRow(template,
                    multiplayerSection.transform);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<
                    MultiplayerRulesParticipantCountOption>(true) == null)
            {
                CreateMultiplayerRulesParticipantCountRow(template,
                    multiplayerSection.transform);
                CreateMultiplayerRulesCopyParticipantValuesRow(template,
                    multiplayerSection.transform);
                CreateMultiplayerRulesHealthCombinationRow(template,
                    multiplayerSection.transform);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<MultiplayerRuleGroupOption>(true) ==
                    null)
            {
                CreateMultiplayerRuleGroupRow(template,
                    multiplayerSection.transform, categoryController);
            }

            if (multiplayerSection != null &&
                __instance.GetComponentInChildren<MultiplayerRuleOption>(true) == null)
            {
                int offset = 8;
                int groupIndex = 0;
                foreach (MultiplayerRulePresentationGroup group in
                    MultiplayerRulePresentationGroups.All)
                {
                    foreach (MultiplayerRuleId ruleId in group.RuleIds)
                    {
                        CreateMultiplayerRuleRow(template,
                            multiplayerSection.transform,
                            MultiplayerRuleCatalog.Get(ruleId), groupIndex,
                            offset++);
                    }
                    groupIndex++;
                }
            }

            NormalizeInjectedOrder(section, multiplayerSection);
            categoryController.RefreshVisibility();
        }

        private static void NormalizeInjectedOrder(OptionsSectionMarker section,
            MultiplayerSectionMarker multiplayerSection)
        {
            Transform parent = section.transform.parent;
            int siblingIndex = section.transform.GetSiblingIndex() + 1;
            string[] mainRows =
            {
                "Option_SephiriaEnhancements_Enabled",
                "Option_SephiriaEnhancements_Category",
                "Option_SephiriaEnhancements_NativeCompanion",
                "Option_SephiriaEnhancements_DefeatRetry",
                "Option_SephiriaEnhancements_ShowHiddenRooms",
                "Option_SephiriaEnhancements_DeveloperConsole",
                "Option_SephiriaEnhancements_DeveloperPlayerDamage",
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                "Option_SephiriaEnhancements_InventoryReproduction",
#endif
                "Option_SephiriaEnhancements_CombatRelationOutlines",
                "Option_SephiriaEnhancements_CombatVisualPreset",
                "Option_SephiriaEnhancements_CompanionBody",
                "Option_SephiriaEnhancements_CompanionEffects",
                "Option_SephiriaEnhancements_CombatOutlineScope",
                "Option_SephiriaEnhancements_DisplayPolicy",
                "Option_SephiriaEnhancements_HitStreakFeedback",
                "Option_SephiriaEnhancements_DamageStatisticsScale",
                "Option_SephiriaEnhancements_TargetingMode",
                "Option_SephiriaEnhancements_MouseAimAssist",
                "Option_SephiriaEnhancements_ViewDistance",
                "Option_SephiriaEnhancements_InventoryOptimizationTendency"
            };
            foreach (string rowName in mainRows)
            {
                MoveInjectedChild(parent, rowName, ref siblingIndex);
            }

            if (multiplayerSection == null)
            {
                return;
            }

            multiplayerSection.transform.SetSiblingIndex(siblingIndex++);
            string[] multiplayerRows =
            {
                "Option_SephiriaEnhancements_MidRunAdmission",
                "Option_SephiriaEnhancements_MultiplayerRulesPreset",
                "Option_SephiriaEnhancements_MultiplayerRulesExternalStacking",
                "Option_SephiriaEnhancements_MultiplayerRulesParticipantCount",
                "Option_SephiriaEnhancements_MultiplayerRulesCopyParticipantValues",
                "Option_SephiriaEnhancements_MultiplayerRulesHealthCombination",
                "Option_SephiriaEnhancements_MultiplayerRuleGroup"
            };
            foreach (string rowName in multiplayerRows)
            {
                MoveInjectedChild(parent, rowName, ref siblingIndex);
            }
            foreach (MultiplayerRulePresentationGroup group in
                MultiplayerRulePresentationGroups.All)
            {
                foreach (MultiplayerRuleId ruleId in group.RuleIds)
                {
                    MoveInjectedChild(parent,
                        "Option_SephiriaEnhancements_MultiplayerRule_" + ruleId,
                        ref siblingIndex);
                }
            }
        }

        private static void MoveInjectedChild(Transform parent, string name,
            ref int siblingIndex)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                child.SetSiblingIndex(siblingIndex++);
            }
        }

        private static OptionsSectionMarker CreateSectionHeader(
            UI_OptionBox_PartyMemberDamage template)
        {
            Transform parent = template.transform.parent;
            Transform source = FindPreviousSectionHeader(parent,
                template.transform.GetSiblingIndex());
            if (source == null)
            {
                return null;
            }

            GameObject header = Object.Instantiate(source.gameObject, parent);
            header.name = "Section_SephiriaEnhancements";
            header.SetActive(false);
            UI_LocalizationStringText[] labels =
                header.GetComponentsInChildren<UI_LocalizationStringText>(true);
            for (int index = 0; index < labels.Length; index++)
            {
                labels[index].UpdateKey(ModLocalization.Section);
            }
            OptionsSectionMarker marker = header.AddComponent<OptionsSectionMarker>();
            header.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            header.SetActive(true);
            SupportLogger.Info("combat_settings_attached", "[SephiriaEnhancements] Native Combat Insights settings section attached.");
            return marker;
        }

        private static Transform FindPreviousSectionHeader(Transform parent, int beforeIndex)
        {
            for (int index = beforeIndex - 1; index >= 0; index--)
            {
                Transform candidate = parent.GetChild(index);
                if (candidate.GetComponentInChildren<UI_HorizontalSelectionBox>(true) != null)
                {
                    continue;
                }
                if (candidate.GetComponentInChildren<UI_LocalizationStringText>(true) != null)
                {
                    return candidate;
                }
            }
            return null;
        }

        internal static void WireNavigation(UI_OptionsPanel panel,
            UI_OptionBox_PartyMemberDamage template)
        {
            OptionsNavigationState state = panel.GetComponent<OptionsNavigationState>();
            if (state == null)
            {
                state = panel.gameObject.AddComponent<OptionsNavigationState>();
                state.OriginalDown = template.box.forceNavDown;
            }

            var chain = new List<UI_HorizontalSelectionBox> { template.box };
            var entries = new List<ModOptionsNavigationEntry>(
                panel.GetComponentsInChildren<ModOptionsNavigationEntry>(true));
            entries.Sort((left, right) => left.transform.GetSiblingIndex()
                .CompareTo(right.transform.GetSiblingIndex()));
            foreach (ModOptionsNavigationEntry entry in entries)
            {
                if (entry.Box != null && entry.gameObject.activeInHierarchy)
                {
                    chain.Add(entry.Box);
                }
            }
            for (int index = 0; index < chain.Count; index++)
            {
                UI_HorizontalSelectionBox current = chain[index];
                if (current == null) continue;
                current.forceNavUp = index > 0 ? chain[index - 1] : current.forceNavUp;
                current.forceNavDown = index + 1 < chain.Count ? chain[index + 1] : state.OriginalDown;
            }

            if (state.OriginalDown is UI_HorizontalSelectionBox downstream)
            {
                downstream.forceNavUp = chain[chain.Count - 1];
            }
        }

        private static GameObject CloneRow(UI_OptionBox_PartyMemberDamage template,
            Transform section, string name, string labelKey, string helpKey, int siblingOffset,
            out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText)
        {
            GameObject row = Object.Instantiate(template.gameObject, template.transform.parent);
            row.name = name;
            row.SetActive(false);

            UI_OptionBox_PartyMemberDamage old = row.GetComponent<UI_OptionBox_PartyMemberDamage>();
            box = old.box;
            valueText = old.valueText;
            Object.DestroyImmediate(old);

            UI_LocalizationStringText[] labels = row.GetComponentsInChildren<UI_LocalizationStringText>(true);
            foreach (UI_LocalizationStringText label in labels)
            {
                if (label != valueText)
                {
                    label.UpdateKey(labelKey);
                    break;
                }
            }

            GameObject helpTarget = box != null ? box.gameObject : row;
            UI_CommonTooltipOpener help = helpTarget.AddComponent<UI_CommonTooltipOpener>();
            help.tooltipName = new LocalizedString(labelKey);
            help.tooltipContext = new LocalizedString(helpKey);
            help.offset = new Vector2(12f, 0f);
            help.UpdateTooltipData();

            row.transform.SetSiblingIndex(section.GetSiblingIndex() + siblingOffset);
            row.AddComponent<ModOptionsNavigationEntry>().Configure(box);
            return row;
        }

        private static void CreateOptionsCategoryRow(
            UI_OptionBox_PartyMemberDamage template, Transform section,
            OptionsCategoryController controller)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_Category",
                OptionsCategoryLocalization.Setting,
                OptionsCategoryLocalization.Help, 2,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<OptionsCategoryOption>().Configure(
                box, valueText, controller);
            row.SetActive(true);
        }

        private static void MarkCategory(GameObject target,
            OptionsCategory category, bool requiresCustomPreset = false,
            int multiplayerRuleGroup = -1)
        {
            target.AddComponent<OptionsCategoryMember>().Configure(category,
                requiresCustomPreset, multiplayerRuleGroup);
        }

        private static void CreateMasterEnabledRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_Enabled",
                ModLocalization.SettingMasterEnabled, ModLocalization.HelpMasterEnabled, 1,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MasterEnabledOption>().Configure(box, valueText);
            // The suite switch owns every category and therefore remains visible.
            row.SetActive(true);
        }

        private static void CreateCombatRelationOutlinesRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_CombatRelationOutlines",
                ModLocalization.SettingCombatRelationOutlines,
                ModLocalization.HelpCombatRelationOutlines, 3,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<CombatRelationOutlinesOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
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

        private static void CreateNativeCompanionRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_NativeCompanion",
                ModLocalization.SettingNativeCompanion,
                ModLocalization.HelpNativeCompanion, 4,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<NativeCompanionOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }

        private static void CreateHitStreakFeedbackRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_HitStreakFeedback",
                ModLocalization.SettingHitStreakFeedback,
                ModLocalization.HelpHitStreakFeedback, 7,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<HitStreakFeedbackOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }

        private static void CreateDamageStatisticsScaleRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DamageStatisticsScale",
                ModLocalization.SettingDamageStatisticsScale,
                ModLocalization.HelpDamageStatisticsScale, 8,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DamageStatisticsScaleOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
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

        private static void CreateShowHiddenRoomsRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_ShowHiddenRooms",
                MapEnhancementsLocalization.SettingShowHiddenRooms,
                MapEnhancementsLocalization.HelpShowHiddenRooms, 5,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<ShowHiddenRoomsOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }

        private static void CreateDisplayPolicyRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_DisplayPolicy",
                ModLocalization.SettingDisplayPolicy, ModLocalization.HelpDisplayPolicy, 6,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<DisplayPolicyOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.CombatAndDisplay);
            row.SetActive(true);
        }

        private static void CreateTargetingModeRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_TargetingMode",
                ControlLocalization.SettingTargetingMode,
                ControlLocalization.HelpTargetingMode, 9,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<TargetingModeOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.ControlsAndCamera);
            row.SetActive(true);
        }

        private static void CreateMouseAimAssistRow(
            UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_MouseAimAssist",
                ControlLocalization.SettingMouseAimAssist,
                ControlLocalization.HelpMouseAimAssist, 10,
                out UI_HorizontalSelectionBox box, out UI_LocalizationStringText valueText);
            row.AddComponent<MouseAimAssistOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.ControlsAndCamera);
            row.SetActive(true);
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

        private static void CreateDeveloperConsoleRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DeveloperConsole",
                ModLocalization.SettingDeveloperConsole,
                ModLocalization.HelpDeveloperConsole, 12,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DeveloperConsoleOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }

        private static void CreateInventoryOptimizationTendencyRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_InventoryOptimizationTendency",
                InventoryOptimizationLocalization.SettingOptimizationTendency,
                InventoryOptimizationLocalization.HelpOptimizationTendency, 14,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<InventoryOptimizationTendencyOption>().Configure(
                box, valueText);
            MarkCategory(row, OptionsCategory.InventoryArrangement);
            row.SetActive(true);
        }

#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
        private static void CreateDeveloperPlayerDamageRow(
            UI_OptionBox_PartyMemberDamage template, Transform section)
        {
            GameObject row = CloneRow(template, section,
                "Option_SephiriaEnhancements_DeveloperPlayerDamage",
                ModLocalization.SettingDeveloperPlayerDamage,
                ModLocalization.HelpDeveloperPlayerDamage, 13,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<DeveloperPlayerDamageOption>().Configure(box, valueText);
            MarkCategory(row, OptionsCategory.General);
            row.SetActive(true);
        }
#endif

        private static MultiplayerSectionMarker
            CreateMultiplayerSectionHeader(OptionsSectionMarker source)
        {
            GameObject header = Object.Instantiate(source.gameObject,
                source.transform.parent);
            header.name = "Section_MultiplayerRules";
            header.SetActive(false);
            OptionsSectionMarker copiedMarker = header.GetComponent<OptionsSectionMarker>();
            if (copiedMarker != null) Object.DestroyImmediate(copiedMarker);
            UI_LocalizationStringText[] labels =
                header.GetComponentsInChildren<UI_LocalizationStringText>(true);
            foreach (UI_LocalizationStringText label in labels)
            {
                label.UpdateKey(MultiplayerRulesLocalization.Section);
            }

            MultiplayerSectionMarker marker =
                header.AddComponent<MultiplayerSectionMarker>();
            MarkCategory(header, OptionsCategory.Multiplayer);
            int siblingIndex = source.transform.GetSiblingIndex() + 1;
            Transform parent = source.transform.parent;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name.StartsWith("Option_SephiriaEnhancements_",
                        System.StringComparison.Ordinal))
                {
                    siblingIndex = System.Math.Max(siblingIndex,
                        child.GetSiblingIndex() + 1);
                }
            }
            header.transform.SetSiblingIndex(siblingIndex);
            header.SetActive(true);
            return marker;
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
            MarkCategory(row, OptionsCategory.Multiplayer);
            row.SetActive(true);
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
            MarkCategory(row, OptionsCategory.Multiplayer,
                requiresCustomPreset: true, multiplayerRuleGroup: groupIndex);
            row.SetActive(true);
        }

    }

    [HarmonyPatch(typeof(UI_OptionsPanel), "OnClosed")]
    internal static class NativeControlOptionsClosedPatch
    {
        private static void Postfix()
        {
            try
            {
                NativeControlCoordinator.ReloadOfficialBindings();
            }
            catch (System.Exception ex)
            {
                SupportLogger.Warning("controls_restart_required", "[SephiriaEnhancements] Updated native controls " +
                    "will take effect after restart: " + ex.Message);
            }
        }
    }

    internal sealed class OptionsSectionMarker : MonoBehaviour
    {
    }

    internal sealed class MultiplayerSectionMarker : MonoBehaviour
    {
    }

    internal sealed class ModOptionsNavigationEntry : MonoBehaviour
    {
        internal UI_HorizontalSelectionBox Box { get; private set; }

        internal void Configure(UI_HorizontalSelectionBox box)
        {
            Box = box;
        }
    }

    internal sealed class OptionsCategoryMember : MonoBehaviour
    {
        internal OptionsCategory Category { get; private set; }
        internal bool RequiresCustomPreset { get; private set; }
        internal int MultiplayerRuleGroup { get; private set; } = -1;

        internal void Configure(OptionsCategory category,
            bool requiresCustomPreset, int multiplayerRuleGroup)
        {
            Category = category;
            RequiresCustomPreset = requiresCustomPreset;
            MultiplayerRuleGroup = multiplayerRuleGroup;
        }
    }

    internal sealed class OptionsCategoryController : MonoBehaviour
    {
        private UI_OptionsPanel panel;
        private UI_OptionBox_PartyMemberDamage template;
        private RectTransform content;
        private UI_HorizontalSelectionBox categoryBox;

        internal OptionsCategory SelectedCategory { get; private set; } =
            OptionsCategory.General;
        internal int SelectedMultiplayerRuleGroup { get; private set; }

        internal void Configure(UI_OptionsPanel optionsPanel,
            UI_OptionBox_PartyMemberDamage rowTemplate, Transform contentTransform)
        {
            panel = optionsPanel;
            template = rowTemplate;
            content = contentTransform as RectTransform;
        }

        internal void RegisterCategoryBox(UI_HorizontalSelectionBox box)
        {
            categoryBox = box;
        }

        internal void SelectCategory(OptionsCategory category)
        {
            if ((int)category < 0 ||
                (int)category >= OptionsCategoryLocalization.CategoryKeys.Length)
            {
                category = OptionsCategory.General;
            }
            SelectedCategory = category;
            RefreshVisibility();
        }

        internal void SelectMultiplayerRuleGroup(int groupIndex)
        {
            int count = MultiplayerRulePresentationGroups.All.Count;
            SelectedMultiplayerRuleGroup = count == 0
                ? 0 : Mathf.Clamp(groupIndex, 0, count - 1);
            RefreshVisibility();
        }

        internal void RefreshVisibility()
        {
            if (panel == null || template == null) return;

            Vector2 anchoredPosition = content != null
                ? content.anchoredPosition : Vector2.zero;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool selectedWillHide = false;
            OptionsCategoryMember[] members =
                panel.GetComponentsInChildren<OptionsCategoryMember>(true);
            MultiplayerRulesPreset displayedPreset =
                MultiplayerRulesOptionsRefresh.DisplayedPreset();
            for (int index = 0; index < members.Length; index++)
            {
                OptionsCategoryMember member = members[index];
                bool visible = OptionsCategoryVisibility.IsVisible(
                    member.Category, SelectedCategory,
                    member.RequiresCustomPreset,
                    displayedPreset == MultiplayerRulesPreset.Custom,
                    member.MultiplayerRuleGroup,
                    SelectedMultiplayerRuleGroup);

                if (!visible && selected != null &&
                    (selected == member.gameObject ||
                     selected.transform.IsChildOf(member.transform)))
                {
                    selectedWillHide = true;
                }
                if (member.gameObject.activeSelf != visible)
                {
                    member.gameObject.SetActive(visible);
                }
            }

            if (content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Canvas.ForceUpdateCanvases();
                // Rows below the category selector change the content height. Keep
                // the current viewport anchor so the selected row does not jump.
                content.anchoredPosition = anchoredPosition;
            }
            OptionsPanelPatch.WireNavigation(panel, template);

            if (selectedWillHide && EventSystem.current != null &&
                categoryBox != null && categoryBox.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(categoryBox.gameObject);
            }

        }
    }

    internal sealed class OptionsCategoryOption : MonoBehaviour
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
            controller?.RegisterCategoryBox(box);
        }

        private void OnEnable()
        {
            if (box == null || controller == null) return;
            box.numberOfElements = OptionsCategoryLocalization.CategoryKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)controller.SelectedCategory;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(
                OptionsCategoryLocalization.CategoryKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            OptionsCategory category = value >= 0 &&
                value < OptionsCategoryLocalization.CategoryKeys.Length
                ? (OptionsCategory)value : OptionsCategory.General;
            valueText?.UpdateKey(
                OptionsCategoryLocalization.CategoryKeys[(int)category]);
            controller?.SelectCategory(category);
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
            MultiplayerRulesLifecyclePolicy.CanEditHostPreferences(
                !NetworkClient.active || NetworkServer.active,
                MultiplayerRulesController.TryGetActivePreset(out _));

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

    internal sealed class MasterEnabledOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = EnhancementsSettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? ModLocalization.SuiteOn : ModLocalization.SuiteOff);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            EnhancementsSettings.Enabled = value == 1;
            EnhancementsSettings.Save();
            CombatVisualRuntime.RefreshCompanionBodies();
            valueText?.UpdateKey(value == 1
                ? ModLocalization.SuiteOn : ModLocalization.SuiteOff);
        }
    }

    internal enum CombatVisualOptionKind
    {
        Preset,
        CompanionBody,
        CompanionEffects,
        OutlineScope
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

    internal sealed class CombatRelationOutlinesOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = CombatRelationOutlinesSettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            CombatRelationOutlinesSettings.Enabled = value == 1;
            CombatRelationOutlinesSettings.Save();
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }
    }

    internal sealed class NativeCompanionOption : MonoBehaviour
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
            box.numberOfElements = NativeCompanionSettings.ModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)NativeCompanionSettings.Mode;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.NativeCompanionModeKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            NativeCompanionSettings.Mode = (NativeCompanionMode)value;
            NativeCompanionSettings.Save();
            valueText?.UpdateKey(ModLocalization.NativeCompanionModeKeys[value]);
        }
    }

    internal sealed class HitStreakFeedbackOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = ModSettings.HitStreakFeedback ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            ModSettings.HitStreakFeedback = value == 1;
            ModSettings.Save();
            valueText?.UpdateKey(value == 1 ? ModLocalization.On : ModLocalization.Off);
        }
    }

    internal sealed class DefeatRetryOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = DefeatRetrySettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DefeatRetryOn
                : ModLocalization.DefeatRetryOff);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            DefeatRetrySettings.Enabled = value == 1;
            DefeatRetrySettings.Save();
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DefeatRetryOn
                : ModLocalization.DefeatRetryOff);
        }
    }

    internal sealed class ShowHiddenRoomsOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = MapEnhancementsSettings.ShowHiddenRooms ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? MapEnhancementsLocalization.On
                : MapEnhancementsLocalization.Off);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            MapEnhancementsSettings.ShowHiddenRooms = value == 1;
            EnhancementsSettings.Save();
            valueText?.UpdateKey(value == 1
                ? MapEnhancementsLocalization.On
                : MapEnhancementsLocalization.Off);
        }
    }

    internal sealed class DamageStatisticsScaleOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;

        internal UI_HorizontalSelectionBox Box => box;

        internal void Configure(UI_HorizontalSelectionBox selectionBox, UI_LocalizationStringText text)
        {
            box = selectionBox;
            valueText = text;
        }

        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = ModSettings.DamageStatisticsScaleCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = ModSettings.DamageStatisticsScaleIndex;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.ScaleKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            ModSettings.DamageStatisticsScaleIndex = value;
            ModSettings.Save();
            valueText?.UpdateKey(ModLocalization.ScaleKeys[value]);
        }
    }

    internal sealed class DisplayPolicyOption : MonoBehaviour
    {
        private UI_HorizontalSelectionBox box;
        private UI_LocalizationStringText valueText;
        internal UI_HorizontalSelectionBox Box => box;
        internal void Configure(UI_HorizontalSelectionBox selectionBox, UI_LocalizationStringText text)
        { box = selectionBox; valueText = text; }
        private void OnEnable()
        {
            if (box == null) return;
            box.numberOfElements = ModLocalization.DisplayPolicyKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)ModSettings.DisplayPolicy;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.DisplayPolicyKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            ModSettings.DisplayPolicy = (CombatInsightsDisplayPolicy)value;
            ModSettings.Save();
            valueText?.UpdateKey(ModLocalization.DisplayPolicyKeys[value]);
        }
    }

    internal sealed class InventoryOptimizationTendencyOption : MonoBehaviour
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
            if (box == null)
            {
                return;
            }

            box.numberOfElements = InventoryOptimizationLocalization.
                OptimizationTendencyKeys.Length;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)ModSettings.InventoryOptimizationTendency;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(InventoryOptimizationLocalization.
                OptimizationTendencyKeys[value]);
        }

        private void OnDisable()
        {
            if (box != null)
            {
                box.OnValueChanged -= Changed;
            }
        }

        private void Changed(int value)
        {
            ModSettings.InventoryOptimizationTendency =
                (InventoryOptimizationTendency)value;
            ModSettings.Save();
            valueText?.UpdateKey(InventoryOptimizationLocalization.
                OptimizationTendencyKeys[value]);
        }
    }

    internal sealed class TargetingModeOption : MonoBehaviour
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
            box.numberOfElements = CombatTargetingSettings.TargetingModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = (int)CombatTargetingSettings.TargetingMode;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ControlLocalization.TargetingModeKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            CombatTargetingSettings.TargetingMode = (TargetingMode)value;
            CombatTargetingSettings.Save();
            NativeControlCoordinator.OnTargetingSettingChanged(
                value != (int)TargetingMode.Disabled);
            valueText?.UpdateKey(ControlLocalization.TargetingModeKeys[value]);
        }
    }

    internal sealed class MouseAimAssistOption : MonoBehaviour
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
            box.numberOfElements = CombatTargetingSettings.MouseAimAssistModeCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = CombatTargetingSettings.MouseAimAssistEnabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ControlLocalization.MouseAimAssistKeys[value]);
        }
        private void OnDisable() { if (box != null) box.OnValueChanged -= Changed; }
        private void Changed(int value)
        {
            CombatTargetingSettings.MouseAimAssistEnabled = value == 1;
            CombatTargetingSettings.Save();
            valueText?.UpdateKey(ControlLocalization.MouseAimAssistKeys[value]);
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

    internal sealed class DeveloperConsoleOption : MonoBehaviour
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
            box.numberOfElements = 2;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = DeveloperConsoleSettings.Enabled ? 1 : 0;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DeveloperConsoleOn
                : ModLocalization.DeveloperConsoleOff);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            DeveloperConsoleSettings.Enabled = value == 1;
            DeveloperConsoleSettings.Save();
            valueText?.UpdateKey(value == 1
                ? ModLocalization.DeveloperConsoleOn
                : ModLocalization.DeveloperConsoleOff);
        }
    }

#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
    internal sealed class DeveloperPlayerDamageOption : MonoBehaviour
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
            box.numberOfElements = DeveloperPlayerDamageSettings.MultiplierCount;
            box.overflowType = UI_HorizontalSelectionBox.OverflowType.Repeat;
            box.OnValueChanged += Changed;
            int value = DeveloperPlayerDamageSettings.MultiplierIndex;
            box.ChangeValueWithoutNotify(value);
            valueText?.UpdateKey(ModLocalization.DeveloperPlayerDamageMultiplierKeys[
                value]);
        }

        private void OnDisable()
        {
            if (box != null) box.OnValueChanged -= Changed;
        }

        private void Changed(int value)
        {
            DeveloperPlayerDamageSettings.MultiplierIndex = value;
            DeveloperPlayerDamageSettings.Save();
            valueText?.UpdateKey(ModLocalization.DeveloperPlayerDamageMultiplierKeys[
                DeveloperPlayerDamageSettings.MultiplierIndex]);
        }
    }
#endif

    internal sealed class OptionsNavigationState : MonoBehaviour
    {
        internal Selectable OriginalDown { get; set; }
    }
}
