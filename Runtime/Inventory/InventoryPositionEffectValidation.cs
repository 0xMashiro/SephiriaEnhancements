#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal static class InventoryPositionEffectValidation
    {
        internal static bool Validate(InventorySnapshot snapshot, List<string> issues)
        {
            var state = snapshot.PositionEffects;
            if (state.Issues.Count > 0)
            {
                issues.AddRange(state.Issues);
                return false;
            }
            if (state.Rules.Count == 0)
            {
                if (state.Observed.Count == 0) return true;
                issues.Add("PositionEffectRulesUnavailable");
                return false;
            }
            var artifacts = snapshot.Items.Where(item => item.Artifact != null).ToDictionary(item => item.ItemKey);
            if (state.Rules.Any(rule => rule == null || !artifacts.ContainsKey(rule.Source) || !RuleValid(rule)) ||
                state.Rules.Select(rule => rule.Source).Distinct().Count() != state.Rules.Count ||
                state.Traits.Any(trait => trait == null || !artifacts.ContainsKey(trait.Item)) ||
                state.Traits.Select(trait => trait.Item).Distinct().Count() != state.Traits.Count ||
                state.Traits.Count != artifacts.Count)
            {
                issues.Add("PositionEffectParametersUnavailable");
                return false;
            }
            // These native effects write a boolean, so multiple sources can depend
            // on refresh history. Do not substitute additive stacking semantics.
            if (state.Rules.Count(rule => rule.Kind == InventoryPositionEffectKind.SameRowCompanionMode) > 1)
            {
                issues.Add("PositionEffectCompanionRefreshOrderUnavailable");
                return false;
            }
            if (state.Rules.Any(rule => rule.Kind == InventoryPositionEffectKind.RowCategoryStats &&
                (artifacts[rule.Source].Artifact.CategoryRule.Kind != ArtifactCategoryRuleKind.RowModulo ||
                 artifacts[rule.Source].Artifact.CategoryRule.RowCategories.Count != rule.Channels.Count)))
            {
                issues.Add("PositionEffectRowCategoryCycleUnavailable");
                return false;
            }
            var actual = artifacts.Values.Select(item => new ProjectedInventoryArtifactSettlement(
                item.ItemKey, item.Artifact.EffectEnabled, item.Artifact.PenaltyEnabled,
                item.Artifact.DisplayedLevel, item.Artifact.LimitedEffectEnabledLevel)).ToArray();
            var differences = InventoryPositionEffectComparison.Differences(
                InventoryPositionEffectProjector.Evaluate(snapshot, InventoryLayoutProjection.Current(snapshot), actual),
                state.Observed);
            issues.AddRange(differences);
            return differences.Length == 0;
        }

        private static bool RuleValid(InventoryPositionEffectRule rule)
        {
            if (!Enum.IsDefined(typeof(InventoryPositionEffectKind), rule.Kind) ||
                rule.ValuesByLevel.Concat(rule.SecondaryValuesByLevel).Any(value => double.IsNaN(value) || double.IsInfinity(value)) ||
                rule.Offsets.Any(offset => offset == null) ||
                rule.Offsets.Select(offset => (offset.X, offset.Y)).Distinct().Count() != rule.Offsets.Count)
                return false;
            switch (rule.Kind)
            {
                case InventoryPositionEffectKind.RowCategoryStats:
                    return rule.ValuesByLevel.Count > 0 && rule.Channels.Count > 0 &&
                        rule.Channels.All(channel => !string.IsNullOrEmpty(channel));
                case InventoryPositionEffectKind.NeighborArtifactLevelDamage:
                    return rule.ValuesByLevel.Count > 0 && rule.Offsets.Count > 0;
                case InventoryPositionEffectKind.AdjacentPlanetEnhancement:
                    return rule.Offsets.Count > 0 && rule.TargetCategory.Length > 0;
                case InventoryPositionEffectKind.SameRowCompanionMode:
                    return true;
                case InventoryPositionEffectKind.MagicCostReduction:
                case InventoryPositionEffectKind.MagicCooldownRecovery:
                    return rule.ValuesByLevel.Count > 0 && rule.Offsets.Count == 1;
                case InventoryPositionEffectKind.FirstSlotsElementDamage:
                    return rule.ValuesByLevel.Count > 0 && rule.Boundary > 0 &&
                        rule.Channels.Count > 0 && rule.Channels.All(channel => !string.IsNullOrEmpty(channel));
                case InventoryPositionEffectKind.HalfBoardStats:
                    return rule.ValuesByLevel.Count > 0 && rule.SecondaryValuesByLevel.Count > 0 &&
                        ValidModes(rule);
                case InventoryPositionEffectKind.HalfBoardWeaponMode:
                    return ValidModes(rule);
                case InventoryPositionEffectKind.DependencyDamage:
                    return rule.Offsets.Count == 1 && rule.ValuesByLevel.Count > 0 &&
                        (!rule.ConditionalDamage || rule.SecondaryValuesByLevel.Count > 0);
                default:
                    return false;
            }
        }

        private static bool ValidModes(InventoryPositionEffectRule rule) => rule.Boundary >= 0 &&
            rule.Channels.Count == 2 && rule.Channels.All(channel => !string.IsNullOrEmpty(channel)) &&
            rule.Channels[0] != rule.Channels[1];
    }
}
