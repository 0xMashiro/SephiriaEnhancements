using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(UnitAvatar), nameof(UnitAvatar.HealPercent),
        new[] { typeof(float) })]
    internal static class FestivalOfBloodHealingRulePatch
    {
        // These strings are native hard-mode and KeywordDatabase contracts.
        private const string NativeFestivalOfBloodEnvironmentKey = "BLOODFESTIVAL";
        private const string NativeRegularEnemyHealingKeyword =
            "hardModeBloodFestivalHeal";
        private const string NativeBossAndMinibossHealingKeyword =
            "hardModeBloodFestivalHealBossAndMiniboss";

        private static void Prefix(UnitAvatar __instance, ref float percent)
        {
            int participantCount = ServerParticipantCountReader.Read();
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.FestivalOfBloodEnemyHealingMultiplier,
                    participantCount, out float configuredMultiplier))
                return;
            MethodBase caller = new StackFrame(2, false).GetMethod();
            if (caller?.DeclaringType != typeof(CombatManager) ||
                caller.Name != nameof(CombatManager.AttackEvent) ||
                DungeonManager.Instance == null ||
                !DungeonManager.Instance.hardModeEnvironment.TryGetValue(
                    NativeFestivalOfBloodEnvironmentKey,
                    out int environmentLevel) ||
                environmentLevel <= 0)
                return;

            string nativeBaseKey = __instance.monsterType == EMonsterType.Boss ||
                __instance.monsterType == EMonsterType.Miniboss
                    ? NativeBossAndMinibossHealingKeyword
                    : NativeRegularEnemyHealingKeyword;
            percent = KeywordDatabase.GetConstValue(nativeBaseKey) *
                environmentLevel * configuredMultiplier;
        }
    }
}
