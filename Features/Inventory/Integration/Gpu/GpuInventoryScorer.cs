#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class GpuInventoryScorer
{
    private readonly GpuInventorySnapshot packed;
    private readonly ArtifactTarget[] artifactTargets;
    private readonly (ResolvedComboOptimizationRule Rule, int Index, string Key)[] comboTargets;
    private readonly int priorityCount;

    internal GpuInventoryScorer(GpuInventorySnapshot packed, ResolvedInventoryOptimizationPolicy policy)
    {
        this.packed = packed;
        priorityCount = policy.ArtifactInstanceRules.Values.Where(r => r.Level == InventoryPreferenceLevel.Priority && r.PriorityOrder >= 0)
            .Select(r => r.PriorityOrder).DefaultIfEmpty(-1).Max() + 1;
        var items = packed.Snapshot.Items;
        ArtifactTarget Target(ResolvedArtifactOptimizationRule rule, bool instance) => new(rule, instance,
            Enumerable.Range(0, items.Count).Where(i => items[i].Artifact != null && (instance ? items[i].ItemKey == rule.ItemKey :
                items[i].EntityId == rule.EntityId && !policy.ArtifactInstanceRules.ContainsKey(items[i].ItemKey))).ToArray());
        artifactTargets = policy.ArtifactInstanceRules.Values.Select(r => Target(r, true))
            .Concat(policy.ArtifactEntityRules.Values.Select(r => Target(r, false))).ToArray();
        comboTargets = policy.ComboRules.Values.Select(r => (r, Array.IndexOf(packed.Categories, r.CategoryId), "Combo:" + r.CategoryId)).ToArray();
    }

    internal InventoryOptimizationScore Score(int[] output, int candidate, IDictionary<string, InventoryTargetSearchEvidence> evidence)
    {
        int start = candidate * packed.ResultStride;
        if (output[start] != 1) return null;
        int prioritySatisfied = 0, priorityPoints = 0, avoided = 0, presetSatisfied = 0, presetPoints = 0;
        int[] ordered = priorityCount == 0 ? Array.Empty<int>() : new int[priorityCount];
        foreach (var target in artifactTargets)
        {
            var rule = target.Rule;
            int active = 0, value = target.Indexes.Length == 0 ? 0 : int.MinValue, points = 0;
            bool reached = false;
            foreach (int index in target.Indexes)
            {
                int p = start + 1 + index * 3;
                bool enabled = output[p] != 0;
                int level = enabled ? output[p + 2] : 0;
                if (enabled) active++;
                value = Math.Max(value, level);
                reached |= enabled && level >= rule.MinimumEffectiveLevel;
                points = Math.Max(points, InventoryOptimizationScorer.CalculateTargetCompletionPoints(enabled, level, rule.MinimumEffectiveLevel));
            }
            if (rule.Level == InventoryPreferenceLevel.Avoid)
            {
                avoided += active;
                value = active;
                reached = target.Indexes.Length != 0 && active == 0;
                points = reached ? InventoryOptimizationScorer.TargetCompletionScale : 0;
            }
            else if (target.Instance)
            {
                if (rule.Level == InventoryPreferenceLevel.Priority)
                {
                    if (reached) prioritySatisfied++;
                    priorityPoints += points;
                    if (rule.PriorityOrder >= 0 && rule.PriorityOrder < ordered.Length) ordered[rule.PriorityOrder] = points;
                }
            }
            else
            {
                if (reached) presetSatisfied++;
                presetPoints += points;
            }
            Observe(evidence, target.Key, value, points, reached);
        }
        int categoriesStart = start + 1 + packed.Snapshot.Items.Count * 3;
        foreach (var (rule, index, key) in comboTargets)
        {
            int value = index < 0 ? 0 : output[categoriesStart + index];
            bool reached = rule.Level == InventoryPreferenceLevel.Avoid ? value <= rule.TargetCount : value >= rule.TargetCount;
            int points = rule.Level == InventoryPreferenceLevel.Avoid ? (reached ? InventoryOptimizationScorer.TargetCompletionScale : 0) :
                InventoryOptimizationScorer.CalculateTargetCompletionPoints(true, value, rule.TargetCount);
            if (rule.Level == InventoryPreferenceLevel.Avoid) { if (!reached) avoided++; }
            else if (rule.Level == InventoryPreferenceLevel.Priority)
            {
                if (rule.Source == InventoryPreferenceSource.NativePreset) { if (reached) presetSatisfied++; presetPoints += points; }
                else { if (reached) prioritySatisfied++; priorityPoints += points; }
            }
            Observe(evidence, key, value, points, reached);
        }
        int basic = start + packed.ResultStride - 7;
        return new InventoryOptimizationScore(prioritySatisfied, priorityPoints, avoided, presetSatisfied, presetPoints,
            output[basic], output[basic + 1], output[basic + 2], output[basic + 3], output[basic + 4], output[basic + 5], output[basic + 6], ordered);
    }

    private static void Observe(IDictionary<string, InventoryTargetSearchEvidence> evidence, string key, int value, int points, bool reached)
    {
        if (evidence.TryGetValue(key, out var prior)) prior.Observe(value, points, reached);
        else evidence.Add(key, new InventoryTargetSearchEvidence(value, points, reached));
    }

    private sealed class ArtifactTarget
    {
        internal readonly ResolvedArtifactOptimizationRule Rule;
        internal readonly bool Instance;
        internal readonly int[] Indexes;
        internal readonly string Key;
        internal ArtifactTarget(ResolvedArtifactOptimizationRule rule, bool instance, int[] indexes)
        {
            Rule = rule; Instance = instance; Indexes = indexes;
            Key = "Artifact:" + rule.EntityId + ":" + (instance && rule.ItemKey.NativeInstanceId >= 0 ? rule.ItemKey.NativeInstanceId.ToString() : "*");
        }
    }
}
