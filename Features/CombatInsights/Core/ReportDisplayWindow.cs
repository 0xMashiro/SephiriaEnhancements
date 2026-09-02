namespace SephiriaEnhancements.Combat
{
    internal sealed class ReportDisplayWindow
    {
        private float endsAt = -1f;
        private float unavailableSince = -1f;

        internal bool HasStarted => endsAt >= 0f;
        internal bool IsPaused => unavailableSince >= 0f;
        internal bool IsOpen(float now) => unavailableSince >= 0f ||
            now <= endsAt;
        internal bool IsVisible(float now) => unavailableSince < 0f &&
            now <= endsAt;

        internal void Start(float now, float duration)
        {
            endsAt = now + duration;
            unavailableSince = -1f;
        }

        internal void SetPresentationAvailable(bool available, float now)
        {
            if (!available)
            {
                if (unavailableSince < 0f && now <= endsAt)
                    unavailableSince = now;
                return;
            }

            if (unavailableSince < 0f) return;
            endsAt += now - unavailableSince;
            unavailableSince = -1f;
        }

        internal void Clear()
        {
            endsAt = -1f;
            unavailableSince = -1f;
        }
    }
}
