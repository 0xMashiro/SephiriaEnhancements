using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Core
{
    internal sealed class BossEncounterTracker
    {
        private readonly Dictionary<long, float> damage = new Dictionary<long, float>(4);
        private float startedAt = -1000f;
        private float completedDuration;
        private float accumulatedDuration;
        private bool timing;

        internal IReadOnlyDictionary<long, float> Damage => damage;
        internal bool Active { get; private set; }
        internal bool IsTiming => timing;
        internal float Total { get; private set; }

        internal bool Begin(float now)
        {
            if (Active) return false;
            Reset();
            Active = true;
            startedAt = now;
            timing = true;
            return true;
        }

        internal bool Pause(float now)
        {
            if (!Active || !timing) return false;
            accumulatedDuration += Math.Max(0f, now - startedAt);
            timing = false;
            return true;
        }

        internal bool Resume(float now)
        {
            if (!Active || timing) return false;
            startedAt = now;
            timing = true;
            return true;
        }

        internal bool End(float now)
        {
            if (!Active) return false;
            if (timing) accumulatedDuration += Math.Max(0f, now - startedAt);
            completedDuration = accumulatedDuration;
            Active = false;
            timing = false;
            return true;
        }

        internal void Record(long playerKey, float amount)
        {
            if (!Active || amount <= 0f) return;
            damage.TryGetValue(playerKey, out float previous);
            damage[playerKey] = previous + amount;
            Total += amount;
        }

        internal float GetDamage(long playerKey) =>
            damage.TryGetValue(playerKey, out float value) ? value : 0f;

        internal float Elapsed(float now) => Active
            ? accumulatedDuration + (timing ? Math.Max(0f, now - startedAt) : 0f)
            : completedDuration;

        internal float AverageDps(long playerKey, float now)
        {
            float elapsed = Elapsed(now);
            return elapsed > 0f ? GetDamage(playerKey) / elapsed : 0f;
        }

        internal void Reset()
        {
            damage.Clear();
            Total = 0f;
            Active = false;
            startedAt = -1000f;
            completedDuration = 0f;
            accumulatedDuration = 0f;
            timing = false;
        }
    }
}
