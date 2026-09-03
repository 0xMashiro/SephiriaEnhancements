using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryArrangementLifecyclePolicyChecks
{
    internal static void Run()
    {
        InventorySnapshot rowSnapshot = InventorySnapshotFixture.RowDependentArtifact();
        ResolvedInventoryOptimizationPolicy defaultPolicy =
            InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
                InventoryOptimizationPreferences.Default);
        var lifecycleCases = new[]
        {
            (InventoryArrangementOperationPhase.Idle, false, false, false, false,
                false, InventoryArrangementInvalidationReason.None),
            (InventoryArrangementOperationPhase.Searching, false, true, true, true,
                true, InventoryArrangementInvalidationReason.FeatureDisabled),
            (InventoryArrangementOperationPhase.Searching, true, false, true, true,
                true, InventoryArrangementInvalidationReason.InventoryOptimizationUnavailable),
            (InventoryArrangementOperationPhase.Searching, true, true, false, true,
                true, InventoryArrangementInvalidationReason.GameplayContextChanged),
            (InventoryArrangementOperationPhase.Searching, true, true, true, false,
                true, InventoryArrangementInvalidationReason.InventoryStateChanged),
            (InventoryArrangementOperationPhase.Searching, true, true, true, true,
                false, InventoryArrangementInvalidationReason.InventoryLayoutChanged),
            (InventoryArrangementOperationPhase.Searching, true, true, true, true,
                true, InventoryArrangementInvalidationReason.None)
        };
        foreach (var lifecycleCase in lifecycleCases)
        {
            InventoryArrangementInvalidationReason actual =
                InventoryArrangementLifecyclePolicy.Evaluate(lifecycleCase.Item1,
                    lifecycleCase.Item2, lifecycleCase.Item3, lifecycleCase.Item4,
                    lifecycleCase.Item5, lifecycleCase.Item6);
            if (actual != lifecycleCase.Item7)
                throw new InvalidOperationException(
                    "inventory arrangement lifecycle matrix mismatch: " + actual);
        }

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            bool cancellationObserved = false;
            try
            {
                InventoryOptimizer.Solve(rowSnapshot, defaultPolicy,
                    new InventorySearchBudget(maximumElapsedMilliseconds: 1000),
                    cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                cancellationObserved = true;
            }
            if (!cancellationObserved)
                throw new InvalidOperationException(
                    "inventory search must observe cancellation before exploring candidates");
        }
        Console.WriteLine("InventoryArrangementLifecyclePolicy: invalidation matrix and cancellation passed");

        if (!InventoryArrangementLifecyclePolicy.HasSameCapacity(
                sourceWidth: 6, sourceStorage: 30,
                currentWidth: 6, currentStorage: 30) ||
            InventoryArrangementLifecyclePolicy.HasSameCapacity(
                sourceWidth: 6, sourceStorage: 30,
                currentWidth: 6, currentStorage: 32) ||
            InventoryArrangementLifecyclePolicy.HasSameCapacity(
                sourceWidth: 6, sourceStorage: 32,
                currentWidth: 6, currentStorage: 30) ||
            InventoryArrangementLifecyclePolicy.HasSameCapacity(
                sourceWidth: 6, sourceStorage: 30,
                currentWidth: 5, currentStorage: 30))
        {
            throw new InvalidOperationException(
                "inventory application must reject stale capacity snapshots");
        }
        Console.WriteLine("InventoryArrangementLifecyclePolicy: growth, shrink and " +
            "width mismatch application gates passed");
    }
}
