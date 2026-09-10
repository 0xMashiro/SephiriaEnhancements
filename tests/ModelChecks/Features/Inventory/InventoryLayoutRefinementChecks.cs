using System.Diagnostics;
using System.Text.Json;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryLayoutRefinementChecks
{
    internal static void Run()
    {
        foreach (var effort in Enum.GetValues<InventorySearchEffort>())
        {
            var budget = InventorySearchBudget.ForEffort(effort);
            var first = budget.InitialSearchBudget();
            int expectedCandidates = effort == InventorySearchEffort.Fast ? 1500 : effort == InventorySearchEffort.Thorough ? 15000 : 5000;
            int expectedTime = effort == InventorySearchEffort.Fast ? 50 : effort == InventorySearchEffort.Thorough ? 1500 : 200;
            Require(!budget.UseCandidateEvaluationLimit && !first.UseCandidateEvaluationLimit &&
                first.MaximumCandidateEvaluations == expectedCandidates && first.MaximumElapsedMilliseconds == expectedTime,
                "refinement must not take the original search allowance");
            using var json = JsonDocument.Parse(InventoryReproductionJson.Serialize(budget));
            var replay = InventoryReproductionReplay.Read<InventorySearchBudget>(json.RootElement);
            Require(replay.RefinementCandidateEvaluations == budget.RefinementCandidateEvaluations &&
                replay.RefinementElapsedMilliseconds == budget.RefinementElapsedMilliseconds &&
                replay.UseCandidateEvaluationLimit == budget.UseCandidateEvaluationLimit, "recorded phase budgets");
        }
        var snapshot = Chain(5);
        var flat = InventorySnapshotFixture.ArtifactsAtLevels(new int[30], new[] { 0, 1, 2 }, 3);
        var flatPolicy = InventoryOptimizationPolicyResolver.Resolve(flat, InventoryOptimizationPreferences.Default);
        var flatResult = InventoryOptimizerSelector.Solve(flat, flatPolicy, InventorySearchBudget.ForEffort(InventorySearchEffort.Balanced));
        var flatInitial = InventoryOptimizerSelector.Solve(flat, flatPolicy, InventorySearchBudget.ForEffort(InventorySearchEffort.Balanced).InitialSearchBudget());
        Require(flatResult.BestScore.CompareTo(flatInitial.BestScore) == 0 && !flatResult.SearchStages.Any(stage => stage.Stage == InventorySearchStage.GroupRelocation),
            "an already optimal large flat board must not spend refinement time");
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
        CountLimitModes(snapshot, policy);
        var budgetWithRefinement = new InventorySearchBudget(16, 25000, 1650, false, 10000, 150);
        var initial = InventoryOptimizer.Solve(snapshot, policy, budgetWithRefinement.InitialSearchBudget());
        var refined = InventoryOptimizerSelector.Solve(snapshot, policy, budgetWithRefinement);
        Require(refined.BestScore.CompareTo(initial.BestScore) > 0 &&
            refined.SearchStages.Any(stage => stage.Stage == InventorySearchStage.GroupRelocation && stage.Improvements > 0),
            "selector must improve a five-item dependency group after the original search");
        Require(refined.BestScore.PositionEffectRegressions == 0 && refined.BestScore.AutomaticLevelRegressions == 0 &&
            refined.TargetEvaluations.All(target => target.AfterConditionReached), "retained goals and benefits");
        Require(refined.CandidateEvaluations <= budgetWithRefinement.MaximumCandidateEvaluations &&
            InventoryLayoutPlanner.TryCreate(snapshot, refined.Layout, out _, out _), "bounded and applicable refinement");
        foreach (int allowance in new[] { 0, 1, 7 })
        {
            var budget = new InventorySearchBudget(16, 15000 + allowance, 1650, false, allowance, 150);
            var result = InventoryOptimizerSelector.Solve(snapshot, policy, budget);
            Require(result.CandidateEvaluations <= budget.MaximumCandidateEvaluations && result.BestScore.CompareTo(initial.BestScore) >= 0,
                "partial refinement retains the original best within its allowance");
        }
        var request = new InventoryOptimizationRequest(snapshot, policy, budgetWithRefinement);
        var proven = request.CreateProposal(refined.Layout, refined.CandidateEvaluations,
            InventorySearchTerminationReason.SearchSpaceExhausted, 0, optimalityProven: true);
        Require(ReferenceEquals(proven, InventoryLayoutRefinement.Improve(request, proven, Stopwatch.StartNew(), default)),
            "proven solutions skip refinement");
        var expired = new InventoryOptimizationRequest(snapshot, policy, new InventorySearchBudget(16, 25000, 0, true, 10000, 0));
        Require(ReferenceEquals(initial, InventoryLayoutRefinement.Improve(expired, initial, Stopwatch.StartNew(), default)),
            "expired total deadline does not start another phase");

        var impossible = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            Array.Empty<ArtifactOptimizationPreference>(), new[] { new ComboOptimizationPreference("TestPlanet",
                InventoryPreferenceLevel.Priority, 99, InventoryConstraintStrength.Hard) });
        var impossiblePolicy = InventoryOptimizationPolicyResolver.Resolve(snapshot, impossible);
        var rejected = InventoryOptimizer.Solve(snapshot, impossiblePolicy, new InventorySearchBudget(1, 100, 0, false));
        Require(ReferenceEquals(rejected, InventoryLayoutRefinement.Improve(new InventoryOptimizationRequest(snapshot, impossiblePolicy,
            budgetWithRefinement), rejected, Stopwatch.StartNew(), default)), "unmet targets skip refinement");

        using var cancelled = new CancellationTokenSource();
        var original = InventoryLayoutProjection.Current(snapshot);
        using var moves = InventoryGroupRelocation.Enumerate(snapshot, original,
            InventorySettlementProjector.Evaluate(snapshot, original), cancelled.Token).GetEnumerator();
        Require(moves.MoveNext(), "chain has translations");
        cancelled.Cancel();
        bool interrupted = false;
        try { moves.MoveNext(); } catch (OperationCanceledException) { interrupted = true; }
        Require(interrupted, "cancellation reaches group generation");
        interrupted = false;
        try { InventoryLayoutRefinement.Improve(request, initial, Stopwatch.StartNew(), cancelled.Token); }
        catch (OperationCanceledException) { interrupted = true; }
        Require(interrupted, "cancellation reaches refinement");
        TranslationAndRotation();
        QualityRegressionMatrix();
        Console.WriteLine("Inventory refinement: phase budgets, replay, dependency group, retained goals, cutoffs, skips, cancellation, overlap and rotation passed");
    }

    private static void CountLimitModes(InventorySnapshot snapshot, ResolvedInventoryOptimizationPolicy policy)
    {
        var limited = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(1, 1, 1000));
        Require(limited.CandidateEvaluations == 1 && limited.TerminationReason == InventorySearchTerminationReason.CandidateEvaluationLimit,
            "offline evaluation limit still stops search");
        var timed = InventoryOptimizer.Solve(snapshot, policy,
            new InventorySearchBudget(1, 1, 1000, useCandidateEvaluationLimit: false));
        Require(timed.CandidateEvaluations > 1 && timed.TerminationReason != InventorySearchTerminationReason.CandidateEvaluationLimit,
            "time-only search crosses the count limit");
        var thoroughPolicy = InventoryOptimizationPolicyResolver.Resolve(snapshot,
            InventoryOptimizationPreferences.Default.WithExecutionSettings(InventorySearchEffort.Thorough, true));
        var multiRequest = new InventoryOptimizationRequest(snapshot, thoroughPolicy,
            new InventorySearchBudget(1, 1, 1000, false, useCandidateEvaluationLimit: false));
        Require(new MultiStartInventoryLayoutOptimizer().TryOptimize(multiRequest, default, out var multi) &&
            multi.SearchStages.Any(stage => stage.Stage == InventorySearchStage.Restart && stage.CandidateEvaluations > 0) &&
            multi.TerminationReason != InventorySearchTerminationReason.CandidateEvaluationLimit,
            "multistart retains the disabled count limit after initial search");
        var expired = InventoryOptimizer.Solve(snapshot, policy,
            new InventorySearchBudget(1, 1, 0, useCandidateEvaluationLimit: false));
        Require(expired.TerminationReason == InventorySearchTerminationReason.ElapsedTimeLimit,
            "time-only search respects the deadline");
        var refinementBudget = new InventorySearchBudget(4, 2, 1000, true, 1, 500, false);
        var request = new InventoryOptimizationRequest(snapshot, policy, refinementBudget);
        var initial = request.CreateProposal(timed.Layout, 3, InventorySearchTerminationReason.NeighborhoodLocalOptimum, 0);
        var refined = InventoryLayoutRefinement.Improve(request, initial, Stopwatch.StartNew(), default);
        Require(refined.CandidateEvaluations > initial.CandidateEvaluations + 1 &&
            refined.TerminationReason != InventorySearchTerminationReason.CandidateEvaluationLimit,
            "time-only refinement crosses both phase and total count limits");
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        bool interrupted = false;
        try { InventoryOptimizer.Solve(snapshot, policy, refinementBudget, cancelled.Token); }
        catch (OperationCanceledException) { interrupted = true; }
        Require(interrupted, "time-only search remains cancellable");
    }

    private static void TranslationAndRotation()
    {
        var random = new Random(3809);
        foreach (int storage in new[] { 30, 32, 42 })
        {
            var c = InventoryKnownSolutionFixture.Create(storage, 419, "SignedEffects");
            var layout = InventoryLayoutProjection.Current(c.Snapshot);
            for (int step = 0; step < 100; step++)
            {
                var group = Enumerable.Range(0, layout.ItemCount).OrderBy(_ => random.Next()).Take(random.Next(2, 6)).ToHashSet();
                int dx = random.Next(-2, 3), dy = random.Next(-2, 3);
                var moved = InventoryGroupRelocation.Translate(c.Snapshot, layout, group, dx, dy);
                if (moved == null) continue;
                Require(moved.CopyCells().Distinct().Count() == layout.ItemCount && moved.CopyCells().All(cell => cell >= 0 && cell < storage) &&
                    group.All(item => moved.GetCell(item) == layout.GetCell(item) + dy * c.Snapshot.Width + dx) &&
                    moved.CopyRotations().SequenceEqual(layout.CopyRotations()) &&
                    InventoryLayoutPlanner.TryCreate(c.Snapshot, moved, out _, out _), "overlap and partial last row preserve identities");
            }
            var policy = InventoryOptimizationPolicyResolver.Resolve(c.Snapshot,
                c.Preferences.WithExecutionSettings(InventorySearchEffort.Balanced, false));
            var result = InventoryOptimizerSelector.Solve(c.Snapshot, policy, new InventorySearchBudget(16, 25000, 0, false, 10000, 0));
            Require(result.Layout.CopyRotations().SequenceEqual(layout.CopyRotations()), "refinement respects prohibited rotation");
        }
    }

    private static void QualityRegressionMatrix()
    {
        int improved = 0, checkedCases = 0;
        foreach (int storage in new[] { 30, 42 })
            foreach (string variant in new[] { "NeighborPair", "PlacementAndNeighbors", "SignedEffects" })
                foreach (int seed in new[] { 419, 2027 })
                    foreach (var effort in new[] { InventorySearchEffort.Balanced, InventorySearchEffort.Thorough })
                    {
                        var c = InventoryKnownSolutionFixture.Create(storage, seed, variant);
                        var policy = InventoryOptimizationPolicyResolver.Resolve(c.Snapshot,
                            c.Preferences.WithExecutionSettings(effort, true));
                        var configured = InventorySearchBudget.ForEffort(effort);
                        var budget = new InventorySearchBudget(configured.MaximumImprovementRounds, configured.MaximumCandidateEvaluations,
                            configured.MaximumElapsedMilliseconds, false, configured.RefinementCandidateEvaluations, configured.RefinementElapsedMilliseconds);
                        var initial = InventoryOptimizerSelector.Solve(c.Snapshot, policy, budget.InitialSearchBudget());
                        var result = InventoryOptimizerSelector.Solve(c.Snapshot, policy, budget);
                        Require(result.BestScore.CompareTo(initial.BestScore) >= 0 && result.CandidateEvaluations <= budget.MaximumCandidateEvaluations,
                            "post-search quality and total budget on held-out fixtures");
                        if (initial.TargetEvaluations.All(target => target.AfterConditionReached))
                            Require(result.TargetEvaluations.All(target => target.AfterConditionReached), "previously achieved targets survive refinement");
                        else Require(result.BestScore.CompareTo(initial.BestScore) == 0, "unmet targets do not enter refinement");
                        if (result.BestScore.CompareTo(initial.BestScore) > 0) improved++;
                        checkedCases++;
                    }
        Console.WriteLine($"Inventory refinement quality: {checkedCases} comparisons; {improved} improved; no regressions");
    }

    private static InventorySnapshot Chain(int count)
    {
        var levels = Enumerable.Range(0, 30).Select(cell => cell % 6 == 4 ? 3 : 0).ToArray();
        var rules = Enumerable.Range(1, count - 1).Select(index => new InventoryPositionEffectRule(new InventoryItemKey(9000 + index, 0),
            InventoryPositionEffectKind.DependencyDamage, new[] { 1.0, 2.0, 3.0, 4.0 },
            offsets: new[] { new InventoryOffsetSnapshot(0, -1) })).ToArray();
        return InventoryPositionEffectChecks.Board(6, levels, Enumerable.Range(0, count).Select(index => index * 6).ToArray(),
            rules, Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
