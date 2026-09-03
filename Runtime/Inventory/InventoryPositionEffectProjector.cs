#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class InventoryPositionEffectProjector
    {
        private readonly InventorySnapshot snapshot;
        private readonly Dictionary<InventoryItemKey, int> indexes;
        private readonly Dictionary<InventoryItemKey, InventoryPositionTargetTraits> traits;
        private readonly Dictionary<InventoryItemKey, InventoryPositionEffectRule> dependencyRules;
        private readonly ulong[] visitedDependencies;
        private readonly Dictionary<InventoryItemKey, ProjectedInventoryArtifactSettlement> settlement = new();

        internal InventoryPositionEffectProjector(InventorySnapshot snapshot)
        {
            this.snapshot = snapshot;
            indexes = snapshot.Items.Select((item, index) => (item.ItemKey, index))
                .ToDictionary(pair => pair.ItemKey, pair => pair.index);
            traits = snapshot.PositionEffects.Traits.ToDictionary(item => item.Item);
            dependencyRules = snapshot.PositionEffects.Rules.Where(rule =>
                rule.Kind == InventoryPositionEffectKind.DependencyDamage).ToDictionary(rule => rule.Source);
            visitedDependencies = dependencyRules.Count == 0 ? Array.Empty<ulong>() :
                new ulong[(snapshot.Items.Count + 63) / 64];
        }

        internal static InventoryPositionEffectValue[] Evaluate(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, IReadOnlyList<ProjectedInventoryArtifactSettlement> artifacts)
        {
            if (snapshot.PositionEffects.Rules.Count == 0) return Array.Empty<InventoryPositionEffectValue>();
            return new InventoryPositionEffectProjector(snapshot).Evaluate(layout, artifacts);
        }

        internal InventoryPositionEffectValue[] Evaluate(InventoryLayoutProjection layout,
            IReadOnlyList<ProjectedInventoryArtifactSettlement> artifacts)
        {
            var result = new List<InventoryPositionEffectValue>();
            settlement.Clear();
            foreach (var artifact in artifacts) settlement.Add(artifact.ItemKey, artifact);

            foreach (var rule in snapshot.PositionEffects.Rules)
            {
                int sourceIndex = indexes[rule.Source];
                int sourceCell = layout.GetCell(sourceIndex);
                int sourceX = sourceCell % snapshot.Width;
                int sourceY = sourceCell / snapshot.Width;
                var source = settlement[rule.Source];
                double value = InventoryPositionEffectRule.AtLevel(rule.ValuesByLevel, source.CappedEffectiveLevel);
                double secondary = InventoryPositionEffectRule.AtLevel(rule.SecondaryValuesByLevel, source.CappedEffectiveLevel);
                bool enabled = source.Enabled;
                switch (rule.Kind)
                {
                    case InventoryPositionEffectKind.RowCategoryStats:
                        // Channels follow the captured category cycle, including repeated entries.
                        // Category membership is projected separately and survives deactivation.
                        Add(enabled ? value : 0, channel: rule.Channels[sourceY % rule.Channels.Count]);
                        break;
                    case InventoryPositionEffectKind.NeighborArtifactLevelDamage:
                        float damage = 0;
                        if (enabled)
                            foreach (int index in OffsetTargets(rule.Offsets))
                                if (snapshot.Items[index].Artifact != null)
                                {
                                    var target = settlement[snapshot.Items[index].ItemKey];
                                    // Native counts displayed levels even when the target is inactive.
                                    damage += (float)value * Math.Min(target.DisplayedLevel,
                                        snapshot.Items[index].Artifact.MaxLevel);
                                }
                        Add(Math.Floor(damage));
                        break;
                    case InventoryPositionEffectKind.AdjacentPlanetEnhancement:
                        if (enabled)
                            foreach (int index in OffsetTargets(rule.Offsets))
                            {
                                var item = snapshot.Items[index];
                                if (traits.TryGetValue(item.ItemKey, out var target) && target.Planet &&
                                    item.BaseCategories.Contains(rule.TargetCategory)) Add(1, item.ItemKey);
                            }
                        break;
                    case InventoryPositionEffectKind.SameRowCompanionMode:
                        if (enabled)
                            for (int index = 0; index < snapshot.Items.Count; index++)
                                if (layout.GetCell(index) / snapshot.Width == sourceY &&
                                    traits.TryGetValue(snapshot.Items[index].ItemKey, out var target) && target.Companion)
                                    Add(1, target.Item);
                        break;
                    case InventoryPositionEffectKind.MagicCostReduction:
                    case InventoryPositionEffectKind.MagicCooldownRecovery:
                        if (enabled)
                            foreach (int index in OffsetTargets(rule.Offsets))
                                if (traits.TryGetValue(snapshot.Items[index].ItemKey, out var magic) && magic.MagicArtifact)
                                    Add(value, snapshot.Items[index].ItemKey);
                        break;
                    case InventoryPositionEffectKind.FirstSlotsElementDamage:
                        int count = enabled ? Enumerable.Range(0, snapshot.Items.Count).Count(index =>
                            snapshot.Items[index].NativeType == NativeInventoryItemType.Charm &&
                            layout.GetCell(index) < rule.Boundary) : 0;
                        foreach (string channel in rule.Channels) Add(value * count, channel: channel);
                        break;
                    case InventoryPositionEffectKind.HalfBoardStats:
                        bool left = sourceX <= rule.Boundary;
                        Add(enabled ? (left ? value : secondary) : 0, channel: rule.Channels[0]);
                        Add(enabled ? (left ? secondary : value) : 0, channel: rule.Channels[1]);
                        Add(enabled ? (left ? 0 : 1) : -1, channel: "Mode", mode: true);
                        break;
                    case InventoryPositionEffectKind.HalfBoardWeaponMode:
                        bool leftMode = sourceX <= rule.Boundary;
                        Add(enabled && leftMode ? 1 : 0, channel: rule.Channels[0]);
                        Add(enabled && !leftMode ? 1 : 0, channel: rule.Channels[1]);
                        Add(enabled ? (leftMode ? 0 : 1) : -1, channel: "Mode", mode: true);
                        break;
                    case InventoryPositionEffectKind.DependencyDamage:
                        // Native dependency requests use level zero for inactive sources.
                        // Their network presence and target eligibility, not Enabled, gate the contribution.
                        if (!traits[rule.Source].NetworkReady) break;
                        foreach (var target in snapshot.Items.Where(item => item.Artifact != null &&
                            item.ItemKey != rule.Source && traits[item.ItemKey].NetworkReady))
                        {
                            // A dependency artifact rejects itself as a request root,
                            // so native traversal never visits its incoming dependencies.
                            bool eligible = target.Artifact.Attackable && !dependencyRules.ContainsKey(target.ItemKey);
                            if (!eligible) continue;
                            Array.Clear(visitedDependencies, 0, visitedDependencies.Length);
                            if (Reaches(rule.Source, target.ItemKey))
                            {
                                double bonus = value;
                                if (rule.ConditionalDamage && traits[target.ItemKey].Rarity <= rule.MaximumRarity)
                                    bonus += secondary;
                                Add(bonus, target.ItemKey);
                            }
                        }
                        break;
                }

                void Add(double amount, InventoryItemKey? target = null, string channel = "", bool mode = false) =>
                    result.Add(new InventoryPositionEffectValue(new InventoryPositionEffectKey(
                        rule.Source, rule.Kind, target, channel), amount, mode));

                IEnumerable<int> OffsetTargets(IReadOnlyList<InventoryOffsetSnapshot> offsets)
                {
                    foreach (var offset in offsets)
                    {
                        int x = sourceX + offset.X;
                        int y = sourceY + offset.Y;
                        if (x < 0 || x >= snapshot.Width || y < 0 || y * snapshot.Width + x >= snapshot.Storage) continue;
                        for (int index = 0; index < snapshot.Items.Count; index++)
                            if (layout.GetCell(index) == y * snapshot.Width + x) yield return index;
                    }
                }
            }
            return result.ToArray();

            bool Reaches(InventoryItemKey from, InventoryItemKey target)
            {
                int sourceIndex = indexes[from];
                ulong bit = 1UL << (sourceIndex & 63);
                int word = sourceIndex >> 6;
                if ((visitedDependencies[word] & bit) != 0) return false;
                visitedDependencies[word] |= bit;
                if (!dependencyRules.TryGetValue(from, out var rule) ||
                    !traits[from].NetworkReady) return false;
                int origin = layout.GetCell(sourceIndex);
                var offset = rule.Offsets[0];
                int x = origin % snapshot.Width + offset.X;
                int y = origin / snapshot.Width + offset.Y;
                if (x < 0 || x >= snapshot.Width || y < 0 || y * snapshot.Width + x >= snapshot.Storage) return false;
                for (int index = 0; index < snapshot.Items.Count; index++)
                {
                    var item = snapshot.Items[index];
                    if (layout.GetCell(index) != y * snapshot.Width + x || item.Artifact == null) continue;
                    if (item.ItemKey == target) return true;
                    return Reaches(item.ItemKey, target);
                }
                return false;
            }
        }
    }
}
