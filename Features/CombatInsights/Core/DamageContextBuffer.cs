using System;

namespace SephiriaEnhancements.Core
{
    internal sealed class DamageContextBuffer
    {
        private const int Capacity = 48;
        private const float MatchWindow = 0.6f;
        private const float MatchDistanceSquared = 0.16f;
        private readonly Entry[] entries = new Entry[Capacity];
        private int next;

        internal void Record(float now, int targetId, int damage, float x,
            float y, bool indirectDamage,
            EncounterDamageType damageType = EncounterDamageType.Unknown)
        {
            if (targetId == 0 || damage <= 0)
            {
                return;
            }

            entries[next] = new Entry
            {
                Time = now,
                TargetId = targetId,
                Damage = damage,
                X = x,
                Y = y,
                IndirectDamage = indirectDamage,
                DamageType = damageType,
                Available = true
            };
            next = (next + 1) % entries.Length;
        }

        internal bool TryMatch(float now, int targetId, int damage, float x, float y,
            out bool indirectDamage)
        {
            indirectDamage = false;
            if (!TryTake(now, targetId, damage, x, y, out Entry match))
                return false;
            indirectDamage = match.IndirectDamage;
            return true;
        }

        internal bool TryMatchDamageType(float now, int targetId, int damage,
            float x, float y, out EncounterDamageType damageType)
        {
            damageType = EncounterDamageType.Unknown;
            if (!TryTake(now, targetId, damage, x, y, out Entry match))
                return false;
            damageType = match.DamageType;
            return true;
        }

        private bool TryTake(float now, int targetId, int damage, float x,
            float y, out Entry match)
        {
            match = default;
            int bestIndex = -1;
            float bestScore = float.MaxValue;
            for (int offset = 1; offset <= entries.Length; offset++)
            {
                int index = (next - offset + entries.Length) % entries.Length;
                ref Entry candidate = ref entries[index];
                float age = now - candidate.Time;
                if (!candidate.Available || age < 0f || age > MatchWindow ||
                    candidate.TargetId != targetId || candidate.Damage != damage)
                {
                    continue;
                }

                float dx = candidate.X - x;
                float dy = candidate.Y - y;
                float distance = dx * dx + dy * dy;
                if (distance > MatchDistanceSquared)
                {
                    continue;
                }

                float score = age + distance;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = index;
                }
            }

            if (bestIndex < 0)
            {
                return false;
            }

            entries[bestIndex].Available = false;
            match = entries[bestIndex];
            return true;
        }

        internal void Clear()
        {
            Array.Clear(entries, 0, entries.Length);
            next = 0;
        }

        private struct Entry
        {
            internal float Time;
            internal int TargetId;
            internal int Damage;
            internal float X;
            internal float Y;
            internal bool IndirectDamage;
            internal EncounterDamageType DamageType;
            internal bool Available;
        }
    }
}
