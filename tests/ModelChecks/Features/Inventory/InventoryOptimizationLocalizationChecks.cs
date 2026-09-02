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
                !texts.ContainsKey(InventoryOptimizationLocalization.PositionEffectsUnavailable) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudComboTargets) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudLevelEditUnbound) ||
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
        VerifyTargetConditions(inventoryTexts);
    }

    private static void VerifyTargetConditions(Dictionary<string, Dictionary<string, string>> texts)
    {
        var cases = new[]
        {
            (InventoryPreferenceChoice.Automatic, 3, "跟随自动整理", "Follow automatic sorting"),
            (InventoryPreferenceChoice.Priority, 0, "计数不限（0）", "No minimum count (0)"),
            (InventoryPreferenceChoice.Priority, 3, "计数至少 3", "Count: 3 or more"),
            (InventoryPreferenceChoice.Avoid, 0, "计数最多 0", "Count: 0 or fewer"),
            (InventoryPreferenceChoice.Avoid, 3, "计数最多 3", "Count: 3 or fewer")
        };
        if (InventoryOptimizationLocalization.PreferenceChoiceKeys.Length != 3)
            throw new InvalidOperationException("target editor must expose only Automatic, Priority and Avoid");
        foreach (var (choice, value, chinese, english) in cases)
        {
            var target = new InventoryComboTarget("FIRE", choice, value, 5);
            foreach (var (language, entries) in texts)
            {
                string condition = InventoryOptimizationLocalization.FormatTargetCondition(target, key => entries[key]);
                if (string.IsNullOrWhiteSpace(condition) ||
                    (language == "zh-CN" && condition != chinese) ||
                    (language != "zh-CN" && language != "zh-TW" && condition != english))
                    throw new InvalidOperationException("target conditions must preserve threshold direction, zero semantics and whole-group English fallback");
            }
        }
        foreach (var (language, entries) in texts)
        {
            string zero = InventoryOptimizationLocalization.FormatArtifactMinimumLevel(0, key => entries[key]);
            string three = InventoryOptimizationLocalization.FormatArtifactMinimumLevel(3, key => entries[key]);
            if (language == "zh-CN" && (zero != "只需生效" || three != "至少 3 级") ||
                language == "en-US" && (zero != "Keep active" || three != "Level 3 or higher"))
                throw new InvalidOperationException("queue level conditions must distinguish activation from a minimum level");
            string hint = string.Format(entries[InventoryOptimizationLocalization.HudIntentBoardHint], "BOUND-ACTION");
            if (!hint.Contains("BOUND-ACTION", StringComparison.Ordinal))
                throw new InvalidOperationException("level edit hint must display the active binding, not a hard-coded physical key");
        }
        foreach (var (language, entries) in texts.Where(pair => pair.Key != "zh-CN" && pair.Key != "zh-TW"))
        {
            if (entries.Count != texts["en-US"].Count || entries.Any(pair => texts["en-US"][pair.Key] != pair.Value))
                throw new InvalidOperationException($"inventory localization must fall back as a complete group: {language}");
        }
    }
}
