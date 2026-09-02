#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Runtime
{
    internal enum EncounterKind
    {
        None,
        Ordinary,
        Boss
    }

    internal enum EncounterTransition
    {
        None,
        GameplayContextReset,
        Started,
        Paused,
        Resumed,
        CompletionStarted,
        ContinuationPrepared,
        Cleared,
        Defeated
    }

    internal sealed class EncounterLifecycleEvent
    {
        internal const int CurrentContractVersion = 1;

        internal EncounterLifecycleEvent(long gameplayContextEpoch,
            long lifecycleRevision, EncounterKind kind,
            EncounterTransition transition, int sourceInstanceId,
            int previousSourceInstanceId, float occurredAt)
        {
            GameplayContextEpoch = gameplayContextEpoch;
            LifecycleRevision = lifecycleRevision;
            Kind = kind;
            Transition = transition;
            SourceInstanceId = sourceInstanceId;
            PreviousSourceInstanceId = previousSourceInstanceId;
            OccurredAt = occurredAt;
        }

        internal int ContractVersion => CurrentContractVersion;
        internal long GameplayContextEpoch { get; }
        internal long LifecycleRevision { get; }
        internal EncounterKind Kind { get; }
        internal EncounterTransition Transition { get; }
        internal int SourceInstanceId { get; }
        internal int PreviousSourceInstanceId { get; }
        internal float OccurredAt { get; }
    }

    internal readonly struct EncounterLifecycleObservation
    {
        internal EncounterLifecycleObservation(EncounterKind kind,
            EncounterTransition transition, int sourceInstanceId,
            int relatedSourceInstanceId, float occurredAt)
        {
            Kind = kind;
            Transition = transition;
            SourceInstanceId = sourceInstanceId;
            RelatedSourceInstanceId = relatedSourceInstanceId;
            OccurredAt = occurredAt;
        }

        internal EncounterKind Kind { get; }
        internal EncounterTransition Transition { get; }
        internal int SourceInstanceId { get; }
        internal int RelatedSourceInstanceId { get; }
        internal float OccurredAt { get; }
    }
}
