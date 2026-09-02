using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.ModelChecks.Runtime.State;

internal static class EncounterLifecycleHubChecks
{
    internal static void Run()
    {
        var encounterLifecycleHub = new EncounterLifecycleHub();
        var encounterEvents = new List<EncounterLifecycleEvent>();
        encounterLifecycleHub.Changed += encounterEvents.Add;
        EncounterLifecycleEvent contextReset =
            encounterLifecycleHub.BeginGameplayContext(7, 1f);
        EncounterLifecycleEvent ordinaryCleared = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Ordinary,
                EncounterTransition.Cleared, 11, 0, 2f));
        if (!encounterLifecycleHub.IsOrdinaryEncounterCleared(11) ||
            encounterLifecycleHub.IsOrdinaryEncounterCleared(12) ||
            encounterLifecycleHub.IsOrdinaryEncounterCleared(0))
            throw new InvalidOperationException(
                "ordinary encounter clear state was not retained by source");
        EncounterLifecycleEvent bossStarted = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Started, 21, 0, 3f));
        EncounterLifecycleEvent bossCompletionStarted = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.CompletionStarted, 21, 0, 4f));
        EncounterLifecycleEvent continuationPrepared = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.ContinuationPrepared, 21, 22, 5f));
        EncounterLifecycleEvent continuationStarted = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Started, 22, 0, 6f));
        EncounterLifecycleEvent bossCleared = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Cleared, 22, 0, 7f));
        EncounterLifecycleEvent bossStartedForDefeat = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Started, 31, 0, 7.1f));
        EncounterLifecycleEvent bossDefeated = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Defeated, 31, 0, 7.2f));
        int publishedEncounterEventCount = encounterEvents.Count;
        EncounterLifecycleEvent invalidEncounterEvent = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.None,
                EncounterTransition.Cleared, 0, 0, 8f));
        if (contextReset.GameplayContextEpoch != 7 ||
            contextReset.Transition != EncounterTransition.GameplayContextReset ||
            ordinaryCleared.Kind != EncounterKind.Ordinary ||
            ordinaryCleared.Transition != EncounterTransition.Cleared ||
            bossStarted.Transition != EncounterTransition.Started ||
            bossCompletionStarted.Transition !=
                EncounterTransition.CompletionStarted ||
            continuationPrepared.Transition !=
                EncounterTransition.ContinuationPrepared ||
            continuationPrepared.SourceInstanceId != 22 ||
            continuationPrepared.PreviousSourceInstanceId != 21 ||
            continuationStarted.Transition != EncounterTransition.Resumed ||
            bossCleared.Transition != EncounterTransition.Cleared ||
            bossStartedForDefeat.Transition != EncounterTransition.Started ||
            bossDefeated.Transition != EncounterTransition.Defeated ||
            bossCleared.LifecycleRevision <= bossStarted.LifecycleRevision ||
            invalidEncounterEvent != bossDefeated ||
            encounterEvents.Count != publishedEncounterEventCount)
            throw new InvalidOperationException(
                "encounter lifecycle publication or continuation semantics failed");
        encounterLifecycleHub.Observe(new EncounterLifecycleObservation(
            EncounterKind.Boss, EncounterTransition.ContinuationPrepared,
            41, 42, 9f));
        EncounterLifecycleEvent nextContextReset =
            encounterLifecycleHub.BeginGameplayContext(8, 10f);
        EncounterLifecycleEvent startAfterContextReset = encounterLifecycleHub.Observe(
            new EncounterLifecycleObservation(EncounterKind.Boss,
                EncounterTransition.Started, 42, 0, 11f));
        if (nextContextReset.GameplayContextEpoch != 8 ||
            startAfterContextReset.GameplayContextEpoch != 8 ||
            startAfterContextReset.Transition != EncounterTransition.Started ||
            encounterLifecycleHub.IsOrdinaryEncounterCleared(11))
            throw new InvalidOperationException(
                "gameplay context reset must discard floor-bound encounter state");
        Console.WriteLine("EncounterLifecycleHub: context, clear, boss continuation and " +
            "invalid observation checks passed");
    }
}
