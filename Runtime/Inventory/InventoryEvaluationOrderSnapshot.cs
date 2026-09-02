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

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal bool Accepted { get; }
    }

    internal sealed class InventoryEvaluationOrderSnapshot
    {
        internal InventoryEvaluationOrderSnapshot(long traceRevision,
            InventoryItemKey[] categoryRefreshItemKeys, InventoryItemKey[] artifactRefreshItemKeys,
            UniqueEffectRegistrationSnapshot[] uniqueRegistrations)
        {
            TraceRevision = traceRevision;
            CategoryRefreshItemKeys = Array.AsReadOnly(
                categoryRefreshItemKeys == null
                    ? Array.Empty<InventoryItemKey>()
                    : (InventoryItemKey[])categoryRefreshItemKeys.Clone());
            ArtifactRefreshItemKeys = Array.AsReadOnly(
                artifactRefreshItemKeys == null
                    ? Array.Empty<InventoryItemKey>()
                    : (InventoryItemKey[])artifactRefreshItemKeys.Clone());
            UniqueRegistrations = Array.AsReadOnly(uniqueRegistrations == null
                ? Array.Empty<UniqueEffectRegistrationSnapshot>()
                : (UniqueEffectRegistrationSnapshot[])uniqueRegistrations.Clone());
        }

        internal long TraceRevision { get; }
        internal IReadOnlyList<InventoryItemKey> CategoryRefreshItemKeys { get; }
        internal IReadOnlyList<InventoryItemKey> ArtifactRefreshItemKeys { get; }
        internal IReadOnlyList<UniqueEffectRegistrationSnapshot>
            UniqueRegistrations
        { get; }
    }
}
