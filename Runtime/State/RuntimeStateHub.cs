#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;

namespace SephiriaEnhancements.Runtime
{
    internal sealed class RuntimeStateHub
    {
        private readonly string gameBuildFingerprint;
        private RuntimeStateSnapshot current;

        internal RuntimeStateHub(string gameBuildFingerprint)
        {
            this.gameBuildFingerprint = gameBuildFingerprint ?? string.Empty;
            current = Create(RuntimeConsistencyState.Unavailable,
                RuntimeCapabilities.None, 0, 0f, string.Empty);
        }

        internal event Action<RuntimeStateSnapshot> Changed;

        internal RuntimeStateSnapshot Current => current;

        internal RuntimeStateSnapshot BeginGameplayContext(float capturedAt)
        {
            current = new RuntimeStateSnapshot(gameBuildFingerprint,
                current.GameplayContextEpoch + 1, current.RuntimeRevision + 1, 0,
                current.CatalogRevision, 0,
                current.Capabilities & RuntimeCapabilities.InventoryCatalog,
                RuntimeConsistencyState.Unavailable,
                capturedAt, string.Empty);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot AttachPlayer(uint playerNetId,
            RuntimeCapabilities capabilities, float capturedAt)
        {
            if (current.PlayerNetId == playerNetId && playerNetId != 0 &&
                current.Capabilities == capabilities)
            {
                return current;
            }

            current = new RuntimeStateSnapshot(gameBuildFingerprint,
                current.GameplayContextEpoch, current.RuntimeRevision + 1,
                current.InventoryRevision, current.CatalogRevision, playerNetId,
                capabilities,
                RuntimeConsistencyState.PendingSettlement, capturedAt,
                string.Empty);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot MarkInventoryPending(float capturedAt)
        {
            if (current.Consistency == RuntimeConsistencyState.PendingSettlement)
            {
                return current;
            }

            current = Create(RuntimeConsistencyState.PendingSettlement,
                current.Capabilities & ~RuntimeCapabilities.InventorySnapshot,
                current.PlayerNetId, capturedAt, string.Empty);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot PublishInventory(bool settledObservation,
            float capturedAt, bool currentSettlementVerified = true,
            bool layoutProjectionReady = true)
        {
            RuntimeCapabilities capabilities = current.Capabilities |
                RuntimeCapabilities.InventorySnapshot;
            if (settledObservation)
            {
                capabilities |= RuntimeCapabilities.SettledInventoryObservation;
            }
            if (currentSettlementVerified)
            {
                capabilities |= RuntimeCapabilities.
                    CurrentInventorySettlementVerified;
            }
            else
            {
                capabilities &= ~RuntimeCapabilities.
                    CurrentInventorySettlementVerified;
            }
            if (layoutProjectionReady)
            {
                capabilities |= RuntimeCapabilities.InventoryLayoutProjection;
            }
            else
            {
                capabilities &= ~RuntimeCapabilities.InventoryLayoutProjection;
            }

            current = new RuntimeStateSnapshot(gameBuildFingerprint,
                current.GameplayContextEpoch, current.RuntimeRevision + 1,
                current.InventoryRevision + 1, current.CatalogRevision,
                current.PlayerNetId, capabilities,
                settledObservation
                    ? RuntimeConsistencyState.Consistent
                    : RuntimeConsistencyState.PendingSettlement,
                capturedAt, string.Empty);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot PublishInventoryCatalog(float capturedAt)
        {
            current = new RuntimeStateSnapshot(gameBuildFingerprint,
                current.GameplayContextEpoch, current.RuntimeRevision + 1,
                current.InventoryRevision, current.CatalogRevision + 1,
                current.PlayerNetId,
                current.Capabilities | RuntimeCapabilities.InventoryCatalog,
                current.Consistency, capturedAt, current.Issue);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot PublishIssue(string issue, bool invalid,
            float capturedAt)
        {
            current = Create(invalid
                    ? RuntimeConsistencyState.Invalid
                    : RuntimeConsistencyState.Degraded,
                current.Capabilities & ~RuntimeCapabilities.InventorySnapshot,
                current.PlayerNetId, capturedAt, issue);
            Changed?.Invoke(current);
            return current;
        }

        internal RuntimeStateSnapshot Detach(float capturedAt)
        {
            if (current.Consistency == RuntimeConsistencyState.Unavailable &&
                current.PlayerNetId == 0)
            {
                return current;
            }

            current = Create(RuntimeConsistencyState.Unavailable,
                RuntimeCapabilities.None, 0, capturedAt, string.Empty);
            Changed?.Invoke(current);
            return current;
        }

        private RuntimeStateSnapshot Create(RuntimeConsistencyState consistency,
            RuntimeCapabilities capabilities, uint playerNetId, float capturedAt,
            string issue)
        {
            return new RuntimeStateSnapshot(gameBuildFingerprint,
                current?.GameplayContextEpoch ?? 0,
                (current?.RuntimeRevision ?? 0) + 1,
                current?.InventoryRevision ?? 0, current?.CatalogRevision ?? 0,
                playerNetId, capabilities,
                consistency, capturedAt, issue);
        }
    }
}
