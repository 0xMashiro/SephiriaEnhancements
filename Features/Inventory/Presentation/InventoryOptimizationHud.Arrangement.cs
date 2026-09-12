using static SephiriaEnhancements.Inventory.NativeInventoryHudControls;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationHud
    {
        private Button preferencesToggle;
        private Button comboPreferences;
        private Button undoArrangement;
        private Button clearArtifactPriorities;
        private TextMeshProUGUI clearArtifactPrioritiesText;
        private TextMeshProUGUI preferencesToggleText;
        private TextMeshProUGUI comboPreferencesText;
        private TextMeshProUGUI undoArrangementText;
        private Action requestUndo;
        private bool canUndo;
        private bool preferencesExpanded;

        private float OpenPanelHeight => preferencesExpanded ? PanelHeight : InventoryOptimizationHudLayout.CompactHeight;
        private float ActionsTop => preferencesExpanded ? InventoryOptimizationHudLayout.ActionsTop : InventoryOptimizationHudLayout.CompactActionsTop;

        internal void ConfigureArrangementActions(Action undo, bool undoAvailable)
        {
            requestUndo = undo;
            canUndo = undoAvailable;
        }

        private void CreateArrangementActions(RectTransform parent, TextMeshProUGUI template)
        {
            preferencesToggle = controls.CreateButton("Preferences", parent, template, new Vector2(24f, -56f),
                new Vector2(148f, 32f), TogglePreferences, out preferencesToggleText);
            comboPreferences = controls.CreateButton("ComboPreferences", parent, template, new Vector2(188f, -56f),
                new Vector2(148f, 32f), () => SetPreferencesView(true, true), out comboPreferencesText);
            CreateMagicCostToggle(parent, template);
            clearArtifactPriorities = controls.CreateButton("ClearArtifactPriorities", parent, template,
                new Vector2(188f, -InventoryOptimizationHudLayout.DetailsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.DetailsHeight),
                ClearArtifactPriorities, out clearArtifactPrioritiesText);
            undoArrangement = controls.CreateButton("UndoArrangement", parent, template, new Vector2(24f, -ActionsTop),
                new Vector2(148f, 36f), () => requestUndo?.Invoke(), out undoArrangementText);
        }

        private void TogglePreferences() => SetPreferencesView(!preferencesExpanded, false);

        private void ClearArtifactPriorities()
        {
            if (!panelOpen || !preferencesExpanded || detailsExpanded || goalEditor.Visible ||
                !interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem) return;
            endPriorityMarking?.Invoke();
            ReplacePreferences(InventoryArtifactIntentEditor.Clear(WorldSessionInventoryIntentStore.Capture()));
            previewItemKey = null;
            intentPage = 0;
            ProjectIntentBoard(WorldSessionInventoryIntentStore.Capture());
            RefreshArrangementActions();
            RefreshPageNavigation();
            EventSystem.current?.SetSelectedGameObject(prioritySlots[0].Root);
            nextProjectionAt = 0f;
        }

        private void SetPreferencesView(bool expanded, bool showCombos)
        {
            ClearArtifactPickup();
            endPriorityMarking?.Invoke();
            preferencesExpanded = expanded;
            detailsExpanded = showCombos;
            previewItemKey = null;
            page = 0;
            expandedComboCategoryId = null;
            ApplyDisclosureLayout();
            PositionBesideInventory();
            RefreshNavigation();
            EventSystem.current?.SetSelectedGameObject(preferencesToggle.gameObject);
            nextProjectionAt = 0f;
        }

        private void RefreshArrangementActions()
        {
            if (preferencesToggle == null || undoArrangement == null) return;
            preferencesToggleText.text = Loc._(preferencesExpanded
                ? InventoryArrangementLocalization.BackToArrangement : InventoryArrangementLocalization.ArtifactPriorities);
            comboPreferencesText.text = Loc._(InventoryArrangementLocalization.ComboPriorities);
            undoArrangementText.text = Loc._(InventoryArrangementLocalization.Undo);
            clearArtifactPrioritiesText.text = Loc._(InventoryArrangementLocalization.ClearArtifactPriorities);
            clearArtifactPrioritiesText.color = SecondaryText;
            clearArtifactPriorities.gameObject.SetActive(panelOpen && preferencesExpanded && !detailsExpanded && !goalEditor.Visible);
            clearArtifactPriorities.interactable = interaction.Editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem && WorldSessionInventoryIntentStore.Capture().ArtifactPreferences.Count > 0;
            preferencesToggleText.color = SecondaryText;
            comboPreferencesText.color = SecondaryText;
            undoArrangementText.color = SecondaryText;
            preferencesToggle.gameObject.SetActive(panelOpen);
            preferencesToggle.interactable = !interaction.HasPickup;
            comboPreferences.gameObject.SetActive(panelOpen && !preferencesExpanded);
            comboPreferences.interactable = !interaction.HasPickup;
            SetTopRect((RectTransform)preferencesToggle.transform, new Vector2(24f, -56f),
                new Vector2(preferencesExpanded && detailsExpanded ? 312f : 148f, 32f));
            undoArrangement.gameObject.SetActive(panelOpen);
            undoArrangement.interactable = currentPhase == InventoryOptimizationHudPhase.Ready && canUndo &&
                !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem;
            if (!undoArrangement.interactable && EventSystem.current?.currentSelectedGameObject == undoArrangement.gameObject)
                SelectFirstCustomEntry();
            SetTopRect((RectTransform)undoArrangement.transform, new Vector2(24f, -ActionsTop),
                new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight));
            if (optimize != null)
                SetTopRect((RectTransform)optimize.transform, new Vector2(188f, -ActionsTop),
                    new Vector2(148f, InventoryOptimizationHudLayout.ActionsHeight));
            var toggle = (UI_HorayButton)preferencesToggle;
            var undo = (UI_HorayButton)undoArrangement;
            undo.SetForceNavRight(optimize);
            undo.SetForceNavUp(preferencesExpanded && !detailsExpanded && editGoals.interactable ? editGoals : preferencesToggle);
            Button below = !preferencesExpanded ? optimize : detailsExpanded
                ? rows.FirstOrDefault(row => row.Root.activeInHierarchy)?.Choice ?? optimize : prioritySlots[0].Button;
            toggle.SetForceNavDown(!preferencesExpanded && undoArrangement.interactable ? undoArrangement : below);
            toggle.SetForceNavRight(preferencesExpanded ? detailsExpanded ? null : markPriorities : comboPreferences);
            (comboPreferences as UI_HorayButton)?.SetForceNavLeft(preferencesToggle);
            (comboPreferences as UI_HorayButton)?.SetForceNavDown(optimize);
            (markPriorities as UI_HorayButton)?.SetForceNavDown(below);
            (markPriorities as UI_HorayButton)?.SetForceNavLeft(preferencesToggle);
            RefreshMagicCostToggle();
            if (!preferencesExpanded)
            {
                Button next = magicCostToggle.IsInteractable() ? magicCostToggle
                    : undoArrangement.interactable ? undoArrangement : optimize;
                toggle.SetForceNavDown(next);
                ((UI_HorayButton)comboPreferences).SetForceNavDown(next);
                var cost = (UI_HorayButton)magicCostToggle;
                cost.SetForceNavUp(preferencesToggle);
                cost.SetForceNavDown(undoArrangement.interactable ? undoArrangement : optimize);
                cost.SetForceNavLeft(preferencesToggle);
                cost.SetForceNavRight(comboPreferences);
                Button above = magicCostToggle.IsInteractable() ? magicCostToggle : preferencesToggle;
                undo.SetForceNavUp(above);
                ((UI_HorayButton)optimize).SetForceNavUp(above);
            }
            else if (detailsExpanded) ((UI_HorayButton)optimize)?.SetForceNavUp(
                rows.LastOrDefault(row => row.Root.activeInHierarchy)?.Choice ?? preferencesToggle);
        }
    }
}
