#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryLayoutRefinement
    {
        internal static InventoryOptimizationProposal Improve(InventoryOptimizationRequest request,
            InventoryOptimizationProposal initial, Stopwatch totalElapsed, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var budget = request.Budget;
            if (budget.RefinementCandidateEvaluations == 0 || !initial.Succeeded || initial.OptimalityProven ||
                (request.Snapshot.PositionEffects.Rules.Count == 0 && !request.Snapshot.Items.Any(item => item.StoneTablet != null)) ||
                initial.BestScore.HasDefaultProtectionTradeoff || !initial.BestScore.HardConstraintsSatisfied ||
                initial.TargetEvaluations.Any(target => !target.AfterConditionReached) ||
                InventoryAdditiveScoreBound.IsAttained(request.Snapshot, request.Policy, initial.BestScore)) return initial;

            var elapsed = Stopwatch.StartNew();
            var snapshot = request.Snapshot;
            var scorer = new InventoryOptimizationScorer(snapshot, request.Policy);
            var workspace = new InventorySettlementProjectionWorkspace(snapshot);
            var best = initial.Layout;
            var bestScore = initial.BestScore;
            int evaluations = 0, duplicates = 0;
            var visited = new HashSet<InventoryLayoutProjection>(InventoryCandidateEvaluator.LayoutComparer.Instance) { best };
            var evidence = InventoryTargetSearchEvidence.Capture(initial.TargetEvaluations);
            var stages = initial.SearchStages.ToList();
            var reason = InventorySearchTerminationReason.RefinementCompleted;
            for (int round = 0; round < Math.Min(4, budget.MaximumImprovementRounds) && !Stopped(); round++)
            {
                var startingScore = bestScore;
                var settlement = InventorySettlementProjector.Evaluate(snapshot, best);
                Search(InventorySearchStage.GroupRelocation, round + 1,
                    InventoryGroupRelocation.Enumerate(snapshot, best, settlement, cancellationToken));
                if (!Stopped()) Search(InventorySearchStage.Simple, round + 1,
                    InventoryCandidateNeighborhoods.Simple(snapshot, best, request.Policy.AllowStoneTabletRotation));
                if (bestScore.CompareTo(startingScore) <= 0) break;
            }
            if (evaluations == 0) return initial;
            // All strategies return through the same projection and result contract.
            return request.CreateProposal(best, initial.CandidateEvaluations + evaluations, reason,
                totalElapsed.ElapsedMilliseconds, initial.SearchMethod, false,
                initial.DuplicateLayoutsSkipped + duplicates, evidence, stages.ToArray());

            bool Stopped()
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (budget.UseCandidateEvaluationLimit && (evaluations >= budget.RefinementCandidateEvaluations ||
                    initial.CandidateEvaluations + evaluations >= budget.MaximumCandidateEvaluations))
                {
                    reason = InventorySearchTerminationReason.CandidateEvaluationLimit;
                    return true;
                }
                if (budget.UseElapsedTimeLimit && (elapsed.ElapsedMilliseconds >= budget.RefinementElapsedMilliseconds ||
                    totalElapsed.ElapsedMilliseconds >= budget.MaximumElapsedMilliseconds))
                {
                    reason = InventorySearchTerminationReason.ElapsedTimeLimit;
                    return true;
                }
                return false;
            }

            void Search(InventorySearchStage stage, int round, IEnumerable<InventoryLayoutProjection> candidates)
            {
                var statistics = new InventorySearchStageStatistics(stage, round);
                long startedAt = elapsed.ElapsedMilliseconds;
                stages.Add(statistics);
                foreach (var candidate in candidates)
                {
                    if (Stopped()) break;
                    if (!visited.Add(candidate)) { duplicates++; statistics.DuplicateLayoutsSkipped++; continue; }
                    var settlement = InventorySettlementProjector.EvaluateForScoring(snapshot, candidate, workspace);
                    evaluations++;
                    statistics.CandidateEvaluations++;
                    if (!settlement.Succeeded) continue;
                    scorer.ObserveTargets(settlement, evidence);
                    var score = scorer.Score(candidate, settlement);
                    if (score.CompareTo(bestScore) <= 0 || score.HasDefaultProtectionTradeoff || !score.HardConstraintsSatisfied ||
                        scorer.EvaluateTargets(settlement, settlement).Any(target => !target.AfterConditionReached)) continue;
                    if (!InventoryLayoutPlanner.TryCreate(snapshot, candidate, out _, out _, cancellationToken)) continue;
                    best = candidate;
                    bestScore = score;
                    statistics.Improvements++;
                    statistics.LastImprovementCandidate = initial.CandidateEvaluations + evaluations;
                }
                statistics.ElapsedMilliseconds = elapsed.ElapsedMilliseconds - startedAt;
            }
        }
    }
}
