using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Diagnostics;
using HarmonyLib;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using SephiriaEnhancements.CombatInsights.Integration;
using SephiriaEnhancements.CombatRelationOutlines.Integration;
using SephiriaEnhancements.CombatVisuals.Integration;
using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.NativeCompanion.Integration;
using SephiriaEnhancements.DefeatRetry.Integration;
using SephiriaEnhancements.MapEnhancements.Integration;
using SephiriaEnhancements.ResourceBarValues.Integration;
using SephiriaEnhancements.CombatTargeting.Integration;
using SephiriaEnhancements.ViewDistance.Integration;
using SephiriaEnhancements.DeveloperConsole.Integration;
using SephiriaEnhancements.DeveloperTools.Integration;
using SephiriaEnhancements.MultiplayerAccess.Integration;
using static SephiriaEnhancements.Configuration.NativeOptionsRows;

namespace SephiriaEnhancements.Configuration
{
    [HarmonyPatch(typeof(UI_OptionsPanel), "OnOpened")]
    internal static class OptionsPanelPatch
    {
        private static void Postfix(UI_OptionsPanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Settings))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Settings, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_OptionsPanel __instance)
        {
            try
            {
                Inject(__instance);
            }
            catch (System.Exception ex)
            {
                SupportLogger.Warning("settings_integration_failed", "[SephiriaEnhancements] Native settings integration disabled: " + ex.Message);
            }
        }

        private static void Inject(UI_OptionsPanel __instance)
        {
            NativeOptionsLifetime.Track(__instance);
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

            CombatInsightsOptions.Inject(__instance, template, section.transform);
            CombatRelationOutlinesOptions.Inject(__instance, template, section.transform);
            CombatVisualsOptions.Inject(__instance, template, section.transform);
            NativeCompanionOptions.Inject(__instance, template, section.transform);
            DefeatRetryOptions.Inject(__instance, template, section.transform);
            MapEnhancementsOptions.Inject(__instance, template, section.transform);
            ResourceBarValuesOptions.Inject(__instance, template, section.transform);
            CombatTargetingOptions.Inject(__instance, template, section.transform);
            ViewDistanceOptions.Inject(__instance, template, section.transform);
            DeveloperConsoleOptions.Inject(__instance, template, section.transform);
            ModInformation.Integration.ModInformationOptions.Inject(__instance, template, section.transform);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            DeveloperToolsOptions.Inject(__instance, template, section.transform);
#endif

            MultiplayerSectionMarker multiplayerSection =
                __instance.GetComponentInChildren<MultiplayerSectionMarker>(true);
            if (multiplayerSection == null)
            {
                multiplayerSection = CreateMultiplayerSectionHeader(section);
            }

            if (multiplayerSection != null)
            {
                MultiplayerAccessOptions.Inject(__instance, template, multiplayerSection.transform);
                MultiplayerRulesOptions.Inject(__instance, template, multiplayerSection.transform, categoryController);
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
                "Option_SephiriaEnhancements_DefeatRetryCutscenes",
                "Option_SephiriaEnhancements_MapEnabled",
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
                "Option_SephiriaEnhancements_ManaReservationNumbers",
                "Option_SephiriaEnhancements_TeammateResourceNumbers",
                "Option_SephiriaEnhancements_CompanionHealthNumbers",
                "Option_SephiriaEnhancements_CreatureHealthNumbers",
                "Option_SephiriaEnhancements_CreatureSuperArmorNumbers",
                "Option_SephiriaEnhancements_MiniBossHealthNumbers",
                "Option_SephiriaEnhancements_MiniBossSuperArmorNumbers",
                "Option_SephiriaEnhancements_BossHealthNumbers",
                "Option_SephiriaEnhancements_PropHealthNumbers",
                "Option_SephiriaEnhancements_TargetingMode",
                "Option_SephiriaEnhancements_MouseAimAssist",
                "Option_SephiriaEnhancements_ViewDistance",
                "Option_SephiriaEnhancements_ModInformation_Version",
                "Option_SephiriaEnhancements_ModInformation_GameVersion",
                "Option_SephiriaEnhancements_ModInformation_Welcome",
                "Option_SephiriaEnhancements_ModInformation_Automatic",
                "Option_SephiriaEnhancements_ModInformation_Check",
                "Option_SephiriaEnhancements_ModInformation_LastChecked",
                "Option_SephiriaEnhancements_ModInformation_Nexus",
                "Option_SephiriaEnhancements_ModInformation_GitHub"
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
                "Option_SephiriaEnhancements_ReconnectSupport",
                "Option_SephiriaEnhancements_MultiplayerRulesLobby"
            };
            foreach (string rowName in multiplayerRows)
            {
                MoveInjectedChild(parent, rowName, ref siblingIndex);
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
            NativeOptionsLifetime.Track(header);
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
                state.Capture(template.box);
            }

            var chain = new List<UI_HorizontalSelectionBox> { template.box };
            var entries = new List<ModOptionsNavigationEntry>(
                panel.GetComponentsInChildren<ModOptionsNavigationEntry>(true));
            entries.Sort((left, right) => left.transform.GetSiblingIndex()
                .CompareTo(right.transform.GetSiblingIndex()));
            foreach (ModOptionsNavigationEntry entry in entries)
            {
                if (entry.Box != null && entry.gameObject.activeInHierarchy && entry.Box.IsInteractable())
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

        private static void CreateMasterEnabledRow(UI_OptionBox_PartyMemberDamage template,
            Transform section)
        {
            GameObject row = CloneRow(template, section, "Option_SephiriaEnhancements_Enabled",
                ModLocalization.SettingMasterEnabled, ModLocalization.HelpMasterEnabled, 1,
                out UI_HorizontalSelectionBox box,
                out UI_LocalizationStringText valueText);
            row.AddComponent<MasterEnabledOption>().Configure(box, valueText);
            NativeSettingsInteraction.Bind(row, box, valueText, SettingInteractionKind.Master, template.valueText.text);
            // The suite switch owns every category and therefore remains visible.
            row.SetActive(true);
        }

        private static MultiplayerSectionMarker
            CreateMultiplayerSectionHeader(OptionsSectionMarker source)
        {
            GameObject header = Object.Instantiate(source.gameObject,
                source.transform.parent);
            NativeOptionsLifetime.Track(header);
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

    }

    [HarmonyPatch(typeof(UI_OptionsPanel), "OnClosed")]
    internal static class NativeControlOptionsClosedPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Settings))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Settings, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore()
        {
            try
            {
                NativeControlCoordinator.ReloadOfficialBindings();
            }
            catch (System.Exception ex)
            {
                SupportLogger.Warning("controls_restart_required", "[SephiriaEnhancements] Updated native controls " + "will take effect after restart: " + ex.Message);
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

        internal void Configure(OptionsCategory category)
        {
            Category = category;
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


        internal void RefreshVisibility()
        {
            if (panel == null || template == null) return;

            Vector2 anchoredPosition = content != null
                ? content.anchoredPosition : Vector2.zero;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            bool selectedWillHide = false;
            bool suiteEnabled = EnhancementsSettings.Enabled;
            var categoryOption = panel.GetComponentInChildren<OptionsCategoryOption>(true);
            if (categoryOption != null)
            {
                if (!suiteEnabled && selected != null &&
                    (selected == categoryOption.gameObject ||
                     selected.transform.IsChildOf(categoryOption.transform)))
                    selectedWillHide = true;
                categoryOption.gameObject.SetActive(suiteEnabled);
            }
            OptionsCategoryMember[] members =
                panel.GetComponentsInChildren<OptionsCategoryMember>(true);

            for (int index = 0; index < members.Length; index++)
            {
                OptionsCategoryMember member = members[index];
                bool visible = suiteEnabled && OptionsCategoryVisibility.IsVisible(
                    member.Category, SelectedCategory);

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

            if (selectedWillHide && EventSystem.current != null)
            {
                var target = suiteEnabled ? categoryBox
                    : panel.GetComponentInChildren<MasterEnabledOption>(true)?.Box;
                if (target != null && target.gameObject.activeInHierarchy)
                    EventSystem.current.SetSelectedGameObject(target.gameObject);
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
            if (!NativeSettingsInteraction.CanEdit(SettingInteractionKind.Master))
            {
                box.ChangeValueWithoutNotify(EnhancementsSettings.Enabled ? 1 : 0);
                return;
            }
            EnhancementsSettings.Enabled = value == 1;
            EnhancementsSettings.Save();
            CombatVisualRuntime.RefreshCompanionBodies();
            valueText?.UpdateKey(value == 1
                ? ModLocalization.SuiteOn : ModLocalization.SuiteOff);
            GetComponentInParent<OptionsCategoryController>()?.RefreshVisibility();
        }
    }

}
