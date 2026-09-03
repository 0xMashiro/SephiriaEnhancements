#nullable disable
using System;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryAdditiveScoreBound
    {
        // A certificate for an unchanged, already optimal layout, not a plateau heuristic.
        // Unsupported mechanics only disable this certificate; they remain searchable.
        internal static bool IsAttained(InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy, InventoryOptimizationScore score)
        {
            if (policy.ArtifactInstanceRules.Count != 0 || policy.ArtifactEntityRules.Count != 0 ||
                policy.ComboRules.Count != 0 || snapshot.ArrangementBonusesEnabled ||
                snapshot.FixedTabletSources.Count != 0 || snapshot.PositionEffects.Rules.Count != 0 ||
                score.PositionEffectRegressions != 0 || score.SourceEnabledArtifactsDeactivated != 0 ||
                score.ExcessArtifactLevelTotal != 0 || score.MovedItemCount != 0 || score.RotatedTabletCount != 0)
                return false;

            foreach (var cell in snapshot.Cells)
            {
                var source = cell.Settlement;
                if (source.BaselineLevel != 0 || source.FixedLevel != 0 ||
                    source.BaselineLevelMultiplier != 0 || source.FixedLevelMultiplier != 0) return false;
            }

            int artifacts = 0, tablets = 0;
            long availableLevels = 0, maximumLevels = 0;
            foreach (var item in snapshot.Items)
            {
                var artifact = item.Artifact;
                if (artifact != null)
                {
                    if (artifact.Criteria?.Kind != ArtifactActivationConditionKind.None ||
                        artifact.CategoryRule.Kind != ArtifactCategoryRuleKind.Static) return false;
                    artifacts++;
                    availableLevels += Math.Max(0, artifact.Enchant);
                    maximumLevels += Math.Max(0, artifact.MaxLevel);
                }
                var tablet = item.StoneTablet;
                if (tablet == null) continue;
                if (tablet.Custom || tablet.PlacementProjections.Count == 0) return false;
                tablets++;
                long maximumContribution = 0;
                foreach (var placement in tablet.PlacementProjections)
                    foreach (var rotation in placement.Rotations)
                    {
                        if (rotation.Criteria.Count != 0) return false;
                        long contribution = 0;
                        foreach (var effect in rotation.Effects)
                        {
                            if (effect.EffectKind != TabletEffectKind.IncreaseLevel) return false;
                            if (effect.ValidCell) contribution += Math.Max(0, effect.LevelParameter);
                        }
                        maximumContribution = Math.Max(maximumContribution, contribution);
                    }
                // Ignore negative effects, overlap, and rotation restrictions: these can only
                // make this upper bound more generous for purely additive levels.
                availableLevels += maximumContribution;
            }
            // Static categories keep combo scores constant. With no targets, all artifacts
            // enabled, maximum level yield, no overflow, and zero moves/rotations, every
            // component of the lexicographic score is at its best possible value.
            return artifacts > 0 && tablets > 0 && score.EnabledArtifactCount == artifacts &&
                score.CappedEffectiveArtifactLevelTotal == Math.Min(availableLevels, maximumLevels);
        }
    }
}
