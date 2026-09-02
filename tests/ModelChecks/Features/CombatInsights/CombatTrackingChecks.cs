using SephiriaEnhancements.Core;
using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class CombatTrackingChecks
{
    internal static void Run()
    {
        var encounter = new BossEncounterTracker();
        if (!encounter.Begin(10f) || encounter.Begin(11f))
            throw new InvalidOperationException("boss encounter must start exactly once");
        encounter.Record(1, 120f);
        encounter.Record(2, 80f);
        encounter.Record(1, 30f);
        encounter.Record(1, -5f);
        Near(encounter.Total, 230f, "boss total accumulates positive damage");
        Near(encounter.GetDamage(1), 150f, "player damage accumulates");
        Near(encounter.AverageDps(1, 20f), 15f, "live average uses shared encounter time");
        if (!encounter.Pause(20f) || encounter.Pause(21f))
            throw new InvalidOperationException("boss phase transition must pause timing exactly once");
        Near(encounter.Elapsed(25f), 10f, "phase transition time is excluded");
        if (!encounter.Resume(25f) || encounter.Resume(26f))
            throw new InvalidOperationException("next boss phase must resume timing exactly once");
        if (!encounter.End(35f) || encounter.End(36f))
            throw new InvalidOperationException("boss encounter must end exactly once");
        Near(encounter.Elapsed(99f), 20f, "completed duration is frozen");
        Near(encounter.AverageDps(2, 99f), 4f, "post-fight average uses frozen duration");
        encounter.Record(1, 500f);
        Near(encounter.Total, 230f, "post-fight damage is ignored");
        encounter.Reset();
        Near(encounter.Total, 0f, "reset clears the encounter");
        Console.WriteLine("BossEncounterTracker: lifecycle, totals and shared-time DPS checks passed");

        var hitStreak = new HitStreakTracker();
        HitStreakUpdate first = hitStreak.Register(0f, 10, HitStreakImpact.Normal,
            indirectDamage: false);
        if (first.Count != 1 || first.ShouldRender) throw new InvalidOperationException("first hit must arm without rendering");
        HitStreakUpdate second = hitStreak.Register(0.1f, 20, HitStreakImpact.Normal,
            indirectDamage: false);
        if (second.Count != 2 || !second.ShouldRender) throw new InvalidOperationException("second hit must begin visible hit streak");
        HitStreakUpdate dot = hitStreak.Register(0.2f, 3, HitStreakImpact.Normal,
            indirectDamage: true);
        if (dot.Count != 0 || dot.ShouldRender || hitStreak.Count != 2) throw new InvalidOperationException("indirect tick must not extend hit streak");
        for (int count = 3; count <= 10; count++) hitStreak.Register(0.2f + count * 0.1f,
            1, HitStreakImpact.Normal, indirectDamage: false);
        var milestoneTracker = new HitStreakTracker();
        HitStreakUpdate ten = default;
        for (int count = 1; count <= 10; count++)
            ten = milestoneTracker.Register(count * 0.1f, 1, HitStreakImpact.Normal,
                indirectDamage: false);
        if (!ten.IsMilestone || !ten.ShouldRender || ten.Count != 10 || ten.Tier != 1)
            throw new InvalidOperationException("ten-hit milestone must render and enter tier one");
        HitStreakUpdate milestone = hitStreak.Register(1.21f, 1, HitStreakImpact.Critical,
            indirectDamage: false);
        if (milestone.Count != 11 || !milestone.ShouldRender || milestone.Tier != 1)
            throw new InvalidOperationException("critical hit must render in the ten-hit tier");
        HitStreakUpdate reset = hitStreak.Register(3f, 5, HitStreakImpact.Normal,
            indirectDamage: false);
        if (reset.Count != 1 || reset.ShouldRender) throw new InvalidOperationException("hit-streak timeout must restart at one");
        Console.WriteLine("HitStreakTracker: timeout, cadence, critical, tier and DOT checks passed");

        var contexts = new DamageContextBuffer();
        contexts.Record(1f, 7, 42, 3f, 4f, indirectDamage: true);
        if (!contexts.TryMatch(1.2f, 7, 42, 3.1f, 4.1f, out bool indirect) || !indirect)
            throw new InvalidOperationException("nearby damage context must correlate");
        if (contexts.TryMatch(1.21f, 7, 42, 3.1f, 4.1f, out _))
            throw new InvalidOperationException("damage context must be consumed once");
        contexts.Record(2f, 8, 50, 0f, 0f, indirectDamage: false);
        if (contexts.TryMatch(2.7f, 8, 50, 0f, 0f, out _))
            throw new InvalidOperationException("expired damage context must not correlate");
        contexts.Record(3f, 9, 60, 1f, 2f, indirectDamage: false,
            EncounterDamageType.Lightning);
        if (!contexts.TryMatchDamageType(3.1f, 9, 60, 1f, 2f,
                out EncounterDamageType damageType) ||
            damageType != EncounterDamageType.Lightning)
            throw new InvalidOperationException(
                "damage context must preserve the native damage type mapping");
        Console.WriteLine("DamageContextBuffer: proximity, type, consumption and expiry checks passed");

        var window = new RollingDamageWindow(5f, 8);
        window.Record(0.2f, 20f);
        Near(window.Dps(0.2f), 20f, "rolling window uses one-second warmup floor");
        window.Record(1.2f, 30f);
        Near(window.Dps(1.2f), 50f / 1f, "rolling window aggregates recent damage");
        window.Reset();
        Near(window.Dps(2f), 0f, "source reset clears rolling damage");
        for (int hit = 0; hit < 100; hit++) window.Record(3f, 1f);
        Near(window.Damage, 100f, "high-rate hits coalesce without overflowing the ring");
        Console.WriteLine("RollingDamageWindow: delta, warmup, expiry and source-reset checks passed");

        var ordinaryScope = EncounterScope.Create("floor", 10, EncounterScopeKind.Ordinary,
            0f, 0f, 10f, 10f);
        var bossScope = EncounterScope.Create("floor", 20, EncounterScopeKind.Boss,
            -5f, -5f, 15f, 15f);
        if (ordinaryScope == null || !ordinaryScope.AllowsDamage("floor", 1f, 1f, 9f, 9f) ||
            ordinaryScope.AllowsDamage("other", 1f, 1f, 9f, 9f) ||
            ordinaryScope.AllowsDamage("floor", 1f, 1f, 11f, 9f) ||
            EncounterScope.SelectContaining(ordinaryScope, bossScope, 5f, 5f) != bossScope)
            throw new InvalidOperationException(
                "encounter-area isolation or boss priority failed");
        if (PlayerIdentityKey.Resolve(42, 7) != 42 ||
            PlayerIdentityKey.Resolve(0, 7) >= 0 ||
            PlayerIdentityKey.Resolve(0, 7) == PlayerIdentityKey.Resolve(0, 8))
            throw new InvalidOperationException("stable player identity key failed");
        Console.WriteLine("EncounterScope: encounter-area isolation, boss priority and " +
            "source identity checks passed");

        var defeats = new EncounterDefeatTracker();
        if (!defeats.RecordDefeat(1, EncounterEnemyTier.Normal) ||
            defeats.RecordDefeat(1, EncounterEnemyTier.Normal) ||
            !defeats.RecordDefeat(2, EncounterEnemyTier.Miniboss) ||
            !defeats.RecordDefeat(3, EncounterEnemyTier.Boss))
            throw new InvalidOperationException("defeat tracker deduplication failed");
        defeats.RecordLocalFinalBlow();
        if (defeats.DefeatedCount != 3 || defeats.LocalFinalBlows != 1 ||
            defeats.NormalDefeated != 1 || defeats.MinibossDefeated != 1 ||
            defeats.BossDefeated != 1 || defeats.DefeatedCount !=
                defeats.NormalDefeated + defeats.MinibossDefeated + defeats.BossDefeated)
            throw new InvalidOperationException("defeat tracker totals or tiers failed");
        defeats.Reset();
        if (defeats.DefeatedCount != 0 || defeats.LocalFinalBlows != 0 ||
            defeats.NormalDefeated != 0 || defeats.MinibossDefeated != 0 ||
            defeats.BossDefeated != 0)
            throw new InvalidOperationException("defeat tracker reset failed");
        Console.WriteLine("EncounterDefeatTracker: dedupe, tiers, local final blows and reset checks passed");
    }

    private static void Near(float actual, float expected, string name)
    {
        if (Math.Abs(actual - expected) > 0.001f)
            throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
    }
}
