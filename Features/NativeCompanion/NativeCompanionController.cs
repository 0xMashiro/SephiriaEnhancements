using Mirror;
using SephiriaEnhancements.Configuration;
using UnityEngine;

namespace SephiriaEnhancements.NativeCompanion
{
    internal sealed class NativeCompanionController : MonoBehaviour
    {
        private const float PollInterval = 0.5f;
        private const float ContextStabilityDelay = 2f;
        private const float ReturnDelay = 1f;
        private const float DiscoveryRetryDelay = 5f;

        private UnitAvatar companion;
        private NativeCompanion cachedCompanion;
        private NativeCompanionSessionKind observedSessionKind;
        private NativeCompanionSessionKind stableSessionKind;
        private int observedHumanPlayerCount;
        private int stableHumanPlayerCount;
        private bool sessionActive;
        private bool companionWasSpawned;
        private bool awaitingOutOfCombatReturn;
        private bool discoveryWarningLogged;
        private bool runtimeCompatible = true;
        private float observationChangedAt;
        private float nextPollAt;
        private float nextSpawnAt;

        internal void ResetSession()
        {
            Despawn();
            sessionActive = true;
            runtimeCompatible = true;
            cachedCompanion = default;
            discoveryWarningLogged = false;
            awaitingOutOfCombatReturn = false;
            ResetObservedContext();
            nextPollAt = 0f;
            nextSpawnAt = Time.unscaledTime + 1f;
        }

        internal void ResetFloor()
        {
            awaitingOutOfCombatReturn = false;
            ResetObservedContext();
            nextPollAt = 0f;
            nextSpawnAt = Time.unscaledTime + ReturnDelay;
        }

        private void Update()
        {
            if (!runtimeCompatible)
            {
                return;
            }

            try
            {
                Tick();
            }
            catch (System.Exception ex)
            {
                try { Despawn(); }
                catch { }
                runtimeCompatible = false;
                Debug.LogWarning("[SephiriaEnhancements] Combat companion disabled for this " +
                    "session: " + ex);
            }
        }

        private void Tick()
        {
            if (Time.unscaledTime < nextPollAt)
            {
                return;
            }

            nextPollAt = Time.unscaledTime + PollInterval;
            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer;
            if (companion == null && companionWasSpawned)
            {
                companionWasSpawned = false;
                awaitingOutOfCombatReturn = true;
            }
            else if (companion != null && companion.IsDead)
            {
                Despawn();
                awaitingOutOfCombatReturn = true;
            }
            else if (companion != null && companion.NetworkLeader != player)
            {
                Despawn();
                nextSpawnAt = Time.unscaledTime + ReturnDelay;
            }

            NativeCompanionSessionKind sessionKind = ObserveStableSession(player,
                out int humanPlayerCount);
            NativeCompanionPresence presence = NativeCompanionPolicy.Evaluate(
                EnhancementsSettings.Enabled, NativeCompanionSettings.Mode,
                NetworkServer.active, sessionActive, player != null,
                player != null && player.isServer, player != null && !player.IsDead,
                sessionKind, humanPlayerCount, companion != null,
                player != null && player.IsInBattle);

            if (presence == NativeCompanionPresence.Absent)
            {
                Despawn();
                awaitingOutOfCombatReturn = false;
                return;
            }

            if (presence == NativeCompanionPresence.Hold)
            {
                return;
            }

            if (companion != null)
            {
                return;
            }

            if (awaitingOutOfCombatReturn)
            {
                if (player.IsInBattle)
                {
                    return;
                }

                awaitingOutOfCombatReturn = false;
                nextSpawnAt = Time.unscaledTime + ReturnDelay;
            }

            if (Time.unscaledTime >= nextSpawnAt)
            {
                Spawn(player);
            }
        }

        private void Spawn(PlayerAvatar player)
        {
            NativeCompanion candidate = cachedCompanion.Prefab != null &&
                IsRegistered(cachedCompanion.Prefab)
                    ? cachedCompanion : FindNativeCompanion();
            if (candidate.Prefab == null)
            {
                if (!discoveryWarningLogged)
                {
                    Debug.LogWarning("[SephiriaEnhancements] Combat companion unavailable: " +
                        "no registered native companion prefab was found.");
                    discoveryWarningLogged = true;
                }

                nextSpawnAt = Time.unscaledTime + DiscoveryRetryDelay;
                return;
            }

            cachedCompanion = candidate;

            Vector3 position = player.transform.position + (Vector3)(Random.insideUnitCircle * 0.8f);
            GameObject instance = Object.Instantiate(candidate.Prefab, position,
                Quaternion.identity, player.transform.parent);
            UnitAI_NewBasic ai = instance.GetComponent<UnitAI_NewBasic>();
            UnitAvatar avatar = instance.GetComponent<UnitAvatar>();
            if (ai == null || avatar == null)
            {
                Object.Destroy(instance);
                nextSpawnAt = Time.unscaledTime + DiscoveryRetryDelay;
                return;
            }

            NetworkServer.Spawn(instance);
            instance.AddComponent<UnitAddon_CorpseRemover>();
            avatar.ChangeFaction(player.faction);
            avatar.SetLeader(player);
            // followerDamageId is the game's native damage-attribution field for
            // companions. Keep this API name at the integration boundary.
            avatar.followerDamageId = candidate.DamageAttributionId;
            companion = avatar;
            companionWasSpawned = true;
            discoveryWarningLogged = false;
            Debug.Log("[SephiriaEnhancements] Native combat companion spawned from prefab '" +
                candidate.Prefab.name + "'.");
        }

