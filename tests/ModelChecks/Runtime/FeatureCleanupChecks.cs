using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.ModelChecks.Runtime;

internal static class FeatureCleanupChecks
{
    internal static void Run()
    {
        var calls = new List<string>();
        var failures = new List<FeatureId>();
        var cleanup = new FeatureCleanup((feature, _) => failures.Add(feature));
        cleanup.Add(FeatureId.Inventory, () => calls.Add("inventory-unload"), () => calls.Add("inventory-failure"));
        cleanup.Add(FeatureId.CombatInsights, () => calls.Add("statistics"));
        cleanup.Add(FeatureId.Inventory, () => throw new InvalidOperationException("cleanup fault"));
        cleanup.Add(FeatureId.Inventory, () => calls.Add("inventory-release"));
        cleanup.Add(FeatureId.DefeatRetry, () => calls.Add("retry"));
        cleanup.Stop(FeatureId.Inventory);
        Require(calls.SequenceEqual(new[] { "inventory-failure", "inventory-release" }) &&
            failures.SequenceEqual(new[] { FeatureId.Inventory }),
            "Failure uses its specific action, continues after an exception and leaves other features alive");
        cleanup.Stop(FeatureId.Inventory);
        cleanup.Unload();
        cleanup.Unload();
        Require(calls.SequenceEqual(new[] { "inventory-failure", "inventory-release", "statistics", "retry" }),
            "Unload preserves remaining registration order and never repeats failed-feature cleanup");

        calls.Clear();
        cleanup.Add(FeatureId.Inventory, () => calls.Add("normal"), () => calls.Add("failure"));
        cleanup.Add(FeatureId.Gameplay, () => calls.Add("shared-state"));
        cleanup.Unload();
        Require(calls.SequenceEqual(new[] { "normal", "shared-state" }),
            "A new load uses normal teardown rather than the failure path");

        calls.Clear();
        cleanup.Add(FeatureId.Inventory, () => { cleanup.Stop(FeatureId.Inventory); calls.Add("once"); });
        cleanup.Stop(FeatureId.Inventory);
        Require(calls.SequenceEqual(new[] { "once" }), "Reentrant cleanup consumes its registration before invoking callbacks");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
