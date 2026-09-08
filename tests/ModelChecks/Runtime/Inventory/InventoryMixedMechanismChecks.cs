using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryMixedMechanismChecks
{
    internal static void Run()
    {
        int exactCases = 0, largeCases = 0, improved = 0, heuristicOptima = 0;
        foreach (int storage in new[] { 6, 30, 32, 42 })
            foreach (int pack in Enumerable.Range(0, 3))
                foreach (int seed in new[] { 17, 83, 419, 2027 })
                {
                    var snapshot = Create(storage, pack, seed);
                    string id = $"mixed/{storage}/{pack}/{seed}";
                    Check(snapshot.SettlementValidation.LayoutProjectionReady, id + ": " +
                        string.Join(";", snapshot.SettlementValidation.Issues));
                    var preferences = seed == 17 || seed == 419 ? InventoryOptimizationPreferences.Default :
                        new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
                            new[] { new ArtifactOptimizationPreference(0, 9003, InventoryPreferenceLevel.Priority, 3, 0) },
                            new[] { new ComboOptimizationPreference("TestPlanet", InventoryPreferenceLevel.Priority,
                        snapshot.Items.Count(item => item.Artifact != null)) });
                    var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
                    var result = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(16, 15000, int.MaxValue));
                    Check(result.Succeeded && result.Improved && result.BestScore.CompareTo(result.CurrentScore) > 0 && result.CandidateEvaluations <= 15000 &&
                        InventoryLayoutPlanner.TryCreate(snapshot, result.Layout, out _, out _), id + ": invalid result or budget");
                    var projected = InventorySettlementProjector.Evaluate(snapshot, result.Layout);
                    Check(projected.Succeeded && new InventoryOptimizationScorer(snapshot, policy).Score(result.Layout, projected)
                        .CompareTo(result.BestScore) == 0, id + ": retained score mismatch");
                    if (result.Improved) improved++;
                    foreach (int cutoff in new[] { 1, 64, 512 })
                    {
                        var bounded = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(16, cutoff, int.MaxValue));
                        Check(bounded.Succeeded && bounded.CandidateEvaluations <= cutoff &&
                            bounded.BestScore.CompareTo(bounded.CurrentScore) >= 0 &&
                            InventoryLayoutPlanner.TryCreate(snapshot, bounded.Layout, out _, out _), id + ": cutoff result");
                    }
                    var fixedPolicy = InventoryOptimizationPolicyResolver.Resolve(snapshot,
                        preferences.WithExecutionSettings(InventorySearchEffort.Balanced, false));
                    var fixedResult = InventoryOptimizer.Solve(snapshot, fixedPolicy, new InventorySearchBudget(16, 512, int.MaxValue));
                    Check(fixedResult.Succeeded && snapshot.Items.Select((item, index) => item.StoneTablet == null ||
                        fixedResult.Layout.GetRotation(index) == item.StoneTablet.Rotation).All(value => value), id + ": forbidden rotation");
                    if (storage == 6)
                    {
                        var exact = InventoryExhaustiveSearchOracle.Solve(snapshot, policy,
                            new InventoryExhaustiveSearchLimits(10000, 10000));
                        var selected = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(16, 15000, int.MaxValue));
                        Check(exact.ProvenOptimal && selected.OptimalityProven && selected.BestScore.CompareTo(exact.BestScore) == 0,
                            id + ": production strategy must use the affordable exact result");
                        if (result.BestScore.CompareTo(exact.BestScore) == 0) heuristicOptima++;
                        else Console.WriteLine("MixedHeuristicGap: " + id);
                        exactCases++;
                    }
                    else largeCases++;
                    Console.WriteLine($"MixedMechanismCase: {id}; improved={result.Improved}; levels={result.BestScore.CappedEffectiveArtifactLevelTotal}; utilization={result.BestScore.PositionEffectUtilizationPoints}; evaluations={result.CandidateEvaluations}");
                }
        Console.WriteLine($"Mixed mechanisms: exact={exactCases}; large={largeCases}; improved={improved}; heuristicOptima={heuristicOptima}/{exactCases}");
    }

    private static InventorySnapshot Create(int storage, int pack, int seed)
    {
        int width = storage == 6 ? 3 : 6;
        int tablets = storage == 6 ? 1 : 3;
        int artifacts = storage == 6 ? 4 : storage - 6;
        int[] positions = Enumerable.Range(0, storage - tablets).ToArray();
        new Random(seed).Shuffle(positions);
        positions = positions.Take(artifacts).ToArray();
        var traits = Enumerable.Range(0, artifacts).Select(i => new InventoryPositionTargetTraits(Key(i),
            planet: pack == 1 && i >= 3 && i % 3 == 0, companion: pack == 1 && i >= 3 && i % 3 == 1, networkReady: true,
            rarity: i % 4, magicArtifact: pack != 1 && i >= 3 && i % 3 == 0)).ToArray();
        var right = new[] { new InventoryOffsetSnapshot(1, 0) };
        var left = new[] { new InventoryOffsetSnapshot(-1, 0) };
        var down = new[] { new InventoryOffsetSnapshot(0, 1) };
        InventoryPositionEffectRule Rule(int source, InventoryPositionEffectKind kind, InventoryOffsetSnapshot[] offsets) =>
            new(Key(source), kind, new[] { 1.0, 2.0, 3.0, 4.0 }, offsets: offsets, targetCategory: "TestPlanet");
        var rules = pack switch
        {
            0 => new[] { Rule(0, InventoryPositionEffectKind.MagicCostReduction, left),
                Rule(1, InventoryPositionEffectKind.MagicCooldownRecovery, right),
                Rule(2, InventoryPositionEffectKind.NeighborArtifactLevelDamage, right.Concat(down).ToArray()) },
            1 => new[] { Rule(0, InventoryPositionEffectKind.SameRowCompanionMode, Array.Empty<InventoryOffsetSnapshot>()),
                Rule(1, InventoryPositionEffectKind.AdjacentPlanetEnhancement, left.Concat(right).ToArray()),
                Rule(2, InventoryPositionEffectKind.NeighborArtifactLevelDamage, right.Concat(down).ToArray()) },
            _ => new[] { Rule(0, InventoryPositionEffectKind.DependencyDamage, down),
                Rule(1, InventoryPositionEffectKind.DependencyDamage, right),
                Rule(2, InventoryPositionEffectKind.MagicCooldownRecovery, right) }
        };
        int[] levels = Enumerable.Range(0, storage).Select(cell => (cell + seed) % 3).ToArray();
        var board = InventoryPositionEffectChecks.Board(width, levels, positions, rules,
            Array.Empty<InventoryPositionEffectValue>(), traits, observationsAvailable: false);
        var stones = Enumerable.Range(0, tablets).Select(t =>
        {
            int origin = storage - 1 - t;
            var placements = Enumerable.Range(0, storage).Select(cell => new TabletPlacementProjectionSnapshot(cell,
                cell % width, cell / width, Enumerable.Range(0, 4).Select(rotation =>
                {
                    var direction = new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }[rotation];
                    TabletAdditionSnapshot Effect(int sign)
                    {
                        int x = cell % width + direction.Item1 * sign, y = cell / width + direction.Item2 * sign;
                        return new(x, y, "synthetic", x >= 0 && x < width && y >= 0 && y * width + x < storage,
                            false, false, false, false, false, false, effectKind: TabletEffectKind.IncreaseLevel,
                            levelParameter: sign > 0 ? 2 : -1);
                    }
                    var criteria = Enumerable.Range(0, width).Select(x => new TabletAdditionSnapshot(x, 0, "PLACED", true,
                        true, true, false, false, false, false, TabletCriteriaKind.Placed)).ToArray();
                    return new TabletRotationProjectionSnapshot(rotation, criteria, new[] { Effect(1), Effect(-1) }, true);
                }).ToArray())).ToArray();
            return new InventoryItemSnapshot(100 + t, 20000 + t, 1, origin, origin % width, origin / width,
                "Synthetic tablet", "", "StoneTablet", "Normal", Array.Empty<string>(), InventoryItemKind.StoneTablet, null,
                new StoneTabletSnapshot(0, true, false, false, false, "synthetic", "synthetic", placementProjections: placements));
        }).ToArray();
        return new InventorySnapshot(width, storage, board.Cells.ToArray(), board.Items.Concat(stones).ToArray(),
            comboCategories: board.ComboCategories.ToArray(), positionEffects: board.PositionEffects);
    }

    private static InventoryItemKey Key(int i) => new(9000 + i, 0);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
