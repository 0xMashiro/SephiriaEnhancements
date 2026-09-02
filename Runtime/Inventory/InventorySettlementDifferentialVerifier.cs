#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class InventoryMechanicCoverageSnapshot
    {
        internal InventoryMechanicCoverageSnapshot(InventorySnapshot snapshot)
        {
            if (snapshot == null)
            {
                NativeItemTypes = Array.Empty<string>();
                ActivationConditions = Array.Empty<string>();
                DynamicCategoryKinds = Array.Empty<string>();
                PositionEffectKinds = Array.Empty<string>();
                return;
            }

            ArtifactCount = snapshot.Items.Count(item => item.Artifact != null);
            PositionEffectSourceCount = snapshot.PositionEffects.Rules.Count;
            PositionEffectKinds = snapshot.PositionEffects.Rules.Select(rule => rule.Kind.ToString())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            RestrictedArtifactCount = snapshot.Items.Count(item =>
                item.Kind == InventoryItemKind.RestrictedArtifact);
            EnchantedArtifactCount = snapshot.Items.Count(item =>
                item.Artifact != null && item.Artifact.Enchant != 0);
            UniqueArtifactCount = snapshot.Items.Count(item =>
                item.Artifact?.UniqueEffect == true);
            WeaponRestrictedArtifactCount = snapshot.Items.Count(item =>
                item.Artifact?.WeaponRestricted == true);
            DynamicCategoryArtifactCount = snapshot.Items.Count(item =>
                item.Artifact != null && item.Artifact.CategoryRule.Kind !=
                    ArtifactCategoryRuleKind.Static);
            TabletCount = snapshot.Items.Count(item => item.StoneTablet != null);
            RotatableTabletCount = snapshot.Items.Count(item =>
                item.StoneTablet?.Rotatable == true);
            FixedTabletCount = snapshot.FixedTabletSources.Count;
            MysticCellCount = snapshot.Cells.Count(cell => cell.Mystic);
            OtherItemCount = snapshot.Items.Count(item =>
                item.Kind == InventoryItemKind.Other);
            NativeItemTypes = snapshot.Items.Select(item =>
                    item.NativeType.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            ActivationConditions = snapshot.Items.Select(item =>
                    item.Artifact?.Criteria?.Kind ??
                        ArtifactActivationConditionKind.None)
                .Where(value => value != ArtifactActivationConditionKind.None)
                .Select(value => value.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            DynamicCategoryKinds = snapshot.Items.Where(item =>
                    item.Artifact != null && item.Artifact.CategoryRule.Kind !=
                        ArtifactCategoryRuleKind.Static)
                .Select(item => item.Artifact.CategoryRule.Kind.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        internal int ArtifactCount { get; }
        internal int PositionEffectSourceCount { get; }
        internal IReadOnlyList<string> PositionEffectKinds { get; }
        internal int RestrictedArtifactCount { get; }
        internal int EnchantedArtifactCount { get; }
        internal int UniqueArtifactCount { get; }
        internal int WeaponRestrictedArtifactCount { get; }
        internal int DynamicCategoryArtifactCount { get; }
        internal int TabletCount { get; }
        internal int RotatableTabletCount { get; }
        internal int FixedTabletCount { get; }
        internal int MysticCellCount { get; }
        internal int OtherItemCount { get; }
        internal IReadOnlyList<string> NativeItemTypes { get; }
        internal IReadOnlyList<string> ActivationConditions { get; }
        internal IReadOnlyList<string> DynamicCategoryKinds { get; }
    }

    internal sealed class InventorySettlementDifferentialReport
    {
        internal InventorySettlementDifferentialReport(string[] mismatches,
            InventoryMechanicCoverageSnapshot coverage = null)
        {
            Mismatches = Array.AsReadOnly(mismatches ?? Array.Empty<string>());
            Coverage = coverage;
        }

        internal IReadOnlyList<string> Mismatches { get; }
        internal InventoryMechanicCoverageSnapshot Coverage { get; }
        internal bool Matched => Mismatches.Count == 0;
    }

    internal static class InventorySettlementDifferentialVerifier
    {
        internal static InventorySettlementDifferentialReport Compare(
            InventorySnapshot source, InventoryLayoutProjection targetLayout,
            ProjectedInventorySettlement expected, InventorySnapshot actual)
        {
            var mismatches = new List<string>();
            var coverage = new InventoryMechanicCoverageSnapshot(source);
            if (source == null || targetLayout == null || expected == null ||
                actual == null || !expected.Succeeded)
            {
                return new InventorySettlementDifferentialReport(
                    new[] { "DifferentialInputUnavailable" }, coverage);
            }
            if (source.Width != actual.Width ||
                source.Storage != actual.Storage ||
                targetLayout.ItemCount != source.Items.Count)
            {
                return new InventorySettlementDifferentialReport(
                    new[] { "DifferentialInventoryShapeMismatch" }, coverage);
            }

            if (actual.Items.Count != source.Items.Count)
            {
                mismatches.Add("ItemCount");
            }
            var actualItems = new Dictionary<InventoryItemKey, InventoryItemSnapshot>();
            foreach (InventoryItemSnapshot item in actual.Items)
            {
                if (item == null || !actualItems.TryAdd(item.ItemKey, item))
                {
                    return new InventorySettlementDifferentialReport(
                        new[] { "DifferentialItemIdentityInvalid" }, coverage);
                }
            }
            for (int index = 0; index < source.Items.Count; index++)
            {
                InventoryItemSnapshot sourceItem = source.Items[index];
                if (!actualItems.TryGetValue(sourceItem.ItemKey,
                        out InventoryItemSnapshot actualItem))
                {
                    mismatches.Add("ItemMissing:" + sourceItem.ItemKey);
                    continue;
                }
                if (actualItem.Quantity != sourceItem.Quantity)
                {
                    mismatches.Add("ItemQuantity:" + sourceItem.ItemKey);
                }
                if (actualItem.CellIndex != targetLayout.GetCell(index))
                {
                    mismatches.Add("ItemCell:" + sourceItem.ItemKey);
                }
                if (sourceItem.StoneTablet != null &&
                    (actualItem.StoneTablet == null ||
                     actualItem.StoneTablet.Rotation !=
                        targetLayout.GetRotation(index)))
                {
                    mismatches.Add("TabletRotation:" + sourceItem.ItemKey);
                }
            }

            if (expected.Cells.Count != actual.Cells.Count)
            {
                mismatches.Add("CellCount");
            }
            int cellCount = Math.Min(expected.Cells.Count, actual.Cells.Count);
            for (int cell = 0; cell < cellCount; cell++)
            {
                ProjectedInventoryCellSettlement predicted = expected.Cells[cell];
                InventoryCellSnapshot observed = actual.Cells[cell];
                if (predicted.Level != observed.Level)
                    mismatches.Add("CellLevel:" + cell);
                if (predicted.MaximumLevel != observed.MaxLevel)
                    mismatches.Add("CellMaximumLevel:" + cell);
                if (predicted.TemporaryLevel != observed.TemporaryLevel)
                    mismatches.Add("CellTemporaryLevel:" + cell);
                if (predicted.LevelMultiplier != observed.LevelMultiplier)
                    mismatches.Add("CellMultiplier:" + cell);
                if (predicted.DisableCount != observed.DisableCount)
                    mismatches.Add("CellDisable:" + cell);
                if (predicted.CriteriaBypassCount !=
                    observed.IgnoreCriteriaCount)
                    mismatches.Add("CellCriteriaBypass:" + cell);
            }

            Dictionary<InventoryItemKey, ProjectedInventoryArtifactSettlement> expectedArtifacts =
                expected.Artifacts.ToDictionary(item => item.ItemKey);
            foreach (KeyValuePair<InventoryItemKey, ProjectedInventoryArtifactSettlement> pair in
                expectedArtifacts)
            {
                if (!actualItems.TryGetValue(pair.Key,
                        out InventoryItemSnapshot actualItem) ||
                    actualItem.Artifact == null)
                {
                    mismatches.Add("ArtifactMissing:" + pair.Key);
                    continue;
                }
                ProjectedInventoryArtifactSettlement predicted = pair.Value;
                ArtifactSnapshot observed = actualItem.Artifact;
                if (predicted.DisplayedLevel != observed.DisplayedLevel)
                    mismatches.Add("ArtifactDisplayedLevel:" + pair.Key);
                if (predicted.Enabled != observed.EffectEnabled)
                    mismatches.Add("ArtifactEnabled:" + pair.Key);
                if (predicted.PenaltyEnabled != observed.PenaltyEnabled)
                    mismatches.Add("ArtifactPenalty:" + pair.Key);
                if (predicted.CappedEffectiveLevel !=
                    observed.LimitedEffectEnabledLevel)
                    mismatches.Add("ArtifactEffectiveLevel:" + pair.Key);
            }

            Dictionary<string, int> actualCombos = actual.ComboCategories
                .ToDictionary(category => category.CategoryId,
                    category => category.CurrentCount, StringComparer.Ordinal);
            foreach (string category in expected.ComboCounts.Keys.Union(
                actualCombos.Keys, StringComparer.Ordinal))
            {
                int predicted = expected.ComboCounts.TryGetValue(category,
                    out int predictedCount) ? predictedCount : 0;
                int observed = actualCombos.TryGetValue(category,
                    out int observedCount) ? observedCount : 0;
                if (predicted != observed)
                    mismatches.Add("ComboCount:" + category);
            }

            Dictionary<(InventoryItemKey ItemKey, bool Fixed),
                ProjectedInventoryTabletSettlement> expectedTablets = expected.Tablets
                .ToDictionary(item => (item.ItemKey, item.FixedSource));
            foreach (ProjectedInventoryTabletSettlement predicted in
                expectedTablets.Values)
            {
                bool found;
                bool applied;
                if (predicted.FixedSource)
                {
                    FixedTabletSourceSnapshot observed = actual.FixedTabletSources
                        .FirstOrDefault(item => item.ItemKey ==
                            predicted.ItemKey);
                    found = observed != null;
                    applied = observed?.Applied == true;
                }
                else
                {
                    found = actualItems.TryGetValue(predicted.ItemKey,
                        out InventoryItemSnapshot observed) &&
                        observed.StoneTablet != null;
                    applied = found && observed.StoneTablet.Applied;
                }
                if (!found)
                    mismatches.Add("TabletMissing:" + predicted.ItemKey);
                else if (predicted.Applied != applied)
                    mismatches.Add("TabletApplied:" + predicted.ItemKey);
            }

            mismatches.AddRange(actual.PositionEffects.Issues);
            if (!InventoryPositionEffectComparison.ParametersMatch(source.PositionEffects, actual.PositionEffects))
                mismatches.Add("PositionEffectParametersChanged");
            mismatches.AddRange(InventoryPositionEffectComparison.Differences(
                expected.PositionEffects, actual.PositionEffects.Observed));
            return new InventorySettlementDifferentialReport(mismatches
                .Distinct(StringComparer.Ordinal).ToArray(), coverage);
        }
    }
}
