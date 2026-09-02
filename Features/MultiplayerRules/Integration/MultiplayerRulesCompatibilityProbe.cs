using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesCompatibilityProbe
    {
        internal static bool Validate()
        {
            var missing = new List<string>();
            NativeContractProbe.RequireMethod(typeof(Money), "AddToInventory", missing,
                typeof(PlayerAvatar));
            NativeContractProbe.RequireMethod(typeof(SeedBossSpawner),
                "SpawnBoss", missing);
            NativeContractProbe.RequireMethod(typeof(HiddenRoomRewardSpawner),
                "SpawnProp", missing,
                typeof(GameObject), typeof(Vector3), typeof(System.Random));
            NativeContractProbe.RequireField(typeof(RandomEnemyPhaseSpawner),
                "MultiplayerLimit", missing);
            NativeContractProbe.RequireField(typeof(CommonEnemySpawner),
                "stageHPPercent", missing);
            NativeContractProbe.RequireField(typeof(PropDatabase),
                "hpBreakables", missing);
            // These native callback signatures are integration-boundary API.
            // Supplying the arguments avoids falsely probing for zero-argument
            // overloads when AccessTools performs an exact signature lookup.
            NativeContractProbe.RequireMethod(typeof(QTempleTrioAIController),
                "OnPhaseSpawnEnd", missing,
                typeof(List<UnitAvatar>));
            NativeContractProbe.RequireMethod(typeof(QTempleTrioAIController),
                "SetAIState", missing,
                typeof(IQTempleTrioAI), typeof(bool));
            NativeContractProbe.RequireField(typeof(QTempleTrioAIController),
                "ais", missing);
            NativeContractProbe.RequireField(typeof(QTempleTrioAIController),
                "isFullParty", missing);
            foreach (Type generatorType in new[]
            {
                typeof(EnhancedProceduralFloorGenerator),
                typeof(FixedFloorGenerator),
                typeof(FullyDesignedFloorGenerator),
                typeof(LibraryFloorGenerator)
            })
            {
                var generateInner = AccessTools.DeclaredMethod(generatorType,
                    "GenerateInner");
                if (generateInner == null ||
                    !typeof(IEnumerator).IsAssignableFrom(generateInner.ReturnType))
                    missing.Add(generatorType.Name + ".GenerateInner");
            }

            if (missing.Count == 0) return true;
            Debug.LogWarning("[SephiriaEnhancements] Multiplayer Rules native " +
                "contracts changed: " + string.Join(", ", missing));
            return false;
        }
    }
}
