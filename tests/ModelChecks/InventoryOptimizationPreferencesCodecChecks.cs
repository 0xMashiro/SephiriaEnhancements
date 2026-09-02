using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Inventory;

internal static class InventoryOptimizationPreferencesCodecChecks
{
    internal static string Run()
    {
        VerifyStableRuleRoundTrip();
        VerifyMalformedPayloadRejection();
        VerifyDuplicateRuleResolution();
        return "stable rules;malformed fallback;duplicate resolution passed";
    }

    private static void VerifyStableRuleRoundTrip()
    {
        var source = new InventoryOptimizationPreferences(
            InventorySearchEffort.Thorough,
            allowStoneTabletRotation: false,
            new[]
            {
                new ArtifactOptimizationPreference(81, 101,
                    InventoryPreferenceLevel.Priority, 5),
                new ArtifactOptimizationPreference(-1, 202,
                    InventoryPreferenceLevel.Avoid, 9),
                new ArtifactOptimizationPreference(-1, 101,
                    InventoryPreferenceLevel.Core, 4)
            },
            new[]
            {
                new ComboOptimizationPreference("EMBER|LINE\nBREAK",
                    InventoryPreferenceLevel.Prefer, 3),
                new ComboOptimizationPreference("WARD",
                    InventoryPreferenceLevel.Neutral, 1)
            });

        string payload = InventoryOptimizationPreferencesCodec.Encode(source);
        if (!InventoryOptimizationPreferencesCodec.TryDecode(payload,
                InventorySearchEffort.Fast,
                allowStoneTabletRotation: true, out var decoded) ||
            decoded.SearchEffort != InventorySearchEffort.Fast ||
            !decoded.AllowStoneTabletRotation ||
            decoded.ArtifactPreferences.Count != 2 ||
            decoded.ArtifactPreferences.Any(rule => rule.TargetsInstance) ||
            decoded.ArtifactPreferences.Single(rule => rule.EntityId == 202).
                MinimumEffectiveLevel != 0 ||
            decoded.ComboPreferences.Single(rule => rule.CategoryId ==
                "EMBER|LINE\nBREAK").MinimumCount != 3)
        {
            throw new InvalidOperationException(
                "persisted preferences must round-trip stable target rules only");
        }
    }

    private static void VerifyMalformedPayloadRejection()
    {
        string[] invalidPayloads =
        {
            string.Empty,
            "v2\nA|1|2|3",
            "v1\nA|-1|2|3",
            "v1\nC||2|3",
            "v1\nC|EMBER|99|3",
            "v1\nA|1|2|-1"
        };
        if (invalidPayloads.Any(payload =>
            InventoryOptimizationPreferencesCodec.TryDecode(payload,
                InventorySearchEffort.Balanced, true, out _)))
        {
            throw new InvalidOperationException(
                "malformed or unsupported preference payloads must be rejected");
        }
        if (!InventoryOptimizationPreferencesCodec.TryDecode("v1",
                InventorySearchEffort.Balanced, true, out var empty) ||
            empty.ArtifactPreferences.Count != 0 ||
            empty.ComboPreferences.Count != 0)
        {
            throw new InvalidOperationException(
                "a versioned empty preference payload must remain valid");
        }
    }

    private static void VerifyDuplicateRuleResolution()
    {
        const string payload = "v1\nA|10|1|2\nA|10|4|4\n" +
            "C|EMBER|1|2\nC|EMBER|4|6";
        if (!InventoryOptimizationPreferencesCodec.TryDecode(payload,
                InventorySearchEffort.Balanced, true, out var decoded) ||
            decoded.ArtifactPreferences.Single().Level !=
                InventoryPreferenceLevel.Priority ||
            decoded.ArtifactPreferences.Single().MinimumEffectiveLevel != 4 ||
            decoded.ComboPreferences.Single().Level !=
                InventoryPreferenceLevel.Priority ||
            decoded.ComboPreferences.Single().MinimumCount != 6)
        {
            throw new InvalidOperationException(
                "the last persisted rule for a stable target must win");
        }
    }
}
