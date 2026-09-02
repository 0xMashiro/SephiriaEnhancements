using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    // ECustomStat.AllDamageBonus, SeedBossSpawner and Unit_RootDemon are native
    // API contracts. Domain types deliberately use participant and encounter terms.
    internal delegate bool EnemyHealthMultiplierResolver(
        EnemySpawnOrigin spawnOrigin,
        EnemyHealthCategory healthCategory,
        int participantCount,
        float otherModifierPercent,
        out float multiplier);

    internal static class EnemyHealthAdjustmentBridge
    {
        // Native environment identifiers are integration contracts. Do not rename
        // them to player-facing terminology.
        private const string TenaciousBodyEnvironment = "TENACIOUSBODY";
        private const string FerociousClawsEnvironment = "FEROCIOUSCLAWS";
        private const string MoreHealthBattleParameter = "MORE_HP";
        private const string TestEnemyHealthMultiplier = "TEST_EnemyHpMultiplier";
        // stageHPPercent is a private native integration contract.
        private static readonly FieldInfo CommonSpawnerStageHealthPercent =
            AccessTools.Field(typeof(CommonEnemySpawner), "stageHPPercent");

        private static ConditionalWeakTable<UnitAvatar, CapturedSpawn> capturedSpawns =
            new ConditionalWeakTable<UnitAvatar, CapturedSpawn>();
        private static EnemyHealthMultiplierResolver resolver;

        internal static void SetResolver(EnemyHealthMultiplierResolver value)
        {
            resolver = value;
            capturedSpawns = new ConditionalWeakTable<UnitAvatar, CapturedSpawn>();
            EnemySpawnOriginCapture.SetObserver(value == null ? null : Capture);
        }

        private static void Capture(UnitAvatar unit, EnemySpawnRoutineFrame frame)
        {
            Capture(unit, frame, capturedAfterNativeStats: false);
        }

        private static void Capture(UnitAvatar unit, EnemySpawnRoutineFrame frame,
            bool capturedAfterNativeStats)
        {
            if (unit == null || frame == null || !NetworkServer.active)
            {
                return;
            }

            float otherModifierPercent = ReadOtherModifierPercent(frame.Source);
            int participantCount = ServerParticipantCountReader.Read();

            capturedSpawns.Remove(unit);
            capturedSpawns.Add(unit, new CapturedSpawn(frame.Origin,
                unit.maxHp, capturedAfterNativeStats ? 0 :
                    unit.GetCustomStat(ECustomStat.AllDamageBonus),
                participantCount, otherModifierPercent));
        }

        internal static void ApplyBeforeCurrentHealthInitialization(UnitAvatar unit,
            ref float requestedHealth)
        {
            if (resolver == null || unit == null ||
                !TryGetCapturedSpawn(unit, out CapturedSpawn captured))
            {
                return;
            }

            capturedSpawns.Remove(unit);
            EnemyHealthCategory category = unit.monsterType == EMonsterType.Normal
                ? EnemyHealthCategory.Regular
                : EnemyHealthCategory.Elite;
            ApplyDamageBonusOverride(unit, captured, category);
            if (resolver(captured.Origin, category, captured.ParticipantCount,
                    captured.OtherModifierPercent, out float multiplier))
            {
                unit.NetworkmaxHp = captured.BaseHealth * multiplier;
                requestedHealth = unit.MaxHp;
            }
        }

        private static void ApplyDamageBonusOverride(UnitAvatar unit,
            CapturedSpawn captured, EnemyHealthCategory category)
        {
            MultiplayerRuleId ruleId;
            switch (captured.Origin)
            {
                case EnemySpawnOrigin.RandomEncounter:
                    ruleId = MultiplayerRuleId.RandomEncounterDamageBonus;
                    break;
                case EnemySpawnOrigin.MindEaterRootSummon:
                    ruleId = MultiplayerRuleId.MindEaterRootSummonDamageBonus;
                    break;
                case EnemySpawnOrigin.StandardBoss:
                case EnemySpawnOrigin.SeedEncounterBoss:
                    ruleId = MultiplayerRuleId.BossEncounterDamageBonus;
                    break;
                default:
                    ruleId = category == EnemyHealthCategory.Regular
                        ? MultiplayerRuleId.RegularEnemyDamageBonus
                        : MultiplayerRuleId.EliteEnemyDamageBonus;
                    break;
            }

            if (!MultiplayerRulesController.TryGetActiveOverride(ruleId,
                    captured.ParticipantCount, out float configuredBonus))
            {
                return;
            }

            int hardModeBonus = 0;
            if (HardModeManager.Instance != null && HardModeManager.Instance.IsHardMode &&
                DungeonManager.Instance != null)
            {
                DungeonManager.Instance.hardModeEnvironment.TryGetValue(
                    FerociousClawsEnvironment, out hardModeBonus);
            }
            int currentBonus = unit.GetCustomStat(ECustomStat.AllDamageBonus);
            int sourceDamageBonus = captured.Origin == EnemySpawnOrigin.SeedEncounterBoss &&
                EnemySpawnRoutineContext.CurrentFrame?.Source is SeedBossSpawner seedBoss
                    ? Mathf.RoundToInt(seedBoss.damageBonusPercent) : 0;
            int nativeParticipantBonus = currentBonus - captured.InitialDamageBonus -
                hardModeBonus - sourceDamageBonus;
            unit.AddCustomStat(ECustomStat.AllDamageBonus,
                Mathf.RoundToInt(configuredBonus) - nativeParticipantBonus);
        }

        private static bool TryGetCapturedSpawn(UnitAvatar unit,
            out CapturedSpawn captured)
        {
            if (capturedSpawns.TryGetValue(unit, out captured))
            {
                return true;
            }

            EnemySpawnRoutineFrame frame = EnemySpawnRoutineContext.CurrentFrame;
            if (frame == null)
            {
                return false;
            }

            Capture(unit, frame, capturedAfterNativeStats: true);
            return capturedSpawns.TryGetValue(unit, out captured);
        }

        private static float ReadOtherModifierPercent(object source)
        {
            float modifier;
            string hardBattleParameter = null;
            if (source is RandomEnemyPhaseSpawner randomEncounter)
            {
                modifier = randomEncounter.additionalHPPercent;
                hardBattleParameter = randomEncounter.hardBattleParameter;
            }
            else if (source is SeedBossSpawner seedBoss)
            {
                modifier = seedBoss.hpBonusPercent;
                if (HardModeManager.Instance != null &&
                    HardModeManager.Instance.IsHardMode &&
                    DungeonManager.Instance != null &&
                    DungeonManager.Instance.hardModeEnvironment.TryGetValue(
                        TenaciousBodyEnvironment, out int seedBossHardHealthPercent))
                {
                    // Native SeedBossSpawner writes TENACIOUSBODY after hpBonusPercent.
                    modifier = seedBossHardHealthPercent;
                }
            }
            else if (source is Unit_RootDemon)
            {
                modifier = 0f;
            }
            else if (source is EnemySpawner enemySpawner)
            {
                modifier = enemySpawner.additionalHPPercent;
            }
            else if (source is CommonEnemySpawner commonSpawner)
            {
                modifier = CommonSpawnerStageHealthPercent?.GetValue(commonSpawner)
                    is float stageHealthPercent ? stageHealthPercent : 0f;
            }
            else
            {
                return 0f;
            }
            if (!(source is SeedBossSpawner) && HardModeManager.Instance != null &&
                HardModeManager.Instance.IsHardMode && DungeonManager.Instance != null)
            {
                if (DungeonManager.Instance.hardModeEnvironment.TryGetValue(
                        TenaciousBodyEnvironment, out int tenaciousBodyPercent))
                {
                    modifier += tenaciousBodyPercent;
                }
            }

            if (hardBattleParameter == MoreHealthBattleParameter)
            {
                modifier += 30f;
            }

            if (!(source is SeedBossSpawner) && !(source is Unit_RootDemon) &&
                PlayerInputController.Instance != null &&
                PlayerInputController.Instance.isTestMode &&
                DungeonManager.Instance != null &&
                DungeonManager.Instance.dungeonEnvironment.TryGetValue(
                    TestEnemyHealthMultiplier, out int testHealthPercent))
            {
                modifier += testHealthPercent;
            }

            return modifier;
        }

        private sealed class CapturedSpawn
        {
            internal CapturedSpawn(EnemySpawnOrigin origin, float baseHealth,
                int initialDamageBonus, int participantCount,
                float otherModifierPercent)
            {
                Origin = origin;
                BaseHealth = baseHealth;
                InitialDamageBonus = initialDamageBonus;
                ParticipantCount = participantCount;
                OtherModifierPercent = otherModifierPercent;
            }

            internal EnemySpawnOrigin Origin { get; }
            internal float BaseHealth { get; }
            internal int InitialDamageBonus { get; }
            internal int ParticipantCount { get; }
            internal float OtherModifierPercent { get; }
        }
    }

    [HarmonyPatch(typeof(UnitAvatar), nameof(UnitAvatar.SetHp))]
    internal static class EnemyHealthInitializationPatch
    {
        private static void Prefix(UnitAvatar __instance, ref float amount)
        {
            EnemyHealthAdjustmentBridge.ApplyBeforeCurrentHealthInitialization(
                __instance, ref amount);
        }
    }
}
