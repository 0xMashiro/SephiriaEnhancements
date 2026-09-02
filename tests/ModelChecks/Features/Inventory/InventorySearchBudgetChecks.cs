using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySearchBudgetChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ResolvedInventoryOptimizationPolicy defaultPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                InventoryOptimizationPreferences.Default);
        InventoryOptimizationProposal evaluationLimited = InventoryOptimizer.Solve(
            rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 1,
                maximumElapsedMilliseconds: 1000));
        if (!evaluationLimited.Succeeded ||
            evaluationLimited.CandidateEvaluations != 1 ||
            evaluationLimited.TerminationReason !=
                InventorySearchTerminationReason.CandidateEvaluationLimit)
            throw new InvalidOperationException(
                "candidate evaluation budget must stop search after the initial layout");

        InventoryOptimizationProposal timeLimited = InventoryOptimizer.Solve(
            rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumImprovementRounds: 4,
                maximumCandidateEvaluations: 100,
                maximumElapsedMilliseconds: 0));
        if (!timeLimited.Succeeded || timeLimited.CandidateEvaluations != 1 ||
            timeLimited.TerminationReason !=
                InventorySearchTerminationReason.ElapsedTimeLimit)
            throw new InvalidOperationException(
                "elapsed time budget must stop search after the initial layout");
        Console.WriteLine("InventorySearchBudget: evaluation and elapsed-time limits passed");
    }
}
