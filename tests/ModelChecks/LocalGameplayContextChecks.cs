using SephiriaEnhancements.Runtime;

internal static class LocalGameplayContextChecks
{
    internal static string Run()
    {
        var tracker = new LocalGameplayContextTracker();
        var localPlayer = new object();
        Expect(tracker, null, null, false, LocalGameplayContextChange.None);
        Expect(tracker, localPlayer, "town", false,
            LocalGameplayContextChange.PlayerChanged);
        Expect(tracker, localPlayer, "town", false,
            LocalGameplayContextChange.None);

        // Loading begins before the destination floor is synchronized.
        Expect(tracker, localPlayer, "town", true,
            LocalGameplayContextChange.TravelStarted);
        Expect(tracker, localPlayer, "town", true,
            LocalGameplayContextChange.None);
        Expect(tracker, localPlayer, "town", false,
            LocalGameplayContextChange.None);
        Expect(tracker, localPlayer, "floor-a", false,
            LocalGameplayContextChange.FloorChanged);
        Expect(tracker, localPlayer, "floor-a", false,
            LocalGameplayContextChange.None);

        // Existing floor objects need no new generation event on re-entry.
        Expect(tracker, localPlayer, "town", false,
            LocalGameplayContextChange.FloorChanged);
        Expect(tracker, localPlayer, "floor-a", false,
            LocalGameplayContextChange.FloorChanged);

        // Also accept a destination arriving while the loading overlay remains.
        Expect(tracker, localPlayer, "floor-a", true,
            LocalGameplayContextChange.TravelStarted);
        Expect(tracker, localPlayer, "floor-b", true,
            LocalGameplayContextChange.FloorChanged);
        Expect(tracker, localPlayer, "floor-b", false,
            LocalGameplayContextChange.None);

        // An in-floor teleport invalidates departure work without inventing a new floor.
        Expect(tracker, localPlayer, "floor-b", true,
            LocalGameplayContextChange.TravelStarted);
        Expect(tracker, localPlayer, "floor-b", false,
            LocalGameplayContextChange.None);
        Expect(tracker, null, null, false,
            LocalGameplayContextChange.PlayerChanged);
        Expect(tracker, null, null, false, LocalGameplayContextChange.None);
        var reconnectedPlayer = new object();
        Expect(tracker, reconnectedPlayer, "floor-b", false,
            LocalGameplayContextChange.PlayerChanged);
        Expect(tracker, new object(), "floor-b", false,
            LocalGameplayContextChange.PlayerChanged);
        return "departure, arrival ordering, existing-floor re-entry, duplicate observations and player replacement passed";
    }

    private static void Expect(LocalGameplayContextTracker tracker,
        object? player, string? floor, bool traveling,
        LocalGameplayContextChange expected)
    {
        LocalGameplayContextChange actual = tracker.Observe(player, floor,
            traveling);
        if (actual != expected || tracker.IsTraveling !=
            (player != null && traveling))
            throw new InvalidOperationException(
                $"local context expected {expected}, received {actual}");
    }
}
