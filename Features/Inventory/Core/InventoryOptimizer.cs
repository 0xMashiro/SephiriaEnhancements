#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizer
    {
        internal static InventoryOptimizationProposal Solve(
            InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget = null,
            CancellationToken cancellationToken = default,
            IInventoryCandidateBatchEvaluator batchEvaluator = null)
        {
            var elapsed = Stopwatch.StartNew();
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return Failure("SnapshotUnavailable", elapsed.ElapsedMilliseconds);
            }
            if (!snapshot.SettlementValidation.LayoutProjectionReady)
            {
                return new InventoryOptimizationProposal(false, null, null, null,
                    0, snapshot.SettlementValidation?.Issues.ToArray(),
                    elapsedMilliseconds: elapsed.ElapsedMilliseconds);
            }
            if (policy == null)
            {
                return Failure("OptimizationPolicyUnavailable",
                    elapsed.ElapsedMilliseconds);
            }

            budget ??= InventorySearchBudget.ForEffort(policy.SearchEffort);
            var scorer = new InventoryOptimizationScorer(snapshot, policy);
            InventoryLayoutProjection current = InventoryLayoutProjection.Current(
                snapshot);
            ProjectedInventorySettlement currentSettlement =
                InventorySettlementProjector.Evaluate(snapshot, current);
            if (!currentSettlement.Succeeded)
            {
                return new InventoryOptimizationProposal(false, null, null, null,
                    1, currentSettlement.Issues.ToArray(),
                    elapsedMilliseconds: elapsed.ElapsedMilliseconds);
            }

            InventoryOptimizationScore initialScore = scorer.Score(current,
                currentSettlement);
            if ((!budget.UseElapsedTimeLimit || elapsed.ElapsedMilliseconds < budget.MaximumElapsedMilliseconds) &&
                InventoryAdditiveScoreBound.IsAttained(snapshot, policy, initialScore))
            {
                return new InventoryOptimizationProposal(true, current, initialScore, initialScore, 1,
                    Array.Empty<string>(), policy, scorer.EvaluateTargets(currentSettlement, currentSettlement),
                    InventorySearchTerminationReason.ScoreUpperBoundReached, elapsed.ElapsedMilliseconds,
                    optimalityProven: true, outcome: InventoryOptimizationOutcomeBuilder.Build(snapshot,
                        currentSettlement, currentSettlement, initialScore, initialScore));
            }
            var evaluator = new InventoryCandidateEvaluator(snapshot, policy, budget, elapsed, cancellationToken, current, batchEvaluator);
            scorer.ObserveTargets(currentSettlement, evaluator.TargetEvidence);
            InventoryOptimizationScore currentScore = initialScore;
            InventorySearchTerminationReason terminationReason =
                InventorySearchTerminationReason.ImprovementRoundLimit;
            bool searchStopped = false;
            InventoryLayoutProjection bestLayout = current;
            InventoryOptimizationScore bestScore = currentScore;

            for (int round = 0; round < budget.MaximumImprovementRounds;
                round++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bestLayout = current;
                bestScore = currentScore;

                if (!evaluator.Search(InventorySearchStage.Simple, round + 1, InventoryCandidateNeighborhoods.Simple(snapshot, current, policy.AllowStoneTabletRotation),
                        false, ref bestLayout, ref bestScore, out terminationReason))
                    searchStopped = true;

                if (searchStopped)
                {
                    break;
                }
                bool resumeLocalSearchAfterImprovement = false;
                if (bestScore.CompareTo(currentScore) <= 0)
                {
                    // Once every target is reached, exploit a compound improvement
                    // with another local-search round. The final round must finish
                    // comparing candidates because it cannot resume local search.
                    if (round + 1 < budget.MaximumImprovementRounds)
                    {
                        ProjectedInventorySettlement settlement =
                            InventorySettlementProjector.EvaluateForScoring(snapshot,
                                current, evaluator.EvaluationWorkspace);
                        resumeLocalSearchAfterImprovement = scorer.EvaluateTargets(
                            settlement, settlement).All(target => target.AfterConditionReached);
                    }
                    if (!evaluator.Search(InventorySearchStage.SwapAndRotation, round + 1, InventoryCandidateNeighborhoods.SwapAndRotation(snapshot, current, policy.AllowStoneTabletRotation),
                            resumeLocalSearchAfterImprovement, ref bestLayout, ref bestScore, out terminationReason))
                    {
                        searchStopped = true;
                        break;
                    }
                }
                if (bestScore.CompareTo(currentScore) <= 0)
                {
                    if (!evaluator.Search(InventorySearchStage.TwoSwaps, round + 1, InventoryCandidateNeighborhoods.TwoSwaps(snapshot, current),
                            resumeLocalSearchAfterImprovement, ref bestLayout, ref bestScore, out terminationReason))
                    {
                        searchStopped = true;
                        break;
                    }
                    int neighborhoodComparison = bestScore.CompareTo(
                        currentScore);
                    if (neighborhoodComparison < 0 ||
                        neighborhoodComparison == 0 &&
                        bestLayout.ContentEquals(current))
                    {
                        if (!evaluator.Search(InventorySearchStage.TwoItemRelocationAndRotation, round + 1, InventoryCandidateNeighborhoods.TwoItemRelocationAndRotation(snapshot, current, policy.AllowStoneTabletRotation),
                            false, ref bestLayout, ref bestScore, out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                        neighborhoodComparison = bestScore.CompareTo(
                            currentScore);
                    }
                    if (neighborhoodComparison < 0 ||
                        neighborhoodComparison == 0 &&
                        bestLayout.ContentEquals(current))
                    {
                        if (!evaluator.Search(InventorySearchStage.ThreeItemRelocation, round + 1, InventoryCandidateNeighborhoods.ThreeItemRelocation(snapshot, current),
                            false, ref bestLayout, ref bestScore, out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                        neighborhoodComparison = bestScore.CompareTo(
                            currentScore);
                        if (neighborhoodComparison < 0 ||
                            neighborhoodComparison == 0 &&
                            bestLayout.ContentEquals(current))
                        {
                            terminationReason = InventorySearchTerminationReason.
                                NeighborhoodLocalOptimum;
                            break;
                        }
                    }

                    // Equal-score layouts can expose a better neighborhood on
                    // the next round. Stable ordering makes this plateau walk
                    // strictly monotonic, while the shared evaluation and time
                    // budgets keep it bounded.
                }
                current = bestLayout;
                currentScore = bestScore;
            }

            // A budget cutoff can interrupt any neighborhood after it found an
            // improvement. Retain that evaluated result even for a partial round.
            current = bestLayout;
            currentScore = bestScore;
            evaluator.CompleteEvidence();
            ProjectedInventorySettlement bestSettlement =
                InventorySettlementProjector.Evaluate(snapshot, current);
            InventoryOptimizationOutcome outcome =
                InventoryOptimizationOutcomeBuilder.Build(snapshot,
                    currentSettlement, bestSettlement, initialScore,
                    currentScore);
            return new InventoryOptimizationProposal(true, current, initialScore,
                currentScore, evaluator.CandidateEvaluations, Array.Empty<string>(), policy,
                scorer.EvaluateTargets(currentSettlement, bestSettlement,
                    evaluator.TargetEvidence),
                terminationReason, elapsed.ElapsedMilliseconds,
                duplicateLayoutsSkipped:
                    evaluator.DuplicateLayoutsSkipped,
                outcome: outcome, searchStages: evaluator.SearchStages.ToArray());
        }

        private static InventoryOptimizationProposal Failure(string issue,
            long elapsedMilliseconds)
        {
            return new InventoryOptimizationProposal(false, null, null, null, 0,
                new[] { issue }, elapsedMilliseconds: elapsedMilliseconds);
        }
    }
}
