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
        private float displaySeconds;
        private ReportDisplayState closedState = ReportDisplayState.Closed;

        internal bool ShowFloorStatistics { get; private set; }
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
            ShowFloorStatistics = false;
            displaySeconds = duration;
            endsAt = now + duration;
            unavailableSince = -1f;
        }

        internal bool TrySelectPage(bool floor, float now)
        {
            if (!IsVisible(now) || ShowFloorStatistics == floor) return false;
            ShowFloorStatistics = floor;
            endsAt = now + (floor ? System.Math.Max(8f, displaySeconds) : displaySeconds);
            return true;
        }

        internal bool TryDismiss(float now)
        {
            if (!IsVisible(now)) return false;
            Clear(ReportDisplayState.Dismissed);
            return true;
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
            ShowFloorStatistics = false;
            displaySeconds = 0f;
            endsAt = -1f;
            unavailableSince = -1f;
            closedState = state;
        }
    }
}
