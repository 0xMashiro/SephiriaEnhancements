using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal sealed class InventoryDamagePriorityFixture
{
    internal readonly int[] Levels;
    internal static readonly int[] Original = { 0, 1, 2, 4, 3, 5 };
    internal InventoryDamagePriorityFixture(int[]? levels = null) { Levels = levels ?? Enumerable.Repeat(6, 6).ToArray(); }
    internal static InventoryItemKey Key(int i) => new(i == 2 || i == 3 ? 30000 : 10000 + i, i);
    internal (int Source, int Target, double Value)[] Observe(int[] positions)
    {
        var values = new List<(int, int, double)>();
        for (int root = 0; root < 2; root++)
        {
            var queue = new Queue<int>(); queue.Enqueue(root); var seen = new HashSet<int> { root };
            while (queue.Count > 0)
            {
                int target = queue.Dequeue();
                foreach (int source in new[] { 2, 3 })
                {
                    if (positions[source] - 2 != positions[target] || !seen.Add(source)) continue;
                    int level = Math.Clamp(Levels[positions[source]], 0, 6);
                    values.Add((source, root, level * (root == 0 ? 7 : 5))); queue.Enqueue(source);
                }
            }
        }
        return values.ToArray();
    }
    internal InventorySnapshot Snapshot(int[] positions)
    {
        var items = positions.Select((cell, i) =>
        {
            bool enabled = Levels[cell] >= 0;
            var a = new ArtifactSnapshot(Levels[cell], 6, 0, 0, enabled ? Math.Min(6, Levels[cell]) : 0, enabled, !enabled, false, "", true, false, false, "Pre",
                new CriteriaSnapshot(ArtifactActivationConditionKind.None, CriteriaEvaluationState.NotApplicable, CriteriaEvaluationState.NotApplicable),
                Array.Empty<string>(), Array.Empty<string>(), i < 2, null,
                i == 2 || i == 3 ? new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.DependencyTarget, targetY: -1) : ArtifactCategoryRuleSnapshot.Static);
            return new InventoryItemSnapshot(i, Key(i).EntityId, 1, cell, cell % 2, cell / 2, "Synthetic", "", "Charm", "Normal", Array.Empty<string>(), InventoryItemKind.Artifact, a, null);
        }).ToArray();
        var cells = Levels.Select((v, c) => new InventoryCellSnapshot(c, c % 2, c / 2, v, 6, 0, 0, 0, 0, false, new InventoryCellSettlementSnapshot(true, v, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        var rules = new[] { 2, 3 }.Select(i => new InventoryPositionEffectRule(Key(i), InventoryPositionEffectKind.DependencyDamage,
            Enumerable.Range(0, 7).Select(l => (double)(l * 5)).ToArray(), Enumerable.Range(0, 7).Select(l => (double)(l * 2)).ToArray(),
            new[] { new InventoryOffsetSnapshot(0, -1) }, conditionalDamage: true, maximumRarity: 1)).ToArray();
        return new InventorySnapshot(2, 6, cells, items, positionEffects: new InventoryPositionEffectsSnapshot(rules,
            Enumerable.Range(0, 6).Select(i => new InventoryPositionTargetTraits(Key(i), false, false, true, i == 1 ? 3 : 1)).ToArray(),
            Observe(positions).Select(v => new InventoryPositionEffectValue(new InventoryPositionEffectKey(Key(v.Source), InventoryPositionEffectKind.DependencyDamage, Key(v.Target)), v.Value)).ToArray(), null));
    }
    internal static InventoryOptimizationPreferences Preferences(int[] order, int minimum = 0, int hardTarget = -1)
        => new(InventorySearchEffort.Thorough, false, order.Select((i, rank) => new ArtifactOptimizationPreference(i, Key(i).EntityId, InventoryPreferenceLevel.Priority,
            minimum, rank, strength: i == hardTarget ? InventoryConstraintStrength.Hard : InventoryConstraintStrength.Soft)).ToArray(), Array.Empty<ComboOptimizationPreference>());
}
