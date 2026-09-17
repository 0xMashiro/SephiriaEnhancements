namespace SephiriaEnhancements.DefeatRetry
{
    internal enum RetryCheckpointKind
    {
        None,
        FloorEntry,
        BossEncounter
    }

    internal enum RetryConclusionKind
    {
        Unknown,
        CombatDefeat,
        ForcedDefeat,
        ScriptedDefeat,
        Victory,
        Abandoned
    }

    internal static class DefeatRetryPolicy
    {
        internal static bool ShouldCaptureFloorEntryCheckpoint(
            bool enhancementsEnabled, bool retryEnabled, bool retrying,
            bool serverActive, bool hasCurrentSave, bool hasCurrentRunSave,
            bool runStarted)
        {
            return enhancementsEnabled && retryEnabled && !retrying &&
                serverActive && hasCurrentSave && hasCurrentRunSave && runStarted;
        }

        internal static bool ShouldCaptureBossEncounter(
            bool enhancementsEnabled, bool retryEnabled, bool retrying,
            bool serverActive, bool hasCurrentSave, bool hasCurrentRunSave,
            bool runStarted, bool hasFloor, bool hasBoss)
        {
            return ShouldCaptureFloorEntryCheckpoint(enhancementsEnabled,
                    retryEnabled, retrying, serverActive, hasCurrentSave,
                    hasCurrentRunSave, runStarted) && hasFloor && hasBoss;
        }

        internal static bool ShouldOffer(bool enhancementsEnabled, bool retryEnabled,
            bool hasCheckpoint, bool serverActive, bool runStarted,
            RetryConclusionKind conclusion,
            bool gaveUp, bool saveIdle, bool nativeRestarting)
        {
            return ShouldPresent(enhancementsEnabled, retryEnabled, serverActive,
                runStarted, conclusion, gaveUp) && hasCheckpoint && saveIdle && !nativeRestarting;
        }

        internal static bool ShouldPresent(bool enhancementsEnabled, bool retryEnabled,
            bool serverActive, bool runStarted, RetryConclusionKind conclusion, bool gaveUp) =>
            enhancementsEnabled && retryEnabled && serverActive && runStarted && !gaveUp &&
            conclusion == RetryConclusionKind.CombatDefeat;

        internal static bool ShouldApplyPlacement(bool restorePending,
            string checkpointFloorGuid, string requestedFloorGuid)
        {
            return restorePending && !string.IsNullOrEmpty(checkpointFloorGuid) &&
                string.Equals(checkpointFloorGuid, requestedFloorGuid,
                    System.StringComparison.Ordinal);
        }
    }
}
