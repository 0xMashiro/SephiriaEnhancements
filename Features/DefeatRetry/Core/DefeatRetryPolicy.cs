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
        ScriptedDefeat,
        Victory
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

        internal static bool ShouldCaptureRenderedCombatFloorFallback(
            bool enhancementsEnabled, bool retryEnabled, bool retrying,
            bool serverActive, bool hasCurrentSave, bool hasCurrentRunSave,
            bool runStarted, bool explorationActivated, bool combatThreat,
            bool checkpointMatchesFloor)
        {
            return ShouldCaptureFloorEntryCheckpoint(enhancementsEnabled,
                    retryEnabled, retrying, serverActive, hasCurrentSave,
                    hasCurrentRunSave, runStarted) &&
                !explorationActivated && combatThreat && !checkpointMatchesFloor;
        }

        internal static RetryConclusionKind ClassifyConclusion(
            int nativeGameOverType)
        {
            // Native type 2 is a story-directed defeat transition, not a party wipe.
            switch (nativeGameOverType)
            {
                case 0:
                    return RetryConclusionKind.CombatDefeat;
                case 2:
                    return RetryConclusionKind.ScriptedDefeat;
                case 1:
                case 3:
                case 4:
                case 5:
                case 6:
                    return RetryConclusionKind.Victory;
                default:
                    return RetryConclusionKind.Unknown;
            }
        }

        internal static bool ShouldOffer(bool enhancementsEnabled, bool retryEnabled,
            bool hasCheckpoint, bool serverActive, bool runStarted,
            int nativeGameOverType,
            bool gaveUp, bool saveIdle, bool nativeRestarting)
        {
            return enhancementsEnabled && retryEnabled &&
                hasCheckpoint && serverActive &&
                runStarted && ClassifyConclusion(nativeGameOverType) ==
                    RetryConclusionKind.CombatDefeat &&
                !gaveUp && saveIdle && !nativeRestarting;
        }

        internal static bool ShouldApplyPlacement(bool restorePending,
            string checkpointFloorGuid, string requestedFloorGuid)
        {
            return restorePending && !string.IsNullOrEmpty(checkpointFloorGuid) &&
                string.Equals(checkpointFloorGuid, requestedFloorGuid,
                    System.StringComparison.Ordinal);
        }
    }
}
