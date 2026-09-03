using System.Diagnostics;
using System.Text.Json;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryKnownSolutionChecks
{
    internal static InventoryKnownSolution[] CreateCases() => (from capacity in new[] { 30, 32, 36, 42 }
                                                               from seed in new[] { 101, 202, 303 }
                                                               select InventoryKnownSolutionFixture.Create(capacity, seed)).ToArray();

    internal static void Run()
    {
        InventoryPriorityTradeoffChecks.Run();
        foreach (InventoryKnownSolution scenario in CreateCases()) Validate(scenario);
        VerifyCompoundImprovementResumesLocalSearch();
        Console.WriteLine("Inventory known solutions: 4 exhaustive priority comparisons, combo maximum/counting checks, 12 planted late-game model fixtures passed");
    }

    private static void VerifyCompoundImprovementResumesLocalSearch()
    {
        var scenario = InventoryKnownSolutionFixture.Create(32, 101);
        var policy = InventoryOptimizationPolicyResolver.Resolve(scenario.Snapshot, scenario.Preferences);
        var result = InventoryOptimizer.Solve(scenario.Snapshot, policy, new InventorySearchBudget(16, 15000, int.MaxValue));
        Require(result.Succeeded && result.TargetEvaluations.All(target => target.AfterConditionReached) &&
            result.BestScore.PositionEffectRegressions == 0 && result.BestScore.CappedEffectiveArtifactLevelTotal >= 129,
            scenario, "compound improvement leaves budget to redistribute levels; previously stopped at 128");
    }

    private static void Validate(InventoryKnownSolution scenario)
    {
        var snapshot = scenario.Snapshot;
        var current = InventoryLayoutProjection.Current(snapshot);
        var initial = InventorySettlementProjector.Evaluate(snapshot, current);
        var witness = InventorySettlementProjector.Evaluate(snapshot, scenario.Witness);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, scenario.Preferences);
        var scorer = new InventoryOptimizationScorer(snapshot, policy);
        var parity = InventorySettlementDifferentialVerifier.Compare(snapshot, current, initial, snapshot);
        Require(snapshot.SettlementValidation.LayoutProjectionReady && parity.Matched, scenario,
            "independent initial settlement: " + string.Join(";", snapshot.SettlementValidation.Issues.Concat(parity.Mismatches)));
        Require(witness.Succeeded && scorer.EvaluateTargets(initial, witness).All(target => target.AfterConditionReached), scenario, "all planted goals");
        Require(witness.ComboCounts["FIRE"] == 10 && witness.ComboCounts["ICE"] == 10 &&
            witness.Artifacts.All(artifact => artifact.Enabled && artifact.CappedEffectiveLevel == 6) &&
            witness.PositionEffects.All(effect => effect.Value == 36), scenario, "independent witness expectations");
        Require(scorer.Score(scenario.Witness, witness).PositionEffectRegressions == 0 &&
            InventoryLayoutPlanner.TryCreate(snapshot, scenario.Witness, out _, out _), scenario, "admissible, applicable witness");
        Require(scorer.EvaluateTargets(initial, initial).Any(target => !target.AfterConditionReached), scenario, "shuffled problem starts unsolved");
        // Serialization must preserve the problem, including the planted answer's validity.
        using JsonDocument document = JsonDocument.Parse(InventoryReproductionJson.Serialize(snapshot));
        var restored = InventoryReproductionReplay.Read<InventorySnapshot>(document.RootElement);
        var restoredWitness = InventorySettlementProjector.Evaluate(restored, scenario.Witness);
        Require(scorer.Score(scenario.Witness, restoredWitness).CompareTo(scorer.Score(scenario.Witness, witness)) == 0, scenario, "reproduction round-trip");
    }

    internal static void Benchmark(string output, bool fixedWork = false)
    {
        InventoryKnownSolution[] cases = CreateCases();
        foreach (var scenario in cases) Validate(scenario);
        // Never mix a new experiment with an existing output file.
        using var writer = new StreamWriter(new FileStream(output, FileMode.CreateNew, FileAccess.Write));
        writer.WriteLine(JsonSerializer.Serialize(new { Event = "priority_comparisons", Cases = InventoryPriorityTradeoffChecks.Run() }));
        var configurations = fixedWork ? new[]
        {
            ("WorkBalanced", new InventorySearchBudget(8, 5000, int.MaxValue)),
            ("WorkThorough", new InventorySearchBudget(16, 15000, int.MaxValue))
        } : new[]
        {
            ("Fast", InventorySearchBudget.ForEffort(InventorySearchEffort.Fast)),
            ("Balanced", InventorySearchBudget.ForEffort(InventorySearchEffort.Balanced)),
            ("Thorough", InventorySearchBudget.ForEffort(InventorySearchEffort.Thorough)),
            ("Time200", new InventorySearchBudget(64, 100000, 200)),
            ("Time1000", new InventorySearchBudget(64, 100000, 1000))
        };
        foreach (var scenario in cases)
            InventoryOptimizer.Solve(scenario.Snapshot, InventoryOptimizationPolicyResolver.Resolve(scenario.Snapshot, scenario.Preferences), new InventorySearchBudget(2, 500, 10000));
        var jobs = (from scenario in cases
                    from configuration in configurations
                    from repeat in Enumerable.Range(0, 3)
                    select (scenario, configuration, repeat)).ToArray();
        new Random(4711).Shuffle(jobs);
        foreach (var (scenario, configuration, repeat) in jobs)
        {
            var policy = InventoryOptimizationPolicyResolver.Resolve(scenario.Snapshot, scenario.Preferences);
            var scorer = new InventoryOptimizationScorer(scenario.Snapshot, policy);
            var initial = InventorySettlementProjector.Evaluate(scenario.Snapshot, InventoryLayoutProjection.Current(scenario.Snapshot));
            var witness = InventorySettlementProjector.Evaluate(scenario.Snapshot, scenario.Witness);
            long allocated = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            var result = InventoryOptimizer.Solve(scenario.Snapshot, policy, configuration.Item2);
            timer.Stop();
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            Require(result.Succeeded && result.BestScore.CompareTo(result.CurrentScore) >= 0 && result.BestScore.PositionEffectRegressions == 0 &&
                InventoryLayoutPlanner.TryCreate(scenario.Snapshot, result.Layout, out _, out _), scenario, "search validity");
            var reached = result.TargetEvaluations;
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                Event = "known_solution_search",
                scenario.Id,
                scenario.Seed,
                Configuration = configuration.Item1,
                Repeat = repeat,
                BudgetRounds = configuration.Item2.MaximumImprovementRounds,
                BudgetEvaluations = configuration.Item2.MaximumCandidateEvaluations,
                BudgetMilliseconds = configuration.Item2.MaximumElapsedMilliseconds,
                WallMs = timer.Elapsed.TotalMilliseconds,
                result.CandidateEvaluations,
                Termination = result.TerminationReason.ToString(),
                AllocatedBytes = allocated,
                ArtifactGoalsReached = reached.Count(t => t.Kind == InventoryOptimizationTargetKind.Artifact && t.AfterConditionReached),
                ComboGoalsReached = reached.Count(t => t.Kind == InventoryOptimizationTargetKind.ComboCategory && t.AfterConditionReached),
                AllGoalsReached = reached.All(t => t.AfterConditionReached),
                ComparisonToWitness = result.BestScore.CompareTo(scorer.Score(scenario.Witness, witness)),
                WitnessScore = InventoryReproductionJson.Serialize(scorer.Score(scenario.Witness, witness)),
                Score = InventoryReproductionJson.Serialize(result.BestScore),
                Targets = reached.Select(t => new { t.Target, t.RequiredValue, t.AfterValue, t.AfterConditionReached }),
                Cells = result.Layout.CopyCells(),
                Rotations = result.Layout.CopyRotations()
            }));
            writer.Flush();
        }
        Console.WriteLine("Inventory known-solution benchmark: " + jobs.Length + " searches recorded");
    }

    private static void Require(bool value, InventoryKnownSolution scenario, string message)
    {
        if (!value) throw new InvalidOperationException(scenario.Id + "/" + scenario.Seed + ": " + message);
    }
}
