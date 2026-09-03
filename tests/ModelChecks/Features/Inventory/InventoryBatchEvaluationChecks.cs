using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryBatchEvaluationChecks
{
    internal static void Run()
    {
        var snapshot = InventoryNeighborhoodFixture.StoneTabletMoveAndRotation();
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
        foreach (int limit in new[] { 1, 2, 255, 256, 257, 513, 5000 })
        {
            var backend = new CountingEvaluator(snapshot, policy);
            var budget = new InventorySearchBudget(8, limit, int.MaxValue);
            var expected = InventoryOptimizer.Solve(snapshot, policy, budget);
            var actual = InventoryOptimizer.Solve(snapshot, policy, budget, batchEvaluator: backend);
            if (actual.CandidateEvaluations != 1 + backend.Computed || actual.CandidateEvaluations > limit ||
                actual.CandidateEvaluations != expected.CandidateEvaluations || actual.DuplicateLayoutsSkipped != expected.DuplicateLayoutsSkipped ||
                !actual.Layout.ContentEquals(expected.Layout) || actual.BestScore.CompareTo(expected.BestScore) != 0 ||
                actual.TerminationReason != expected.TerminationReason)
                throw new InvalidOperationException("every admitted unique candidate must count and participate in deterministic batch selection");
        }
        using var cancellation = new CancellationTokenSource();
        var canceling = new CountingEvaluator(snapshot, policy) { AfterBatch = cancellation.Cancel };
        try
        {
            InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(8, 5000, int.MaxValue), cancellation.Token, canceling);
            throw new InvalidOperationException("cancellation after a batch must prevent publishing a proposal");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        Console.WriteLine("InventoryBatchEvaluation: physical budget, batch boundaries, duplicate filtering and cancellation passed");
    }

    private sealed class CountingEvaluator : IInventoryCandidateBatchEvaluator
    {
        private readonly InventorySnapshot snapshot;
        private readonly InventorySettlementProjectionWorkspace workspace;
        private readonly InventoryOptimizationScorer scorer;
        internal int Computed { get; private set; }
        internal Action? AfterBatch { get; init; }
        public int BatchSize => 256;
        internal CountingEvaluator(InventorySnapshot snapshot, ResolvedInventoryOptimizationPolicy policy)
        { this.snapshot = snapshot; workspace = new(snapshot); scorer = new(snapshot, policy); }
        public void Evaluate(IReadOnlyList<InventoryLayoutProjection> candidates, InventoryOptimizationScore[] scores,
            IDictionary<string, InventoryTargetSearchEvidence> evidence, CancellationToken cancellationToken)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var settlement = InventorySettlementProjector.EvaluateForScoring(snapshot, candidates[i], workspace);
                scores[i] = settlement.Succeeded ? scorer.Score(candidates[i], settlement) : null!;
                scorer.ObserveTargets(settlement, evidence);
                Computed++;
            }
            AfterBatch?.Invoke();
        }
    }
}
