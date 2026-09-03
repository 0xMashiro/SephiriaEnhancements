namespace SephiriaEnhancements.Core
{
    internal enum HitStreakImpact
    {
        Normal,
        Critical,
        Execution
    }

    internal readonly struct HitStreakUpdate
    {
        internal HitStreakUpdate(int count, int tier, bool milestone, bool animate)
        {
            Count = count;
            Tier = tier;
            IsMilestone = milestone;
            ShouldAnimate = animate;
        }

        internal int Count { get; }

        internal int Tier { get; }

        internal bool IsMilestone { get; }

        internal bool ShouldAnimate { get; }
    }

    internal sealed class HitStreakTracker
    {
        private const float HitStreakTimeout = 1.6f;
        private const float MinimumVisualGap = 0.12f;
        private float lastHitAt = -1000f;
        private float lastVisualAt = -1000f;

        internal int Count { get; private set; }

        internal int TotalDamage { get; private set; }

        internal HitStreakUpdate Register(float now, int damage, HitStreakImpact impact,
            bool indirectDamage)
        {
            if (damage <= 0 || indirectDamage)
            {
                return default;
            }

            if (now - lastHitAt > HitStreakTimeout)
            {
                Count = 0;
                TotalDamage = 0;
            }

            Count++;
            TotalDamage += damage;
            lastHitAt = now;

            bool milestone = IsMilestone(Count);
            bool cadence = Count >= 2 && (Count <= 5 ||
                (Count < 10 ? Count % 2 == 0 : Count < 25 ? Count % 3 == 0 : Count % 5 == 0));
            bool important = impact != HitStreakImpact.Normal || milestone;
            bool animate = Count >= 2 && (important || cadence) &&
                (milestone || now - lastVisualAt >= MinimumVisualGap);
            if (animate)
            {
                lastVisualAt = now;
            }

            return new HitStreakUpdate(Count, GetTier(Count), milestone, animate);
        }

        internal void Reset()
        {
            Count = 0;
            TotalDamage = 0;
            lastHitAt = -1000f;
            lastVisualAt = -1000f;
        }

        private static bool IsMilestone(int count) =>
            count == 10 || count == 25 || count == 50 || count == 100 ||
            (count > 100 && count % 100 == 0);

        private static int GetTier(int count)
        {
            if (count >= 100) return 4;
            if (count >= 50) return 3;
            if (count >= 25) return 2;
            return count >= 10 ? 1 : 0;
        }
    }
}
