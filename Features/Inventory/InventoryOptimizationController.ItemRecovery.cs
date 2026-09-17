using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Inventory.Integration;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationController
    {
        private enum ItemRecoveryPhase { None, Checking, Choosing, MoveRequested, Moving, GroupingRequested }
        private ItemRecoveryPhase itemRecovery;
        private GridInventory recoveryInventory;
        private InventorySnapshot recoverySnapshot;
        private InventoryItemSnapshot recoveryItem;
        private NativeInventorySubBagTransfer subBagTransfer;
        private NativeInventoryItemRecoveryView recoveryView;
        private readonly Queue<InventoryItemKey> approvedRecoveryItems = new();
        private long recoveryEpoch;
        private long recoveryRevision;
        private float recoveryCheckAt;
        private float recoveryDeadline;
        private bool recoveryMoveIssued;

        private void BeginItemRecovery(GridInventory inventory)
        {
            recoveryInventory = inventory;
            recoveryEpoch = runtimeKernel.State.GameplayContextEpoch;
            itemRecovery = ItemRecoveryPhase.Checking;
            // Refresh a rejected cached observation once after allowing native synchronization.
            recoveryCheckAt = Time.unscaledTime + 0.5f;
            recoveryDeadline = Time.unscaledTime + ApplyTimeout;
            ShowMessage(InventoryItemRecoveryLocalization.Checking);
        }

        private static string RecoveryItemName(InventoryItemSnapshot item) =>
            (ItemDatabase.FindItemById(item.EntityId)?.Name ?? item.Name) +
            " (" + (item.X + 1) + ", " + (item.Y + 1) + ")";

        private void AdvanceItemRecovery()
        {
            if (itemRecovery == ItemRecoveryPhase.None) return;
            var panel = UIManager.Instance?.GetElement<UI_CharacterStatusPanel>();
            var inventory = panel?.PlayerAvatar?.Inventory;
            if (runtimeKernel.State.GameplayContextEpoch != recoveryEpoch ||
                panel?.IsOpened != true || inventory != recoveryInventory ||
                !LocalPlayerResolver.IsLocal(panel.PlayerAvatar))
            {
                StopItemRecovery(InventoryOptimizationLocalization.GameplayContextChanged);
                return;
            }
            if (NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup)
            {
                StopItemRecovery(InventoryOptimizationLocalization.MovingItemInterrupted);
                return;
            }
            if (itemRecovery == ItemRecoveryPhase.GroupingRequested)
            {
                StartItemGrouping(inventory);
                return;
            }
            if (itemRecovery == ItemRecoveryPhase.Choosing)
            {
                if (runtimeKernel.State.InventoryRevision != recoveryRevision)
                {
                    itemRecovery = ItemRecoveryPhase.Checking;
                    recoveryCheckAt = Time.unscaledTime + 0.5f;
                    recoveryDeadline = Time.unscaledTime + ApplyTimeout;
                    recoveryView.SetChecking();
                }
                return;
            }
            if (Time.unscaledTime > recoveryDeadline)
            {
                StopItemRecovery(itemRecovery == ItemRecoveryPhase.Moving
                    ? InventoryOptimizationLocalization.ApplyTimedOut : InventoryOptimizationLocalization.RuntimeNotReady);
                return;
            }
            if (itemRecovery == ItemRecoveryPhase.MoveRequested)
            {
                if (!inventory.IsPickable || panel.InventoryMode != UI_CharacterStatusPanel.EInventoryMode.None ||
                    runtimeKernel.State.InventoryRevision != recoveryRevision ||
                    !MatchesInventory(recoverySnapshot, inventory))
                {
                    StopItemRecovery(InventoryOptimizationLocalization.Changed);
                    return;
                }
                string reason = NativeInventorySubBagTransfer.UnavailableReason(inventory, recoveryItem, out var slot);
                if (reason != null) { StopItemRecovery(reason); return; }
                subBagTransfer = new NativeInventorySubBagTransfer(inventory, recoverySnapshot, recoveryItem, slot);
                undo = null;
                LastAppliedOutcome = null;
                intentFeedback = null;
                recoveryMoveIssued = true;
                itemRecovery = ItemRecoveryPhase.Moving;
                recoveryView?.SetMoving();
                recoveryDeadline = Time.unscaledTime + ApplyTimeout;
                SupportLogger.Record("inventory_sub_bag_requested", "item=" + recoveryItem.ItemKey + " slot=" + slot);
                subBagTransfer.Issue();
                return;
            }
            if (itemRecovery == ItemRecoveryPhase.Moving)
            {
                if (!runtimeKernel.TryGetSettledInventorySnapshot(out var observed, out var state) ||
                    state.InventoryRevision <= recoveryRevision || !subBagTransfer.Confirmed(observed)) return;
                SupportLogger.Record("inventory_sub_bag_confirmed", "item=" + recoveryItem.ItemKey);
                string movedName = RecoveryItemName(recoveryItem);
                NativeModNotifications.Important("inventory/sub-bag/" + notificationOperation + "/" + recoveryItem.ItemKey,
                    () => string.Format(ModLocalization.Get(InventoryItemRecoveryLocalization.Moved), movedName));
                recoveryMoveIssued = false;
                subBagTransfer = null;
                itemRecovery = ItemRecoveryPhase.Checking;
                recoveryCheckAt = Time.unscaledTime + 0.5f;
                recoveryView?.SetChecking();
                return;
            }
            if (Time.unscaledTime < recoveryCheckAt || !inventory.IsPickable || runtimeKernel.InventoryCapturePending) return;
            runtimeKernel.RefreshInventoryForArrangement();
            if (runtimeKernel.TryGetProjectableInventorySnapshot(out _, out _))
            {
                ClearItemRecovery();
                TryStartOptimization(allowItemRecovery: false);
                return;
            }
            if (!runtimeKernel.TryGetLatestInventorySnapshot(out recoverySnapshot, out var current) ||
                !current.HasSettledInventoryObservation)
            {
                StopItemRecovery(InventoryOptimizationLocalization.RuntimeNotReady);
                return;
            }
            var blockers = InventoryArrangementBlockers.Find(recoverySnapshot);
            LogItemBlockers(recoverySnapshot);
            if (!InventoryItemGrouping.TryCreate(recoverySnapshot, out _))
            {
                var rejected = recoverySnapshot;
                ClearItemRecovery();
                ShowStartUnavailable(InventoryOptimizationLocalization.ObservationUnavailable, inventory, rejected);
                return;
            }
            recoveryRevision = current.InventoryRevision;
            // Approval covers only the selected identities that still need removal.
            // Removing one item can resolve another item's previously mismatching effect.
            while (approvedRecoveryItems.Count > 0)
            {
                var key = approvedRecoveryItems.Dequeue();
                recoveryItem = blockers.FirstOrDefault(item => item.ItemKey == key);
                if (recoveryItem == null) continue;
                itemRecovery = ItemRecoveryPhase.MoveRequested;
                recoveryDeadline = Time.unscaledTime + ApplyTimeout;
                return;
            }
            var holder = UIManager.Instance.GetElement<UI_MessageBoxHolder>();
            if (holder == null || holder.HasOpenedBox)
            {
                StopItemRecovery(InventoryOptimizationLocalization.RuntimeNotReady);
                return;
            }
            itemRecovery = ItemRecoveryPhase.Choosing;
            recoveryItem = null;
            if (recoveryView == null)
                recoveryView = NativeInventoryItemRecoveryView.Create(panel, selected =>
                {
                    if (itemRecovery != ItemRecoveryPhase.Choosing || selected.Length == 0) return;
                    approvedRecoveryItems.Clear();
                    foreach (var key in selected) approvedRecoveryItems.Enqueue(key);
                    var first = approvedRecoveryItems.Dequeue();
                    recoveryItem = recoverySnapshot.Items.First(item => item.ItemKey == first);
                    itemRecovery = ItemRecoveryPhase.MoveRequested;
                    recoveryDeadline = Time.unscaledTime + ApplyTimeout;
                    recoveryView.SetMoving();
                }, () =>
                {
                    recoveryView = null;
                    if (recoveryMoveIssued) StopItemRecovery(InventoryOptimizationLocalization.OperationStopped);
                    else ClearItemRecovery();
                }, () =>
                {
                    if (itemRecovery == ItemRecoveryPhase.Choosing)
                        itemRecovery = ItemRecoveryPhase.GroupingRequested;
                });
            recoveryView.ShowItems(blockers);
        }

        private void StartItemGrouping(GridInventory inventory)
        {
            if (!inventory.IsPickable || runtimeKernel.State.InventoryRevision != recoveryRevision ||
                !runtimeKernel.State.HasSettledInventoryObservation || !MatchesInventory(recoverySnapshot, inventory) ||
                !InventoryItemGrouping.TryCreate(recoverySnapshot, out var layout) ||
                !InventoryLayoutPlanner.TryCreate(recoverySnapshot, layout, out var plan, out _, verifyEffectInputs: false))
            {
                StopItemRecovery(InventoryOptimizationLocalization.Changed);
                return;
            }
            var source = recoverySnapshot;
            var runtime = runtimeKernel.State;
            ClearItemRecovery();
            if (plan.OperationCount == 0)
            {
                ShowMessage(InventoryItemRecoveryLocalization.AlreadyGrouped);
                return;
            }
            hud.SuspendEditing();
            undo = null;
            LastAppliedOutcome = null;
            intentFeedback = null;
            application = new NativeInventoryLayoutApplication(inventory,
                new InventoryLayoutApplication(source, runtime, layout, plan, Time.unscaledTime + ApplyTimeout));
            SupportLogger.Record("inventory_grouping_started", "items=" + source.Items.Count +
                " effectsVerified=False issues=" + string.Join(",", source.SettlementValidation.Issues));
            ShowMessage(InventoryOptimizationLocalization.Applying);
        }

        private static void LogItemBlockers(InventorySnapshot snapshot)
        {
            foreach (var item in InventoryArrangementBlockers.Find(snapshot))
                SupportLogger.Record("inventory_item_unverified", InventoryArrangementBlockers.Details(snapshot, item), "WARN");
        }

        private void StopItemRecovery(string reason)
        {
            string name = recoveryItem == null ? string.Empty : RecoveryItemName(recoveryItem) + "\n";
            bool issued = recoveryMoveIssued;
            SupportLogger.Record("inventory_recovery_stopped", "reason=" + reason +
                " item=" + recoveryItem?.ItemKey + " operationIssued=" + issued, "WARN");
            NativeModNotifications.Important("inventory/recovery-stopped/" + notificationOperation, () => name +
                InventoryOptimizationLocalization.FormatOperationMessage(reason, issued, ModLocalization.Get));
            ClearItemRecovery();
        }

        private void ClearItemRecovery()
        {
            itemRecovery = ItemRecoveryPhase.None;
            var view = recoveryView;
            recoveryView = null;
            if (view != null) view.Dismiss();
            approvedRecoveryItems.Clear();
            recoveryInventory = null;
            recoverySnapshot = null;
            recoveryItem = null;
            subBagTransfer = null;
            recoveryMoveIssued = false;
        }
    }
}
