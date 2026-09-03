#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal static class InventorySettlementProjector
    {
        private const int ArtifactConditionRowSpan = 6;
        private const int ArtifactConditionRightEdgeX = 5;

        private static readonly (int X, int Y)[] Neighbors =
        {
            (0, 1), (1, 1), (1, 0), (1, -1),
            (0, -1), (-1, -1), (-1, 0), (-1, 1)
        };

        internal static ProjectedInventorySettlement Evaluate(
            InventorySnapshot snapshot, InventoryLayoutProjection layout)
        {
            return EvaluateCore(snapshot, layout, includeDetails: true);
        }

        internal static ProjectedInventorySettlement EvaluateForScoring(
            InventorySnapshot snapshot, InventoryLayoutProjection layout,
            InventorySettlementProjectionWorkspace workspace)
        {
            return EvaluateCore(snapshot, layout, includeDetails: false,
                workspace);
        }

        private static ProjectedInventorySettlement EvaluateCore(
            InventorySnapshot snapshot, InventoryLayoutProjection layout,
            bool includeDetails,
            InventorySettlementProjectionWorkspace workspace = null)
        {
            List<string> issues = workspace?.Issues ?? new List<string>();
            issues.Clear();
            if (snapshot == null || layout == null)
            {
                return Failure("InputUnavailable");
            }
            if (!snapshot.SettlementValidation.LayoutProjectionReady)
            {
                return new ProjectedInventorySettlement(false, null, null, null,
                    snapshot.SettlementValidation.Issues.ToArray());
            }
            int storage = snapshot.Storage;
            int[] itemAtCell = workspace?.ItemAtCell ?? new int[storage];
            if (!TryBuildOccupancy(snapshot, layout, itemAtCell, issues))
            {
                return new ProjectedInventorySettlement(false, null, null, null,
                    issues.ToArray());
            }

            int[] additiveLevels = workspace?.AdditiveLevels ?? new int[storage];
            int[] multipliers = workspace?.Multipliers ?? new int[storage];
            int[] disables = workspace?.Disables ?? new int[storage];
            int[] bypasses = workspace?.Bypasses ?? new int[storage];
            int[] maximumLevels = workspace?.MaximumLevels ?? new int[storage];
            int[] temporaryLevels = workspace?.TemporaryLevels ??
                new int[storage];
            for (int cell = 0; cell < storage; cell++)
            {
                InventoryCellSettlementSnapshot source =
                    snapshot.Cells[cell].Settlement;
                additiveLevels[cell] = source.BaselineLevel + source.FixedLevel;
                multipliers[cell] = source.BaselineLevelMultiplier +
                    source.FixedLevelMultiplier;
                disables[cell] = source.BaselineDisableCount +
                    source.FixedDisableCount;
                bypasses[cell] = source.BaselineCriteriaBypassCount +
                    source.FixedCriteriaBypassCount;
                maximumLevels[cell] = source.BaselineMaximumLevel;
                temporaryLevels[cell] = source.BaselineTemporaryLevel;
            }

            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                ArtifactSnapshot artifact = snapshot.Items[itemIndex].Artifact;
                if (artifact != null)
                {
                    int cell = layout.GetCell(itemIndex);
                    additiveLevels[cell] += artifact.Enchant;
                    if (snapshot.ArtifactEffectsEnabled)
                    {
                        maximumLevels[cell] = artifact.MaxLevel;
                    }
                }
            }

            List<ProjectedInventoryTabletSettlement> tablets = includeDetails
                ? new List<ProjectedInventoryTabletSettlement>()
                : null;
            ApplyTablets(snapshot, layout, itemAtCell, additiveLevels,
                multipliers, disables, bypasses, issues, tablets);
            ApplyFixedTablets(snapshot, itemAtCell, additiveLevels, multipliers,
                disables, bypasses, issues, tablets);
            if (issues.Count != 0)
            {
                return new ProjectedInventorySettlement(false, null, null, null,
                    issues.ToArray());
            }

            ProjectedInventoryCellSettlement[] cells = includeDetails
                ? new ProjectedInventoryCellSettlement[storage]
                : Array.Empty<ProjectedInventoryCellSettlement>();
            if (includeDetails)
            {
                for (int cell = 0; cell < storage; cell++)
                {
                    int level = multipliers[cell] == 0
                        ? additiveLevels[cell]
                        : additiveLevels[cell] * multipliers[cell];
                    cells[cell] = new ProjectedInventoryCellSettlement(level,
                        maximumLevels[cell], temporaryLevels[cell],
                        multipliers[cell], disables[cell], bypasses[cell]);
                }
            }

            List<ProjectedInventoryArtifactSettlement> artifacts = workspace?.Artifacts ??
                new List<ProjectedInventoryArtifactSettlement>();
            artifacts.Clear();
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                InventoryItemSnapshot item = snapshot.Items[itemIndex];
                ArtifactSnapshot artifact = item.Artifact;
                if (artifact == null)
                {
                    continue;
                }

                int cellIndex = layout.GetCell(itemIndex);
                int level = multipliers[cellIndex] == 0
                    ? additiveLevels[cellIndex]
                    : additiveLevels[cellIndex] * multipliers[cellIndex];
                bool criteria = EvaluateCriteria(snapshot, itemIndex, cellIndex,
                    itemAtCell);
                bool enabled = snapshot.ArtifactEffectsEnabled &&
                    snapshot.GlobalActiveValue > 0 && disables[cellIndex] <= 0 &&
                    level >= 0 &&
                    (bypasses[cellIndex] > 0 || criteria) &&
                    artifact.WeaponCompatible;
                int capped = enabled
                    ? Math.Min(artifact.MaxLevel, level)
                    : 0;
                artifacts.Add(new ProjectedInventoryArtifactSettlement(item.ItemKey,
                    enabled, !enabled, level, capped));
            }

            Dictionary<string, int> combos = CountCombos(snapshot, layout,
                itemAtCell, workspace);
            return new ProjectedInventorySettlement(true, cells,
                artifacts.ToArray(), combos, Array.Empty<string>(),
                tablets?.ToArray(), workspace?.PositionEffectProjector != null
                    ? workspace.PositionEffectProjector.Evaluate(layout, artifacts)
                    : InventoryPositionEffectProjector.Evaluate(snapshot, layout, artifacts));
        }

        private static bool TryBuildOccupancy(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, int[] itemAtCell,
            List<string> issues)
        {
            Array.Fill(itemAtCell, -1, 0, snapshot.Storage);
            if (layout.ItemCount != snapshot.Items.Count)
            {
                issues.Add("LayoutItemCountMismatch");
                return false;
            }

            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                int cell = layout.GetCell(itemIndex);
                if (cell < 0 || cell >= snapshot.Storage)
                {
                    issues.Add("LayoutCellOutOfRange:" + itemIndex);
                    return false;
                }
                if (itemAtCell[cell] >= 0)
                {
                    issues.Add("LayoutCellCollision:" + cell);
                    return false;
                }
                itemAtCell[cell] = itemIndex;

                StoneTabletSnapshot stoneTablet =
                    snapshot.Items[itemIndex].StoneTablet;
                if (stoneTablet != null)
                {
                    int rotation = layout.GetRotation(itemIndex);
                    if (rotation < 0 || rotation > 3 ||
                        (!stoneTablet.Rotatable &&
                         rotation != stoneTablet.Rotation))
                    {
                        issues.Add("LayoutTabletRotationInvalid:" +
                            snapshot.Items[itemIndex].ItemKey);
                        return false;
                    }
                }
            }
            return true;
        }

        private static void ApplyTablets(InventorySnapshot snapshot,
            InventoryLayoutProjection layout, int[] itemAtCell,
            int[] levels, int[] multipliers, int[] disables, int[] bypasses,
            List<string> issues, List<ProjectedInventoryTabletSettlement> settlements)
        {
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                StoneTabletSnapshot stoneTablet =
                    snapshot.Items[itemIndex].StoneTablet;
                if (stoneTablet == null)
                {
                    continue;
                }

                int origin = layout.GetCell(itemIndex);
                TabletRotationProjectionSnapshot projection =
                    stoneTablet.FindProjection(origin,
                        layout.GetRotation(itemIndex));
                if (projection == null || !projection.ParseSucceeded)
                {
                    issues.Add("LayoutProjectionTabletEffectsUnavailable:" +
                        snapshot.Items[itemIndex].ItemKey);
                    continue;
                }
                bool applied = EvaluateTabletCondition(snapshot, projection,
                    origin, itemAtCell);
                settlements?.Add(new ProjectedInventoryTabletSettlement(
                    snapshot.Items[itemIndex].ItemKey, fixedSource: false,
                    applied, origin, layout.GetRotation(itemIndex)));
                if (!applied)
                {
                    continue;
                }

                ApplyTabletEffects(snapshot,
                    snapshot.Items[itemIndex].ItemKey, projection, levels,
                    multipliers, disables, bypasses, issues);
            }
        }

        private static void ApplyFixedTablets(InventorySnapshot snapshot,
            int[] itemAtCell, int[] levels, int[] multipliers, int[] disables,
            int[] bypasses, List<string> issues,
            List<ProjectedInventoryTabletSettlement> settlements)
        {
            foreach (FixedTabletSourceSnapshot source in
                snapshot.FixedTabletSources)
            {
                TabletRotationProjectionSnapshot projection = source.Projection;
                if (projection == null || !projection.ParseSucceeded ||
                    source.CellIndex < 0 || source.CellIndex >= snapshot.Storage)
                {
                    issues.Add("FixedTabletLayoutProjectionUnavailable:" +
                        source.ItemKey);
                    continue;
                }
                bool applied = EvaluateTabletCondition(snapshot, projection,
                    source.CellIndex, itemAtCell);
                settlements?.Add(new ProjectedInventoryTabletSettlement(
                    source.ItemKey, fixedSource: true, applied,
                    source.CellIndex, source.Rotation));
                if (!applied)
                {
                    continue;
                }
                ApplyTabletEffects(snapshot, source.ItemKey, projection,
                    levels, multipliers, disables, bypasses, issues);
            }
        }

        private static void ApplyTabletEffects(InventorySnapshot snapshot,
            InventoryItemKey sourceItemKey,
            TabletRotationProjectionSnapshot projection, int[] levels,
            int[] multipliers, int[] disables, int[] bypasses,
            List<string> issues)
        {
            foreach (TabletAdditionSnapshot effect in projection.Effects)
            {
                if (!effect.ValidCell)
                {
                    continue;
                }
                int cell = effect.Y * snapshot.Width + effect.X;
                switch (effect.EffectKind)
                {
                    case TabletEffectKind.IncreaseLevel:
                        levels[cell] += effect.LevelParameter;
                        break;
                    case TabletEffectKind.Disable:
                        disables[cell]++;
                        break;
                    case TabletEffectKind.IgnoreCriteria:
                        bypasses[cell]++;
                        break;
                    case TabletEffectKind.MultiplyLevel:
                        multipliers[cell] += effect.LevelParameter;
                        break;
                    default:
                        issues.Add("LayoutProjectionTabletEffectUnknown:" +
                            sourceItemKey);
                        break;
                }
            }
        }

        private static bool EvaluateTabletCondition(InventorySnapshot snapshot,
            TabletRotationProjectionSnapshot projection, int origin,
            int[] itemAtCell)
        {
            bool hasPlaced = false;
            bool placedHit = false;
            foreach (TabletAdditionSnapshot criterion in projection.Criteria)
            {
                int cell = criterion.Y * snapshot.Width + criterion.X;
                int itemIndex = criterion.ValidCell ? itemAtCell[cell] : -1;
                switch (criterion.CriteriaKind)
                {
                    case TabletCriteriaKind.AnyItem:
                        if (itemIndex < 0) return false;
                        break;
                    case TabletCriteriaKind.Artifact:
                        if (itemIndex < 0 ||
                            snapshot.Items[itemIndex].Artifact == null)
                            return false;
                        break;
                    case TabletCriteriaKind.Placed:
                        hasPlaced = true;
                        placedHit |= cell == origin;
                        break;
                    default:
                        return false;
                }
            }
            return !hasPlaced || placedHit;
        }

        private static bool EvaluateCriteria(InventorySnapshot snapshot,
            int itemIndex, int cellIndex, int[] itemAtCell)
        {
            CriteriaSnapshot criteria = snapshot.Items[itemIndex].Artifact.Criteria;
            if (criteria == null ||
                criteria.Kind == ArtifactActivationConditionKind.None ||
                criteria.RuntimeState == CriteriaEvaluationState.NotApplicable)
            {
                return true;
            }

            int width = snapshot.Width;
            int x = cellIndex % width;
            int y = cellIndex / width;
            switch (criteria.Kind)
            {
                case ArtifactActivationConditionKind.TopRow:
                    return y == 0;
                case ArtifactActivationConditionKind.BottomRow:
                    // This condition covers the trailing six valid cells. With
                    // a partial final row it intentionally spans two visual rows.
                    return cellIndex >= snapshot.Storage -
                        ArtifactConditionRowSpan;
                case ArtifactActivationConditionKind.SideEdge:
                    return x == 0 || x == ArtifactConditionRightEdgeX;
                case ArtifactActivationConditionKind.Interior:
                    return x > 0 && y > 0 && x < width - 1 &&
                        cellIndex + 7 <= snapshot.Storage - 1;
                case ArtifactActivationConditionKind.Border:
                    return x <= 0 || y <= 0 || x >= width - 1 ||
                        cellIndex >= snapshot.Storage -
                            ArtifactConditionRowSpan;
                case ArtifactActivationConditionKind.BothSidesEmpty:
                    {
                        int remainder = snapshot.Storage % width;
                        return x > 0 && x < width - 1 &&
                            (remainder == 0 || y < snapshot.Height - 1 ||
                             x < remainder - 1) &&
                            itemAtCell[cellIndex - 1] < 0 &&
                            itemAtCell[cellIndex + 1] < 0;
                    }
                case ArtifactActivationConditionKind.BothSidesArtifacts:
                    return x > 0 && x < width - 1 &&
                        cellIndex + 1 < snapshot.Storage &&
                        IsArtifact(snapshot, itemAtCell[cellIndex - 1]) &&
                        IsArtifact(snapshot, itemAtCell[cellIndex + 1]);
                case ArtifactActivationConditionKind.AllNeighborsOccupied:
                    return Neighbors.All(offset => IsOccupied(snapshot,
                        x + offset.X, y + offset.Y, itemAtCell));
                case ArtifactActivationConditionKind.AdjacentMagicArtifact:
                    return Neighbors.Any(offset => IsMagic(snapshot,
                        x + offset.X, y + offset.Y, itemAtCell));
                case ArtifactActivationConditionKind.FullHealth:
                    return criteria.RuntimeState ==
                        CriteriaEvaluationState.Satisfied;
                default:
                    return false;
            }
        }

        private static Dictionary<string, int> CountCombos(
            InventorySnapshot snapshot, InventoryLayoutProjection layout,
            int[] itemAtCell, InventorySettlementProjectionWorkspace workspace = null)
        {
            Dictionary<string, int> result = workspace?.ComboCounts ??
                new Dictionary<string, int>(StringComparer.Ordinal);
            result.Clear();
            HashSet<int> seenEntities = workspace?.SeenComboEntities ?? new HashSet<int>();
            seenEntities.Clear();
            IReadOnlyList<string>[] categories = BuildProjectedCategories(snapshot,
                layout, itemAtCell, workspace);
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                InventoryItemSnapshot item = snapshot.Items[itemIndex];
                if (item.Artifact == null ||
                    (snapshot.SuppressDuplicateComboEntities &&
                     !seenEntities.Add(item.EntityId)))
                {
                    continue;
                }
                foreach (string category in categories[itemIndex])
                {
                    result[category] = result.TryGetValue(category,
                        out int count) ? count + 1 : 1;
                }
            }

            foreach (ComboCategorySnapshot category in snapshot.ComboCategories)
            {
                int invariant = category.BonusCount +
                    category.InferredUniquePairCount;
                result[category.CategoryId] = result.TryGetValue(
                    category.CategoryId, out int count)
                    ? count + invariant
                    : invariant;
            }
            return result;
        }

        private static IReadOnlyList<string>[] BuildProjectedCategories(
            InventorySnapshot snapshot, InventoryLayoutProjection layout,
            int[] itemAtCell, InventorySettlementProjectionWorkspace workspace)
        {
            var result = workspace?.ProjectedCategories ?? new IReadOnlyList<string>[snapshot.Items.Count];
            Array.Clear(result, 0, result.Length);
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                InventoryItemSnapshot item = snapshot.Items[itemIndex];
                ArtifactCategoryRuleSnapshot rule = item.Artifact?.CategoryRule;
                if (item.Artifact == null)
                {
                    result[itemIndex] = Array.Empty<string>();
                }
                else if (rule.Kind == ArtifactCategoryRuleKind.RowModulo)
                {
                    int row = layout.GetCell(itemIndex) / snapshot.Width;
                    result[itemIndex] = workspace != null
                        ? workspace.RowCategoryChoices[itemIndex][row % rule.RowCategories.Count]
                        : new[]
                    {
                        rule.RowCategories[row % rule.RowCategories.Count]
                    };
                }
                else if (rule.Kind == ArtifactCategoryRuleKind.Static)
                {
                    result[itemIndex] = item.BaseCategories;
                }
            }

            var resolving = workspace?.ResolvingCategories ?? new HashSet<int>();
            resolving.Clear();
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                if (snapshot.Items[itemIndex].Artifact?.CategoryRule.Kind ==
                    ArtifactCategoryRuleKind.DependencyTarget)
                {
                    result[itemIndex] = ResolveDependencyCategories(snapshot,
                        layout, itemAtCell, itemIndex, result, resolving);
                }
            }

            for (int itemIndex = 0; itemIndex < snapshot.Items.Count; itemIndex++)
            {
                ArtifactCategoryRuleSnapshot rule =
                    snapshot.Items[itemIndex].Artifact?.CategoryRule;
                if (rule?.Kind != ArtifactCategoryRuleKind.NeighborMatch)
                {
                    continue;
                }

                int origin = layout.GetCell(itemIndex);
                int originX = origin % snapshot.Width;
                int originY = origin / snapshot.Width;
                var counts = workspace?.NeighborCategoryCounts ?? new Dictionary<string, int>(StringComparer.Ordinal);
                counts.Clear();
                foreach (InventoryOffsetSnapshot offset in rule.NeighborOffsets)
                {
                    int x = originX + offset.X;
                    int y = originY + offset.Y;
                    if (x < 0 || x >= snapshot.Width || y < 0 ||
                        y >= snapshot.Height)
                    {
                        continue;
                    }
                    int cell = y * snapshot.Width + x;
                    if (cell < 0 || cell >= snapshot.Storage)
                    {
                        continue;
                    }
                    int neighborItem = itemAtCell[cell];
                    if (neighborItem < 0 ||
                        snapshot.Items[neighborItem].Artifact == null)
                    {
                        continue;
                    }
                    foreach (string category in result[neighborItem] ??
                        Array.Empty<string>())
                    {
                        counts[category] = counts.TryGetValue(category,
                            out int count) ? count + 1 : 1;
                    }
                }
                result[itemIndex] = counts.Where(entry => entry.Value >= rule.Match)
                    .Select(entry => entry.Key)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }

            for (int index = 0; index < result.Length; index++)
            {
                result[index] ??= Array.Empty<string>();
            }
            return result;
        }

        private static IReadOnlyList<string> ResolveDependencyCategories(
            InventorySnapshot snapshot, InventoryLayoutProjection layout,
            int[] itemAtCell, int itemIndex, IReadOnlyList<string>[] categories,
            HashSet<int> resolving)
        {
            if (categories[itemIndex] != null)
            {
                return categories[itemIndex];
            }
            if (!resolving.Add(itemIndex))
            {
                return Array.Empty<string>();
            }

            ArtifactCategoryRuleSnapshot rule =
                snapshot.Items[itemIndex].Artifact.CategoryRule;
            int origin = layout.GetCell(itemIndex);
            int x = origin % snapshot.Width + rule.TargetX;
            int y = origin / snapshot.Width + rule.TargetY;
            IReadOnlyList<string> resolved = Array.Empty<string>();
            if (x >= 0 && x < snapshot.Width && y >= 0 &&
                y < snapshot.Height)
            {
                int targetCell = y * snapshot.Width + x;
                if (targetCell >= 0 && targetCell < snapshot.Storage)
                {
                    int targetIndex = itemAtCell[targetCell];
                    ArtifactSnapshot target = targetIndex < 0
                        ? null
                        : snapshot.Items[targetIndex].Artifact;
                    if (target?.CategoryRule.Kind ==
                        ArtifactCategoryRuleKind.DependencyTarget)
                    {
                        resolved = ResolveDependencyCategories(snapshot, layout,
                            itemAtCell, targetIndex, categories, resolving);
                    }
                    else if (target?.Attackable == true)
                    {
                        resolved = categories[targetIndex] ??
                            Array.Empty<string>();
                    }
                }
            }

            resolving.Remove(itemIndex);
            categories[itemIndex] = resolved;
            return resolved;
        }

        private static bool IsOccupied(InventorySnapshot snapshot, int x, int y,
            int[] itemAtCell)
        {
            return x >= 0 && x < snapshot.Width && y >= 0 &&
                y < snapshot.Height && y * snapshot.Width + x < snapshot.Storage &&
                itemAtCell[y * snapshot.Width + x] >= 0;
        }

        private static bool IsArtifact(InventorySnapshot snapshot, int itemIndex)
        {
            return itemIndex >= 0 && snapshot.Items[itemIndex].Artifact != null;
        }

        private static bool IsMagic(InventorySnapshot snapshot, int x, int y,
            int[] itemAtCell)
        {
            if (!IsOccupied(snapshot, x, y, itemAtCell))
            {
                return false;
            }
            int itemIndex = itemAtCell[y * snapshot.Width + x];
            return snapshot.Items[itemIndex].Artifact?.Magic != null;
        }

        private static ProjectedInventorySettlement Failure(string issue)
        {
            return new ProjectedInventorySettlement(false, null, null, null,
                new[] { issue });
        }
    }
}
