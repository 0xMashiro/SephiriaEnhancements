using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class StatisticsRetryChecks
{
    internal static void Run()
    {
        var floor = new FloorCombatStatistics();
        var retry = new StatisticsRetryCheckpoint();
        floor.ObserveFloor("floor");
        floor.RecordDamage(1, "Local", true, 600, EncounterDamageType.Fire);
        floor.RecordDamage(2, "Guest", false, 400, EncounterDamageType.Ice);
        floor.RecordDefeat(100, EncounterEnemyTier.Normal);
        floor.RecordLocalFinalBlow();
        floor.UpdateClock(10, true);
        floor.UpdateClock(20, false);
        retry.Capture(1, 1, floor);

        for (int attempt = 0; attempt < 2; attempt++)
        {
            floor.RecordDamage(1, "Local", true, 3000, EncounterDamageType.Physical);
            floor.RecordDefeat(200, EncounterEnemyTier.Boss);
            floor.RecordLocalFinalBlow();
            floor.UpdateClock(30, true);
            floor.UpdateClock(40, false);
            retry.Capture(1, 1, floor);
            retry.Begin(true, 1, "floor");
            Require(!retry.TryRestore("floor", 1, true, floor), "wait for world reload");
            floor.Clear();
            retry.ObserveWorldLoaded();
            Require(!retry.TryRestore("floor", 1, true, floor),
                "old player readiness before restart travel must not restore the baseline");
            retry.ObserveTravelStarted();
            Require(!retry.TryRestore("floor", 1, false, floor), "wait for local travel completion");
            Require(retry.TryRestore("floor", 1, true, floor), "restore on local readiness");
            var restored = floor.Capture();
            Require(restored.TotalDamage == 1000 && restored.Duration == 10 &&
                restored.NormalDefeated == 1 && restored.BossDefeated == 0 && restored.LocalFinalBlows == 1 &&
                restored.Players.Count == 2 && restored.DamageTypes.Sum(type => type.Damage) == 1000,
                "boss retry rolls back every statistic, preserving earlier ordinary combat");
            floor.RecordDefeat(100, EncounterEnemyTier.Normal);
            Require(floor.Capture().NormalDefeated == 1, "restored defeat identities still deduplicate");
            floor.UpdateClock(1000, false);
            Require(floor.Capture().Duration == 10, "loading and failed attempts add no elapsed time");
        }

        retry.Begin(true, 1, "floor");
        retry.Cancel();
        Require(!retry.Pending && floor.Capture().TotalDamage == 1000, "cancel preserves current totals");
        retry.Begin(false, 0, "floor");
        Restore(retry, floor, "floor", 1);
        Require(floor.Capture().TotalDamage == 0 && floor.Capture().Duration == 0 &&
            floor.Capture().DefeatedCount == 0, "floor retry clears all totals even for the same GUID");

        foreach (string scenario in new[] { "missing", "identity", "floor", "world" })
        {
            retry.Clear();
            floor.ObserveFloor("floor");
            floor.RecordDamage(1, "Local", true, 1000, EncounterDamageType.Fire);
            if (scenario != "missing") retry.Capture(2, 1, floor);
            if (scenario == "world") retry.ObserveWorldLoaded();
            retry.Begin(true, 2, "floor");
            Restore(retry, floor, scenario == "floor" ? "other" : "floor", scenario == "identity" ? 2u : 1u);
            Require(floor.Capture().TotalDamage == 0, scenario + " must not restore an unrelated baseline");
        }
        Console.WriteLine("StatisticsRetry: repeated boss retry, floor retry, loading order, cancel, identity and missing checkpoints passed");
    }

    private static void Restore(StatisticsRetryCheckpoint retry, FloorCombatStatistics floor, string guid, uint player)
    {
        retry.ObserveTravelStarted();
        retry.ObserveWorldLoaded();
        Require(retry.TryRestore(guid, player, true, floor), "travel may precede world notification");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
