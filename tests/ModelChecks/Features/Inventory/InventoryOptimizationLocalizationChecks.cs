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
                    SettingSearchMode) ||
                !InventoryOptimizationLocalization.SearchModeKeys.All(
                    texts.ContainsKey) ||
                !InventoryOptimizationLocalization.PreferenceChoiceKeys.All(
                    texts.ContainsKey) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.PositionEffectsUnavailable) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudComboTargets) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudLevelEditUnbound) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudEditGoals) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudEditGoalsShortcut) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudConstraintHelp) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudComboPersistence) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudNavigationBoardHint) ||
                !texts.ContainsKey(InventoryOptimizationLocalization.HudNavigationChooseIntentSlot) ||
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
                SearchModeKeys[0]] != "Automatic")
            throw new InvalidOperationException(
                "inventory target editor must localize as one complete feature group");
        Console.WriteLine("InventorySearchMode: intent-level settings and target-editor localization passed");
        VerifyTargetConditions(inventoryTexts);
        VerifyArtifactGoalSummaries(inventoryTexts);
    }

    private static void VerifyArtifactGoalSummaries(Dictionary<string, Dictionary<string, string>> texts)
    {
        var artifact = Runtime.Inventory.InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 1 }, new[] { 0 }, maxLevel: 4, safeAutomaticLevels: new[] { 2 }).Items[0].Artifact;
        foreach (var strength in new[] { InventoryConstraintStrength.Soft, InventoryConstraintStrength.Hard })
            foreach (var mode in new[] { ArtifactLevelTargetMode.Automatic, ArtifactLevelTargetMode.ActiveOnly,
            ArtifactLevelTargetMode.SpecifiedLevel })
                foreach (var level in new[] { InventoryPreferenceLevel.Priority, InventoryPreferenceLevel.Avoid })
                {
                    var rule = new ArtifactOptimizationPreference(100, 1000, level, 3, 0, mode, strength);
                    foreach (var entries in texts.Values)
                    {
                        string summary = InventoryOptimizationLocalization.FormatArtifactGoalSummary(rule, artifact, key => entries[key]);
                        string target = InventoryOptimizationLocalization.FormatArtifactTarget(rule, artifact, key => entries[key]);
                        if (!summary.Contains(target, StringComparison.Ordinal) || summary.Contains("{0}", StringComparison.Ordinal))
                            throw new InvalidOperationException("artifact summaries must include the resolved target in every language");
                    }
                    string chinese = InventoryOptimizationLocalization.FormatArtifactGoalSummary(rule, artifact, key => texts["zh-CN"][key]);
                    string expectedTarget = level == InventoryPreferenceLevel.Avoid ? "保持不生效"
                        : mode == ArtifactLevelTargetMode.Automatic ? "自动（控制负面效果）· 至少 2 级"
                        : mode == ArtifactLevelTargetMode.ActiveOnly ? "只需生效" : "至少 3 级";
                    string expectedRequirement = strength == InventoryConstraintStrength.Hard
                        ? "全部「必须满足」的目标都达到，才会整理。" : "无法满足此目标时，仍可整理。";
                    if (chinese != $"整理时：{expectedTarget}。\n{expectedRequirement}")
                        throw new InvalidOperationException("artifact summaries must distinguish auto, activation, explicit levels, inactivity and required goals");
                }
    }

    private static void VerifyTargetConditions(Dictionary<string, Dictionary<string, string>> texts)
    {
        var cases = new[]
        {
            (InventoryPreferenceChoice.Automatic, 3, "自动", "Automatic"),
            (InventoryPreferenceChoice.Priority, 0, "不设下限", "No minimum"),
            (InventoryPreferenceChoice.Priority, 3, "至少 3", "MIN 3"),
            (InventoryPreferenceChoice.Avoid, 0, "最多 0", "MAX 0"),
            (InventoryPreferenceChoice.Avoid, 3, "最多 3", "MAX 3")
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
                    (language == "en-US" && condition != english))
                    throw new InvalidOperationException("target conditions must preserve threshold direction and zero semantics");
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
        foreach (var (language, entries) in texts)
        {
            if (!entries.Keys.ToHashSet().SetEquals(texts["en-US"].Keys))
                throw new InvalidOperationException($"incomplete inventory localization: {language}");
        }
    }
}
