using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizationArchitectureChecks
{
    internal static string Run()
    {
        VerifyIntentComposition();
        VerifyExplorationIntentLifecycle();
        VerifyOptimizerDiscoveryContract();
        VerifyMechanicOwnership();
        return "runtime mechanics, policy sources, exploration lifetime and automatic optimizer discovery passed";
    }

    private static void VerifyIntentComposition()
    {
        var persistent = new InventoryOptimizationPreferences(
            InventorySearchEffort.Fast, allowStoneTabletRotation: false,
            new[]
            {
                new ArtifactOptimizationPreference(-1, 10,
                    InventoryPreferenceLevel.Prefer, 1),
                new ArtifactOptimizationPreference(-1, 20,
                    InventoryPreferenceLevel.Core, 2)
            },
            new[]
            {
                new ComboOptimizationPreference("FIRE",
                    InventoryPreferenceLevel.Prefer, 2)
            });
        var exploration = new InventoryOptimizationPreferences(
            InventorySearchEffort.Fast, allowStoneTabletRotation: false,
            new[]
            {
                new ArtifactOptimizationPreference(-1, 10,
                    InventoryPreferenceLevel.Priority, 0),
                new ArtifactOptimizationPreference(501, 10,
                    InventoryPreferenceLevel.Avoid, 0)
            },
            new[]
            {
                new ComboOptimizationPreference("FIRE",
                    InventoryPreferenceLevel.Priority, 4)
            });

        InventoryOptimizationPreferences composed =
            InventoryOptimizationPreferenceComposer.Compose(persistent,
                exploration, InventorySearchEffort.Thorough,
                allowStoneTabletRotation: true);
        ArtifactOptimizationPreference entityRule = composed.
            ArtifactPreferences.Single(rule => !rule.TargetsInstance &&
                rule.EntityId == 10);
        if (composed.SearchEffort != InventorySearchEffort.Thorough ||
            !composed.AllowStoneTabletRotation ||
            composed.ArtifactPreferences.Count != 3 ||
            entityRule.Level != InventoryPreferenceLevel.Priority ||
            entityRule.MinimumEffectiveLevel != 0 ||
            composed.ComboPreferences.Single().MinimumCount != 4 ||
            persistent.ArtifactPreferences.Count != 2)
        {
            throw new InvalidOperationException(
                "exploration intent must override matching persistent rules without mutating either source");
        }
    }

    private static void VerifyExplorationIntentLifecycle()
    {
        var intent = new InventoryOptimizationPreferences(
            InventorySearchEffort.Balanced, allowStoneTabletRotation: true,
            new[]
            {
                new ArtifactOptimizationPreference(-1, 30,
                    InventoryPreferenceLevel.Prefer, 1)
            }, Array.Empty<ComboOptimizationPreference>());
        ExplorationInventoryIntentStore.Replace(intent);
        if (!ReferenceEquals(ExplorationInventoryIntentStore.Capture(), intent))
        {
            throw new InvalidOperationException(
                "exploration intent store must retain the active exploration value");
        }
        ExplorationInventoryIntentStore.Clear();
        if (ExplorationInventoryIntentStore.Capture().ArtifactPreferences.Count !=
            0)
        {
            throw new InvalidOperationException(
                "exploration intent must clear at the exploration boundary");
        }
    }

    private static void VerifyOptimizerDiscoveryContract()
    {
        IInventoryLayoutOptimizer[] optimizers =
            InventoryOptimizerRegistry.Capture();
        if (optimizers.Length < 2 ||
            optimizers[0].Metadata.Id != "builtin.exact" ||
            optimizers[^1].Metadata.Id != "builtin.bounded" ||
            !optimizers[0].Metadata.Capabilities.HasFlag(
                InventoryOptimizerCapabilities.OptimalityProof) ||
            optimizers[^1].Metadata.Capabilities.HasFlag(
                InventoryOptimizerCapabilities.OptimalityProof))
        {
            throw new InvalidOperationException(
                "automatic optimizer selection must expose stable IDs, capabilities and deterministic priority");
        }
    }

    private static void VerifyMechanicOwnership()
    {
        if (typeof(InventorySnapshot).Namespace !=
                "SephiriaEnhancements.Runtime.Inventory" ||
            typeof(InventorySettlementProjector).Namespace !=
                "SephiriaEnhancements.Runtime.Inventory" ||
            typeof(InventoryBaselineInference).Namespace !=
                "SephiriaEnhancements.Runtime.Inventory" ||
            typeof(InventoryOptimizer).Namespace !=
                "SephiriaEnhancements.Inventory")
        {
            throw new InvalidOperationException(
                "inventory capture and settlement projection must belong to Runtime, while layout search remains feature policy");
        }
    }
}
