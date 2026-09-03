#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal enum InventoryPositionEffectKind
    {
        NeighborArtifactLevelDamage,
        AdjacentPlanetEnhancement,
        SameRowCompanionMode,
        MagicCostReduction,
        MagicCooldownRecovery,
        FirstSlotsElementDamage,
        HalfBoardStats,
        HalfBoardWeaponMode,
        DependencyDamage,
        RowCategoryStats
    }

    internal sealed class InventoryPositionEffectRule
    {
        internal InventoryPositionEffectRule(InventoryItemKey source,
            InventoryPositionEffectKind kind, double[] valuesByLevel = null,
            double[] secondaryValuesByLevel = null, InventoryOffsetSnapshot[] offsets = null,
            int boundary = 0, string[] channels = null, string targetCategory = null,
            bool conditionalDamage = false, int maximumRarity = 0)
        {
            Source = source;
            Kind = kind;
            ValuesByLevel = Array.AsReadOnly((double[])(valuesByLevel ?? Array.Empty<double>()).Clone());
            SecondaryValuesByLevel = Array.AsReadOnly((double[])(secondaryValuesByLevel ?? Array.Empty<double>()).Clone());
            Offsets = Array.AsReadOnly((InventoryOffsetSnapshot[])(offsets ?? Array.Empty<InventoryOffsetSnapshot>()).Clone());
            Boundary = boundary;
            Channels = Array.AsReadOnly((string[])(channels ?? Array.Empty<string>()).Clone());
            TargetCategory = targetCategory ?? string.Empty;
            ConditionalDamage = conditionalDamage;
            MaximumRarity = maximumRarity;
        }

        internal InventoryItemKey Source { get; }
        internal InventoryPositionEffectKind Kind { get; }
        internal IReadOnlyList<double> ValuesByLevel { get; }
        internal IReadOnlyList<double> SecondaryValuesByLevel { get; }
        internal IReadOnlyList<InventoryOffsetSnapshot> Offsets { get; }
        internal int Boundary { get; }
        internal IReadOnlyList<string> Channels { get; }
        internal string TargetCategory { get; }
        internal bool ConditionalDamage { get; }
        internal int MaximumRarity { get; }

        internal static double AtLevel(IReadOnlyList<double> values, int level) =>
            values.Count == 0 ? 0 : values[Math.Max(0, Math.Min(level, values.Count - 1))];
    }

    internal sealed class InventoryPositionTargetTraits
    {
        internal InventoryPositionTargetTraits(InventoryItemKey item, bool planet,
            bool companion, bool networkReady, int rarity, bool magicArtifact = false)
        {
            Item = item;
            Planet = planet;
            Companion = companion;
            NetworkReady = networkReady;
            Rarity = rarity;
            MagicArtifact = magicArtifact;
        }

        internal InventoryItemKey Item { get; }
        internal bool Planet { get; }
        internal bool Companion { get; }
        internal bool NetworkReady { get; }
        internal int Rarity { get; }
        internal bool MagicArtifact { get; }
    }

    internal readonly struct InventoryPositionEffectKey : IEquatable<InventoryPositionEffectKey>
    {
        internal InventoryPositionEffectKey(InventoryItemKey source,
            InventoryPositionEffectKind kind, InventoryItemKey? target = null,
            string channel = "")
        {
            Source = source;
            Kind = kind;
            Target = target;
            Channel = channel ?? string.Empty;
        }

        internal InventoryItemKey Source { get; }
        internal InventoryPositionEffectKind Kind { get; }
        internal InventoryItemKey? Target { get; }
        internal string Channel { get; }
        public bool Equals(InventoryPositionEffectKey other) => Source == other.Source &&
            Kind == other.Kind && Target == other.Target && Channel == other.Channel;
        public override bool Equals(object obj) => obj is InventoryPositionEffectKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Source, Kind, Target, Channel);
        public override string ToString() => Source + ":" + Kind + ":" + Target + ":" + Channel;
    }

    internal sealed class InventoryPositionEffectValue
    {
        internal InventoryPositionEffectValue(InventoryPositionEffectKey key,
            double value, bool mode = false)
        {
            Key = key;
            Value = value;
            Mode = mode;
        }

        internal InventoryPositionEffectKey Key { get; }
        internal double Value { get; }
        internal bool Mode { get; }
    }

    internal sealed class InventoryPositionEffectsSnapshot
    {
        internal static readonly InventoryPositionEffectsSnapshot Empty = new(null, null, null, null);

        internal InventoryPositionEffectsSnapshot(InventoryPositionEffectRule[] rules,
            InventoryPositionTargetTraits[] traits, InventoryPositionEffectValue[] observed,
            string[] issues, bool observationsAvailable = true)
        {
            Rules = Array.AsReadOnly((InventoryPositionEffectRule[])(rules ?? Array.Empty<InventoryPositionEffectRule>()).Clone());
            Traits = Array.AsReadOnly((InventoryPositionTargetTraits[])(traits ?? Array.Empty<InventoryPositionTargetTraits>()).Clone());
            Observed = Array.AsReadOnly((InventoryPositionEffectValue[])(observed ?? Array.Empty<InventoryPositionEffectValue>()).Clone());
            Issues = Array.AsReadOnly((string[])(issues ?? Array.Empty<string>()).Clone());
            ObservationsAvailable = observationsAvailable;
        }

        internal IReadOnlyList<InventoryPositionEffectRule> Rules { get; }
        internal IReadOnlyList<InventoryPositionTargetTraits> Traits { get; }
        internal IReadOnlyList<InventoryPositionEffectValue> Observed { get; }
        internal bool ObservationsAvailable { get; }
        internal IReadOnlyList<string> Issues { get; }
    }

    internal static class InventoryPositionEffectComparison
    {
        internal static bool ParametersMatch(InventoryPositionEffectsSnapshot source,
            InventoryPositionEffectsSnapshot current)
        {
            if (source.Issues.Count != 0 || current.Issues.Count != 0 ||
                source.ObservationsAvailable != current.ObservationsAvailable ||
                source.Rules.Count != current.Rules.Count ||
                source.Rules.Any(rule => rule == null) || current.Rules.Any(rule => rule == null) ||
                source.Rules.Select(rule => rule.Source).Distinct().Count() != source.Rules.Count ||
                current.Rules.Select(rule => rule.Source).Distinct().Count() != current.Rules.Count) return false;
            foreach (var before in source.Rules)
            {
                var after = current.Rules.SingleOrDefault(rule => rule.Source == before.Source);
                if (after == null || before.Kind != after.Kind || before.Boundary != after.Boundary ||
                    before.TargetCategory != after.TargetCategory || before.ConditionalDamage != after.ConditionalDamage ||
                    before.MaximumRarity != after.MaximumRarity ||
                    !before.ValuesByLevel.SequenceEqual(after.ValuesByLevel) ||
                    !before.SecondaryValuesByLevel.SequenceEqual(after.SecondaryValuesByLevel) ||
                    !before.Channels.SequenceEqual(after.Channels) ||
                    !before.Offsets.Select(offset => (offset.X, offset.Y)).SequenceEqual(
                        after.Offsets.Select(offset => (offset.X, offset.Y)))) return false;
            }
            if (source.Rules.Count == 0) return true;
            return source.Traits.Count == current.Traits.Count && source.Traits.All(before =>
                current.Traits.Any(after => before.Item == after.Item && before.Planet == after.Planet &&
                    before.Companion == after.Companion && before.NetworkReady == after.NetworkReady &&
                    before.Rarity == after.Rarity && before.MagicArtifact == after.MagicArtifact));
        }

        internal static string[] Differences(IEnumerable<InventoryPositionEffectValue> predicted,
            IEnumerable<InventoryPositionEffectValue> observed)
        {
            if (!TryIndex(predicted, out var left) || !TryIndex(observed, out var right))
                return new[] { "PositionEffectIdentityInvalid" };
            return left.Keys.Union(right.Keys).Where(key =>
            {
                left.TryGetValue(key, out var expected);
                right.TryGetValue(key, out var actual);
                return (expected?.Mode ?? false) != (actual?.Mode ?? false) ||
                    (expected?.Value ?? 0) != (actual?.Value ?? 0);
            }).Select(key => "PositionEffectMismatch:" + key).ToArray();
        }

        internal static bool TryIndex(IEnumerable<InventoryPositionEffectValue> values,
            out Dictionary<InventoryPositionEffectKey, InventoryPositionEffectValue> result)
        {
            result = new Dictionary<InventoryPositionEffectKey, InventoryPositionEffectValue>();
            foreach (var value in values)
                if (value == null || double.IsNaN(value.Value) || double.IsInfinity(value.Value) ||
                    !result.TryAdd(value.Key, value)) return false;
            return true;
        }
    }
}
