using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryAdditiveScoreBoundChecks
{
    internal static void Run()
    {
        int certificates = 0;
        foreach (int first in Enumerable.Range(0, 3))
            foreach (int second in Enumerable.Range(0, 3).Where(cell => cell != first))
                foreach (int negative in new[] { 1, -1 })
                {
                    var snapshot = Create(new[] { first, second, 3 - first - second }, negative);
                    if (!snapshot.SettlementValidation.LayoutProjectionReady)
                        throw new InvalidOperationException(string.Join(";", snapshot.SettlementValidation.Issues));
                    var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
                    var budget = new InventorySearchBudget(8, 200, int.MaxValue);
                    var exact = InventoryOptimizerSelector.Solve(snapshot, policy, budget);
                    var result = InventoryOptimizer.Solve(snapshot, policy, budget);
                    Require(exact.OptimalityProven, "small reference must exhaust its search space");
                    if (result.TerminationReason == InventorySearchTerminationReason.ScoreUpperBoundReached)
                    {
                        certificates++;
                        Require(result.OptimalityProven && result.CandidateEvaluations == 1 && !result.Improved &&
                            result.BestScore.CompareTo(exact.BestScore) == 0 && result.SearchStages.Count == 0,
                            "every certificate must match the exhaustive optimum, including movement and overflow");
                        var deepPolicy = InventoryOptimizationPolicyResolver.Resolve(snapshot,
                            InventoryOptimizationPreferences.Default.WithExecutionSettings(InventorySearchEffort.Thorough, true));
                        var deepRequest = new InventoryOptimizationRequest(snapshot, deepPolicy,
                            new InventorySearchBudget(8, 200, 0, false));
                        Require(new MultiStartInventoryLayoutOptimizer().TryOptimize(deepRequest, default, out var deepResult) &&
                            deepResult.OptimalityProven && deepResult.CandidateEvaluations == 1 &&
                            deepResult.TerminationReason == InventorySearchTerminationReason.ScoreUpperBoundReached &&
                            deepResult.Layout.ContentEquals(result.Layout) && deepResult.BestScore.CompareTo(exact.BestScore) == 0,
                            "deep search must retain the proven optimum without spending restart evaluations");
                    }
                    else Require(result.BestScore.CompareTo(exact.BestScore) == 0, "ordinary search remains available below the upper bound");
                }
        Require(certificates == 4, "positive additions and layouts keeping negative effects off the board can attain the bound");
        var basis = Create(new[] { 0, 1, 2 });
        var item = basis.Items[1];
        var targets = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            new[] { new ArtifactOptimizationPreference(item.InstanceId, item.EntityId, InventoryPreferenceLevel.Priority, 1, 0) },
            Array.Empty<ComboOptimizationPreference>());
        foreach (var (snapshot, preferences) in new[] {
            (basis, targets),
            (Create(new[] { 0, 1, 2 }, kind: TabletEffectKind.MultiplyLevel), InventoryOptimizationPreferences.Default),
            (InventoryKnownSolutionFixture.Create(30, 101).Snapshot, InventoryOptimizationPreferences.Default) })
        {
            var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
            var current = InventoryLayoutProjection.Current(snapshot);
            var score = new InventoryOptimizationScorer(snapshot, policy).Score(current, InventorySettlementProjector.Evaluate(snapshot, current));
            Require(!InventoryAdditiveScoreBound.IsAttained(snapshot, policy, score),
                "targets, multiplication and placement effects must not use the additive certificate");
        }
        Console.WriteLine("InventoryAdditiveScoreBound: exhaustive certificates, negative effects and unsupported-mechanism fallback passed");
    }

    private static InventorySnapshot Create(int[] positions, int secondValue = 1, TabletEffectKind kind = TabletEffectKind.IncreaseLevel)
    {
        var placements = Enumerable.Range(0, 3).Select(cell => new TabletPlacementProjectionSnapshot(cell, cell, 0,
            Enumerable.Range(0, 4).Select(rotation => new TabletRotationProjectionSnapshot(rotation, Array.Empty<TabletAdditionSnapshot>(),
                new[] { 1, secondValue }.Select((value, index) => new TabletAdditionSnapshot(cell + index + 1, 0, "test",
                    cell + index + 1 < 3, false, false, false, false, false, false,
                    effectKind: kind, levelParameter: value)).ToArray(), true)).ToArray())).ToArray();
        var tablet = new StoneTabletSnapshot(0, false, false, true, false, "", "test", placements[positions[0]].Rotations.ToArray(), placements);
        var contributions = new int[3];
        foreach (var effect in placements[positions[0]].Rotations[0].Effects.Where(effect => effect.ValidCell))
            contributions[effect.X] += effect.LevelParameter;
        int Level(int cell) => kind == TabletEffectKind.IncreaseLevel ? contributions[cell] : 0;
        var items = positions.Select((cell, index) => new InventoryItemSnapshot(index, 7000 + index, 1, cell, cell, 0,
            "Test", "", index == 0 ? "StoneTablet" : "Charm", "Normal", Array.Empty<string>(),
            index == 0 ? InventoryItemKind.StoneTablet : InventoryItemKind.Artifact,
            index == 0 ? null : new ArtifactSnapshot(Level(cell), 2, 0, Level(cell) >= 0 ? Level(cell) : 0,
                Level(cell) >= 0 ? Math.Min(2, Level(cell)) : 0, Level(cell) >= 0, Level(cell) < 0, false, "", true,
                false, false, "Pre", new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                    CriteriaEvaluationState.NotApplicable, CriteriaEvaluationState.NotApplicable),
                Array.Empty<string>(), Array.Empty<string>(), false, null), index == 0 ? tablet : null)).ToArray();
        var cells = Enumerable.Range(0, 3).Select(cell => new InventoryCellSnapshot(cell, cell, 0, Level(cell),
            cell == positions[0] ? -1 : 2, 0, kind == TabletEffectKind.MultiplyLevel ? contributions[cell] : 0, 0, 0, false,
            new InventoryCellSettlementSnapshot(true, 0, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                kind == TabletEffectKind.IncreaseLevel ? contributions[cell] : 0, 0, 0,
                kind == TabletEffectKind.MultiplyLevel ? contributions[cell] : 0))).ToArray();
        return new InventorySnapshot(3, 3, cells, items);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Additive score bound: " + message);
    }
}
