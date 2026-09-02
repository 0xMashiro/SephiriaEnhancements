using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Runtime.Execution;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class FloorGenerationContext
    {
        internal static FloorGenerator Current =>
            AmbientExecutionContext<Frame>.Current?.Generator;
        internal static Frame CurrentFrame =>
            AmbientExecutionContext<Frame>.Current;

        internal static IEnumerator Wrap(IEnumerator routine, FloorGenerator generator) =>
            AmbientExecutionContext<Frame>.WrapCoroutine(routine,
                new Frame(generator), completed: LifeSupplyRuleApplier.Complete);

        internal sealed class Frame
        {
            internal Frame(FloorGenerator generator) => Generator = generator;
            internal FloorGenerator Generator { get; }
            internal bool LifeSupplyCreated { get; set; }
        }
    }

    internal static class LifeSupplyRuleApplier
    {
        // allBreakableProps and hpBreakables are native integration contracts.
        private static readonly FieldInfo HpBreakablesField = AccessTools.Field(
            typeof(PropDatabase), "hpBreakables");

        internal static bool BeforeCreateProp(PropEntity prop)
        {
            FloorGenerationContext.Frame frame = FloorGenerationContext.CurrentFrame;
            int participantCount = ServerParticipantCountReader.Read();
            if (!MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor,
                    participantCount, out float enabled))
                return true;
            var hpBreakables = HpBreakablesField?.GetValue(null) as List<PropEntity>;
            if (frame == null || hpBreakables == null ||
                !(frame.Generator is EnhancedProceduralFloorGenerator) &&
                !(frame.Generator is LibraryFloorGenerator) ||
                !hpBreakables.Contains(prop)) return true;
            if (enabled <= 0f) return false;
            frame.LifeSupplyCreated = true;
            return true;
        }

        internal static void Complete(FloorGenerationContext.Frame frame)
        {
            if (frame == null || frame.LifeSupplyCreated ||
                !(frame.Generator is EnhancedProceduralFloorGenerator) &&
                !(frame.Generator is LibraryFloorGenerator) ||
                DungeonManager.Instance == null ||
                !DungeonManager.Instance.generatedFloors.TryGetValue(
                    frame.Generator.guid, out FloorData floor) ||
                floor.nodeProgress <= 0 ||
                !MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor,
                    ServerParticipantCountReader.Read(), out float enabled) ||
                enabled <= 0f)
                return;

            FieldInfo breakablesField = AccessTools.Field(
                frame.Generator.GetType(), "allBreakableProps");
            var breakables = breakablesField?.GetValue(frame.Generator) as
                List<GameObject>;
            if (breakables == null || breakables.Count == 0) return;

            var random = new System.Random(floor.seed);
            int index = random.Next(0, breakables.Count);
            GameObject placeholder = breakables[index];
            BreakableProp breakable = placeholder.GetComponent<BreakableProp>();
            if (breakable == null) return;
            frame.Generator.CreateProp(breakable.RandomID,
                PropDatabase.GetRandomHPBreakable(random),
                placeholder.transform.position, Vector3.one, null, null);
            frame.Generator.DestroyProp(placeholder);
            breakables.RemoveAt(index);
            frame.LifeSupplyCreated = true;
        }
    }

    [HarmonyPatch(typeof(FloorGenerator), nameof(FloorGenerator.CreateProp))]
    internal static class LifeSupplyCreatePropRulePatch
    {
        private static bool Prefix(PropEntity prop) =>
            LifeSupplyRuleApplier.BeforeCreateProp(prop);
    }

    [HarmonyPatch]
    internal static class FloorGenerationRuleContextPatch
    {
        private const string GenerateInnerNativeMethod = "GenerateInner";

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Require(typeof(EnhancedProceduralFloorGenerator));
            yield return Require(typeof(FixedFloorGenerator));
            yield return Require(typeof(FullyDesignedFloorGenerator));
            yield return Require(typeof(LibraryFloorGenerator));
        }

        private static MethodInfo Require(Type type)
        {
            // GenerateInner is a protected native integration contract.
            MethodInfo method = AccessTools.DeclaredMethod(type,
                GenerateInnerNativeMethod);
            if (method == null || !typeof(IEnumerator).IsAssignableFrom(
                    method.ReturnType))
                throw new MissingMethodException(type.FullName,
                    GenerateInnerNativeMethod);
            return method;
        }

        private static void Postfix(FloorGenerator __instance,
            ref IEnumerator __result)
        {
            int participantCount = ServerParticipantCountReader.Read();
            bool required = MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.EnemyGroupDifficultyOffset, participantCount,
                    out _) ||
                MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor,
                    participantCount, out _);
            if (required && __result != null)
                __result = FloorGenerationContext.Wrap(__result, __instance);
        }
    }

    [HarmonyPatch(typeof(StageEntity), nameof(StageEntity.GenerateMonsterSpawnData))]
    internal static class EnemyGroupDifficultyOffsetRulePatch
    {
        private const string DifficultyUpBattleParameter = "DIFFICULTY_UP";

        private static void Prefix(ref int difficulty)
        {
            FloorGenerator generator = FloorGenerationContext.Current;
            int participantCount = ServerParticipantCountReader.Read();
            if (generator == null || DungeonManager.Instance == null ||
                !MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.EnemyGroupDifficultyOffset, participantCount,
                    out float configuredOffset) ||
                !DungeonManager.Instance.generatedFloors.TryGetValue(
                    generator.guid, out FloorData floor))
                return;

            int hardBattleOffset = generator.currentHardBattleMode ==
                DifficultyUpBattleParameter ? 1 : 0;
            difficulty = floor.difficulty + Mathf.RoundToInt(configuredOffset) +
                hardBattleOffset;
        }
    }
}
