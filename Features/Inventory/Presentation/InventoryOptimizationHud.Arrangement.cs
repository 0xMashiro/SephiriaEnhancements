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
            preferencesToggle = CreateButton("Preferences", parent, template, new Vector2(24f, -56f),
                new Vector2(148f, 32f), TogglePreferences, out preferencesToggleText);
            comboPreferences = CreateButton("ComboPreferences", parent, template, new Vector2(188f, -56f),
                new Vector2(148f, 32f), () => SetPreferencesView(true, true), out comboPreferencesText);
            CreateSpecialEffects(parent, template);
            undoArrangement = CreateButton("UndoArrangement", parent, template, new Vector2(24f, -ActionsTop),
                new Vector2(148f, 36f), () => requestUndo?.Invoke(), out undoArrangementText);
        }

        private void TogglePreferences() => SetPreferencesView(!preferencesExpanded, false);

        private void SetPreferencesView(bool expanded, bool showCombos, bool showSpecialEffects = false)
        {
            ClearArtifactPickup();
            endPriorityMarking?.Invoke();
            preferencesExpanded = expanded;
            specialEffectsExpanded = showSpecialEffects;
            detailsExpanded = showCombos || showSpecialEffects;
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
            if (!preferencesExpanded)
            {
                toggle.SetForceNavDown(specialEffectsEntry);
                ((UI_HorayButton)comboPreferences).SetForceNavDown(specialEffectsEntry);
                ((UI_HorayButton)specialEffectsEntry).SetForceNavUp(preferencesToggle);
                ((UI_HorayButton)specialEffectsEntry).SetForceNavDown(undoArrangement.interactable ? undoArrangement : optimize);
                undo.SetForceNavUp(specialEffectsEntry);
                ((UI_HorayButton)optimize).SetForceNavUp(specialEffectsEntry);
            }
            else if (detailsExpanded) ((UI_HorayButton)optimize)?.SetForceNavUp(
                rows.LastOrDefault(row => row.Root.activeInHierarchy)?.Choice ?? preferencesToggle);
            RefreshSpecialEffects();
        }
    }
}
