using System;

namespace SephiriaEnhancements.Core
{
    internal sealed class RollingDamageWindow
    {
        private const float BucketSeconds = 0.2f;
        private readonly Sample[] samples;
        private readonly float duration;
        private int head;
        private int count;
        private float rollingDamage;
        private float firstDamageAt;

        internal RollingDamageWindow(float durationSeconds, int capacity = 32)
        {
            if (durationSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            duration = durationSeconds;
            samples = new Sample[Math.Max(2, capacity)];
            Reset();
        }

        internal float Damage => rollingDamage;

        internal float Dps(float now)
        {
            Expire(now);
            if (rollingDamage <= 0f || firstDamageAt < 0f) return 0f;
            float elapsed = Math.Max(1f, Math.Min(duration, now - firstDamageAt));
            return rollingDamage / elapsed;
        }

        internal void Record(float now, float damage)
        {
            if (damage > 0f) Add(now, damage);
            Expire(now);
        }

        internal void Reset()
        {
            head = 0;
            count = 0;
            rollingDamage = 0f;
            firstDamageAt = -1f;
        }

        private void Add(float now, float damage)
        {
            long bucket = (long)Math.Floor(now / BucketSeconds);
            if (count > 0)
            {
                int tail = (head + count - 1) % samples.Length;
                Sample latest = samples[tail];
                if (latest.Bucket == bucket)
                {
                    samples[tail] = new Sample(latest.Time,
                        latest.Damage + damage, bucket);
                    rollingDamage += damage;
                    return;
                }
            }
            if (count == samples.Length)
            {
                rollingDamage -= samples[head].Damage;
                head = (head + 1) % samples.Length;
                count--;
            }
            int index = (head + count) % samples.Length;
            samples[index] = new Sample(now, damage, bucket);
            count++;
            rollingDamage += damage;
            if (firstDamageAt < 0f) firstDamageAt = now;
        }

        private void Expire(float now)
        {
            float cutoff = now - duration;
            while (count > 0 && samples[head].Time < cutoff)
            {
                rollingDamage -= samples[head].Damage;
                head = (head + 1) % samples.Length;
                count--;
            }
            if (count == 0)
            {
                rollingDamage = 0f;
                firstDamageAt = -1f;
            }
        }

        private readonly struct Sample
        {
            internal Sample(float time, float damage, long bucket)
            {
                Time = time;
                Damage = damage;
                Bucket = bucket;
            }
            internal float Time { get; }
            internal float Damage { get; }
            internal long Bucket { get; }
        }
    }
}
