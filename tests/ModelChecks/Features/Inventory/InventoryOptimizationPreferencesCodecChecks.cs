using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryOptimizationPreferencesCodecChecks
{
    internal static string Run()
    {
        var source = new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, false,
            new[] { new ArtifactOptimizationPreference(81, 101, InventoryPreferenceLevel.Priority, 5, 0) },
            new[]
            {
                new ComboOptimizationPreference("EMBER|LINE\nBREAK", InventoryPreferenceLevel.Priority, 3),
                new ComboOptimizationPreference("WARD", InventoryPreferenceLevel.Avoid, 0)
            });
        string payload = InventoryOptimizationPreferencesCodec.Encode(source);
        if (!InventoryOptimizationPreferencesCodec.TryDecode(payload, InventorySearchEffort.Fast, true, out var decoded) ||
            decoded.SearchEffort != InventorySearchEffort.Fast || !decoded.AllowStoneTabletRotation ||
            decoded.ArtifactPreferences.Count != 0 || decoded.ComboPreferences.Count != 2 ||
            decoded.ComboPreferences.Single(rule => rule.CategoryId == "EMBER|LINE\nBREAK").TargetCount != 3 ||
            decoded.ComboPreferences.Single(rule => rule.CategoryId == "WARD").TargetCount != 0)
            throw new InvalidOperationException("only stable combo targets may persist; artifact queue entries belong to the current exploration");

        string[] invalid = { "", "v2\nC|WARD|1|0", "v3\nA|101|1|5", "v3\nC||1|0",
            "v3\nC|WARD|99|0", "v3\nC|WARD|1|-1" };
        if (invalid.Any(value => InventoryOptimizationPreferencesCodec.TryDecode(value,
            InventorySearchEffort.Balanced, true, out _)))
            throw new InvalidOperationException("unsupported artifact-type rules and malformed combo payloads must be rejected");
        if (!InventoryOptimizationPreferencesCodec.TryDecode("v3", InventorySearchEffort.Balanced, true, out var empty) ||
            empty.ComboPreferences.Count != 0 || empty.ArtifactPreferences.Count != 0)
            throw new InvalidOperationException("empty combo preferences must remain valid");
        if (!InventoryOptimizationPreferencesCodec.TryDecode("v3\nC|EMBER|0|2\nC|EMBER|1|6",
            InventorySearchEffort.Balanced, true, out var repeated) ||
            repeated.ComboPreferences.Single().Level != InventoryPreferenceLevel.Priority ||
            repeated.ComboPreferences.Single().TargetCount != 6)
            throw new InvalidOperationException("the last persisted combo rule must win");
        return "combo round-trip;zero targets;artifact rules excluded;invalid payloads rejected";
    }
}
