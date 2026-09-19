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
        private Button magicCostToggle;
        private TextMeshProUGUI magicCostText;
        private UI_CommonTooltipOpener magicCostTooltip;
        private Button presetComboToggle;
        private TextMeshProUGUI presetComboText;
        private UI_CommonTooltipOpener presetComboTooltip;

        private void CreateArrangementOptions(RectTransform parent, TextMeshProUGUI template)
        {
            template = (nativeTemplates.ContentButton as UI_HorayButton)?.text ?? template;
            presetComboToggle = controls.CreateButton("PresetComboPriority", parent, template,
                new Vector2(24f, -96f), new Vector2(312f, 32f), TogglePresetComboPriority, out presetComboText);
            presetComboTooltip = presetComboToggle.gameObject.AddComponent<UI_CommonTooltipOpener>();
            magicCostToggle = controls.CreateButton("AdditionalMagicCost", parent, template,
                new Vector2(24f, -136f), new Vector2(312f, 32f), ToggleAdditionalMagicCost, out magicCostText);
            magicCostTooltip = magicCostToggle.gameObject.AddComponent<UI_CommonTooltipOpener>();
        }

        private void ToggleAdditionalMagicCost()
        {
            if (!magicCostToggle.IsInteractable()) return;
            var preferences = LocalPlayerInventoryIntentStore.Capture();
            ReplacePreferences(preferences.WithAdditionalMagicCost(!preferences.AllowAdditionalMagicCost));
            nextProjectionAt = 0;
        }

        private void TogglePresetComboPriority()
        {
            if (!presetComboToggle.IsInteractable()) return;
            var preferences = LocalPlayerInventoryIntentStore.Capture();
            ReplacePreferences(preferences.WithPresetComboPriority(!preferences.PreferPresetCombos));
            nextProjectionAt = 0;
        }

        private void RefreshArrangementOptions()
        {
            var preferences = LocalPlayerInventoryIntentStore.Capture();
            magicCostText.text = Loc._(preferences.AllowAdditionalMagicCost
                ? InventoryMagicCostLocalization.AllowCost : InventoryMagicCostLocalization.KeepCost);
            presetComboText.text = Loc._(preferences.PreferPresetCombos
                ? InventoryPresetComboLocalization.Enabled : InventoryPresetComboLocalization.Disabled);
            var nativeText = (nativeTemplates.ContentButton as UI_HorayButton)?.text;
            if (nativeText != null)
            {
                float designSize = nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale;
                NativeLocalizedText.SetShrinkOnlySize(magicCostText, designSize, designSize * 0.75f);
                NativeLocalizedText.SetShrinkOnlySize(presetComboText, designSize, designSize * 0.75f);
            }
            bool editable = interaction.Editable && !interaction.HasPickup && !NativeInventoryIntentDrop.HasHeldItem;
            bool costApplicable = currentSnapshot?.Items.Any(item => item.Artifact != null &&
                item.Artifact.MagicCostSafeLevel < item.Artifact.MaxLevel) == true;
            bool comboApplicable = currentSnapshot?.BuildIntent.NativePresetEnabled == true &&
                (InventoryFruitSkewerPriorities.Resolve(currentSnapshot, preferences.ComboPreferences.Select(rule => rule.CategoryId)).Length > 0 ||
                 currentSnapshot.BuildIntent.PreferredCategories.Except(preferences.ComboPreferences.Select(rule => rule.CategoryId)).Any());
            RefreshOption(magicCostToggle, magicCostTooltip, costApplicable,
                InventoryMagicCostLocalization.Help, InventoryMagicCostLocalization.Unavailable);
            RefreshOption(presetComboToggle, presetComboTooltip, comboApplicable,
                InventoryPresetComboLocalization.Help, InventoryPresetComboLocalization.Unavailable);

            void RefreshOption(Button button, UI_CommonTooltipOpener tooltip, bool applicable, string help, string unavailable)
            {
                button.gameObject.SetActive(panelOpen && !preferencesExpanded);
                button.interactable = editable && applicable;
                tooltip.tooltipContext = new LocalizedString(applicable ? help : unavailable);
                tooltip.UpdateTooltipData();
                if (!button.IsInteractable() && EventSystem.current?.currentSelectedGameObject == button.gameObject)
                    EventSystem.current.SetSelectedGameObject(preferencesToggle.gameObject);
            }
        }
    }
}
