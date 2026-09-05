#nullable disable
using System.Linq;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryPendingOperation { None, Swap, Rotation }

    internal sealed class InventoryLayoutApplication
    {
        private long pendingRevision;
        private int pendingRotation;

        internal InventoryLayoutApplication(InventorySnapshot source, RuntimeStateSnapshot runtime,
            InventoryOptimizationProposal proposal, InventoryApplicationPlan plan,
            ProjectedInventorySettlement expectedSettlement, float deadline)
        {
            SourceSnapshot = source;
            SourceRuntime = runtime;
            Proposal = proposal;
            Plan = plan;
            ExpectedSettlement = expectedSettlement;
            Deadline = deadline;
            ConfirmedLayout = InventoryLayoutProjection.Current(source);
            ConfirmedRevision = runtime.InventoryRevision;
        }

        internal InventorySnapshot SourceSnapshot { get; }
        internal RuntimeStateSnapshot SourceRuntime { get; }
        internal InventoryOptimizationProposal Proposal { get; }
        internal InventoryApplicationPlan Plan { get; }
        internal ProjectedInventorySettlement ExpectedSettlement { get; }
        internal float Deadline { get; }
        internal InventoryLayoutProjection ConfirmedLayout { get; private set; }
        internal long ConfirmedRevision { get; private set; }
        internal int NextSwap { get; private set; }
        internal int NextRotation { get; private set; }
        internal InventoryPendingOperation PendingOperation { get; private set; }

        internal void BeginSwap(long revision)
        {
            pendingRevision = revision;
            PendingOperation = InventoryPendingOperation.Swap;
        }

        internal void BeginRotation(long revision, int rotation)
        {
            pendingRevision = revision;
            pendingRotation = rotation;
            PendingOperation = InventoryPendingOperation.Rotation;
        }

        internal void SkipCompletedRotation() => NextRotation++;

        internal bool CanObserveAcknowledgement(RuntimeStateSnapshot runtime) =>
            PendingOperation != InventoryPendingOperation.None && runtime?.HasSettledInventoryObservation == true &&
            runtime.GameplayContextEpoch == SourceRuntime.GameplayContextEpoch &&
            runtime.PlayerNetId == SourceRuntime.PlayerNetId && runtime.InventoryRevision > pendingRevision;

        // Only a verified complete intermediate layout advances the operation cursor.
        // A rotation can require several native clicks before reaching its target.
        internal bool TryObservePendingOperation(InventorySnapshot snapshot, RuntimeStateSnapshot runtime,
            out InventorySettlementDifferentialReport verification)
        {
            verification = null;
            if (!CanObserveAcknowledgement(runtime)) return false;
            InventoryLayoutProjection observedLayout;
            if (PendingOperation == InventoryPendingOperation.Swap)
            {
                var operation = Plan.Swaps[NextSwap];
                if (!InventoryApplicationConfirmation.IsSwapObserved(snapshot, operation)) return false;
                observedLayout = ConfirmedLayout.WithCellsSwapped(operation.FirstCell, operation.SecondCell);
            }
            else
            {
                var operation = Plan.Rotations[NextRotation];
                if (!InventoryApplicationConfirmation.IsRotationStepObserved(snapshot, operation, pendingRotation)) return false;
                var item = snapshot.Items.First(value => value.ItemKey == operation.ItemKey);
                int itemIndex = Enumerable.Range(0, SourceSnapshot.Items.Count)
                    .First(index => SourceSnapshot.Items[index].ItemKey == operation.ItemKey);
                observedLayout = ConfirmedLayout.WithRotation(itemIndex, item.StoneTablet.Rotation);
            }
            verification = InventoryApplicationConfirmation.VerifyStep(snapshot, SourceSnapshot, observedLayout);
            if (!verification.Matched) return true;
            if (PendingOperation == InventoryPendingOperation.Swap) NextSwap++;
            else
            {
                var operation = Plan.Rotations[NextRotation];
                var item = snapshot.Items.First(value => value.ItemKey == operation.ItemKey);
                if (item.StoneTablet.Rotation == operation.TargetRotation) NextRotation++;
            }
            ConfirmedLayout = observedLayout;
            ConfirmedRevision = runtime.InventoryRevision;
            PendingOperation = InventoryPendingOperation.None;
            return true;
        }
    }
}
