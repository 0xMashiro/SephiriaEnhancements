using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration
{
    internal enum InventoryApplicationProgress
    {
        Pending,
        MovingItemInterrupted,
        TimedOut,
        InventoryUnavailable,
        GameplayContextChanged,
        InventoryChanged,
        PreferencesChanged,
        PositionEffectsChanged,
        StepRejected,
        Completed
    }

    // Owns the native inventory binding for one application, including undo.
    internal sealed class NativeInventoryLayoutApplication
    {
        private readonly GridInventory inventory;
        internal InventoryLayoutApplication State { get; }
        internal InventorySnapshot ObservedSnapshot { get; private set; }
        internal RuntimeStateSnapshot ObservedRuntime { get; private set; }
        internal InventorySettlementDifferentialReport Verification { get; private set; }
        internal bool LayoutMatched { get; private set; }

        internal NativeInventoryLayoutApplication(GridInventory inventory, InventoryLayoutApplication state)
        {
            this.inventory = inventory;
            State = state;
        }

        internal InventoryApplicationProgress Advance(RuntimeKernel kernel, float now, bool itemBeingMoved)
        {
            if (itemBeingMoved) return InventoryApplicationProgress.MovingItemInterrupted;
            if (now > State.Deadline) return InventoryApplicationProgress.TimedOut;
            if (!NativeInventoryOptimizationContext.TryGetOpenInventory(out GridInventory current))
                return InventoryApplicationProgress.InventoryUnavailable;
            if (!State.MatchesGameplayContext(kernel.State))
                return InventoryApplicationProgress.GameplayContextChanged;
            if (current != inventory) return InventoryApplicationProgress.InventoryChanged;
            if (!State.IsUndo && !kernel.MatchesNativePreset(State.SourceSnapshot.NativePreset))
                return InventoryApplicationProgress.PreferencesChanged;
            if (State.PendingOperation != InventoryPendingOperation.None)
                return ConfirmPendingOperation(kernel);

            // An acknowledgement does not authorize a later move after an unrelated update.
            if (!State.CanIssueOperation(kernel.State) ||
                !MatchesLayout(current, State.SourceSnapshot, State.ConfirmedLayout))
                return InventoryApplicationProgress.InventoryChanged;

            if (State.NextSwap < State.Plan.Swaps.Count)
            {
                InventorySwapOperation operation = State.Plan.Swaps[State.NextSwap];
                if (GetItemKey(current, operation.FirstCell) != operation.ExpectedFirstItemKey ||
                    GetItemKey(current, operation.SecondCell) != operation.ExpectedSecondItemKey)
                    return InventoryApplicationProgress.InventoryChanged;
                ItemPosition first = current.IdxToPos(operation.FirstCell);
                ItemPosition second = current.IdxToPos(operation.SecondCell);
                State.BeginSwap(kernel.State.InventoryRevision);
                current.Swap(first.x, first.y, second.x, second.y);
                return InventoryApplicationProgress.Pending;
            }

            if (State.NextRotation < State.Plan.Rotations.Count)
            {
                InventoryRotationOperation operation = State.Plan.Rotations[State.NextRotation];
                ItemPosition position = current.IdxToPos(operation.Cell);
                NewItemOwnInstance item = current.FindItem(position);
                if (GetItemKey(current, operation.Cell) != operation.ItemKey || item.StoneTablet == null)
                    return InventoryApplicationProgress.InventoryChanged;
                if (item.StoneTablet.rotation == operation.TargetRotation)
                {
                    State.SkipCompletedRotation();
                    return InventoryApplicationProgress.Pending;
                }
                State.BeginRotation(kernel.State.InventoryRevision, item.StoneTablet.rotation);
                current.DoClickAction(position);
                return InventoryApplicationProgress.Pending;
            }

            if (!kernel.TryGetSettledInventorySnapshot(out InventorySnapshot snapshot,
                    out RuntimeStateSnapshot runtime) || !State.MatchesGameplayContext(runtime) ||
                !InventoryApplicationConfirmation.MatchesTarget(snapshot, State.SourceSnapshot, State.TargetLayout))
                return InventoryApplicationProgress.Pending;
            ObservedSnapshot = snapshot;
            ObservedRuntime = runtime;
            LayoutMatched = MatchesLayout(current, State.SourceSnapshot, State.TargetLayout);
            Verification = InventorySettlementDifferentialVerifier.Compare(State.SourceSnapshot,
                State.TargetLayout, State.ExpectedSettlement, snapshot);
            return InventoryApplicationProgress.Completed;
        }

        private InventoryApplicationProgress ConfirmPendingOperation(RuntimeKernel kernel)
        {
            if (!kernel.TryGetSettledInventorySnapshot(out InventorySnapshot snapshot,
                    out RuntimeStateSnapshot runtime) || !State.CanObserveAcknowledgement(runtime))
                return InventoryApplicationProgress.Pending;
            ObservedSnapshot = snapshot;
            ObservedRuntime = runtime;
            if (snapshot.SettlementValidation.HasPositionEffectIssue ||
                !InventoryPositionEffectComparison.ParametersMatch(State.SourceSnapshot.PositionEffects,
                    snapshot.PositionEffects))
                return InventoryApplicationProgress.PositionEffectsChanged;
            if (!State.TryObservePendingOperation(snapshot, runtime, out var verification))
                return InventoryApplicationProgress.Pending;
            Verification = verification;
            return verification.Matched ? InventoryApplicationProgress.Pending : InventoryApplicationProgress.StepRejected;
        }

        internal static bool MatchesLayout(GridInventory inventory,
            InventorySnapshot snapshot, InventoryLayoutProjection layout)
        {
            if (snapshot == null || inventory == null || layout == null ||
                layout.ItemCount != snapshot.Items.Count)
            {
                return false;
            }
            if (!InventoryArrangementLifecyclePolicy.HasSameCapacity(
                    snapshot.Width, snapshot.Storage, inventory.Width,
                    inventory.CurrentInventoryStorage))
            {
                return false;
            }

            int occupied = 0;
            for (int cell = 0; cell < snapshot.Storage; cell++)
            {
                if (GetItemKey(inventory, cell).HasValue) occupied++;
            }
            if (occupied != snapshot.Items.Count) return false;

            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot expected = snapshot.Items[index];
                int cell = layout.GetCell(index);
                ItemPosition position = inventory.IdxToPos(cell);
                NewItemOwnInstance item = inventory.FindItem(position);
                if (GetItemKey(inventory, cell) != expected.ItemKey ||
                    item.Quantity != expected.Quantity ||
                    expected.StoneTablet != null && item.StoneTablet?.rotation !=
                    layout.GetRotation(index))
                {
                    return false;
                }
            }
            return true;
        }

        internal static InventoryItemKey? GetItemKey(GridInventory inventory, int cell)
        {
            NewItemOwnInstance item = inventory.FindItem(inventory.IdxToPos(cell));
            return item == null ? null : new InventoryItemKey(item.EntityID, item.InstanceID);
        }
    }
}
