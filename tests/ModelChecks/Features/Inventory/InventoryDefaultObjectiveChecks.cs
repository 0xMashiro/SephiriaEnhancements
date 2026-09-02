using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryDefaultObjectiveChecks
{
    internal static string Run()
    {
        InventorySnapshot snapshot = InventorySnapshotFixture.RowDependentArtifact();
        ResolvedInventoryOptimizationPolicy policy =
            InventoryOptimizationPolicyResolver.Resolve(snapshot,
                InventoryOptimizationPreferences.Default);
        if (policy.SearchEffort != InventorySearchEffort.Balanced ||
            !policy.AllowStoneTabletRotation ||
            policy.ArtifactInstanceRules.Count != 0 ||
            policy.ArtifactEntityRules.Count != 0 ||
            policy.ComboRules.Count != 0)
        {
            throw new InvalidOperationException(
                "default inventory policy must expose only implemented behavior");
        }

        AssertHigher("excluded targets", Score(avoidedTargets: 0),
            Score(avoidedTargets: 1, priorityTargets: 100));
        AssertHigher("ordered first target",
            Score(orderedPriorityCompletion: new[] { 1, 0 }),
            Score(orderedPriorityCompletion: new[] { 0, 10_000 },
                priorityTargets: 100));
        AssertHigher("priority targets", Score(priorityTargets: 1),
            Score(priorityCompletion: 100));
        AssertHigher("priority completion", Score(priorityCompletion: 1),
            Score(presetTargets: 100));
        AssertHigher("preset targets", Score(presetTargets: 1),
            Score(presetCompletion: 100));
        AssertHigher("preset completion",
            Score(presetCompletion: 1, deactivatedArtifacts: 100),
            Score(deactivatedArtifacts: 0));
        AssertHigher("preserved active artifacts",
            Score(deactivatedArtifacts: 0),
            Score(deactivatedArtifacts: 1, enabledArtifacts: 100));
        AssertHigher("enabled artifacts", Score(enabledArtifacts: 1),
            Score(breakpoints: 100));
        AssertHigher("all category breakpoints", Score(breakpoints: 1),
            Score(effectiveLevels: 100));
        AssertHigher("effective levels", Score(effectiveLevels: 1,
                wastedLevels: 100), Score(wastedLevels: 0));
        AssertHigher("wasted levels", Score(wastedLevels: 0,
                movedItems: 100), Score(wastedLevels: 1, movedItems: 0));
        AssertHigher("moved items", Score(movedItems: 0,
                rotatedTablets: 100), Score(movedItems: 1,
                rotatedTablets: 0));
        AssertHigher("rotated tablets", Score(rotatedTablets: 0),
            Score(rotatedTablets: 1));

        return "balanced budget;tablet rotation enabled;lexicographic " +
            "priority contract passed";
    }

    private static void AssertHigher(string priority,
        InventoryOptimizationScore higher, InventoryOptimizationScore lower)
    {
        if (higher.CompareTo(lower) <= 0)
        {
            throw new InvalidOperationException(
                "inventory objective ordering changed at " + priority);
        }
    }

    private static InventoryOptimizationScore Score(
        int priorityTargets = 0, int priorityCompletion = 0,
        int avoidedTargets = 0, int presetTargets = 0,
        int presetCompletion = 0, int deactivatedArtifacts = 0,
        int enabledArtifacts = 0, int breakpoints = 0,
        int effectiveLevels = 0,
        int wastedLevels = 0, int movedItems = 0, int rotatedTablets = 0,
        int[]? orderedPriorityCompletion = null)
    {
        return new InventoryOptimizationScore(
            priorityTargetsSatisfied: priorityTargets,
            priorityTargetCompletionPoints: priorityCompletion,
            avoidedTargetsActive: avoidedTargets,
            presetTargetsSatisfied: presetTargets,
            presetTargetCompletionPoints: presetCompletion,
            sourceEnabledArtifactsDeactivated: deactivatedArtifacts,
            enabledArtifactCount: enabledArtifacts,
            comboBreakpointValue: breakpoints,
            cappedEffectiveArtifactLevelTotal: effectiveLevels,
            excessArtifactLevelTotal: wastedLevels,
            movedItemCount: movedItems,
            rotatedTabletCount: rotatedTablets,
            orderedPriorityCompletionPoints: orderedPriorityCompletion);
    }
}
