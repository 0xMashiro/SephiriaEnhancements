#nullable disable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Core
{
    internal sealed class FloorCombatStatistics
    {
        private readonly Dictionary<long, CombatStatisticsPlayerSnapshot> players =
            new Dictionary<long, CombatStatisticsPlayerSnapshot>();
        private readonly Dictionary<EncounterDamageType, float> damageTypes =
            new Dictionary<EncounterDamageType, float>();
        private readonly EncounterDefeatTracker defeats = new EncounterDefeatTracker();
        private string floorGuid;
        private float duration, lastTime;
        private bool timing;

        internal string FloorGuid => floorGuid;

        internal void CopyFrom(FloorCombatStatistics source)
        {
            Clear();
            floorGuid = source.floorGuid;
            duration = source.duration;
            foreach (var pair in source.players) players.Add(pair.Key, pair.Value);
            foreach (var pair in source.damageTypes) damageTypes.Add(pair.Key, pair.Value);
            defeats.CopyFrom(source.defeats);
        }

        internal void ObserveFloor(string guid)
        {
            if (string.IsNullOrEmpty(guid) || guid == floorGuid) return;
            Clear();
            floorGuid = guid;
        }

        // One clock for the locally observed fight, shared by every player.
        // The caller supplies game time, so an actual pause adds no duration.
        internal void UpdateClock(float now, bool active)
        {
            if (timing) duration += Math.Max(0f, now - lastTime);
            lastTime = now;
            timing = active;
        }

        internal void RecordDamage(long key, string name, bool isLocal,
            float damage, EncounterDamageType type)
        {
            if (damage <= 0f) return;
            players.TryGetValue(key, out CombatStatisticsPlayerSnapshot previous);
            players[key] = new CombatStatisticsPlayerSnapshot(key, name, isLocal,
                (previous?.Damage ?? 0f) + damage);
            damageTypes.TryGetValue(type, out float total);
            damageTypes[type] = total + damage;
        }

        internal void RecordDefeat(uint identity, EncounterEnemyTier tier) =>
            defeats.RecordDefeat(identity, tier);

        internal void RecordLocalFinalBlow() => defeats.RecordLocalFinalBlow();

        internal CombatStatisticsSnapshot Capture()
        {
            var rows = new List<CombatStatisticsPlayerSnapshot>(players.Values);
            rows.Sort((left, right) =>
            {
                int damage = right.Damage.CompareTo(left.Damage);
                if (damage != 0) return damage;
                if (left.IsLocal != right.IsLocal) return left.IsLocal ? -1 : 1;
                return left.Key.CompareTo(right.Key);
            });
            var mix = new List<CombatStatisticsDamageTypeSnapshot>();
            foreach (var pair in damageTypes)
                mix.Add(new CombatStatisticsDamageTypeSnapshot(pair.Key, pair.Value));
            mix.Sort((left, right) => right.Damage.CompareTo(left.Damage));
            return new CombatStatisticsSnapshot(rows, duration, defeats.NormalDefeated,
                defeats.MinibossDefeated, defeats.BossDefeated,
                defeats.LocalFinalBlows, mix);
        }

        internal void Clear()
        {
            players.Clear();
            damageTypes.Clear();
            defeats.Reset();
            floorGuid = null;
            duration = lastTime = 0f;
            timing = false;
        }
    }
}
