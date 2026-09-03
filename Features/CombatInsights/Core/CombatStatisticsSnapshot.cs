using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Core
{
    internal sealed class CombatStatisticsPlayerSnapshot
    {
        internal CombatStatisticsPlayerSnapshot(long key, string name,
            bool isLocal, float damage)
        {
            Key = key;
            Name = name ?? string.Empty;
            IsLocal = isLocal;
            Damage = Math.Max(0f, damage);
        }

        internal long Key { get; }
        internal string Name { get; }
        internal bool IsLocal { get; }
        internal float Damage { get; }
    }

    internal sealed class CombatStatisticsDamageTypeSnapshot
    {
        internal CombatStatisticsDamageTypeSnapshot(EncounterDamageType type,
            float damage)
        {
            Type = type;
            Damage = Math.Max(0f, damage);
        }

        internal EncounterDamageType Type { get; }
        internal float Damage { get; }
    }

    internal class CombatStatisticsSnapshot
    {
        private readonly CombatStatisticsPlayerSnapshot[] players;
        private readonly CombatStatisticsDamageTypeSnapshot[] damageTypes;

        internal CombatStatisticsSnapshot(
            IReadOnlyList<CombatStatisticsPlayerSnapshot> players,
            float duration, int normalDefeated, int minibossDefeated,
            int bossDefeated, int localFinalBlows,
            IReadOnlyList<CombatStatisticsDamageTypeSnapshot> damageTypes)
        {
            Duration = Math.Max(0f, duration);
            NormalDefeated = Math.Max(0, normalDefeated);
            MinibossDefeated = Math.Max(0, minibossDefeated);
            BossDefeated = Math.Max(0, bossDefeated);
            LocalFinalBlows = Math.Max(0, localFinalBlows);
            this.players = new CombatStatisticsPlayerSnapshot[
                players.Count];
            for (int index = 0; index < this.players.Length; index++)
            {
                CombatStatisticsPlayerSnapshot player = players[index];
                this.players[index] = new CombatStatisticsPlayerSnapshot(
                    player.Key, player.Name, player.IsLocal, player.Damage);
                TotalDamage += this.players[index].Damage;
            }
            this.damageTypes = new CombatStatisticsDamageTypeSnapshot[
                damageTypes.Count];
            for (int index = 0; index < this.damageTypes.Length; index++)
            {
                CombatStatisticsDamageTypeSnapshot type = damageTypes[index];
                this.damageTypes[index] =
                    new CombatStatisticsDamageTypeSnapshot(type.Type,
                        type.Damage);
            }
        }

        internal IReadOnlyList<CombatStatisticsPlayerSnapshot> Players => players;
        internal IReadOnlyList<CombatStatisticsDamageTypeSnapshot> DamageTypes =>
            damageTypes;
        internal float Duration { get; }
        internal float TotalDamage { get; }
        internal int NormalDefeated { get; }
        internal int MinibossDefeated { get; }
        internal int BossDefeated { get; }
        internal int LocalFinalBlows { get; }
        internal int DefeatedCount =>
            NormalDefeated + MinibossDefeated + BossDefeated;
    }

}
