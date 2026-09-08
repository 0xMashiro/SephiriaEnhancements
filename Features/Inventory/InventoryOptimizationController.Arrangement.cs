using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationController
    {
        private InventoryArrangementUndo undo;

        private bool CanUndo => !Busy && compatible && EnhancementsSettings.Enabled &&
            undo?.Matches(runtimeKernel?.State) == true &&
            runtimeKernel.State.CanProjectInventoryLayouts;

        private void MaintainArrangementHistory()
        {
            if (undo != null && !undo.Matches(runtimeKernel?.State)) undo = null;
        }

        private void RequestUndo()
        {
            if (!CanUndo || NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup) return;
            EndPriorityMarking();
            if (!TryGetOpenInventory(out var inventory) ||
                !runtimeKernel.TryGetProjectableInventorySnapshot(out var source, out var runtime) ||
                !undo.Matches(runtime) || !MatchesInventory(source, inventory)) return;
            var target = undo.RestoreLayout(source);
            if (target == null || !InventoryLayoutPlanner.TryCreate(source, target, out var plan, out _))
            {
                undo = null;
                return;
            }
            var settlement = InventorySettlementProjector.Evaluate(source, target);
            if (!settlement.Succeeded)
            {
                ShowMessage(InventoryOptimizationLocalization.Unsupported);
                return;
            }
            hud.SuspendEditing();
            application = new InventoryLayoutApplication(source, runtime, target, plan, settlement,
                Time.unscaledTime + ApplyTimeout);
            applyingInventory = inventory;
            undo = null;
        }
    }
}
