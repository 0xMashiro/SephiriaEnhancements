using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class FloorCombatStatisticsChecks
{
    internal static void Run()
    {
        var floor = new FloorCombatStatistics();
        floor.ObserveFloor("first");
        floor.UpdateClock(10f, true);
        floor.RecordDamage(1, "Local", true, 600f, EncounterDamageType.Fire);
        floor.RecordDamage(2, "Guest", false, 400f, EncounterDamageType.Ice);
        floor.RecordDefeat(99, EncounterEnemyTier.Normal);
        floor.RecordDefeat(99, EncounterEnemyTier.Normal);
        floor.RecordLocalFinalBlow();
        floor.UpdateClock(20f, false);
        CombatStatisticsSnapshot first = floor.Capture();
        Require(first.TotalDamage == 1000f && first.Duration == 10f,
            "players share one battle clock, not one clock per damage source");
        Require(first.NormalDefeated == 1 && first.LocalFinalBlows == 1,
            "native duplicate death observations count once");
        floor.UpdateClock(100f, false);
        floor.ObserveFloor("first");
        floor.ObserveFloor(null);
        Require(floor.Capture().Duration == 10f && floor.Capture().TotalDamage == 1000f,
            "idle time, repeated same-floor observations and temporary missing GUIDs preserve totals");

        floor.UpdateClock(200f, true);
        floor.RecordDamage(1, "Renamed", true, 500f, EncounterDamageType.Physical);
        floor.UpdateClock(202f, true);
        floor.UpdateClock(202f, true);
        floor.UpdateClock(202f, false);
        floor.UpdateClock(250f, false);
        floor.UpdateClock(300f, true);
        floor.UpdateClock(303f, false);
        floor.RecordDefeat(100, EncounterEnemyTier.Boss);
        CombatStatisticsSnapshot total = floor.Capture();
        Require(total.Duration == 15f && total.TotalDamage == 1500f,
            "two encounters accumulate; actual pauses and paused boss phases add no time");
        Require(total.TotalDamage / total.Duration == 100f &&
            total.Players.Single(row => row.Key == 1).Damage / total.Duration > 73f,
            "floor DPS is damage over shared accumulated battle time, not a mean of encounter DPS");
        Require(total.Players[0].Name == "Renamed" && total.BossDefeated == 1 &&
            total.DamageTypes.Sum(type => type.Damage) == total.TotalDamage,
            "player identity, enemy tiers and elemental damage survive subsequent fights");
        Require(first.Duration == 10f && first.TotalDamage == 1000f,
            "capturing and viewing totals cannot mutate the frozen last-encounter data");

        for (int key = 3; key <= 6; key++)
            floor.RecordDamage(key, "Guest " + key, false, 10f, EncounterDamageType.Normal);
        Require(floor.Capture().Players.Count == 6 && floor.Capture().TotalDamage == 1540f,
            "sequential visitors must not disappear behind the four-concurrent-player limit");
        floor.ObserveFloor("second");
        Require(floor.Capture().TotalDamage == 0f && floor.Capture().Duration == 0f &&
            floor.Capture().DefeatedCount == 0, "a different local floor starts a fresh aggregate");
        floor.RecordDamage(1, "Local", true, 1f, EncounterDamageType.Fire);
        floor.Clear();
        Require(floor.Capture().TotalDamage == 0f && floor.Capture().Players.Count == 0,
            "world/player replacement, disabling and unloading can discard local observations");
        Console.WriteLine("FloorCombatStatistics: shared clock, pauses, encounters, identity, " +
            "death deduplication, snapshots and local-floor boundaries passed");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
