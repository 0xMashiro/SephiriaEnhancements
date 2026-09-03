#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    [Flags]
    internal enum InventorySettlementCapabilities
    {
        None = 0,
        BaselineState = 1 << 0,
        CurrentCellSettlementVerified = 1 << 1,
        CurrentArtifactActivationVerified = 1 << 2,
        CurrentComboAccountingVerified = 1 << 3,
        CurrentTabletApplicationVerified = 1 << 4,
        LayoutProjectionTabletEffects = 1 << 5,
        LayoutProjectionArtifactCriteria = 1 << 6,
        LayoutProjectionDynamicCategories = 1 << 7,
        LayoutProjectionUniqueEffects = 1 << 8,
        LayoutProjectionFixedEngravings = 1 << 9,
        SnapshotShapeVerified = 1 << 10,
        LayoutProjectionArrangementBonuses = 1 << 11,
        LayoutProjectionArtifactEffectsActive = 1 << 12,
        CurrentPositionEffectsVerified = 1 << 13
    }

    internal sealed class InventoryCellSettlementSnapshot
    {
        internal InventoryCellSettlementSnapshot(bool baselineKnown,
            int baselineLevel, int baselineMaximumLevel,
            int baselineTemporaryLevel, int baselineLevelMultiplier,
            int baselineDisableCount, int baselineCriteriaBypassCount,
            int enchantLevel, int fixedLevel, int fixedDisableCount,
            int fixedCriteriaBypassCount, int fixedLevelMultiplier,
            int tabletLevel, int tabletDisableCount,
            int tabletCriteriaBypassCount, int tabletLevelMultiplier)
        {
            BaselineKnown = baselineKnown;
            BaselineLevel = baselineLevel;
            BaselineMaximumLevel = baselineMaximumLevel;
            BaselineTemporaryLevel = baselineTemporaryLevel;
            BaselineLevelMultiplier = baselineLevelMultiplier;
            BaselineDisableCount = baselineDisableCount;
            BaselineCriteriaBypassCount = baselineCriteriaBypassCount;
            EnchantLevel = enchantLevel;
            FixedLevel = fixedLevel;
            FixedDisableCount = fixedDisableCount;
            FixedCriteriaBypassCount = fixedCriteriaBypassCount;
            FixedLevelMultiplier = fixedLevelMultiplier;
            TabletLevel = tabletLevel;
            TabletDisableCount = tabletDisableCount;
            TabletCriteriaBypassCount = tabletCriteriaBypassCount;
            TabletLevelMultiplier = tabletLevelMultiplier;
        }

        internal bool BaselineKnown { get; }
        internal int BaselineLevel { get; }
        internal int BaselineMaximumLevel { get; }
        internal int BaselineTemporaryLevel { get; }
        internal int BaselineLevelMultiplier { get; }
        internal int BaselineDisableCount { get; }
        internal int BaselineCriteriaBypassCount { get; }
        internal int EnchantLevel { get; }
        internal int FixedLevel { get; }
        internal int FixedDisableCount { get; }
        internal int FixedCriteriaBypassCount { get; }
        internal int FixedLevelMultiplier { get; }
        internal int TabletLevel { get; }
        internal int TabletDisableCount { get; }
        internal int TabletCriteriaBypassCount { get; }
        internal int TabletLevelMultiplier { get; }
    }

    internal sealed class InventorySettlementValidationSnapshot
    {
        internal InventorySettlementValidationSnapshot(
            InventorySettlementCapabilities capabilities, string[] issues)
        {
            Capabilities = capabilities;
            Issues = Array.AsReadOnly(issues == null
                ? Array.Empty<string>()
                : (string[])issues.Clone());
        }

        internal InventorySettlementCapabilities Capabilities { get; }
        internal IReadOnlyList<string> Issues { get; }
        internal bool HasItemIdentityConflict => Issues.Any(issue =>
            issue.StartsWith("SnapshotItemIdentityDuplicate:", StringComparison.Ordinal));
        internal bool HasPositionEffectIssue => Issues.Any(issue =>
            issue.StartsWith("PositionEffect", StringComparison.Ordinal));
        internal bool PositionEffectObservationUnavailableOnClient => Issues.Contains(
            InventoryPositionEffectsSnapshot.ObservationUnavailableOnClient);
        internal bool CurrentLayoutVerified =>
            Has(InventorySettlementCapabilities.SnapshotShapeVerified) &&
            Has(InventorySettlementCapabilities.BaselineState) &&
            Has(InventorySettlementCapabilities.CurrentCellSettlementVerified) &&
            Has(InventorySettlementCapabilities.CurrentArtifactActivationVerified) &&
            Has(InventorySettlementCapabilities.CurrentComboAccountingVerified) &&
            Has(InventorySettlementCapabilities.CurrentTabletApplicationVerified) &&
            Has(InventorySettlementCapabilities.CurrentPositionEffectsVerified);
        internal bool LayoutProjectionReady => CurrentLayoutVerified &&
            Has(InventorySettlementCapabilities.LayoutProjectionArtifactEffectsActive) &&
            Has(InventorySettlementCapabilities.LayoutProjectionTabletEffects) &&
            Has(InventorySettlementCapabilities.LayoutProjectionArtifactCriteria) &&
            Has(InventorySettlementCapabilities.LayoutProjectionDynamicCategories) &&
            Has(InventorySettlementCapabilities.LayoutProjectionUniqueEffects) &&
            Has(InventorySettlementCapabilities.LayoutProjectionFixedEngravings) &&
            Has(InventorySettlementCapabilities.LayoutProjectionArrangementBonuses);

        private bool Has(InventorySettlementCapabilities capability) =>
            (Capabilities & capability) == capability;
    }

    internal static class InventorySettlementValidator
    {
        internal static InventorySettlementValidationSnapshot Validate(
            InventorySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return new InventorySettlementValidationSnapshot(
                    InventorySettlementCapabilities.None,
                    new[] { "SnapshotUnavailable" });
            }

            var issues = new List<string>();
            InventorySettlementCapabilities capabilities =
                InventorySettlementCapabilities.None;

            if (!ValidateShape(snapshot, issues))
            {
                return new InventorySettlementValidationSnapshot(capabilities,
                    issues.Distinct(StringComparer.Ordinal).ToArray());
            }
            capabilities |= InventorySettlementCapabilities.
                SnapshotShapeVerified;

            if (snapshot.ArtifactEffectsEnabled)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionArtifactEffectsActive;
            }
            else
            {
                issues.Add("LayoutProjectionArtifactEffectsInactive");
            }

            bool baselineKnown = snapshot.Cells.All(cell =>
                cell.Settlement?.BaselineKnown == true);
            if (baselineKnown)
            {
                capabilities |= InventorySettlementCapabilities.BaselineState;
            }
            else
            {
                issues.Add("BaselineStateUnavailable");
            }

            if (baselineKnown && ValidateCells(snapshot, issues))
            {
                capabilities |= InventorySettlementCapabilities.
                    CurrentCellSettlementVerified;
            }
            if (ValidateArtifacts(snapshot, issues))
            {
                capabilities |= InventorySettlementCapabilities.
                    CurrentArtifactActivationVerified;
            }
            if (ValidateComboAccounting(snapshot, issues))
            {
                capabilities |= InventorySettlementCapabilities.
                    CurrentComboAccountingVerified;
            }
            if (ValidateTablets(snapshot, issues, out bool projectionsComplete))
            {
                capabilities |= InventorySettlementCapabilities.
                    CurrentTabletApplicationVerified;
            }
            if (projectionsComplete)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionTabletEffects;
            }

            AddLayoutProjectionReadiness(snapshot, ref capabilities, issues);
            if (InventoryPositionEffectValidation.Validate(snapshot, issues))
                capabilities |= InventorySettlementCapabilities.CurrentPositionEffectsVerified;
            if (!snapshot.ArrangementBonusesEnabled)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionArrangementBonuses;
            }
            else
            {
                issues.Add("LayoutProjectionArrangementBonusesUnavailable");
            }
            return new InventorySettlementValidationSnapshot(capabilities,
                issues.Distinct(StringComparer.Ordinal).ToArray());
        }

        private static bool ValidateShape(InventorySnapshot snapshot,
            List<string> issues)
        {
            bool valid = true;
            for (int index = 0; index < snapshot.Cells.Count; index++)
            {
                InventoryCellSnapshot cell = snapshot.Cells[index];
                if (cell == null || cell.Index != index ||
                    cell.X != index % snapshot.Width ||
                    cell.Y != index / snapshot.Width)
                {
                    valid = false;
                    issues.Add("SnapshotCellShapeInvalid:" + index);
                }
            }

            var occupiedCells = new HashSet<int>();
            var firstCellByItem = new Dictionary<InventoryItemKey, int>();
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot item = snapshot.Items[index];
                if (item == null)
                {
                    valid = false;
                    issues.Add("SnapshotItemUnavailable:" + index);
                    continue;
                }

                if (item.CellIndex < 0 || item.CellIndex >= snapshot.Storage ||
                    item.X != item.CellIndex % snapshot.Width ||
                    item.Y != item.CellIndex / snapshot.Width ||
                    !occupiedCells.Add(item.CellIndex))
                {
                    valid = false;
                    issues.Add("SnapshotItemPlacementInvalid:" +
                        item.ItemKey);
                }
                if (firstCellByItem.TryGetValue(item.ItemKey, out int firstCell))
                {
                    valid = false;
                    issues.Add("SnapshotItemIdentityDuplicate:" +
                        item.ItemKey + ":Cells=" + firstCell + "," + item.CellIndex);
                }
                else
                {
                    firstCellByItem.Add(item.ItemKey, item.CellIndex);
                }

                bool payloadValid;
                switch (item.Kind)
                {
                    case InventoryItemKind.Artifact:
                        payloadValid = item.Artifact != null &&
                            item.StoneTablet == null &&
                            item.NativeType == NativeInventoryItemType.Charm &&
                            (item.Artifact.Criteria == null ||
                             item.Artifact.Criteria.Kind ==
                                ArtifactActivationConditionKind.None);
                        break;
                    case InventoryItemKind.RestrictedArtifact:
                        payloadValid = item.Artifact != null &&
                            item.StoneTablet == null &&
                            item.NativeType == NativeInventoryItemType.Charm &&
                            item.Artifact.Criteria != null &&
                            item.Artifact.Criteria.Kind !=
                                ArtifactActivationConditionKind.None;
                        break;
                    case InventoryItemKind.StoneTablet:
                        payloadValid = item.Artifact == null &&
                            item.StoneTablet != null &&
                            item.NativeType ==
                                NativeInventoryItemType.StoneTablet;
                        break;
                    case InventoryItemKind.Other:
                        payloadValid = item.Artifact == null &&
                            item.StoneTablet == null &&
                            item.NativeType != NativeInventoryItemType.Unknown &&
                            item.NativeType != NativeInventoryItemType.Charm &&
                            item.NativeType !=
                                NativeInventoryItemType.StoneTablet;
                        break;
                    default:
                        payloadValid = false;
                        break;
                }
                if (!payloadValid)
                {
                    valid = false;
                    issues.Add("SnapshotItemPayloadInvalid:" +
                        item.ItemKey);
                }
            }

            if (snapshot.ComboCategories.Any(category => category == null))
            {
                valid = false;
                issues.Add("SnapshotComboCategoryUnavailable");
            }
            if (snapshot.FixedTabletSources.Any(source => source == null))
            {
                valid = false;
                issues.Add("SnapshotFixedTabletSourceUnavailable");
            }
            return valid;
        }

        private static bool ValidateCells(InventorySnapshot snapshot,
            List<string> issues)
        {
            bool valid = true;
            var itemByCell = snapshot.Items.ToDictionary(item =>
                item.CellIndex);
            foreach (InventoryCellSnapshot cell in snapshot.Cells)
            {
                InventoryCellSettlementSnapshot settlement = cell.Settlement;
                int additiveLevel = settlement.BaselineLevel +
                    settlement.EnchantLevel + settlement.FixedLevel +
                    settlement.TabletLevel;
                int multiplier = settlement.BaselineLevelMultiplier +
                    settlement.FixedLevelMultiplier +
                    settlement.TabletLevelMultiplier;
                int predictedLevel = multiplier == 0
                    ? additiveLevel
                    : additiveLevel * multiplier;
                int predictedDisable = settlement.BaselineDisableCount +
                    settlement.FixedDisableCount + settlement.TabletDisableCount;
                int predictedBypass = settlement.BaselineCriteriaBypassCount +
                    settlement.FixedCriteriaBypassCount +
                    settlement.TabletCriteriaBypassCount;
                int predictedMaximumLevel = settlement.BaselineMaximumLevel;
                if (snapshot.ArtifactEffectsEnabled &&
                    itemByCell.TryGetValue(cell.Index,
                        out InventoryItemSnapshot item) &&
                    item.Artifact != null)
                {
                    predictedMaximumLevel = item.Artifact.MaxLevel;
                }

                if (predictedLevel != cell.Level ||
                    predictedMaximumLevel != cell.MaxLevel ||
                    settlement.BaselineTemporaryLevel != cell.TemporaryLevel ||
                    multiplier != cell.LevelMultiplier ||
                    predictedDisable != cell.DisableCount ||
                    predictedBypass != cell.IgnoreCriteriaCount)
                {
                    valid = false;
                    issues.Add("CellSettlementMismatch:" + cell.Index);
                }
            }
            return valid;
        }

        private static bool ValidateArtifacts(InventorySnapshot snapshot,
            List<string> issues)
        {
            bool valid = true;
            foreach (InventoryItemSnapshot item in snapshot.Items)
            {
                ArtifactSnapshot artifact = item.Artifact;
                if (artifact == null)
                {
                    continue;
                }

                InventoryCellSnapshot cell = snapshot.Cells[item.CellIndex];
                if (artifact.DisplayedLevel != cell.Level)
                {
                    valid = false;
                    issues.Add("ArtifactDisplayedLevelMismatch:" + item.ItemKey);
                }

                bool criteriaKnown = artifact.Criteria == null ||
                    artifact.Criteria.RuntimeState != CriteriaEvaluationState.Unknown;
                if (!criteriaKnown)
                {
                    valid = false;
                    issues.Add("ArtifactCriteriaUnknown:" + item.ItemKey);
                    continue;
                }

                bool criteriaSatisfied = artifact.Criteria == null ||
                    artifact.Criteria.RuntimeState ==
                        CriteriaEvaluationState.NotApplicable ||
                    artifact.Criteria.RuntimeState ==
                        CriteriaEvaluationState.Satisfied;
                bool eligible = snapshot.ArtifactEffectsEnabled &&
                    snapshot.GlobalActiveValue > 0 && !cell.Disabled &&
                    cell.Level >= 0 && (cell.IgnoresCriteria || criteriaSatisfied) &&
                    artifact.WeaponCompatible;
                bool expectedEnabled = eligible &&
                    (!artifact.UniqueEffect || artifact.UniqueEffectRegistered);
                int expectedLimitedLevel = expectedEnabled
                    ? Math.Min(artifact.MaxLevel, cell.Level)
                    : 0;
                if (artifact.EffectEnabled != expectedEnabled ||
                    artifact.LimitedEffectEnabledLevel != expectedLimitedLevel)
                {
                    valid = false;
                    issues.Add("ArtifactActivationMismatch:" + item.ItemKey);
                }
            }
            return valid;
        }

        private static bool ValidateComboAccounting(InventorySnapshot snapshot,
            List<string> issues)
        {
            bool valid = true;
            foreach (ComboCategorySnapshot category in snapshot.ComboCategories)
            {
                int predicted = category.ArtifactCategoryCount +
                    category.BonusCount + category.InferredUniquePairCount;
                if (!category.AccountingConsistent ||
                    predicted != category.CurrentCount)
                {
                    valid = false;
                    issues.Add("ComboAccountingMismatch:" + category.CategoryId);
                }
            }
            return valid;
        }

        private static bool ValidateTablets(InventorySnapshot snapshot,
            List<string> issues, out bool projectionsComplete)
        {
            projectionsComplete = true;
            bool valid = true;
            var occupied = new HashSet<int>(snapshot.Items.Select(item =>
                item.CellIndex));
            var artifacts = new HashSet<int>(snapshot.Items.Where(item =>
                item.Artifact != null).Select(item => item.CellIndex));

            foreach (InventoryItemSnapshot item in snapshot.Items)
            {
                StoneTabletSnapshot stoneTablet = item.StoneTablet;
                if (stoneTablet == null)
                {
                    continue;
                }

                TabletRotationProjectionSnapshot projection =
                    stoneTablet.FindProjection(item.CellIndex,
                        stoneTablet.Rotation);
                if (projection == null || !projection.ParseSucceeded)
                {
                    projectionsComplete = false;
                    valid = false;
                    issues.Add("TabletProjectionUnavailable:" + item.ItemKey);
                    continue;
                }

                if (stoneTablet.PlacementProjections.Count != snapshot.Storage ||
                    stoneTablet.PlacementProjections.Any(placement =>
                        placement.Rotations.Count != 4 ||
                        placement.Rotations.Any(rotation =>
                            !rotation.ParseSucceeded)))
                {
                    projectionsComplete = false;
                    issues.Add("TabletLayoutProjectionIncomplete:" +
                        item.ItemKey);
                }

                bool conditionSatisfied = EvaluateTabletCondition(projection,
                    item.CellIndex, snapshot.Width, occupied, artifacts);
                if (conditionSatisfied != stoneTablet.Applied)
                {
                    valid = false;
                    issues.Add("TabletApplicationMismatch:" + item.ItemKey);
                }
            }
            foreach (FixedTabletSourceSnapshot source in
                snapshot.FixedTabletSources)
            {
                if (source.CellIndex < 0 || source.CellIndex >= snapshot.Storage ||
                    source.Projection == null ||
                    !source.Projection.ParseSucceeded)
                {
                    projectionsComplete = false;
                    valid = false;
                    issues.Add("FixedTabletProjectionUnavailable:" +
                        source.ItemKey);
                    continue;
                }
                bool conditionSatisfied = EvaluateTabletCondition(
                    source.Projection, source.CellIndex, snapshot.Width,
                    occupied, artifacts);
                if (conditionSatisfied != source.Applied)
                {
                    valid = false;
                    issues.Add("FixedTabletApplicationMismatch:" +
                        source.ItemKey);
                }
            }
            return valid;
        }

        private static bool EvaluateTabletCondition(
            TabletRotationProjectionSnapshot projection, int originIndex,
            int width, HashSet<int> occupied, HashSet<int> artifacts)
        {
            bool anyPlaced = false;
            bool placedHit = false;
            foreach (TabletAdditionSnapshot criterion in projection.Criteria)
            {
                int index = criterion.Y * width + criterion.X;
                switch (criterion.CriteriaKind)
                {
                    case TabletCriteriaKind.AnyItem:
                        if (!criterion.ValidCell || !occupied.Contains(index))
                        {
                            return false;
                        }
                        break;
                    case TabletCriteriaKind.Artifact:
                        if (!criterion.ValidCell || !artifacts.Contains(index))
                        {
                            return false;
                        }
                        break;
                    case TabletCriteriaKind.Placed:
                        anyPlaced = true;
                        placedHit |= index == originIndex;
                        break;
                    default:
                        return false;
                }
            }
            return !anyPlaced || placedHit;
        }

        private static void AddLayoutProjectionReadiness(InventorySnapshot snapshot,
            ref InventorySettlementCapabilities capabilities,
            List<string> issues)
        {
            bool criteriaSupported = snapshot.Items.Where(item =>
                    item.Artifact?.Criteria != null)
                .All(item => item.Artifact.Criteria.Kind !=
                        ArtifactActivationConditionKind.Unknown &&
                    item.Artifact.Criteria.RuntimeState !=
                        CriteriaEvaluationState.Unknown);
            if (criteriaSupported)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionArtifactCriteria;
            }
            else
            {
                issues.Add("LayoutProjectionArtifactCriteriaUnavailable");
            }

            InventoryItemSnapshot[] dynamicItems = snapshot.Items.Where(item =>
                item.Artifact != null &&
                item.Artifact.CategoryRule != null &&
                item.Artifact.CategoryRule.Kind !=
                    ArtifactCategoryRuleKind.Static).ToArray();
            int neighborMatchCount = dynamicItems.Count(item =>
                item.Artifact.CategoryRule.Kind ==
                    ArtifactCategoryRuleKind.NeighborMatch);
            bool dynamicCategoriesSupported = neighborMatchCount <= 1 &&
                dynamicItems.All(item => IsCategoryRuleComplete(
                    item.Artifact.CategoryRule));
            if (dynamicCategoriesSupported)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionDynamicCategories;
            }
            else
            {
                issues.Add("LayoutProjectionDynamicCategoriesUnavailable");
            }

            bool uniqueConflict = snapshot.Items.Where(item =>
                    item.Artifact?.UniqueEffect == true)
                .GroupBy(item => item.EntityId)
                .Any(group => group.Count() > 1);
            if (!uniqueConflict)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionUniqueEffects;
            }
            else
            {
                issues.Add("LayoutProjectionUniqueResolutionUnavailable");
            }

            bool dynamicMysticContribution = snapshot.Items.Any(item =>
                item.Artifact != null &&
                item.Artifact.CategoryRule != null &&
                item.Artifact.CategoryRule.Kind !=
                    ArtifactCategoryRuleKind.Static &&
                item.Artifact.PossibleCategories.Contains("MYSTIC",
                    StringComparer.Ordinal));
            bool fixedTabletSourcesReady = snapshot.FixedTabletSources.All(
                source => source.CellIndex >= 0 &&
                    source.CellIndex < snapshot.Storage &&
                    source.Projection?.ParseSucceeded == true);
            if (!dynamicMysticContribution && fixedTabletSourcesReady)
            {
                capabilities |= InventorySettlementCapabilities.
                    LayoutProjectionFixedEngravings;
            }
            else
            {
                issues.Add("LayoutProjectionFixedEngravingsUnavailable");
            }
        }

        private static bool IsCategoryRuleComplete(
            ArtifactCategoryRuleSnapshot rule)
        {
            if (rule == null)
            {
                return false;
            }
            switch (rule.Kind)
            {
                case ArtifactCategoryRuleKind.Static:
                case ArtifactCategoryRuleKind.DependencyTarget:
                    return true;
                case ArtifactCategoryRuleKind.RowModulo:
                    return rule.RowCategories.Count > 0;
                case ArtifactCategoryRuleKind.NeighborMatch:
                    return rule.Match > 0 && rule.NeighborOffsets.Count > 0;
                default:
                    return false;
            }
        }
    }
}
