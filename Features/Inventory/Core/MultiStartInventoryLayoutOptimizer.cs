#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SephiriaEnhancements.Inventory
{
    // A source-level example of a strategy composed with the existing optimizer.
    // The warm-up receives the original budget; restarts only spend what remains.
    internal sealed class MultiStartInventoryLayoutOptimizer : IInventoryLayoutOptimizer
    {
        private const int StepsPerStart = 128;
        private readonly int seed;

        internal MultiStartInventoryLayoutOptimizer(int seed = 0x5E71)
        {
            this.seed = seed;
        }

        public InventoryOptimizerMetadata Metadata { get; } = new(
            "builtin.multistart", 50,
            InventoryOptimizerCapabilities.ArtifactTargets |
            InventoryOptimizerCapabilities.ComboTargets |
            InventoryOptimizerCapabilities.InstanceTargets |
            InventoryOptimizerCapabilities.StoneTabletRotation |
            InventoryOptimizerCapabilities.FullInventory);

        public bool CanOptimize(InventoryOptimizationRequest request) =>
            request?.Snapshot?.Items.Count > 0 && request.Snapshot.Storage > 1 &&
            request.Policy?.SearchEffort == InventorySearchEffort.Thorough;

        public bool TryOptimize(InventoryOptimizationRequest request,
            CancellationToken cancellationToken, out InventoryOptimizationProposal proposal)
        {
            cancellationToken.ThrowIfCancellationRequested();
            proposal = null;
            if (!CanOptimize(request)) return false;

            var elapsed = Stopwatch.StartNew();
            proposal = InventoryOptimizer.Solve(request.Snapshot, request.Policy,
                request.Budget, cancellationToken);
            if (proposal.CurrentScore == null) return true;

            InventorySnapshot snapshot = request.Snapshot;
            InventorySearchBudget budget = request.Budget;
            var scorer = new InventoryOptimizationScorer(snapshot, request.Policy);
            var workspace = new InventorySettlementProjectionWorkspace(snapshot.Storage);
            var random = new Random(seed);
            InventoryLayoutProjection original = InventoryLayoutProjection.Current(snapshot);
            InventoryLayoutProjection bestLayout = proposal.Layout ?? original;
            InventoryOptimizationScore bestScore = proposal.Succeeded
                ? proposal.BestScore : proposal.CurrentScore;
            int evaluations = proposal.CandidateEvaluations;
            int restarts = 0;
            InventorySearchTerminationReason reason = proposal.TerminationReason;

            for (int start = 0; start < budget.MaximumImprovementRounds; start++)
            {
                if (Stopped()) break;
                // Shuffle all cells, including empty ones, preserving item identity.
                int[] cells = Enumerable.Range(0, snapshot.Storage).ToArray();
                for (int index = cells.Length - 1; index > 0; index--)
                {
                    int other = random.Next(index + 1);
                    (cells[index], cells[other]) = (cells[other], cells[index]);
                }
                int[] rotations = original.CopyRotations();
                for (int index = 0; index < rotations.Length; index++)
                    if (request.Policy.AllowStoneTabletRotation &&
                        snapshot.Items[index].StoneTablet?.Rotatable == true)
                        rotations[index] = random.Next(4);

                var current = new InventoryLayoutProjection(
                    cells.Take(snapshot.Items.Count).ToArray(), rotations);
                if (!TryEvaluate(current, out InventoryOptimizationScore currentScore)) break;
                restarts++;
                if (currentScore == null) continue;

                for (int step = 0; step < StepsPerStart; step++)
                {
                    if (Stopped()) break;
                    int item = random.Next(snapshot.Items.Count);
                    InventoryLayoutProjection candidate;
                    if (request.Policy.AllowStoneTabletRotation &&
                        snapshot.Items[item].StoneTablet?.Rotatable == true && random.Next(3) == 0)
                        candidate = current.WithRotation(item,
                            (current.GetRotation(item) + 1 + random.Next(3)) % 4);
                    else
                        candidate = current.WithCellsSwapped(current.GetCell(item),
                            random.Next(snapshot.Storage));
                    if (candidate.ContentEquals(current)) continue;
                    if (!TryEvaluate(candidate, out InventoryOptimizationScore score)) break;
                    // A restart can be worse than the incumbent. Local moves can
                    // cross plateaus, while only the global best is ever submitted.
                    if (score != null && score.CompareTo(currentScore) >= 0)
                    {
                        current = candidate;
                        currentScore = score;
                    }
                }
            }

            if (restarts == 0) return true;
            if (!Stopped()) reason = InventorySearchTerminationReason.ImprovementRoundLimit;
            proposal = request.CreateProposal(bestLayout, evaluations, reason,
                elapsed.ElapsedMilliseconds, InventoryOptimizationSearchMethod.MultiStart,
                duplicateLayoutsSkipped: proposal.DuplicateLayoutsSkipped);
            return true;

            bool Stopped()
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (evaluations >= budget.MaximumCandidateEvaluations)
                {
                    reason = InventorySearchTerminationReason.CandidateEvaluationLimit;
                    return true;
                }
                if (budget.UseElapsedTimeLimit && elapsed.ElapsedMilliseconds >= budget.MaximumElapsedMilliseconds)
                {
                    reason = InventorySearchTerminationReason.ElapsedTimeLimit;
                    return true;
                }
                return false;
            }

            bool TryEvaluate(InventoryLayoutProjection candidate, out InventoryOptimizationScore score)
            {
                score = null;
                if (Stopped()) return false;
                ProjectedInventorySettlement settlement = InventorySettlementProjector.
                    EvaluateForScoring(snapshot, candidate, workspace);
                evaluations++;
                if (!settlement.Succeeded) return true;
                score = scorer.Score(candidate, settlement);
                if (score.CompareTo(bestScore) > 0 || score.CompareTo(bestScore) == 0 &&
                    candidate.CompareStableTo(bestLayout) < 0)
                {
                    bestLayout = candidate;
                    bestScore = score;
                }
                return true;
            }
        }
    }
}
