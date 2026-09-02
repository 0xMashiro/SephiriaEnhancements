using System;
using System.Collections.Generic;
using SephiriaEnhancements.Core;
using UnityEngine;

namespace SephiriaEnhancements.Combat
{
    internal sealed class EncounterAreaLocator
    {
        private const float RescanInterval = 1f;
        private const float PopulatedRescanInterval = 2f;
        private readonly List<EncounterScope> scopes = new List<EncounterScope>(32);
        private EncounterScope lastScope;
        private string cachedFloorGuid;
        private float nextRescanAt;

        internal bool TryLocate(PlayerAvatar player, out EncounterScope scope)
        {
            scope = null;
            if (player == null || string.IsNullOrEmpty(player.NetworkcurrentFloorGuid)) return false;
            string floorGuid = player.NetworkcurrentFloorGuid;
            Vector3 position = player.transform.position;
            if (lastScope != null && lastScope.Contains(position.x, position.y) &&
                string.Equals(lastScope.FloorGuid, floorGuid, StringComparison.Ordinal))
            {
                scope = lastScope;
                return true;
            }

            float now = Time.unscaledTime;
            if (!string.Equals(cachedFloorGuid, floorGuid, StringComparison.Ordinal) ||
                now >= nextRescanAt)
            {
                Refresh(floorGuid, now);
            }

            EncounterScope selected = null;
            for (int index = 0; index < scopes.Count; index++)
            {
                selected = EncounterScope.SelectContaining(selected, scopes[index],
                    position.x, position.y);
            }

            lastScope = selected;
            scope = selected;
            return scope != null;
        }

        internal void Reset()
        {
            scopes.Clear();
            lastScope = null;
            cachedFloorGuid = null;
            nextRescanAt = 0f;
        }

        internal void InvalidateSelection()
        {
            lastScope = null;
        }

        private void Refresh(string floorGuid, float now)
        {
            scopes.Clear();
            BossSpawner[] bossSpawners = UnityEngine.Object.FindObjectsByType<BossSpawner>(
                FindObjectsSortMode.None);
            for (int index = 0; index < bossSpawners.Length; index++)
            {
                BossSpawner spawner = bossSpawners[index];
                if (spawner == null || !spawner.gameObject.activeInHierarchy) continue;
                Vector2 origin = spawner.transform.position;
                Add(floorGuid, spawner.GetInstanceID(), EncounterScopeKind.Boss,
                    origin + spawner.playerPreventArea_lb,
                    origin + spawner.playerPreventArea_rt);
            }
            SeedBossSpawner[] seedBossSpawners =
                UnityEngine.Object.FindObjectsByType<SeedBossSpawner>(
                    FindObjectsSortMode.None);
            for (int index = 0; index < seedBossSpawners.Length; index++)
            {
                SeedBossSpawner spawner = seedBossSpawners[index];
                if (spawner == null || !spawner.gameObject.activeInHierarchy) continue;
                Vector2 origin = spawner.transform.position;
                Add(floorGuid, spawner.GetInstanceID(), EncounterScopeKind.Boss,
                    origin + spawner.playerPreventArea_lb,
                    origin + spawner.playerPreventArea_rt);
            }
            RandomEnemyPhaseSpawner[] randomSpawners =
                UnityEngine.Object.FindObjectsByType<RandomEnemyPhaseSpawner>(
                    FindObjectsSortMode.None);
            for (int index = 0; index < randomSpawners.Length; index++)
            {
                RandomEnemyPhaseSpawner spawner = randomSpawners[index];
                if (spawner == null || !spawner.gameObject.activeInHierarchy) continue;
                Add(floorGuid, spawner.GetInstanceID(), EncounterScopeKind.Ordinary,
                    spawner.NetworkdetectArea_lb, spawner.NetworkdetectArea_rt);
            }
            EnemySpawner[] fixedSpawners = UnityEngine.Object.FindObjectsByType<EnemySpawner>(
                FindObjectsSortMode.None);
            for (int index = 0; index < fixedSpawners.Length; index++)
            {
                EnemySpawner spawner = fixedSpawners[index];
                if (spawner == null || !spawner.gameObject.activeInHierarchy) continue;
                Vector2 origin = spawner.transform.position;
                Add(floorGuid, spawner.GetInstanceID(), EncounterScopeKind.Ordinary,
                    origin + spawner.NetworkplayerPreventArea_lb,
                    origin + spawner.NetworkplayerPreventArea_rt);
            }
            CommonEnemySpawner[] commonSpawners =
                UnityEngine.Object.FindObjectsByType<CommonEnemySpawner>(
                    FindObjectsSortMode.None);
            for (int index = 0; index < commonSpawners.Length; index++)
            {
                CommonEnemySpawner spawner = commonSpawners[index];
                if (spawner == null || !spawner.gameObject.activeInHierarchy) continue;
                Vector2 origin = spawner.transform.position;
                Add(floorGuid, spawner.GetInstanceID(), EncounterScopeKind.Ordinary,
                    origin + spawner.NetworkplayerPreventArea_lb,
                    origin + spawner.NetworkplayerPreventArea_rt);
            }
            cachedFloorGuid = floorGuid;
            nextRescanAt = now + (scopes.Count == 0
                ? RescanInterval : PopulatedRescanInterval);
        }

        private void Add(string floorGuid, int sourceInstanceId,
            EncounterScopeKind kind,
            Vector2 lower, Vector2 upper)
        {
            EncounterScope candidate = EncounterScope.Create(floorGuid,
                sourceInstanceId, kind, lower.x, lower.y, upper.x, upper.y);
            if (candidate != null) scopes.Add(candidate);
        }
    }
}
