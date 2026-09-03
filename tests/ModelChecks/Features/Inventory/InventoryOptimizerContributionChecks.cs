using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizerContributionChecks
{
    internal static string Run()
    {
        VerifyRegisteredStrategies();
        VerifyRestartsImproveAfterWarmup();
        VerifyRepeatabilityAndSharedBudget();
        VerifyCancellationAndRotationPolicy();
        VerifyProposalValidation();
        VerifySelectionRechecksOriginalPolicy();
        return "6 contribution checks passed (contracts, restarts, repeatability, budgets, cancellation, validation)";
    }

    // Reuse this check for a new strategy, with representative supported inputs.
    internal static InventoryOptimizationProposal VerifyContract(
        IInventoryLayoutOptimizer optimizer, InventoryOptimizationRequest request)
    {
        var original = InventoryLayoutProjection.Current(request.Snapshot);
        Check(optimizer.CanOptimize(request), optimizer.Metadata.Id + " must support this contract fixture");
        bool handled = optimizer.TryOptimize(request, default, out var result);
        Check(handled, optimizer.Metadata.Id + " must handle this contract fixture");
        if (!result.Succeeded || result.Layout == null)
            throw new InvalidOperationException(optimizer.Metadata.Id + " must produce a feasible fixture layout: " +
                string.Join(",", result.Issues));
        Check(result.CandidateEvaluations > 0 && result.CandidateEvaluations <= request.Budget.MaximumCandidateEvaluations,
            "all search stages share the candidate budget");
        var after = InventorySettlementProjector.Evaluate(request.Snapshot, result.Layout);
        var before = InventorySettlementProjector.Evaluate(request.Snapshot, original);
        var scorer = new InventoryOptimizationScorer(request.Snapshot, request.Policy);
        Check(after.Succeeded && result.Layout.ItemCount == original.ItemCount &&
            result.Layout.CopyCells().Distinct().Count() == original.ItemCount,
            "strategy must preserve every item in a distinct valid cell");
        Check(result.Policy == request.Policy && result.CurrentScore.CompareTo(scorer.Score(original, before)) == 0 &&
            result.BestScore.CompareTo(scorer.Score(result.Layout, after)) == 0 &&
            result.BestScore.CompareTo(result.CurrentScore) >= 0 && result.BestScore.HardConstraintsSatisfied,
            "reported scores must match the original policy and preserve the baseline");
        Check(result.Outcome != null && result.Outcome.AfterArtifactsEnabled == after.Artifacts.Count(a => a.Enabled),
            "result feedback must reflect the selected layout");
        Check(original.ContentEquals(InventoryLayoutProjection.Current(request.Snapshot)),
            "search must not mutate its source snapshot");
        Check(InventoryLayoutPlanner.TryCreate(request.Snapshot, result.Layout, out _, out _),
            "selected layout must be executable through normal inventory operations");
        return result;
    }

    private static void VerifyRegisteredStrategies()
    {
        var request = Request(InventoryNeighborhoodFixture.StoneTabletMoveAndRotation());
        foreach (var optimizer in InventoryOptimizerRegistry.Capture())
            VerifyContract(optimizer, request);
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 5, 6 }, new[] { 0, 1 }, 6);
        var hardPreferences = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, true,
            new[] { new ArtifactOptimizationPreference(board.Items[0].InstanceId, board.Items[0].EntityId,
                InventoryPreferenceLevel.Priority, 6, strength: InventoryConstraintStrength.Hard) },
            Array.Empty<ComboOptimizationPreference>());
        foreach (var optimizer in InventoryOptimizerRegistry.Capture())
        {
            var result = VerifyContract(optimizer, Request(board, preferences: hardPreferences));
            Check(result.Layout.GetCell(0) == 2 && result.TargetEvaluations.Single().AfterConditionReached,
                "every strategy must satisfy the hard item target and report it accurately");
        }
        Check(InventoryOptimizerRegistry.Capture().Select(o => o.Metadata.Id).SequenceEqual(
            new[] { "builtin.exact", "builtin.multistart", "builtin.bounded" }),
            "exact remains first; multistart precedes the bounded fallback");
        var multistart = new MultiStartInventoryLayoutOptimizer();
        foreach (var effort in new[] { InventorySearchEffort.Fast, InventorySearchEffort.Balanced })
        {
            var preferences = InventoryOptimizationPreferences.Default.WithExecutionSettings(effort, true);
            Check(!multistart.CanOptimize(Request(request.Snapshot, preferences: preferences)),
                "quick and balanced search must keep their existing strategy");
        }
        Check(!multistart.CanOptimize(null!), "missing input is unsupported");
        var largeForBudget = Request(board, new InventorySearchBudget(4, 4, 0, false));
        Check(InventoryOptimizerRegistry.Capture().First(o => o.CanOptimize(largeForBudget)).Metadata.Id == "builtin.multistart",
            "deep search selects multistart when exact enumeration exceeds the budget");
    }

    private static void VerifyRestartsImproveAfterWarmup()
    {
        var request = Request(InventorySnapshotFixture.ArtifactsAtLevels(
            new[] { 1, 2, 3, 4, 5, 6 }, new[] { 0, 1 }, 6),
            new InventorySearchBudget(1, 500, 0, false));
        var warmup = InventoryOptimizer.Solve(request.Snapshot, request.Policy, request.Budget);
        var result = VerifyContract(new MultiStartInventoryLayoutOptimizer(123), request);
        Check(warmup.BestScore.CappedEffectiveArtifactLevelTotal == 8 &&
            result.BestScore.CappedEffectiveArtifactLevelTotal == 11 &&
            result.BestScore.CompareTo(warmup.BestScore) > 0 &&
            result.CandidateEvaluations > warmup.CandidateEvaluations &&
            result.SearchMethod == InventoryOptimizationSearchMethod.MultiStart && !result.OptimalityProven,
            "restarts must do useful work after the one-round warm-up, reaching both highest-value cells");
    }

    private static void VerifyRepeatabilityAndSharedBudget()
    {
        var board = InventoryNeighborhoodFixture.StoneTabletMoveAndRotation();
        foreach (int limit in new[] { 1, 7, 100, 400, 2000 })
        {
            var request = Request(board, new InventorySearchBudget(4, limit, 0, false));
            var optimizer = new MultiStartInventoryLayoutOptimizer(123);
            var first = VerifyContract(optimizer, request);
            var second = VerifyContract(optimizer, request);
            var warmup = InventoryOptimizer.Solve(board, request.Policy, request.Budget);
            Check(first.Layout.ContentEquals(second.Layout) &&
                first.CandidateEvaluations == second.CandidateEvaluations &&
                first.BestScore.CompareTo(warmup.BestScore) >= 0,
                "fixed seed and evaluation budget must reproduce results without losing the warm-up best");
            if (limit == 1)
                Check(first.CandidateEvaluations == 1 && !first.Improved &&
                    first.TerminationReason == InventorySearchTerminationReason.CandidateEvaluationLimit,
                    "a one-evaluation budget preserves the source layout");
        }
        var timed = Request(board, new InventorySearchBudget(4, 1000, 0, true));
        var stopped = VerifyContract(new MultiStartInventoryLayoutOptimizer(), timed);
        Check(stopped.CandidateEvaluations == 1 && !stopped.Improved &&
            stopped.TerminationReason == InventorySearchTerminationReason.ElapsedTimeLimit,
            "zero time budget must not start a restart or evaluate a move");
    }

    private static void VerifyCancellationAndRotationPolicy()
    {
        var preferences = InventoryOptimizationPreferences.Default.WithExecutionSettings(InventorySearchEffort.Thorough, false);
        var request = Request(InventoryNeighborhoodFixture.StoneTabletMoveAndRotation(), preferences: preferences);
        var exactBudget = Request(request.Snapshot, new InventorySearchBudget(4, 30, 0, false), preferences);
        var exact = VerifyContract(new ExactInventoryLayoutOptimizer(), exactBudget);
        Check(exact.CandidateEvaluations == 30 && exact.OptimalityProven &&
            InventoryExhaustiveSearchOracle.EstimateCandidateLayouts(request.Snapshot, allowStoneTabletRotation: false) == 30,
            "exact search estimates and enumerates only the 30 permitted placements when rotation is disabled");
        foreach (var optimizer in InventoryOptimizerRegistry.Capture())
        {
            var result = VerifyContract(optimizer, request);
            Check(result.Layout.CopyRotations().SequenceEqual(InventoryLayoutProjection.Current(request.Snapshot).CopyRotations()),
                "disabled rotation must hold across every search stage");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            bool cancelled = false;
            try { optimizer.TryOptimize(request, cancellation.Token, out _); }
            catch (OperationCanceledException) { cancelled = true; }
            Check(cancelled, "strategies must honor cancellation before evaluating candidates");
        }
    }

    private static void VerifyProposalValidation()
    {
        var request = Request(InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 5, 6 }, new[] { 0, 1 }, 6));
        var good = request.CreateProposal(new InventoryLayoutProjection(new[] { 2, 1 }, new[] { 0, 0 }),
            2, InventorySearchTerminationReason.ImprovementRoundLimit, 0);
        Check(good.Succeeded && good.Improved && good.Outcome.BeforeEffectiveLevels == 6 &&
            good.Outcome.AfterEffectiveLevels == 11, "factory must compute real before/after scores and feedback");
        foreach (var invalid in new[]
        {
            new InventoryLayoutProjection(new[] { 1, 1 }, new[] { 0, 0 }),
            new InventoryLayoutProjection(new[] { 0, 3 }, new[] { 0, 0 }),
            new InventoryLayoutProjection(new[] { 0 }, new[] { 0 }),
            new InventoryLayoutProjection(new[] { 0, 1 }, Array.Empty<int>())
        })
        {
            var rejected = request.CreateProposal(invalid, 2, InventorySearchTerminationReason.ImprovementRoundLimit, 0);
            Check(!rejected.Succeeded && rejected.Layout == null &&
                rejected.TerminationReason == InventorySearchTerminationReason.InputRejected,
                "invalid occupancy, item count and rotation arrays must be rejected");
        }
        var worseRequest = Request(InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 5, 6 }, new[] { 2 }, 6));
        var worse = worseRequest.CreateProposal(new InventoryLayoutProjection(new[] { 0 }, new[] { 0 }),
            2, InventorySearchTerminationReason.ImprovementRoundLimit, 0);
        Check(worse.Succeeded && !worse.Improved && worse.Layout.GetCell(0) == 2,
            "an inferior candidate must preserve the original layout");
        var tabletBoard = InventoryNeighborhoodFixture.StoneTabletMoveAndRotation();
        var noRotation = Request(tabletBoard, preferences: InventoryOptimizationPreferences.Default.
            WithExecutionSettings(InventorySearchEffort.Thorough, false));
        var rotated = noRotation.CreateProposal(InventoryLayoutProjection.Current(tabletBoard).WithRotation(1, 1),
            2, InventorySearchTerminationReason.ImprovementRoundLimit, 0);
        Check(!rotated.Succeeded && rotated.Issues.Contains("StoneTabletRotationDisabled"),
            "the shared factory must reject rotations prohibited by the request");
    }

    private static void VerifySelectionRechecksOriginalPolicy()
    {
        var board = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 1, 5 }, new[] { 0 }, 5);
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, true,
            new[] { new ArtifactOptimizationPreference(board.Items[0].InstanceId, board.Items[0].EntityId,
                InventoryPreferenceLevel.Priority, 5, strength: InventoryConstraintStrength.Hard) },
            Array.Empty<ComboOptimizationPreference>());
        var request = Request(board, preferences: preferences);
        var forged = new SuppliedLayoutOptimizer(InventoryLayoutProjection.Current(board));
        var rejected = InventoryOptimizerSelector.Solve(request, new[] { forged });
        Check(!rejected.Succeeded && rejected.Layout == null && rejected.Policy == request.Policy &&
            rejected.HardConstraintStatus == InventoryHardConstraintStatus.NotFound,
            "a strategy cannot replace Hard requirements with its own policy or fake a proof");

        var invalid = new SuppliedLayoutOptimizer(new InventoryLayoutProjection(new[] { 99 }, new[] { 0 }));
        var recovered = InventoryOptimizerSelector.Solve(request, new IInventoryLayoutOptimizer[]
            { invalid, new BoundedInventoryLayoutOptimizer() });
        Check(recovered.Succeeded && recovered.Layout.GetCell(0) == 1 &&
            recovered.BestScore.HardConstraintsSatisfied,
            "an invalid proposal must fall through to a strategy that satisfies the original requirement");
    }

    private sealed class SuppliedLayoutOptimizer(InventoryLayoutProjection layout) : IInventoryLayoutOptimizer
    {
        public InventoryOptimizerMetadata Metadata { get; } = new("test.supplied", 200, InventoryOptimizerCapabilities.FullInventory);
        public bool CanOptimize(InventoryOptimizationRequest request) => true;
        public bool TryOptimize(InventoryOptimizationRequest request, CancellationToken token, out InventoryOptimizationProposal proposal)
        {
            var otherPolicy = InventoryOptimizationPolicyResolver.Resolve(request.Snapshot, InventoryOptimizationPreferences.Default);
            var original = InventoryLayoutProjection.Current(request.Snapshot);
            var score = new InventoryOptimizationScorer(request.Snapshot, otherPolicy).
                Score(original, InventorySettlementProjector.Evaluate(request.Snapshot, original));
            proposal = new InventoryOptimizationProposal(true, layout, score, score, 1, Array.Empty<string>(),
                otherPolicy, terminationReason: InventorySearchTerminationReason.SearchSpaceExhausted, optimalityProven: true);
            return true;
        }
    }

    private static InventoryOptimizationRequest Request(InventorySnapshot board,
        InventorySearchBudget? budget = null, InventoryOptimizationPreferences? preferences = null) => new(board,
            InventoryOptimizationPolicyResolver.Resolve(board, preferences ??
                InventoryOptimizationPreferences.Default.WithExecutionSettings(InventorySearchEffort.Thorough, true)),
            budget ?? new InventorySearchBudget(4, 2000, 0, false));

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
