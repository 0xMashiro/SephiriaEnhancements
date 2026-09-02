#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime
{
    internal sealed class EncounterLifecycleHub
    {
        private readonly HashSet<int> clearedOrdinaryEncounterSources =
            new HashSet<int>();
        private long gameplayContextEpoch;
        private long lifecycleRevision;
        private int pendingBossContinuationSourceInstanceId;

        internal event Action<EncounterLifecycleEvent> Changed;

        internal EncounterLifecycleEvent Current { get; private set; }

        internal EncounterLifecycleEvent BeginGameplayContext(
            long contextEpoch, float occurredAt)
        {
            gameplayContextEpoch = contextEpoch;
            pendingBossContinuationSourceInstanceId = 0;
            clearedOrdinaryEncounterSources.Clear();
            return Publish(EncounterKind.None,
                EncounterTransition.GameplayContextReset, 0, 0, occurredAt);
        }

        internal bool IsOrdinaryEncounterCleared(int sourceInstanceId) =>
            sourceInstanceId != 0 &&
            clearedOrdinaryEncounterSources.Contains(sourceInstanceId);

        internal EncounterLifecycleEvent Observe(
            EncounterLifecycleObservation observation)
        {
            EncounterTransition transition = observation.Transition;
            int sourceInstanceId = observation.SourceInstanceId;
            int previousSourceInstanceId = 0;

            if (transition == EncounterTransition.ContinuationPrepared)
            {
                if (observation.Kind != EncounterKind.Boss ||
                    observation.RelatedSourceInstanceId == 0)
                {
                    return Current;
                }

                previousSourceInstanceId = sourceInstanceId;
                sourceInstanceId = observation.RelatedSourceInstanceId;
                pendingBossContinuationSourceInstanceId = sourceInstanceId;
            }
            else if (transition == EncounterTransition.Started &&
                observation.Kind == EncounterKind.Boss &&
                sourceInstanceId == pendingBossContinuationSourceInstanceId)
            {
                transition = EncounterTransition.Resumed;
                pendingBossContinuationSourceInstanceId = 0;
            }
            else if (transition == EncounterTransition.Cleared ||
                transition == EncounterTransition.Defeated)
            {
                pendingBossContinuationSourceInstanceId = 0;
            }

            if (transition != EncounterTransition.GameplayContextReset &&
                (observation.Kind == EncounterKind.None || sourceInstanceId == 0))
            {
                return Current;
            }

            if (observation.Kind == EncounterKind.Ordinary &&
                transition == EncounterTransition.Cleared)
            {
                clearedOrdinaryEncounterSources.Add(sourceInstanceId);
            }

            return Publish(observation.Kind, transition, sourceInstanceId,
                previousSourceInstanceId, observation.OccurredAt);
        }

        private EncounterLifecycleEvent Publish(EncounterKind kind,
            EncounterTransition transition, int sourceInstanceId,
            int previousSourceInstanceId, float occurredAt)
        {
            Current = new EncounterLifecycleEvent(gameplayContextEpoch,
                ++lifecycleRevision, kind, transition, sourceInstanceId,
                previousSourceInstanceId, occurredAt);
            Changed?.Invoke(Current);
            return Current;
        }
    }
}
