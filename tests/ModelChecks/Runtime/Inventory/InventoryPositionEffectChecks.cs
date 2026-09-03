using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryPositionEffectChecks
{
    internal static string Run()
    {
        NeighborDamageAndOptimization();
        AdjacentTargets();
        CompanionRow();
        SlotDamage();
        HalfBoardModes();
        DependencyChain();
        DependencyTraversalAcrossWords();
        InvalidAndChangedParameters();
        ClientProjectionWithoutObservations();
        MixedSearchBudget();
        return "9 effect kinds; runtime parameters; native-state mismatch; client prediction without private observations; benefit preservation; inactive targets; dependency chains and cycles passed";
    }

    private static void NeighborDamageAndOptimization()
    {
        var kind = InventoryPositionEffectKind.NeighborArtifactLevelDamage;
        var rule = new InventoryPositionEffectRule(Key(0), kind, new[] { 2.5, 4.0, 8.0 },
            offsets: new[] { new InventoryOffsetSnapshot(1, 0), new InventoryOffsetSnapshot(0, 1) });
        int[] levels = new int[12];
        levels[1] = levels[6] = 1;
        levels[9] = 2;
        var snapshot = Board(6, levels, new[] { 0, 1, 6 }, new[] { rule }, new[] { Value(0, kind, 5) });
        RequireReady(snapshot);
        var current = InventoryLayoutProjection.Current(snapshot);
        var moved = new InventoryLayoutProjection(new[] { 9, 1, 6 }, new int[3]);
        var before = InventorySettlementProjector.Evaluate(snapshot, current);
        var after = InventorySettlementProjector.Evaluate(snapshot, moved);
        Equal(0, after.PositionEffects.Single().Value, "isolated source loses neighbor damage");
        var scorer = Scorer(snapshot);
        var beforeScore = scorer.Score(current, before);
        var afterScore = scorer.Score(moved, after);
        Check(afterScore.CappedEffectiveArtifactLevelTotal > beforeScore.CappedEffectiveArtifactLevelTotal,
            "counterexample must improve aggregate levels");
        Check(afterScore.PositionEffectRegressions == 1 && afterScore.CompareTo(beforeScore) < 0,
            "aggregate levels must not outweigh lost position damage");
        var optimized = InventoryOptimizer.Solve(snapshot,
            InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default),
            new InventorySearchBudget(8, 1500, 5000));
        Check(optimized.Succeeded && Scorer(snapshot).Score(optimized.Layout,
                InventorySettlementProjector.Evaluate(snapshot, optimized.Layout)).PositionEffectRegressions == 0,
            "actual bounded search must preserve existing position benefits");
        var avoid = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            new[] { new ArtifactOptimizationPreference(0, Key(0).EntityId, InventoryPreferenceLevel.Avoid) },
            Array.Empty<ComboOptimizationPreference>());
        var avoidScorer = new InventoryOptimizationScorer(snapshot, InventoryOptimizationPolicyResolver.Resolve(snapshot, avoid));
        Check(avoidScorer.Score(moved, after).PositionEffectRegressions == 0,
            "explicit source exclusion waives that source's benefit preservation");
        var outcome = InventoryOptimizationOutcomeBuilder.Build(snapshot, before, after, beforeScore, afterScore);
        Equal(5, outcome.BeforePositionEffects.Single().Value, "outcome keeps prior damage");
        Equal(0, outcome.AfterPositionEffects.Single().Value, "outcome keeps resulting damage");
        var wrongNativeState = Board(6, levels, new[] { 9, 1, 6 }, new[] { rule }, new[] { Value(0, kind, 5) });
        Check(!InventorySettlementDifferentialVerifier.Compare(snapshot, moved, after, wrongNativeState).Matched,
            "native effect mismatch must fail even if positions and levels match");

        var negative = Board(2, new[] { 0, -1 }, new[] { 0, 1 }, new[] { rule }, new[] { Value(0, kind, -3) });
        RequireReady(negative);
        Equal(-3, Project(negative, 0, 1).Single().Value, "inactive negative displayed level and float floor");
    }

    private static void AdjacentTargets()
    {
        foreach (var kind in new[] { InventoryPositionEffectKind.MagicCostReduction,
                     InventoryPositionEffectKind.MagicCooldownRecovery })
        {
            int offset = kind == InventoryPositionEffectKind.MagicCostReduction ? -1 : 1;
            int target = 1 + offset;
            var rule = new InventoryPositionEffectRule(Key(0), kind, new[] { 3.0, 7.0 },
                offsets: new[] { new InventoryOffsetSnapshot(offset, 0) });
            int[] levels = { -1, 1, -1, 0, 0, 0 };
            var snapshot = Board(3, levels, new[] { 1, target }, new[] { rule }, new[] { Value(0, kind, 7, 1) });
            RequireReady(snapshot);
            Equal(7, Project(snapshot, 1, target).Single().Value, "inactive magic artifact still receives modifier");
            Check(Project(snapshot, 1, 4).Length == 0, "moved magic target no longer receives modifier");
        }
        var planetKind = InventoryPositionEffectKind.AdjacentPlanetEnhancement;
        var planetRule = new InventoryPositionEffectRule(Key(0), planetKind,
            offsets: new[] { new InventoryOffsetSnapshot(1, 0) }, targetCategory: "TestPlanet");
        var planet = Board(2, new[] { 0, -1, 0, 0 }, new[] { 0, 1 }, new[] { planetRule },
            new[] { Value(0, planetKind, 1, 1) });
        RequireReady(planet);
        Check(Project(planet, 1, 2).Length == 0, "adjacency must not wrap across row boundary");
        var changedCategory = new InventoryPositionEffectRule(Key(0), planetKind,
            offsets: planetRule.Offsets.ToArray(), targetCategory: "AnotherPlanet");
        var excluded = Board(2, new[] { 0, 0 }, new[] { 0, 1 }, new[] { changedCategory }, Array.Empty<InventoryPositionEffectValue>());
        RequireReady(excluded);
        var negativeRule = new InventoryPositionEffectRule(Key(0), InventoryPositionEffectKind.MagicCostReduction,
            new[] { -3.0 }, offsets: new[] { new InventoryOffsetSnapshot(1, 0) });
        var negative = Board(2, new int[4], new[] { 0, 3 }, new[] { negativeRule }, Array.Empty<InventoryPositionEffectValue>());
        RequireReady(negative);
        var negativeLayout = new InventoryLayoutProjection(new[] { 0, 1 }, new int[2]);
        Check(Scorer(negative).Score(negativeLayout,
            InventorySettlementProjector.Evaluate(negative, negativeLayout)).PositionEffectRegressions == 1,
            "new negative effects must be compared against an absent effect's zero baseline");
    }

    private static void CompanionRow()
    {
        var kind = InventoryPositionEffectKind.SameRowCompanionMode;
        var rule = new InventoryPositionEffectRule(Key(0), kind);
        var snapshot = Board(4, new int[8], new[] { 0, 1, 3, 7 }, new[] { rule },
            new[] { Value(0, kind, 1, 1), Value(0, kind, 1, 2) });
        RequireReady(snapshot);
        var moved = Project(snapshot, 4, 1, 3, 7);
        Check(moved.Length == 1 && moved[0].Key.Target == Key(3), "row mode follows row and distinct entity identities");
        var ambiguous = Board(4, new int[8], new[] { 0, 1 },
            new[] { rule, new InventoryPositionEffectRule(Key(1), kind) }, Array.Empty<InventoryPositionEffectValue>());
        Check(ambiguous.SettlementValidation.Issues.Contains("PositionEffectCompanionRefreshOrderUnavailable"),
            "multiple boolean writers require refresh-order model");
    }

    private static void SlotDamage()
    {
        var kind = InventoryPositionEffectKind.FirstSlotsElementDamage;
        var rule = new InventoryPositionEffectRule(Key(0), kind, new[] { 4.0, 6.0, 8.0, 10.0 },
            boundary: 3, channels: new[] { "A", "B", "C" });
        var snapshot = Board(6, new int[12], new[] { 7, 0, 2, 5 }, new[] { rule },
            new[] { Value(0, kind, 8, channel: "A"), Value(0, kind, 8, channel: "B"), Value(0, kind, 8, channel: "C") });
        RequireReady(snapshot);
        Check(Project(snapshot, 7, 0, 6, 5).All(value => value.Value == 4),
            "slot count follows captured boundary rather than inventory width or source row");
    }

    private static void HalfBoardModes()
    {
        foreach (var kind in new[] { InventoryPositionEffectKind.HalfBoardStats,
                     InventoryPositionEffectKind.HalfBoardWeaponMode })
        {
            bool stats = kind == InventoryPositionEffectKind.HalfBoardStats;
            var rule = new InventoryPositionEffectRule(Key(0), kind,
                new[] { 3.0, 9.0 }, new[] { -1.0, -4.0 }, boundary: 1, channels: new[] { "A", "B" });
            int[] levels = { 0, 1, 0, 1, 0, -1 };
            var snapshot = Board(3, levels, new[] { 1 }, new[] { rule }, new[] {
                Value(0, kind, stats ? 9 : 1, channel: "A"), Value(0, kind, stats ? -4 : 0, channel: "B"),
                Value(0, kind, 0, channel: "Mode", mode: true) });
            RequireReady(snapshot);
            var right = Project(snapshot, 2);
            Equal(stats ? -1 : 0, right.Single(value => value.Key.Channel == "A").Value, "right-side channel A");
            Equal(stats ? 3 : 1, right.Single(value => value.Key.Channel == "B").Value, "right-side channel B");
            Equal(1, right.Single(value => value.Mode).Value, "right mode");
            var inactive = Project(snapshot, 5);
            Equal(-1, inactive.Single(value => value.Mode).Value, "inactive mode");
            Check(inactive.Where(value => !value.Mode).All(value => value.Value == 0), "inactive stat removal");
            var layout = new InventoryLayoutProjection(new[] { 2 }, new int[1]);
            Check(Scorer(snapshot).Score(layout, InventorySettlementProjector.Evaluate(snapshot, layout)).PositionEffectRegressions > 0,
                "half-board mode changes must be visible to scorer");
        }
    }

    private static void DependencyChain()
    {
        var kind = InventoryPositionEffectKind.DependencyDamage;
        var rules = new[] {
            new InventoryPositionEffectRule(Key(0), kind, new[] { 2.0, 7.0 }, new[] { 11.0, 13.0 },
                new[] { new InventoryOffsetSnapshot(0, -1) }, conditionalDamage: true, maximumRarity: 2),
            new InventoryPositionEffectRule(Key(1), kind, new[] { 3.0, 5.0 },
                offsets: new[] { new InventoryOffsetSnapshot(0, -1) }) };
        int[] levels = { 0, 0, 0, 1, 0, 0, -1, 0, 0 };
        var snapshot = Board(3, levels, new[] { 6, 3, 0 }, rules,
            new[] { Value(0, kind, 13, 2), Value(1, kind, 5, 2) });
        RequireReady(snapshot);
        var values = Project(snapshot, 6, 3, 0);
        Check(values.Length == 2 && values.All(value => value.Key.Target == Key(2)),
            "chain reaches attackable root, not intermediate dependency artifacts");
        Equal(13, values.Single(value => value.Key.Source == Key(0)).Value,
            "inactive source uses level zero plus captured rarity bonus");
        var unreadyTraits = Traits(3);
        unreadyTraits[1] = new InventoryPositionTargetTraits(Key(1), true, true, false, 1, true);
        var unready = Board(3, levels, new[] { 6, 3, 0 }, rules, Array.Empty<InventoryPositionEffectValue>(), unreadyTraits);
        RequireReady(unready);
        Check(Project(unready, 6, 3, 0).Length == 0, "unready intermediate stops dependency traversal");
        var cycleRules = new[] { rules[0], new InventoryPositionEffectRule(Key(1), kind, new[] { 3.0, 5.0 },
            offsets: new[] { new InventoryOffsetSnapshot(0, 1) }) };
        var cycle = Board(3, levels, new[] { 6, 3, 0 }, cycleRules, Array.Empty<InventoryPositionEffectValue>());
        RequireReady(cycle);
        Check(Project(cycle, 6, 3, 0).Length == 0, "dependency cycle terminates without a root");
    }

    private static void DependencyTraversalAcrossWords()
    {
        var kind = InventoryPositionEffectKind.DependencyDamage;
        int[] positions = Enumerable.Range(0, 66).Select(index => index + 2).ToArray();
        positions[0] = 0;
        positions[64] = 1;
        positions[65] = 2;
        var rules = new[] {
            new InventoryPositionEffectRule(Key(0), kind, new[] { 2.0 },
                offsets: new[] { new InventoryOffsetSnapshot(1, 0) }),
            new InventoryPositionEffectRule(Key(64), kind, new[] { 3.0 },
                offsets: new[] { new InventoryOffsetSnapshot(1, 0) }) };
        var snapshot = Board(6, new int[66], positions, rules,
            new[] { Value(0, kind, 2, 65), Value(64, kind, 3, 65) });
        RequireReady(snapshot);
        var effects = Project(snapshot, positions);
        Check(effects.Length == 2 && effects.All(effect => effect.Key.Target == Key(65)),
            "dependency traversal distinguishes items with the same bit in different words");
        rules[1] = new InventoryPositionEffectRule(Key(64), kind, new[] { 3.0 },
            offsets: new[] { new InventoryOffsetSnapshot(-1, 0) });
        var cycle = Board(6, new int[66], positions, rules, Array.Empty<InventoryPositionEffectValue>());
        RequireReady(cycle);
        Check(Project(cycle, positions).Length == 0, "dependency cycles terminate across word boundaries");
    }

    private static void InvalidAndChangedParameters()
    {
        var kind = InventoryPositionEffectKind.MagicCostReduction;
        double[] curve = { 17, 29 };
        var rule = new InventoryPositionEffectRule(Key(0), kind, curve,
            offsets: new[] { new InventoryOffsetSnapshot(1, 0) });
        curve[0] = 99;
        Equal(17, rule.ValuesByLevel[0], "runtime curve must be copied before worker search");
        var snapshot = Board(2, new int[2], new[] { 0, 1 }, new[] { rule }, new[] { Value(0, kind, 17, 1) });
        RequireReady(snapshot);
        var changed = new InventoryPositionEffectRule(Key(0), kind, new[] { 17.0, 30.0 }, offsets: rule.Offsets.ToArray());
        var current = Board(2, new int[2], new[] { 0, 1 }, new[] { changed }, new[] { Value(0, kind, 17, 1) });
        RequireReady(current);
        Check(!InventoryPositionEffectComparison.ParametersMatch(snapshot.PositionEffects, current.PositionEffects),
            "unobserved curve entries changing must invalidate in-flight plans");
        foreach (var invalid in new[] {
                     new InventoryPositionEffectRule(Key(0), kind),
                     new InventoryPositionEffectRule(Key(0), (InventoryPositionEffectKind)999),
                     new InventoryPositionEffectRule(Key(0), kind, new[] { double.NaN }) })
        {
            var unavailable = Board(2, new int[2], new[] { 0, 1 }, new[] { invalid }, Array.Empty<InventoryPositionEffectValue>());
            Check(unavailable.SettlementValidation.HasPositionEffectIssue && !unavailable.SettlementValidation.LayoutProjectionReady,
                "missing or changed parameters must stop projection");
        }
        var failure = Board(2, new int[2], new[] { 0, 1 }, Array.Empty<InventoryPositionEffectRule>(),
            Array.Empty<InventoryPositionEffectValue>(), issues: new[] { "PositionEffectCaptureUnavailable:MissingFieldException" });
        Check(!failure.SettlementValidation.LayoutProjectionReady, "capture failure cannot become empty supported model");
    }

    private static void ClientProjectionWithoutObservations()
    {
        var kind = InventoryPositionEffectKind.NeighborArtifactLevelDamage;
        var rule = new InventoryPositionEffectRule(Key(0), kind, new[] { 2.0, 4.0 },
            offsets: new[] { new InventoryOffsetSnapshot(1, 0) });
        foreach (bool enabled in new[] { false, true })
        {
            int[] levels = { enabled ? 1 : -1, 1, 0, 0 };
            var snapshot = Board(2, levels, new[] { 0, 1 }, new[] { rule },
                Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
            RequireReady(snapshot);
            var current = InventoryLayoutProjection.Current(snapshot);
            var expected = InventorySettlementProjector.Evaluate(snapshot, current);
            Equal(enabled ? 4 : 0, expected.PositionEffects.Single().Value,
                "client effects are predicted rather than treated as observed zeros");
            Check(Scorer(snapshot).Score(current, expected).PositionEffectRegressions == 0,
                "client baseline preserves existing benefits");
            Check(InventorySettlementDifferentialVerifier.Compare(snapshot, current, expected, snapshot).Matched,
                "matching native layout and synchronized state can confirm client settlement");
            var moved = new InventoryLayoutProjection(new[] { 2, 1 }, new int[2]);
            var after = InventorySettlementProjector.Evaluate(snapshot, moved);
            var actual = Board(2, levels, new[] { 2, 1 }, new[] { rule },
                Array.Empty<InventoryPositionEffectValue>(), observationsAvailable: false);
            Check(InventorySettlementDifferentialVerifier.Compare(snapshot, moved, after, actual).Matched,
                "client move can be confirmed without private effect caches");
            Check(!InventorySettlementDifferentialVerifier.Compare(snapshot, moved, after, snapshot).Matched,
                "missing native movement still fails confirmation");
            if (enabled)
            {
                Check(Scorer(snapshot).Score(moved, after).PositionEffectRegressions == 1,
                    "client scoring must penalize losing an existing position benefit");
                var optimized = InventoryOptimizer.Solve(snapshot,
                    InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default),
                    new InventorySearchBudget(8, 1500, 5000));
                Check(optimized.Succeeded && Scorer(snapshot).Score(optimized.Layout,
                        InventorySettlementProjector.Evaluate(snapshot, optimized.Layout)).PositionEffectRegressions == 0,
                    "client search completes while preserving position benefits");
            }
        }
        var invalid = Board(2, new int[2], new[] { 0, 1 }, new[] { rule }, new[] { Value(0, kind, 0) },
            observationsAvailable: false);
        Check(!invalid.SettlementValidation.LayoutProjectionReady,
            "a client snapshot must not claim private cache observations");
    }

    private static void MixedSearchBudget()
    {
        var neighbor = InventoryPositionEffectKind.NeighborArtifactLevelDamage;
        var magic = InventoryPositionEffectKind.MagicCostReduction;
        var slots = InventoryPositionEffectKind.FirstSlotsElementDamage;
        var rules = new[] {
            new InventoryPositionEffectRule(Key(0), neighbor, new[] { 2.0, 4.0 },
                offsets: new[] { new InventoryOffsetSnapshot(1, 0) }),
            new InventoryPositionEffectRule(Key(1), magic, new[] { 3.0, 8.0 },
                offsets: new[] { new InventoryOffsetSnapshot(-1, 0) }),
            new InventoryPositionEffectRule(Key(2), slots, new[] { 3.0, 6.0, 9.0, 12.0 },
                boundary: 3, channels: new[] { "A" }) };
        var traits = Traits(6);
        traits[0] = new InventoryPositionTargetTraits(Key(0), false, false, true, 0, true);
        var snapshot = Board(6, Enumerable.Repeat(1, 30).ToArray(), new[] { 0, 1, 6, 2, 7, 8 }, rules,
            new[] { Value(0, neighbor, 4), Value(1, magic, 8, 0), Value(2, slots, 18, channel: "A") }, traits);
        RequireReady(snapshot);
        long start = GC.GetAllocatedBytesForCurrentThread();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var result = InventoryOptimizer.Solve(snapshot,
            InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default),
            new InventorySearchBudget(8, 1500, 5000));
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Check(result.Succeeded && result.CandidateEvaluations <= 1500 && bytes < 32L * 1024 * 1024,
            "mixed position effects must respect search and allocation budgets");
        Console.WriteLine("InventoryPositionEffectSearch: " + result.CandidateEvaluations + " evaluations; " +
            clock.ElapsedMilliseconds + " ms; " + bytes + " bytes");
    }

    private static InventoryItemKey Key(int index) => new(9000 + index, 0);
    private static InventoryPositionEffectValue Value(int source, InventoryPositionEffectKind kind,
        double amount, int? target = null, string channel = "", bool mode = false) =>
        new(new InventoryPositionEffectKey(Key(source), kind, target.HasValue ? Key(target.Value) : null, channel), amount, mode);
    private static InventoryPositionTargetTraits[] Traits(int count) => Enumerable.Range(0, count)
        .Select(index => new InventoryPositionTargetTraits(Key(index), index > 0, index > 0, true, index, index > 0)).ToArray();

    private static InventorySnapshot Board(int width, int[] levels, int[] positions,
        InventoryPositionEffectRule[] rules, InventoryPositionEffectValue[] observed,
        InventoryPositionTargetTraits[]? traits = null, string[]? issues = null, bool observationsAvailable = true)
    {
        var items = positions.Select((cell, index) =>
        {
            bool enabled = levels[cell] >= 0;
            var artifact = new ArtifactSnapshot(levels[cell], 3, 0, levels[cell], enabled ? Math.Min(3, levels[cell]) : 0,
                enabled, !enabled, false, "", true, false, false, "Default",
                new CriteriaSnapshot(ArtifactActivationConditionKind.None, CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable), Array.Empty<string>(), Array.Empty<string>(), true, null);
            return new InventoryItemSnapshot(0, Key(index).EntityId, 1, cell, cell % width, cell / width,
                "Test", "", "Charm", "Normal", new[] { "TestPlanet" }, InventoryItemKind.Artifact, artifact, null);
        }).ToArray();
        var cells = levels.Select((level, cell) => new InventoryCellSnapshot(cell, cell % width, cell / width,
            level, positions.Contains(cell) ? 3 : -1, 0, 0, 0, 0, false,
            new InventoryCellSettlementSnapshot(true, level, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        return new InventorySnapshot(width, levels.Length, cells, items,
            comboCategories: new[] { new ComboCategorySnapshot("TestPlanet", items.Length, items.Length,
                items.Length, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) },
            positionEffects: new InventoryPositionEffectsSnapshot(rules, traits ?? Traits(items.Length), observed, issues,
                observationsAvailable));
    }

    private static InventoryPositionEffectValue[] Project(InventorySnapshot snapshot, params int[] cells)
    {
        var layout = new InventoryLayoutProjection(cells, new int[cells.Length]);
        var result = InventorySettlementProjector.Evaluate(snapshot, layout);
        Check(result.Succeeded, "candidate projection failed");
        var workspace = new InventorySettlementProjectionWorkspace(snapshot);
        var current = InventoryLayoutProjection.Current(snapshot);
        var initial = InventorySettlementProjector.EvaluateForScoring(snapshot, current, workspace);
        var candidate = InventorySettlementProjector.EvaluateForScoring(snapshot, layout, workspace);
        Check(Effects(candidate).SequenceEqual(Effects(result)), "reused workspace matches full effect projection");
        var restored = InventorySettlementProjector.EvaluateForScoring(snapshot, current, workspace);
        Check(Effects(initial).SequenceEqual(Effects(restored)), "workspace does not retain candidate targets");
        Check(Effects(candidate).SequenceEqual(Effects(result)), "later projection does not mutate retained results");
        var client = WithoutObservations(snapshot);
        RequireReady(client);
        var clientResult = InventorySettlementProjector.Evaluate(client, layout);
        Check(Effects(clientResult).SequenceEqual(Effects(result)), "client and host predict the same position effects");
        Check(Scorer(client).Score(layout, clientResult).CompareTo(Scorer(snapshot).Score(layout, result)) == 0,
            "client and host scoring preserve the same position benefits");
        return result.PositionEffects.ToArray();

        static IEnumerable<(InventoryPositionEffectKey, double, bool)> Effects(ProjectedInventorySettlement settlement) =>
            settlement.PositionEffects.Select(effect => (effect.Key, effect.Value, effect.Mode));
    }
    private static InventorySnapshot WithoutObservations(InventorySnapshot snapshot) =>
        new(snapshot.Width, snapshot.Storage, snapshot.Cells.ToArray(), snapshot.Items.ToArray(),
            comboCategories: snapshot.ComboCategories.ToArray(),
            positionEffects: new InventoryPositionEffectsSnapshot(snapshot.PositionEffects.Rules.ToArray(),
                snapshot.PositionEffects.Traits.ToArray(), null, snapshot.PositionEffects.Issues.ToArray(),
                observationsAvailable: false));
    private static InventoryOptimizationScorer Scorer(InventorySnapshot snapshot) => new(snapshot,
        InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default));
    private static void RequireReady(InventorySnapshot snapshot) => Check(snapshot.SettlementValidation.LayoutProjectionReady,
        "fixture verification failed: " + string.Join(",", snapshot.SettlementValidation.Issues));
    private static void Equal(double expected, double actual, string message) => Check(expected == actual, message + ": " + actual);
    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }
}
