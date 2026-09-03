#nullable enable
using System;

namespace SephiriaEnhancements.DefeatRetry
{
    internal sealed class RetryCheckpoints<T> where T : class
    {
        internal string? FloorGuid { get; private set; }
        internal T? FloorEntry { get; private set; }
        internal T? BossEncounter { get; private set; }
        internal bool BossEncounterStarted { get; private set; }

        internal bool EnterFloor(string floorGuid, T checkpoint)
        {
            bool sameFloor = string.Equals(FloorGuid, floorGuid, StringComparison.Ordinal);
            if (string.IsNullOrEmpty(floorGuid) || checkpoint == null ||
                (sameFloor && (FloorEntry != null || BossEncounterStarted)))
            {
                return false;
            }
            FloorGuid = floorGuid;
            FloorEntry = checkpoint;
            if (!sameFloor)
            {
                BossEncounter = null;
                BossEncounterStarted = false;
            }
            return true;
        }

        internal bool BeginBoss(string floorGuid, T? checkpoint)
        {
            if (string.IsNullOrEmpty(floorGuid) ||
                (checkpoint != null && (FloorEntry == null ||
                    !string.Equals(FloorGuid, floorGuid, StringComparison.Ordinal))) ||
                (BossEncounterStarted && string.Equals(FloorGuid, floorGuid, StringComparison.Ordinal)))
            {
                return false;
            }
            if (!string.Equals(FloorGuid, floorGuid, StringComparison.Ordinal))
            {
                FloorGuid = floorGuid;
                FloorEntry = null;
            }
            BossEncounter = checkpoint;
            BossEncounterStarted = true;
            return true;
        }

        internal void CompleteBossCapture(T checkpoint) => BossEncounter = checkpoint;

        internal T? Get(RetryCheckpointKind kind) =>
            kind == RetryCheckpointKind.FloorEntry ? FloorEntry :
            kind == RetryCheckpointKind.BossEncounter ? BossEncounter : null;

        internal void RestartFloor()
        {
            BossEncounter = null;
            BossEncounterStarted = false;
        }

        internal void Clear()
        {
            FloorGuid = null;
            FloorEntry = null;
            BossEncounter = null;
            BossEncounterStarted = false;
        }
    }
}
