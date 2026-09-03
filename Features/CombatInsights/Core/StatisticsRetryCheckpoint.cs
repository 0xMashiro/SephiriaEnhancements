#nullable disable
namespace SephiriaEnhancements.Core
{
    internal enum StatisticsRetryTransition : byte
    {
        CaptureBoss,
        RetryBoss,
        RetryFloor,
        Cancel
    }

    // This checkpoint contains this peer's observations, not a team-wide history.
    internal sealed class StatisticsRetryCheckpoint
    {
        private readonly FloorCombatStatistics baseline = new FloorCombatStatistics();
        private long checkpointId;
        private uint localPlayerId;
        private string floorGuid;
        private bool restoreBoss, worldLoaded, travelStarted;

        internal bool Pending { get; private set; }

        internal void Capture(long id, uint playerId, FloorCombatStatistics statistics)
        {
            if (Pending || id == checkpointId || playerId == 0) return;
            baseline.CopyFrom(statistics);
            checkpointId = id;
            localPlayerId = playerId;
        }

        internal void Begin(bool boss, long id, string floor)
        {
            if (Pending) return;
            Pending = true;
            worldLoaded = false;
            travelStarted = false;
            floorGuid = floor;
            restoreBoss = boss && id == checkpointId && baseline.FloorGuid == floor;
            if (!boss) checkpointId = 0;
        }

        internal void ObserveWorldLoaded()
        {
            if (Pending) worldLoaded = true;
            else Clear();
        }

        internal void ObserveTravelStarted()
        {
            if (Pending) travelStarted = true;
        }

        internal bool TryRestore(string floor, uint playerId, bool ready,
            FloorCombatStatistics statistics)
        {
            if (!Pending || !worldLoaded || !travelStarted || !ready) return false;
            statistics.Clear();
            if (restoreBoss && floor == floorGuid && playerId == localPlayerId)
                statistics.CopyFrom(baseline);
            statistics.ObserveFloor(floor);
            Pending = false;
            return true;
        }

        internal void Cancel()
        {
            Pending = false;
            worldLoaded = false;
            travelStarted = false;
        }

        internal void Clear()
        {
            Cancel();
            baseline.Clear();
            checkpointId = 0;
            localPlayerId = 0;
            floorGuid = null;
        }
    }
}
