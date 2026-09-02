using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class EnemySpawnRoutineRuleScope
    {
        // MultiplayerLimit is a private native integration contract. It is changed
        // only while the native iterator advances and is restored in the same frame.
        private static readonly FieldInfo RandomEncounterLimits = AccessTools.Field(
            typeof(RandomEnemyPhaseSpawner), "MultiplayerLimit");

        internal static IDisposable Enter(EnemySpawnRoutineFrame frame)
        {
            if (frame?.Origin != EnemySpawnOrigin.RandomEncounter ||
                !MultiplayerRulesController.TryGetActiveOverride(
                    MultiplayerRuleId.RandomEncounterLivingEnemyLimit,
                    ServerParticipantCountReader.Read(), out float configuredLimit))
            {
                return null;
            }

            int[] limits = RandomEncounterLimits?.GetValue(null) as int[];
            if (limits == null || limits.Length == 0) return null;
            int nativeParticipantIndex = Mathf.Clamp(
                PlayerSpawner.MultiplayerList.Count - 1, 0, limits.Length - 1);
            int previous = limits[nativeParticipantIndex];
            limits[nativeParticipantIndex] = Mathf.RoundToInt(configuredLimit);
            return new RestoreLimit(limits, nativeParticipantIndex, previous);
        }

        private sealed class RestoreLimit : IDisposable
        {
            private readonly int[] limits;
            private readonly int index;
            private readonly int previous;
            private bool disposed;

            internal RestoreLimit(int[] limits, int index, int previous)
            {
                this.limits = limits;
                this.index = index;
                this.previous = previous;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                limits[index] = previous;
            }
        }
    }
}
