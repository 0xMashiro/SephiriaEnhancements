using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizationLocalizationChecks
{
    internal static void Run()
    {
        var inventoryTexts = new Dictionary<string,
            Dictionary<string, string>>(StringComparer.Ordinal);
        InventoryOptimizationLocalization.Register((language, key, value) =>
        {
            if (!inventoryTexts.TryGetValue(language,
                    out Dictionary<string, string>? texts))
            {
                texts = new Dictionary<string, string>(StringComparer.Ordinal);
                inventoryTexts.Add(language, texts);
            }
            texts.Add(key, value);
        });
        if (inventoryTexts.Count != 15 ||
            inventoryTexts.Values.Any(texts =>
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    SettingOptimizationTendency) ||
                !InventoryOptimizationLocalization.OptimizationTendencyKeys.All(
                    texts.ContainsKey) ||
                !InventoryOptimizationLocalization.PreferenceChoiceKeys.All(
                    texts.ContainsKey) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudArtifactsTab) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.PositionEffectsUnavailable) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudCombosTab) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudOptimize) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudMarkArtifacts) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudFinishMarking) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudMarkingHint) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudMarkedCount) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudMarkedAndAdjustmentCount) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudPriorityQueue) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudAvoidZone) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudIntentBoardHint) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudChooseIntentSlot) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudOpen) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudAdjustTargets) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudHideTargets) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudAutomaticPreset) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudAutomaticInventory) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.
                    HudAdjustmentCount) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudEnabled) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudNoTargets) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudPage)) ||
            inventoryTexts["en-US"][InventoryOptimizationLocalization.
                OptimizationTendencyKeys[0]] != "Automatic")
            throw new InvalidOperationException(
                "inventory target editor must localize as one complete feature group");
        Console.WriteLine("InventoryOptimizationTendency: intent-level settings and target-editor localization passed");
    }
}
