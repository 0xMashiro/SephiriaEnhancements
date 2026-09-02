namespace SephiriaEnhancements.Combat
{
    internal enum ReportDisplayState
    {
        Closed,
        Visible,
        Paused,
        Expired,
        Dismissed,
        CombatStarted
    }

    internal sealed class ReportDisplayWindow
    {
        private float endsAt = -1f;
        private float unavailableSince = -1f;
        private ReportDisplayState closedState = ReportDisplayState.Closed;

        internal bool HasStarted => endsAt >= 0f;
        internal bool IsPaused => unavailableSince >= 0f;
        internal bool IsOpen(float now) => unavailableSince >= 0f ||
            now <= endsAt;
        internal bool IsVisible(float now) => unavailableSince < 0f &&
            now <= endsAt;

        internal ReportDisplayState State(float now) => !HasStarted ? closedState
            : IsPaused ? ReportDisplayState.Paused
            : IsVisible(now) ? ReportDisplayState.Visible : ReportDisplayState.Expired;

        internal void Start(float now, float duration)
        {
            endsAt = now + duration;
            unavailableSince = -1f;
        }

        internal void OpenUntilDismissed()
        {
            endsAt = float.PositiveInfinity;
            unavailableSince = -1f;
        }

        internal void CloseForEncounter(bool bossActive, bool ordinaryActive,
            bool hasContribution)
        {
            if (HasStarted && (bossActive || (ordinaryActive && hasContribution)))
                Clear(ReportDisplayState.CombatStarted);
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

        internal void Clear(ReportDisplayState state = ReportDisplayState.Closed)
        {
            endsAt = -1f;
            unavailableSince = -1f;
            closedState = state;
        }
    }
}
