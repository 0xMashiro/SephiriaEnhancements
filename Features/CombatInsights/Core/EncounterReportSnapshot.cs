using System;
using System.Collections.Generic;
using System.Text;

namespace SephiriaEnhancements.Core
{
    internal enum EncounterDamageType
    {
        Unknown,
        Physical,
        Fire,
        Ice,
        Lightning,
        Chaos,
        Normal,
        Mixed
    }

    internal enum EncounterReportKind
    {
        Ordinary,
        Boss
    }

    internal sealed class EncounterReportPlayerSnapshot
    {
        internal EncounterReportPlayerSnapshot(long key, string name,
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

    internal sealed class EncounterReportDamageTypeSnapshot
    {
        internal EncounterReportDamageTypeSnapshot(EncounterDamageType type,
            float damage)
        {
            Type = type;
            Damage = Math.Max(0f, damage);
        }

        internal EncounterDamageType Type { get; }
        internal float Damage { get; }
    }

    internal sealed class EncounterReportSnapshot
    {
        private readonly EncounterReportPlayerSnapshot[] players;
        private readonly EncounterReportDamageTypeSnapshot[] damageTypes;

        internal EncounterReportSnapshot(EncounterReportKind kind,
            IReadOnlyList<EncounterReportPlayerSnapshot> players,
            float duration, int normalDefeated, int minibossDefeated,
            int bossDefeated, int localFinalBlows,
            IReadOnlyList<EncounterReportDamageTypeSnapshot> damageTypes)
        {
            Kind = kind;
            Duration = Math.Max(0f, duration);
            NormalDefeated = Math.Max(0, normalDefeated);
            MinibossDefeated = Math.Max(0, minibossDefeated);
            BossDefeated = Math.Max(0, bossDefeated);
            LocalFinalBlows = Math.Max(0, localFinalBlows);
            this.players = new EncounterReportPlayerSnapshot[
                Math.Min(players.Count, 4)];
            for (int index = 0; index < this.players.Length; index++)
            {
                EncounterReportPlayerSnapshot player = players[index];
                this.players[index] = new EncounterReportPlayerSnapshot(
                    player.Key, player.Name, player.IsLocal, player.Damage);
                TotalDamage += this.players[index].Damage;
            }
            this.damageTypes = new EncounterReportDamageTypeSnapshot[
                damageTypes.Count];
            for (int index = 0; index < this.damageTypes.Length; index++)
            {
                EncounterReportDamageTypeSnapshot type = damageTypes[index];
                this.damageTypes[index] =
                    new EncounterReportDamageTypeSnapshot(type.Type,
                        type.Damage);
            }
        }

        internal EncounterReportKind Kind { get; }
        internal IReadOnlyList<EncounterReportPlayerSnapshot> Players => players;
        internal IReadOnlyList<EncounterReportDamageTypeSnapshot> DamageTypes =>
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

    internal static class EncounterReportPresentationPolicy
    {
        internal static float DisplaySeconds(EncounterReportSnapshot report)
        {
            if (report == null) return 0f;
            if (report.Kind == EncounterReportKind.Boss) return 8f;
            if (report.Players.Count > 1 || report.MinibossDefeated > 0 ||
                report.BossDefeated > 0 || report.Duration >= 8f)
                return 6f;
            return 4.5f;
        }
    }

    internal static class CombatInsightsText
    {
        internal static string SingleLinePlayerName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Player";
            var result = new StringBuilder(value.Length);
            bool previousWasSpace = false;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                bool isSpace = char.IsWhiteSpace(character) ||
                    char.IsControl(character);
                if (isSpace)
                {
                    if (!previousWasSpace && result.Length > 0)
                    {
                        result.Append(' ');
                        previousWasSpace = true;
                    }
                    continue;
                }

                result.Append(character);
                previousWasSpace = false;
            }

            string sanitized = result.ToString().Trim();
            return sanitized.Length == 0 ? "Player" : sanitized;
        }
    }
}
