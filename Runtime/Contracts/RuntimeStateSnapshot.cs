#nullable disable
using System;

namespace SephiriaEnhancements.Runtime
{
    [Flags]
    internal enum RuntimeCapabilities
    {
        None = 0,
        LocalPlayer = 1 << 0,
        GridInventory = 1 << 1,
        GridInventoryEvents = 1 << 2,
        SettledInventoryObservation = 1 << 3,
        InventorySnapshot = 1 << 4,
        InventoryCatalog = 1 << 5,
        CurrentInventorySettlementVerified = 1 << 6,
        InventoryLayoutProjection = 1 << 7
    }

    internal enum RuntimeConsistencyState
    {
        Unavailable,
        PendingSettlement,
        Consistent,
        Degraded,
        Invalid
    }

    internal sealed class RuntimeStateSnapshot
    {
        internal const int CurrentContractVersion = 5;

        internal RuntimeStateSnapshot(string gameBuildFingerprint,
            long gameplayContextEpoch, long runtimeRevision, long inventoryRevision,
            long catalogRevision, uint playerNetId,
            RuntimeCapabilities capabilities,
            RuntimeConsistencyState consistency, float capturedAt,
            string issue)
        {
            GameBuildFingerprint = gameBuildFingerprint ?? string.Empty;
            GameplayContextEpoch = gameplayContextEpoch;
            RuntimeRevision = runtimeRevision;
            InventoryRevision = inventoryRevision;
            CatalogRevision = catalogRevision;
            PlayerNetId = playerNetId;
            Capabilities = capabilities;
            Consistency = consistency;
            CapturedAt = capturedAt;
            Issue = issue ?? string.Empty;
        }

        internal int ContractVersion => CurrentContractVersion;
        internal string GameBuildFingerprint { get; }
        // Invalidates local state on world load, player replacement, departure
        // and floor entry. This is neither a run ID nor a count of generated floors.
        internal long GameplayContextEpoch { get; }
        internal long RuntimeRevision { get; }
        internal long InventoryRevision { get; }
        internal long CatalogRevision { get; }
        internal uint PlayerNetId { get; }
        internal RuntimeCapabilities Capabilities { get; }
        internal RuntimeConsistencyState Consistency { get; }
        internal float CapturedAt { get; }
        internal string Issue { get; }
        internal bool HasSettledInventoryObservation =>
            Consistency == RuntimeConsistencyState.Consistent &&
            (Capabilities & (RuntimeCapabilities.InventorySnapshot |
                RuntimeCapabilities.SettledInventoryObservation)) ==
            (RuntimeCapabilities.InventorySnapshot |
                RuntimeCapabilities.SettledInventoryObservation);
        internal bool CanProjectInventoryLayouts =>
            HasSettledInventoryObservation &&
            (Capabilities & (RuntimeCapabilities.InventorySnapshot |
                RuntimeCapabilities.SettledInventoryObservation |
                RuntimeCapabilities.CurrentInventorySettlementVerified |
                RuntimeCapabilities.InventoryLayoutProjection)) ==
            (RuntimeCapabilities.InventorySnapshot |
                RuntimeCapabilities.SettledInventoryObservation |
                RuntimeCapabilities.CurrentInventorySettlementVerified |
                RuntimeCapabilities.InventoryLayoutProjection);
    }
}
