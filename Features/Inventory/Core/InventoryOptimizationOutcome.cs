#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryArtifactOutcome
    {
        internal InventoryArtifactOutcome(int instanceId, int entityId,
            string nameKey, bool beforeEnabled, bool afterEnabled,
            int beforeEffectiveLevel, int afterEffectiveLevel)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            NameKey = nameKey ?? string.Empty;
            BeforeEnabled = beforeEnabled;
            AfterEnabled = afterEnabled;
            BeforeEffectiveLevel = beforeEffectiveLevel;
            AfterEffectiveLevel = afterEffectiveLevel;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal string NameKey { get; }
        internal bool BeforeEnabled { get; }
        internal bool AfterEnabled { get; }
        internal int BeforeEffectiveLevel { get; }
        internal int AfterEffectiveLevel { get; }
    }

    internal sealed class InventoryCategoryOutcome
    {
        internal InventoryCategoryOutcome(string categoryId, int beforeCount,
            int afterCount, int beforeBreakpointValue,
            int afterBreakpointValue)
        {
            CategoryId = categoryId ?? string.Empty;
            BeforeCount = beforeCount;
            AfterCount = afterCount;
            BeforeBreakpointValue = beforeBreakpointValue;
            AfterBreakpointValue = afterBreakpointValue;
        }

        internal string CategoryId { get; }
        internal int BeforeCount { get; }
        internal int AfterCount { get; }
        internal int BeforeBreakpointValue { get; }
        internal int AfterBreakpointValue { get; }
    }

    internal sealed class InventoryOptimizationOutcome
    {
        internal InventoryOptimizationOutcome(int movedItems,
            int rotatedTablets, int beforeArtifactsEnabled,
            int afterArtifactsEnabled, int beforeEffectiveLevels,
            int afterEffectiveLevels, int beforeBreakpointValue,
            int afterBreakpointValue,
            InventoryArtifactOutcome[] artifactChanges,
            InventoryCategoryOutcome[] categoryChanges,
            InventoryPositionEffectValue[] beforePositionEffects = null,
            InventoryPositionEffectValue[] afterPositionEffects = null)
        {
            MovedItems = movedItems;
            RotatedTablets = rotatedTablets;
            BeforeArtifactsEnabled = beforeArtifactsEnabled;
            AfterArtifactsEnabled = afterArtifactsEnabled;
            BeforeEffectiveLevels = beforeEffectiveLevels;
            AfterEffectiveLevels = afterEffectiveLevels;
            BeforeBreakpointValue = beforeBreakpointValue;
            AfterBreakpointValue = afterBreakpointValue;
            ArtifactChanges = Array.AsReadOnly(artifactChanges ??
                Array.Empty<InventoryArtifactOutcome>());
            CategoryChanges = Array.AsReadOnly(categoryChanges ??
                Array.Empty<InventoryCategoryOutcome>());
            BeforePositionEffects = Array.AsReadOnly(beforePositionEffects ?? Array.Empty<InventoryPositionEffectValue>());
            AfterPositionEffects = Array.AsReadOnly(afterPositionEffects ?? Array.Empty<InventoryPositionEffectValue>());
        }

        internal int MovedItems { get; }
        internal int RotatedTablets { get; }
        internal int BeforeArtifactsEnabled { get; }
        internal int AfterArtifactsEnabled { get; }
        internal int BeforeEffectiveLevels { get; }
        internal int AfterEffectiveLevels { get; }
        internal int BeforeBreakpointValue { get; }
        internal int AfterBreakpointValue { get; }
        internal IReadOnlyList<InventoryArtifactOutcome> ArtifactChanges
        { get; }
        internal IReadOnlyList<InventoryCategoryOutcome> CategoryChanges
        { get; }
        internal IReadOnlyList<InventoryPositionEffectValue> BeforePositionEffects { get; }
        internal IReadOnlyList<InventoryPositionEffectValue> AfterPositionEffects { get; }
    }

    internal static class InventoryOptimizationOutcomeBuilder
    {
        internal static InventoryOptimizationOutcome Build(
            InventorySnapshot snapshot,
            ProjectedInventorySettlement before,
            ProjectedInventorySettlement after,
            InventoryOptimizationScore beforeScore,
            InventoryOptimizationScore afterScore)
        {
            if (snapshot == null || before?.Succeeded != true ||
                after?.Succeeded != true || beforeScore == null ||
                afterScore == null)
            {
                return null;
            }

            var beforeArtifacts = before.Artifacts.ToDictionary(
                artifact => artifact.ItemKey);
            var afterArtifacts = after.Artifacts.ToDictionary(
                artifact => artifact.ItemKey);
            var artifactChanges = new List<InventoryArtifactOutcome>();
            foreach (InventoryItemSnapshot item in snapshot.Items)
            {
                if (item.Artifact == null ||
                    !beforeArtifacts.TryGetValue(item.ItemKey,
                        out ProjectedInventoryArtifactSettlement beforeArtifact) ||
                    !afterArtifacts.TryGetValue(item.ItemKey,
                        out ProjectedInventoryArtifactSettlement afterArtifact))
                {
                    continue;
                }
                if (beforeArtifact.Enabled == afterArtifact.Enabled &&
                    beforeArtifact.CappedEffectiveLevel ==
                        afterArtifact.CappedEffectiveLevel)
                {
                    continue;
                }
                artifactChanges.Add(new InventoryArtifactOutcome(
                    item.InstanceId, item.EntityId, item.NameKey,
                    beforeArtifact.Enabled, afterArtifact.Enabled,
                    beforeArtifact.CappedEffectiveLevel,
                    afterArtifact.CappedEffectiveLevel));
            }

            var categoryChanges = new List<InventoryCategoryOutcome>();
            foreach (ComboCategorySnapshot category in
                snapshot.ComboCategories)
            {
                before.ComboCounts.TryGetValue(category.CategoryId,
                    out int beforeCount);
                after.ComboCounts.TryGetValue(category.CategoryId,
                    out int afterCount);
                int beforeBreakpoint = InventoryOptimizationScorer.
                    CalculateReachedBreakpointValue(category, beforeCount);
                int afterBreakpoint = InventoryOptimizationScorer.
                    CalculateReachedBreakpointValue(category, afterCount);
                if (beforeCount == afterCount &&
                    beforeBreakpoint == afterBreakpoint)
                {
                    continue;
                }
                categoryChanges.Add(new InventoryCategoryOutcome(
                    category.CategoryId, beforeCount, afterCount,
                    beforeBreakpoint, afterBreakpoint));
            }

            return new InventoryOptimizationOutcome(afterScore.MovedItemCount,
                afterScore.RotatedTabletCount,
                beforeScore.EnabledArtifactCount,
                afterScore.EnabledArtifactCount,
                beforeScore.CappedEffectiveArtifactLevelTotal,
                afterScore.CappedEffectiveArtifactLevelTotal,
                beforeScore.ComboBreakpointValue,
                afterScore.ComboBreakpointValue,
                artifactChanges.ToArray(), categoryChanges.ToArray(),
                before.PositionEffects.ToArray(), after.PositionEffects.ToArray());
        }
    }
}
