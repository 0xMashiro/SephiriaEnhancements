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

    internal sealed class EncounterReportSnapshot : CombatStatisticsSnapshot
    {
        internal EncounterReportSnapshot(EncounterReportKind kind,
            IReadOnlyList<CombatStatisticsPlayerSnapshot> players,
            float duration, int normalDefeated, int minibossDefeated,
            int bossDefeated, int localFinalBlows,
            IReadOnlyList<CombatStatisticsDamageTypeSnapshot> damageTypes)
            : base(players, duration, normalDefeated, minibossDefeated,
                bossDefeated, localFinalBlows, damageTypes)
        {
            Kind = kind;
        }

        internal EncounterReportKind Kind { get; }
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
