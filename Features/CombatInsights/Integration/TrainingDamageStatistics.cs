using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    // Native DamageKey and followerDamageId stay at the game boundary. Training
    // never writes the native exploration dictionaries or consumes damage RPCs.
    internal sealed class TrainingDamageStatistics : MonoBehaviour
    {
        private sealed class PlayerTraining
        {
            internal string Floor;
            internal long Generation;
            internal readonly Dictionary<DamageKey, float> Damage = new Dictionary<DamageKey, float>();
        }

        internal static TrainingDamageStatistics Instance { get; private set; }
        internal static bool Available => Instance != null && EnhancementsSettings.Enabled &&
            ModSettings.DisplayPolicy != CombatInsightsDisplayPolicy.Disabled &&
            FeatureFailure.IsAvailable(FeatureId.CombatInsights);
        private readonly Dictionary<PlayerAvatar, PlayerTraining> players = new Dictionary<PlayerAvatar, PlayerTraining>();
        private readonly List<PlayerAvatar> removed = new List<PlayerAvatar>();
        private DungeonManager world;
        private int worldSerial;
        private float nextTick;
        private static long nextGeneration;

        private void Awake()
        {
            Instance = this;
            TrainingStatisticsBridge.Initialize();
        }

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights)) { Shutdown(); return; }
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + 0.2f;
            FeatureFailure.Run(FeatureId.CombatInsights, () =>
            {
                ObserveWorld();
                TrainingStatisticsBridge.Tick();
                removed.Clear();
                foreach (var pair in players)
                    if (!Ready(pair.Key) || pair.Value.Floor != pair.Key.NetworkcurrentFloorGuid)
                        removed.Add(pair.Key);
                foreach (var player in removed) players.Remove(player);
            });
        }

        private void ObserveWorld()
        {
            DungeonManager current = DungeonManager.Instance;
            int serial = current != null ? current.sessionSerial : 0;
            if (!NetworkServer.active || !Available || current != world || serial != worldSerial)
                players.Clear();
            world = current;
            worldSerial = serial;
        }

        internal static bool Ready(PlayerAvatar player) => player != null &&
            player.loadingScreenType == -1 && !string.IsNullOrEmpty(player.NetworkcurrentFloorGuid);

        internal void Record(PlayerAvatar owner, UnitAvatar victim, DamageInstance damage, UnitAvatar follower = null)
        {
            if (!NetworkServer.active || !Available || !Ready(owner) || victim == null ||
                victim.monsterType != EMonsterType.Dummy || damage == null || damage.damageResult <= 0) return;
            string source = follower == null ? damage.id :
                string.IsNullOrWhiteSpace(follower.followerDamageId) ? "Companion_Unknown" : follower.followerDamageId;
            if (string.IsNullOrWhiteSpace(source)) return;
            ObserveWorld();
            PlayerTraining training = Get(owner);
            var key = new DamageKey(source, damage.elementalType);
            training.Damage.TryGetValue(key, out float previous);
            training.Damage[key] = previous + damage.damageResult;
        }

        private PlayerTraining Get(PlayerAvatar player)
        {
            if (!players.TryGetValue(player, out var training) || training.Floor != player.NetworkcurrentFloorGuid)
            {
                training = new PlayerTraining { Floor = player.NetworkcurrentFloorGuid, Generation = ++nextGeneration };
                players[player] = training;
            }
            return training;
        }

        internal Dictionary<DamageKey, float> Capture(PlayerAvatar player)
        {
            ObserveWorld();
            return NetworkServer.active && Available && Ready(player)
                ? new Dictionary<DamageKey, float>(Get(player).Damage) : null;
        }

        internal long GetGeneration(PlayerAvatar player)
        {
            ObserveWorld();
            return NetworkServer.active && Available && Ready(player) ? Get(player).Generation : 0;
        }

        internal void Clear(PlayerAvatar player)
        {
            if (NetworkServer.active && player != null) players.Remove(player);
        }

        internal void Shutdown()
        {
            if (Instance != this) return;
            Instance = null;
            enabled = false;
            players.Clear();
            TrainingStatisticsBridge.Shutdown();
            foreach (var panel in UnityEngine.Object.FindObjectsByType<NativeDamageSourcesPanel>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)) panel.Dispose();
        }

        private void OnDestroy() => Shutdown();
    }
}
