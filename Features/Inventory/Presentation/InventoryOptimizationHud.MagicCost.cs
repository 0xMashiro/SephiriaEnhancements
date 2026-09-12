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

        private void CreateMagicCostToggle(RectTransform parent, TextMeshProUGUI template)
        {
            template = (nativeTemplates.ContentButton as UI_HorayButton)?.text ?? template;
            magicCostToggle = controls.CreateButton("AdditionalMagicCost", parent, template,
                new Vector2(24f, -96f), new Vector2(312f, 32f), ToggleAdditionalMagicCost, out magicCostText);
        }

        private void ToggleAdditionalMagicCost()
        {
            if (!magicCostToggle.IsInteractable()) return;
            var preferences = WorldSessionInventoryIntentStore.Capture();
            ReplacePreferences(preferences.WithAdditionalMagicCost(!preferences.AllowAdditionalMagicCost));
            nextProjectionAt = 0;
        }

        private void RefreshMagicCostToggle()
        {
            bool allow = WorldSessionInventoryIntentStore.Capture().AllowAdditionalMagicCost;
            magicCostText.text = Loc._(allow ? InventoryMagicCostLocalization.AllowCost : InventoryMagicCostLocalization.KeepCost);
            var nativeText = (nativeTemplates.ContentButton as UI_HorayButton)?.text;
            if (nativeText != null)
            {
                float designSize = nativeText.fontSize * InventoryOptimizationHudLayout.NativeUnitScale;
                NativeLocalizedText.SetShrinkOnlySize(magicCostText, designSize, designSize * 0.75f);
            }
            magicCostToggle.gameObject.SetActive(panelOpen && !preferencesExpanded);
            bool applicable = currentSnapshot?.Items.Any(item => item.Artifact != null &&
                item.Artifact.StatPenaltySafeLevel > item.Artifact.SafeAutomaticLevel) == true;
            magicCostToggle.interactable = interaction.Editable && !interaction.HasPickup &&
                !NativeInventoryIntentDrop.HasHeldItem && applicable;
            if (!magicCostToggle.IsInteractable() && EventSystem.current?.currentSelectedGameObject == magicCostToggle.gameObject)
                EventSystem.current.SetSelectedGameObject(preferencesToggle.gameObject);
        }
    }
}
