using static SephiriaEnhancements.Inventory.NativeInventoryHudControls;
using System;
using System.Linq;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationHud
    {
        private bool specialEffectsExpanded;
        private Button specialEffectsEntry, positionEffectChoice, magicCostChoice, resetSpecialEffects;
        private TextMeshProUGUI specialEffectsEntryText, positionEffectChoiceText, magicCostChoiceText, resetSpecialEffectsText;
        private TextMeshProUGUI positionEffectHelp, magicCostHelp;
        private GameObject specialEffectsRoot;

        private void CreateSpecialEffects(RectTransform parent, TextMeshProUGUI template)
        {
            specialEffectsEntry = controls.CreateButton("SpecialEffects", parent, template, new Vector2(24f, -96f),
                new Vector2(312f, 32f), () => SetPreferencesView(true, false, true), out specialEffectsEntryText);
            specialEffectsRoot = new GameObject("SpecialEffectsSettings", typeof(RectTransform));
            var rect = (RectTransform)specialEffectsRoot.transform;
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(24f, -104f), new Vector2(312f, 326f));
            positionEffectChoice = controls.CreateButton("PositionEffects", rect, template, Vector2.zero,
                new Vector2(312f, 36f), () => EditSpecialEffects(false), out positionEffectChoiceText);
            positionEffectHelp = CreateText("PositionHelp", rect, template, new Vector2(0, -42f),
                new Vector2(312f, 94f), TextAlignmentOptions.TopLeft);
            magicCostChoice = controls.CreateButton("AdditionalMagicCost", rect, template, new Vector2(0, -146f),
                new Vector2(312f, 36f), () => EditSpecialEffects(true), out magicCostChoiceText);
            magicCostHelp = CreateText("MagicCostHelp", rect, template, new Vector2(0, -188f),
                new Vector2(312f, 94f), TextAlignmentOptions.TopLeft);
            resetSpecialEffects = controls.CreateButton("ResetSpecialEffects", rect, template, new Vector2(0, -290f),
                new Vector2(312f, 32f), ResetSpecialEffects, out resetSpecialEffectsText);
            foreach (var text in new[] { positionEffectHelp, magicCostHelp })
            {
                text.color = SecondaryText;
                text.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void EditSpecialEffects(bool cost)
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem) return;
            var preferences = ExplorationInventoryIntentStore.Capture();
            ReplacePreferences(preferences.WithSpecialEffects(cost ? preferences.PositionEffectPreference :
                (InventoryPositionEffectPreference)(((int)preferences.PositionEffectPreference + 1) % 3),
                cost ? !preferences.AllowAdditionalMagicCost : preferences.AllowAdditionalMagicCost));
            nextProjectionAt = 0;
        }

        private void ResetSpecialEffects()
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem) return;
            ReplacePreferences(ExplorationInventoryIntentStore.Capture().WithSpecialEffects(
                InventoryPositionEffectPreference.Improve, false));
            nextProjectionAt = 0;
        }

        private void RefreshSpecialEffects()
        {
            if (specialEffectsRoot == null) return;
            specialEffectsEntryText.text = Loc._(InventorySpecialEffectLocalization.Title);
            specialEffectsEntry.gameObject.SetActive(panelOpen && !preferencesExpanded);
            specialEffectsEntry.interactable = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem;
            bool visible = panelOpen && preferencesExpanded && specialEffectsExpanded;
            specialEffectsRoot.SetActive(visible);
            if (!visible) return;
            var preferences = ExplorationInventoryIntentStore.Capture();
            title.text = Loc._(InventorySpecialEffectLocalization.Title);
            bool positionAvailable = currentSnapshot?.PositionEffects.Rules.Count > 0;
            bool costAvailable = currentSnapshot?.Items.Any(item => item.Artifact != null &&
                item.Artifact.StatPenaltySafeLevel > item.Artifact.SafeAutomaticLevel) == true;
            positionEffectChoiceText.text = Loc._(InventorySpecialEffectLocalization.PositionModes[(int)preferences.PositionEffectPreference]);
            positionEffectHelp.text = Loc._(positionAvailable ? InventorySpecialEffectLocalization.PositionHelp[(int)preferences.PositionEffectPreference]
                : InventorySpecialEffectLocalization.NoPositionEffects);
            magicCostChoiceText.text = Loc._(preferences.AllowAdditionalMagicCost ? InventorySpecialEffectLocalization.AllowCost : InventorySpecialEffectLocalization.KeepCost);
            magicCostHelp.text = Loc._(costAvailable ? InventorySpecialEffectLocalization.CostHelp : InventorySpecialEffectLocalization.NoCost);
            resetSpecialEffectsText.text = Loc._(InventorySpecialEffectLocalization.Reset);
            bool editable = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem;
            positionEffectChoice.interactable = editable && positionAvailable;
            magicCostChoice.interactable = editable && costAvailable;
            resetSpecialEffects.interactable = editable;
            var nativeText = (nativeTemplates.ContentButton as UI_HorayButton)?.text;
            if (nativeText != null)
                foreach (var text in new[] { positionEffectHelp, magicCostHelp })
                    NativeLocalizedText.SetShrinkOnlySize(text, nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale,
                        nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale * 0.75f);
            Button[] controls = new[] { preferencesToggle, positionEffectChoice, magicCostChoice, resetSpecialEffects, optimize }
                .Where(button => button.IsInteractable()).ToArray();
            for (int i = 0; i < controls.Length; i++)
            {
                ((UI_HorayButton)controls[i]).SetForceNavUp(i > 0 ? controls[i - 1] : close);
                ((UI_HorayButton)controls[i]).SetForceNavDown(i + 1 < controls.Length ? controls[i + 1] : null);
            }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(specialEffectsRoot.transform) &&
                !IsCustomSelection(selected)) EventSystem.current.SetSelectedGameObject(controls[0].gameObject);
        }
    }
}
