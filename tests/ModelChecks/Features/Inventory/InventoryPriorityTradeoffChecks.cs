using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryPriorityTradeoffChecks
{
    internal static object[] Run()
    {
        var reports = new List<object>();
        foreach (bool jointlyFeasible in new[] { false, true })
            foreach (bool reversePriority in new[] { false, true })
            {
                InventorySnapshot snapshot = CreateSnapshot(jointlyFeasible ? new[] { 6, 0, 6, 0 } : new[] { 6, 6, 5, 5 });
                var preferences = Preferences(snapshot, reversePriority);
                var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
                var scorer = new InventoryOptimizationScorer(snapshot, policy);
                var current = InventorySettlementProjector.Evaluate(snapshot, InventoryLayoutProjection.Current(snapshot));
                var candidates = (from first in Enumerable.Range(0, 4)
                                  from second in Enumerable.Range(0, 4)
                                  where first != second
                                  let layout = new InventoryLayoutProjection(new[] { first, second }, new[] { 0, 0 })
                                  let settlement = InventorySettlementProjector.Evaluate(snapshot, layout)
                                  select new Candidate(layout, settlement, scorer.Score(layout, settlement),
                                      scorer.EvaluateTargets(current, settlement).ToArray())).ToArray();
                Candidate selected = candidates.MaxBy(candidate => candidate.Score)!;
                // Experimental comparison only: keep position-effect/exclusion precedence,
                // then compare reached combo minimums before the existing score.
                Candidate comboFirst = candidates.OrderBy(candidate => -candidate.Score.PositionEffectRegressions)
                    .ThenBy(candidate => -candidate.Score.AvoidedTargetsActive)
                    .ThenBy(candidate => candidate.Targets.Count(t => t.Kind == InventoryOptimizationTargetKind.ComboCategory && t.AfterConditionReached))
                    .ThenBy(candidate => candidate.Score).Last();
                var exact = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(8, 100, 10000));
                Require(exact.OptimalityProven && exact.CandidateEvaluations == 12 && exact.BestScore.CompareTo(selected.Score) == 0,
                    "production exact solver must match the complete 12-layout comparison");
                int feasibleLayouts = candidates.Count(candidate => candidate.Targets.All(target => target.AfterConditionReached));
                Require((feasibleLayouts > 0) == jointlyFeasible, "known joint-feasibility result");
                if (jointlyFeasible)
                    Require(selected.Targets.All(target => target.AfterConditionReached) && comboFirst.Targets.All(target => target.AfterConditionReached),
                        "both comparisons must select all goals when jointly feasible");
                else
                {
                    Require(selected.Settlement.Artifacts.All(artifact => artifact.CappedEffectiveLevel == 6) &&
                        selected.Settlement.ComboCounts["FIRE"] == 11 && selected.Settlement.ComboCounts["ICE"] == 9,
                        "current priority trades combo minimum for artifact level");
                    Require(comboFirst.Settlement.ComboCounts["FIRE"] == 10 && comboFirst.Settlement.ComboCounts["ICE"] == 10 &&
                        comboFirst.Settlement.Artifacts[reversePriority ? 1 : 0].CappedEffectiveLevel == 6 &&
                        comboFirst.Settlement.Artifacts[reversePriority ? 0 : 1].CappedEffectiveLevel == 5,
                        "experimental combo precedence preserves the first artifact in the user order");
                }
                reports.Add(new
                {
                    Case = jointlyFeasible ? "JointlyFeasible" : "ArtifactComboConflict",
                    ReverseArtifactPriority = reversePriority,
                    EnumeratedLayouts = candidates.Length,
                    JointlyFeasibleLayouts = feasibleLayouts,
                    Current = Describe(selected),
                    ComboThresholdFirstExperiment = Describe(comboFirst)
                });
            }
        VerifyMaximumAndDisabledCounting();
        return reports.ToArray();
    }

    private sealed record Candidate(InventoryLayoutProjection Layout, ProjectedInventorySettlement Settlement,
        InventoryOptimizationScore Score, InventoryOptimizationTargetEvaluation[] Targets);

    private static object Describe(Candidate candidate) => new
    {
        Cells = candidate.Layout.CopyCells(),
        ArtifactLevels = candidate.Settlement.Artifacts.Select(artifact => artifact.CappedEffectiveLevel).ToArray(),
        Combos = candidate.Settlement.ComboCounts,
        AllTargetsReached = candidate.Targets.All(target => target.AfterConditionReached)
    };

    private static void VerifyMaximumAndDisabledCounting()
    {
        InventorySnapshot snapshot = CreateSnapshot(new[] { 5, 5, 6, 6 });
        var preferences = Preferences(snapshot, false);
        preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true, preferences.ArtifactPreferences.ToArray(),
            new[] { new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 10),
                new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Avoid, 10) });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var exact = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(8, 100, 10000));
        var settlement = InventorySettlementProjector.Evaluate(snapshot, exact.Layout);
        Require(exact.OptimalityProven && settlement.ComboCounts["ICE"] == 10 && settlement.Artifacts[0].CappedEffectiveLevel == 6 &&
            settlement.Artifacts[1].CappedEffectiveLevel == 5, "combo maximum exclusion precedes ordered artifact completion");
        InventorySnapshot inactive = CreateSnapshot(new[] { -1, 6, 6, 6 });
        var observed = InventorySettlementProjector.Evaluate(inactive, InventoryLayoutProjection.Current(inactive));
        Require(!observed.Artifacts[0].Enabled && observed.ComboCounts["FIRE"] == 10 && observed.ComboCounts["ICE"] == 10,
            "disabled artifact still counts toward its category");
    }

    private static InventoryOptimizationPreferences Preferences(InventorySnapshot snapshot, bool reverse) => new(
        InventorySearchEffort.Balanced, true, snapshot.Items.Select((item, index) => new ArtifactOptimizationPreference(
            item.InstanceId, item.EntityId, InventoryPreferenceLevel.Priority, 6, reverse ? 1 - index : index)).ToArray(),
        new[] { new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 10),
            new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Priority, 10) });

    private static InventorySnapshot CreateSnapshot(int[] levels)
    {
        int[] positions = { 0, 2 };
        var items = positions.Select((cell, index) =>
        {
            string category = cell < 2 ? "FIRE" : "ICE";
            bool enabled = levels[cell] >= 0;
            var artifact = new ArtifactSnapshot(levels[cell], 6, 0, 0, enabled ? levels[cell] : 0, enabled, !enabled,
                false, "", true, false, false, "Pre", new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                    CriteriaEvaluationState.NotApplicable, CriteriaEvaluationState.NotApplicable), new[] { category },
                new[] { "FIRE", "ICE" }, false, null, new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo, new[] { "FIRE", "ICE" }));
            return new InventoryItemSnapshot(index, 30000 + index, 1, cell, cell % 2, cell / 2, "Synthetic artifact", "", "Charm", "Normal",
                new[] { category }, InventoryItemKind.Artifact, artifact, null);
        }).ToArray();
        var cells = levels.Select((level, cell) => new InventoryCellSnapshot(cell, cell % 2, cell / 2, level, positions.Contains(cell) ? 6 : -1,
            0, 0, 0, 0, false, new InventoryCellSettlementSnapshot(true, level, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))).ToArray();
        var combos = new[] { "FIRE", "ICE" }.Select(category => new ComboCategorySnapshot(category, 10, 10, 1, 9, 0,
            new[] { 10 }, new[] { 10 }, false)).ToArray();
        var snapshot = new InventorySnapshot(2, 4, cells, items, comboCategories: combos);
        var current = InventoryLayoutProjection.Current(snapshot);
        Require(snapshot.SettlementValidation.LayoutProjectionReady && InventorySettlementDifferentialVerifier.Compare(snapshot, current,
            InventorySettlementProjector.Evaluate(snapshot, current), snapshot).Matched, "small fixture independent initial observations");
        return snapshot;
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Inventory priority tradeoff: " + message);
    }
}
