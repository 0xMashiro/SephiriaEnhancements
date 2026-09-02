#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class UniqueEffectRegistrationSnapshot
    {
        internal UniqueEffectRegistrationSnapshot(int instanceId, int entityId,
            bool accepted)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            Accepted = accepted;
        }

        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal bool Accepted { get; }
    }

    internal sealed class InventoryEvaluationOrderSnapshot
    {
        internal InventoryEvaluationOrderSnapshot(long traceRevision,
            int[] categoryRefreshInstanceIds, int[] artifactRefreshInstanceIds,
            UniqueEffectRegistrationSnapshot[] uniqueRegistrations)
        {
            TraceRevision = traceRevision;
            CategoryRefreshInstanceIds = Array.AsReadOnly(
                categoryRefreshInstanceIds == null
                    ? Array.Empty<int>()
                    : (int[])categoryRefreshInstanceIds.Clone());
            ArtifactRefreshInstanceIds = Array.AsReadOnly(
                artifactRefreshInstanceIds == null
                    ? Array.Empty<int>()
                    : (int[])artifactRefreshInstanceIds.Clone());
            UniqueRegistrations = Array.AsReadOnly(uniqueRegistrations == null
                ? Array.Empty<UniqueEffectRegistrationSnapshot>()
                : (UniqueEffectRegistrationSnapshot[])uniqueRegistrations.Clone());
        }

        internal long TraceRevision { get; }
        internal IReadOnlyList<int> CategoryRefreshInstanceIds { get; }
        internal IReadOnlyList<int> ArtifactRefreshInstanceIds { get; }
        internal IReadOnlyList<UniqueEffectRegistrationSnapshot>
            UniqueRegistrations
        { get; }
    }
}
