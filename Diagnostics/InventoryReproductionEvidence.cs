#nullable disable
using System;
using System.Linq;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class InventoryReproductionEvidence
    {
        // Dictionary insertion order does not change policy; PriorityOrder does.
        internal static object Policy(ResolvedInventoryOptimizationPolicy policy) => policy == null ? null : new
        {
            policy.SearchEffort,
            policy.AllowStoneTabletRotation,
            ArtifactInstanceRules = policy.ArtifactInstanceRules.Values.OrderBy(rule => rule.EntityId)
                .ThenBy(rule => rule.InstanceId).ToArray(),
            ArtifactEntityRules = policy.ArtifactEntityRules.Values.OrderBy(rule => rule.EntityId).ToArray(),
            ComboRules = policy.ComboRules.Values.OrderBy(rule => rule.CategoryId, StringComparer.Ordinal).ToArray()
        };

        internal static object Proposal(InventoryOptimizationProposal proposal) => proposal == null ? null : new
        {
            proposal.Succeeded,
            proposal.Improved,
            proposal.Layout,
            proposal.CurrentScore,
            proposal.BestScore,
            proposal.TargetEvaluations,
            proposal.CandidateEvaluations,
            proposal.ElapsedMilliseconds,
            proposal.TerminationReason,
            proposal.SearchMethod,
            proposal.OptimalityProven,
            proposal.DuplicateLayoutsSkipped,
            proposal.Issues
        };

        internal static object Differential(InventorySettlementDifferentialReport differential) => differential == null ? null : new
        {
            differential.Matched,
            differential.Mismatches
        };
    }
}