        private NativeCompanionSessionKind ObserveStableSession(PlayerAvatar player,
            out int humanPlayerCount)
        {
            NativeCompanionSessionKind current =
                NativeCompanionSessionClassifier.Classify(player,
                    out int currentHumanPlayerCount);
            if (current == NativeCompanionSessionKind.Unknown)
            {
                humanPlayerCount = 0;
                return NativeCompanionSessionKind.Unknown;
            }

            if (current != observedSessionKind ||
                currentHumanPlayerCount != observedHumanPlayerCount)
            {
                observedSessionKind = current;
                observedHumanPlayerCount = currentHumanPlayerCount;
                observationChangedAt = Time.unscaledTime;
            }
            else if ((stableSessionKind != observedSessionKind ||
                stableHumanPlayerCount != observedHumanPlayerCount) &&
                Time.unscaledTime - observationChangedAt >= ContextStabilityDelay)
            {
                stableSessionKind = observedSessionKind;
                stableHumanPlayerCount = observedHumanPlayerCount;
            }

            humanPlayerCount = stableHumanPlayerCount;
            return stableSessionKind;
        }

        private void ResetObservedContext()
        {
            observedSessionKind = NativeCompanionSessionKind.Unknown;
            stableSessionKind = NativeCompanionSessionKind.Unknown;
            observedHumanPlayerCount = 0;
            stableHumanPlayerCount = 0;
            observationChangedAt = Time.unscaledTime;
        }

        private static NativeCompanion FindNativeCompanion()
        {
            int[] itemIds = ItemDatabase.GetAllItemID();
            if (itemIds == null || NetworkManager.singleton == null)
            {
                return default;
            }

            NativeCompanion best = default;
            uint bestAssetId = uint.MaxValue;
            for (int index = 0; index < itemIds.Length; index++)
            {
                ItemEntity item = ItemDatabase.FindItemById(itemIds[index]);
                Charm_SummonUnit summon = item?.resourcePrefab?.GetComponent<Charm_SummonUnit>();
                GameObject prefab = summon?.unitPrefab;
                if (prefab == null || prefab.GetComponent<UnitAI_NewBasic>() == null ||
                    prefab.GetComponent<UnitAvatar>() == null || !IsRegistered(prefab))
                {
                    continue;
                }

                uint assetId = prefab.GetComponent<NetworkIdentity>().assetId;
                if (best.Prefab == null || assetId < bestAssetId ||
                    (assetId == bestAssetId && string.CompareOrdinal(
                        prefab.name, best.Prefab.name) < 0))
                {
                    best = new NativeCompanion(prefab, summon.followerDamageId);
                    bestAssetId = assetId;
                }
            }

            return best;
        }

        private static bool IsRegistered(GameObject prefab)
        {
            NetworkIdentity identity = prefab.GetComponent<NetworkIdentity>();
            if (identity == null || identity.assetId == 0)
            {
                return false;
            }

            System.Collections.Generic.List<GameObject> registered =
                NetworkManager.singleton.spawnPrefabs;
            for (int index = 0; index < registered.Count; index++)
            {
                NetworkIdentity registeredIdentity =
                    registered[index]?.GetComponent<NetworkIdentity>();
                if (registeredIdentity != null && registeredIdentity.assetId == identity.assetId)
                {
                    return true;
                }
            }

            return false;
        }

        private void Despawn()
        {
            companionWasSpawned = false;
            if (companion == null)
            {
                return;
            }

            UnitAvatar current = companion;
            companion = null;
            if (NetworkServer.active && current != null)
            {
                current.SetLeader(null);
                NetworkServer.Destroy(current.gameObject);
            }
        }

        private void OnDestroy()
        {
            sessionActive = false;
            Despawn();
        }

        private readonly struct NativeCompanion
        {
            internal NativeCompanion(GameObject prefab, string damageAttributionId)
            {
                Prefab = prefab;
                DamageAttributionId = damageAttributionId;
            }

            internal GameObject Prefab { get; }
            internal string DamageAttributionId { get; }
        }
    }
}
