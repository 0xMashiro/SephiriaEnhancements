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
        private Button specialEffectsEntry, resetSpecialEffects;
        private readonly Button[] positionEffectChoices = new Button[3], magicCostChoices = new Button[2];
        private readonly TextMeshProUGUI[] positionEffectChoiceTexts = new TextMeshProUGUI[3], magicCostChoiceTexts = new TextMeshProUGUI[2];
        private TextMeshProUGUI specialEffectsEntryText, resetSpecialEffectsText;
        private TextMeshProUGUI positionEffectHelp, magicCostHelp, persistenceHelp;
        private GameObject specialEffectsRoot;

        private void CreateSpecialEffects(RectTransform parent, TextMeshProUGUI template)
        {
            specialEffectsEntry = controls.CreateButton("SpecialEffects", parent, template, new Vector2(24f, -96f),
                new Vector2(312f, 32f), () => SetPreferencesView(true, false, true), out specialEffectsEntryText);
            specialEffectsRoot = new GameObject("SpecialEffectsSettings", typeof(RectTransform));
            var rect = (RectTransform)specialEffectsRoot.transform;
            rect.SetParent(parent, false);
            SetTopRect(rect, new Vector2(24f, -104f), new Vector2(312f, 480f));
            for (int index = 0; index < positionEffectChoices.Length; index++)
            {
                int choice = index;
                positionEffectChoices[index] = controls.CreateButton("PositionEffects" + index, rect, template,
                    new Vector2(0f, -40f * index), new Vector2(312f, 36f),
                    () => EditSpecialEffects((InventoryPositionEffectPreference)choice, null), out positionEffectChoiceTexts[index]);
            }
            positionEffectHelp = CreateText("PositionHelp", rect, template, new Vector2(0, -122f),
                new Vector2(312f, 94f), TextAlignmentOptions.TopLeft);
            for (int index = 0; index < magicCostChoices.Length; index++)
            {
                bool allow = index == 1;
                magicCostChoices[index] = controls.CreateButton("AdditionalMagicCost" + index, rect, template,
                    new Vector2(0f, -224f - 40f * index), new Vector2(312f, 36f),
                    () => EditSpecialEffects(null, allow), out magicCostChoiceTexts[index]);
            }
            magicCostHelp = CreateText("MagicCostHelp", rect, template, new Vector2(0, -306f),
                new Vector2(312f, 84f), TextAlignmentOptions.TopLeft);
            resetSpecialEffects = controls.CreateButton("ResetSpecialEffects", rect, template, new Vector2(0, -448f),
                new Vector2(312f, 32f), ResetSpecialEffects, out resetSpecialEffectsText);
            persistenceHelp = CreateText("Persistence", rect, template, new Vector2(0, -398f),
                new Vector2(312f, 44f), TextAlignmentOptions.TopLeft);
            foreach (var text in new[] { positionEffectHelp, magicCostHelp, persistenceHelp })
            {
                text.color = SecondaryText;
                text.textWrappingMode = TextWrappingModes.Normal;
            }
        }

        private void EditSpecialEffects(InventoryPositionEffectPreference? position, bool? allowCost)
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem) return;
            var preferences = WorldSessionInventoryIntentStore.Capture();
            var chosenPosition = position ?? preferences.PositionEffectPreference;
            bool chosenCost = allowCost ?? preferences.AllowAdditionalMagicCost;
            if (chosenPosition == preferences.PositionEffectPreference && chosenCost == preferences.AllowAdditionalMagicCost) return;
            ReplacePreferences(preferences.WithSpecialEffects(chosenPosition, chosenCost));
            nextProjectionAt = 0;
        }

        private void ResetSpecialEffects()
        {
            if (!interaction.Editable || interaction.HasPickup || NativeInventoryIntentDrop.HasHeldItem) return;
            ReplacePreferences(WorldSessionInventoryIntentStore.Capture().WithSpecialEffects(
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
            var preferences = WorldSessionInventoryIntentStore.Capture();
            title.text = Loc._(InventorySpecialEffectLocalization.Title);
            bool positionAvailable = currentSnapshot?.PositionEffects.Rules.Count > 0;
            bool costAvailable = currentSnapshot?.Items.Any(item => item.Artifact != null &&
                item.Artifact.StatPenaltySafeLevel > item.Artifact.SafeAutomaticLevel) == true;
            for (int index = 0; index < positionEffectChoices.Length; index++)
                positionEffectChoiceTexts[index].text = (index == (int)preferences.PositionEffectPreference ? "● " : "○ ") +
                    Loc._(InventorySpecialEffectLocalization.PositionModes[index]);
            positionEffectHelp.text = Loc._(positionAvailable ? InventorySpecialEffectLocalization.PositionHelp[(int)preferences.PositionEffectPreference]
                : InventorySpecialEffectLocalization.NoPositionEffects);
            for (int index = 0; index < magicCostChoices.Length; index++)
                magicCostChoiceTexts[index].text = ((index == 1) == preferences.AllowAdditionalMagicCost ? "● " : "○ ") +
                    Loc._(index == 1 ? InventorySpecialEffectLocalization.AllowCost : InventorySpecialEffectLocalization.KeepCost);
            magicCostHelp.text = Loc._(costAvailable ? InventorySpecialEffectLocalization.CostHelp : InventorySpecialEffectLocalization.NoCost);
            persistenceHelp.text = Loc._(InventorySpecialEffectLocalization.Persistence);
            resetSpecialEffectsText.text = Loc._(InventorySpecialEffectLocalization.Reset);
            bool editable = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem;
            foreach (var choice in positionEffectChoices) choice.interactable = editable && positionAvailable;
            foreach (var choice in magicCostChoices) choice.interactable = editable && costAvailable;
            resetSpecialEffects.interactable = editable;
            var nativeText = (nativeTemplates.ContentButton as UI_HorayButton)?.text;
            if (nativeText != null)
                foreach (var text in new[] { positionEffectHelp, magicCostHelp, persistenceHelp })
                    NativeLocalizedText.SetShrinkOnlySize(text, nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale,
                        nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale * 0.75f);
            Button[] controls = new[] { preferencesToggle }.Concat(positionEffectChoices).Concat(magicCostChoices)
                .Concat(new[] { resetSpecialEffects, undoArrangement, optimize })
                .Where(button => button.IsInteractable()).ToArray();
            for (int i = 0; i < controls.Length; i++)
            {
                Button up = i > 0 ? controls[i - 1] : close;
                Button down = i + 1 < controls.Length ? controls[i + 1] : controls[i];
                if (controls[i].transform.IsChildOf(specialEffectsRoot.transform))
                {
                    controls[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                        selectOnUp = up, selectOnDown = down, selectOnLeft = controls[i], selectOnRight = controls[i] };
                    ((UI_HorayButton)controls[i]).SetForceNavLeft(controls[i]);
                    ((UI_HorayButton)controls[i]).SetForceNavRight(controls[i]);
                }
                var native = (UI_HorayButton)controls[i];
                native.SetForceNavUp(up);
                native.SetForceNavDown(down);
            }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(specialEffectsRoot.transform) &&
                !controls.Any(control => control.gameObject == selected)) EventSystem.current.SetSelectedGameObject(controls[0].gameObject);
        }
    }
}
