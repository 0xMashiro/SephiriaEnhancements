using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventoryRowCategoryStatChecks
{
    internal static readonly InventoryItemKey Source = new(301, 31);
    private const InventoryPositionEffectKind Kind = InventoryPositionEffectKind.RowCategoryStats;

    internal static void Run()
    {
        RowAndActivationProjection();
        PreserveStatChannelAndRespectAvoid();
        RejectIncompleteOrStaleState();
        ClientRowChangesAndSynchronization();
        Console.WriteLine("InventoryRowCategoryStats: RowAndActivationProjection; PreserveStatChannelAndRespectAvoid; " +
            "RejectIncompleteOrStaleState; ClientRowChangesAndSynchronization passed");
    }

    private static void RowAndActivationProjection()
    {
        foreach (int width in new[] { 2, 3, 5 })
        {
            int[] levels = new int[width * 4];
            levels[width] = 1;
            levels[width * 2] = 8;
            levels[width * 3] = -1;
            var board = Board(width, levels);
            Ready(board);
            AssertProjection(board, width - 1, "A", "PowerA", 7);
            AssertProjection(board, width, "B", "PowerB", 11);
            AssertProjection(board, width * 2, "A", "PowerA", 19);
            AssertProjection(board, width * 3, "B", "PowerB", 0);
        }
        var repeated = Board(2, new int[6], categories: new[] { "A", "B", "A" },
            channels: new[] { "PowerA", "PowerB", "PowerA" });
        Ready(repeated);
        AssertProjection(repeated, 4, "A", "PowerA", 7);
    }

    private static void PreserveStatChannelAndRespectAvoid()
    {
        var board = Board(2, new[] { 0, 0, 2, 2 });
        var current = InventoryLayoutProjection.Current(board);
        var moved = new InventoryLayoutProjection(new[] { 2 }, new int[1]);
        var before = InventorySettlementProjector.Evaluate(board, current);
        var after = InventorySettlementProjector.Evaluate(board, moved);
        var scorer = new InventoryOptimizationScorer(board,
            InventoryOptimizationPolicyResolver.Resolve(board, InventoryOptimizationPreferences.Default));
        var baseline = scorer.Score(current, before);
        var candidate = scorer.Score(moved, after);
        Check(candidate.CappedEffectiveArtifactLevelTotal > baseline.CappedEffectiveArtifactLevelTotal,
            "counterexample must gain levels");
        Check(candidate.PositionEffectRegressions == 1 && candidate.CompareTo(baseline) < 0,
            "higher levels and more PowerB must not silently outweigh loss of PowerA");
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            new[] { new ArtifactOptimizationPreference(Source.NativeInstanceId, Source.EntityId, InventoryPreferenceLevel.Avoid) },
            Array.Empty<ComboOptimizationPreference>());
        var excludedScorer = new InventoryOptimizationScorer(board,
            InventoryOptimizationPolicyResolver.Resolve(board, preferences));
        Check(excludedScorer.Score(moved, after).PositionEffectRegressions == 0,
            "explicit exclusion must waive source contribution preservation");
        var staleActual = Board(2, new[] { 0, 0, 2, 2 }, origin: 2, observedValue: 7);
        Check(!InventorySettlementDifferentialVerifier.Compare(board, moved, after, staleActual).Matched,
            "matching layout and levels must not hide incorrect native stat contribution");
    }

    private static void RejectIncompleteOrStaleState()
    {
        foreach (var board in new[]
        {
            Board(2, new int[4], channels: new[] { "PowerA" }),
            Board(2, new int[4], channels: new[] { "PowerA", "" }),
            Board(2, new int[4], values: Array.Empty<double>()),
            Board(2, new int[4], observedValue: 8)
        })
            Check(!board.SettlementValidation.LayoutProjectionReady && board.SettlementValidation.HasPositionEffectIssue,
                "invalid row cycle, curve or observed contribution must reject projection");
    }

    private static void ClientRowChangesAndSynchronization()
    {
        foreach (int level in new[] { -1, 0, 2 })
        {
            var levels = Enumerable.Repeat(level, 4).ToArray();
            var client = Board(2, levels, observationsAvailable: false);
            Ready(client);
            Check(client.PositionEffects.Observed.Count == 0 && client.PositionEffects.Rules.Count == 1,
                "client retains row rules without inventing observed damage");
            var moved = new InventoryLayoutProjection(new[] { 2 }, new int[1]);
            var expected = InventorySettlementProjector.Evaluate(client, moved);
            var actual = Board(2, levels, origin: 2, observationsAvailable: false);
            Ready(actual);
            Check(InventorySettlementDifferentialVerifier.Compare(client, moved, expected, actual).Matched,
                "row changes confirm from synchronized categories, positions and levels");
            var stale = Board(2, levels, origin: 2, observationsAvailable: false, effectiveCategory: "A");
            Check(!stale.SettlementValidation.CurrentLayoutVerified &&
                stale.SettlementValidation.Issues.Any(issue => issue.StartsWith("PositionEffectRowCategoryMismatch:")),
                "stale category must wait for native synchronization, including inactive sources");
            Check(!InventorySettlementDifferentialVerifier.Compare(client, moved, expected, stale).Matched,
                "matching item positions alone cannot confirm a row category change");
            var host = Board(2, levels);
            var hostExpected = InventorySettlementProjector.Evaluate(host, moved);
            var policy = InventoryOptimizationPolicyResolver.Resolve(client, InventoryOptimizationPreferences.Default);
            Check(new InventoryOptimizationScorer(client, policy).Score(moved, expected).CompareTo(
                    new InventoryOptimizationScorer(host, policy).Score(moved, hostExpected)) == 0,
                "row benefit preservation is identical on client and host");
        }
        var invalid = Board(2, new int[4], channels: new[] { "PowerA" }, observationsAvailable: false);
        Check(!invalid.SettlementValidation.LayoutProjectionReady, "clients still reject incomplete effect models");
    }

    internal static InventorySnapshot Board(int width, int[] levels, int origin = 0,
        string[]? categories = null, string[]? channels = null, double[]? values = null, double? observedValue = null,
        bool observationsAvailable = true, string? effectiveCategory = null)
    {
        categories ??= new[] { "A", "B" };
        channels ??= new[] { "PowerA", "PowerB" };
        values ??= new[] { 7.0, 11.0, 19.0 };
        int level = levels[origin];
        bool enabled = level >= 0;
        string category = categories[origin / width % categories.Length];
        var artifact = new ArtifactSnapshot(level, 2, 0, level, enabled ? Math.Min(2, level) : 0,
            enabled, !enabled, false, "", true, false, false, "Pre",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None, CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable), new[] { effectiveCategory ?? category }, categories.Distinct().ToArray(), true, null,
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo, categories));
        var item = new InventoryItemSnapshot(Source.NativeInstanceId, Source.EntityId, 1, origin, origin % width, origin / width,
            "Synthetic row artifact", "", "Charm", "Normal", Array.Empty<string>(), InventoryItemKind.Artifact, artifact, null);
        var cells = levels.Select((value, cell) => new InventoryCellSnapshot(cell, cell % width, cell / width,
            value, cell == origin ? 2 : -1, 0, 0, 0, 0, false,
            new InventoryCellSettlementSnapshot(true, value, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        // Independent fixture expectation; do not ask the projector to generate its own baseline.
        double observed = observedValue ?? (enabled ? new[] { 7.0, 11.0, 19.0 }[Math.Min(2, level)] : 0);
        return new InventorySnapshot(width, levels.Length, cells, new[] { item },
            comboCategories: categories.Distinct().Select(value => new ComboCategorySnapshot(value,
                value == category ? 1 : 0, value == category ? 1 : 0, value == category ? 1 : 0, 0, 0,
                Array.Empty<int>(), Array.Empty<int>(), false)).ToArray(),
            positionEffects: new InventoryPositionEffectsSnapshot(
                new[] { new InventoryPositionEffectRule(Source, Kind, values, channels: channels) },
                new[] { new InventoryPositionTargetTraits(Source, false, false, true, 0, false) },
                observationsAvailable ? new[] { new InventoryPositionEffectValue(new InventoryPositionEffectKey(Source, Kind, null,
                    channels[origin / width % channels.Length]), observed, false) } : Array.Empty<InventoryPositionEffectValue>(),
                Array.Empty<string>(), observationsAvailable));
    }

    internal static void AssertProjection(InventorySnapshot board, int cell, string category, string channel, double amount)
    {
        var result = InventorySettlementProjector.Evaluate(board, new InventoryLayoutProjection(new[] { cell }, new int[1]));
        Check(result.Succeeded, "row projection failed: " + string.Join(",", board.SettlementValidation.Issues));
        Check(result.ComboCounts.Single(pair => pair.Key == category).Value == 1 &&
            result.ComboCounts.Where(pair => pair.Key != category).All(pair => pair.Value == 0),
            "row category must follow position even when inactive");
        var contribution = result.PositionEffects.Single();
        Check(contribution.Key.Channel == channel && contribution.Value == amount,
            $"expected {channel}={amount}, actual {contribution.Key.Channel}={contribution.Value}");
    }

    private static void Ready(InventorySnapshot board) => Check(board.SettlementValidation.LayoutProjectionReady,
        string.Join(",", board.SettlementValidation.Issues));
    private static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }
}
