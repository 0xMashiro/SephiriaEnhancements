#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using SephiriaEnhancements.Runtime.Inventory;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationController
    {
        private InventoryReproductionLog reproductionLog;
        private InventoryReproductionCase reproductionCase;

        private void InitializeReproductionLog()
        {
            if (reproductionLog != null) return;
            try
            {
                string path = Path.Combine(SaveData.CommonPath, "Mods", "SephiriaEnhancements",
                    "Logs", "Developer", "inventory-reproductions.jsonl");
                string header = InventoryReproductionJson.Serialize(new
                {
                    Event = "inventory_reproduction_start",
                    SchemaVersion = InventoryReproductionJson.SchemaVersion,
                    ModVersion = typeof(InventoryOptimizationController).Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                    ModBuild = typeof(InventoryOptimizationController).Module.ModuleVersionId.ToString(),
                    GameVersion = Application.version,
                    GameBuild = typeof(GridInventory).Module.ModuleVersionId.ToString()
                });
                reproductionLog = new InventoryReproductionLog(path, header);
                SupportLogger.Record("inventory_reproduction_enabled");
                Debug.Log("[SephiriaEnhancements] Inventory reproduction log: " + path);
            }
            catch (Exception exception)
            {
                SupportLogger.Record("inventory_reproduction_start_failed", exception.GetType().Name, "WARN");
            }
        }

        private void PumpReproductionLog()
        {
            string error = reproductionLog?.TakeError();
            if (error != null) SupportLogger.Record("inventory_reproduction_writer_failed", error, "WARN");
            int dropped = reproductionLog?.TakeDroppedCount() ?? 0;
            if (dropped > 0) SupportLogger.Record("inventory_reproduction_records_dropped", "count=" + dropped, "WARN");
            if (error != null || dropped > 0) ShowMessage(InventoryReproductionLocalization.WriteFailed);
        }

        private void HandleReproductionCapture()
        {
            PlayerInputController input = PlayerInputController.Instance;
            NativeControlCoordinator.PreparePlayerInput(input);
            if (!NativeInputActions.WasPressed(input?.playerInput?.actions,
                    ModShortcuts.CaptureInventoryReproduction, rejectKeyboardModifiers: true)) return;

            if (Busy || NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup ||
                !StandardInventoryContext.TryGetOpenInventory(out GridInventory inventory) ||
                runtimeKernel == null || !runtimeKernel.TryGetSettledInventorySnapshot(out var snapshot, out _) ||
                !MatchesInventory(snapshot, inventory))
            {
                ShowMessage(InventoryReproductionLocalization.Unavailable);
                return;
            }

            InventorySearchEffort effort = InventoryOptimizationTendencyPolicy.GetSearchEffort(ModSettings.InventoryOptimizationTendency);
            var preferences = InventoryOptimizationPreferenceComposer.Compose(
                PersistentInventoryOptimizationPolicyStore.Capture(), ExplorationInventoryIntentStore.Capture(),
                effort, InventoryOptimizationPreferences.Default.AllowStoneTabletRotation);
            var policy = snapshot.SettlementValidation.LayoutProjectionReady
                ? InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences) : null;
            var captured = new InventoryReproductionCase(snapshot, preferences, policy, InventorySearchBudget.ForEffort(effort));
            bool queued = reproductionLog?.Record(captured.Record(InventoryReproductionReason.ManualCapture)) == true;
            ShowMessage(queued ? InventoryReproductionLocalization.Queued : InventoryReproductionLocalization.WriteFailed);
        }

        private void RecordRejectedReproduction(InventorySnapshot snapshot)
        {
            InventorySearchEffort effort = InventoryOptimizationTendencyPolicy.GetSearchEffort(ModSettings.InventoryOptimizationTendency);
            InventoryOptimizationPreferences preferences = InventoryOptimizationPreferenceComposer.Compose(
                PersistentInventoryOptimizationPolicyStore.Capture(), ExplorationInventoryIntentStore.Capture(),
                effort, InventoryOptimizationPreferences.Default.AllowStoneTabletRotation);
            var rejected = new InventoryReproductionCase(snapshot, preferences, null, InventorySearchBudget.ForEffort(effort));
            reproductionLog?.Record(rejected.Record(InventoryReproductionReason.InputRejected));
        }

        private static InventoryOptimizationProposal SolveWithReproduction(InventoryReproductionCase input,
            InventoryReproductionLog log, CancellationToken token)
        {
            try
            {
                InventoryOptimizationProposal proposal = InventoryOptimizerSelector.Solve(input.Snapshot, input.Policy, input.Budget, token);
                InventoryReproductionReason reason = input.SearchReason(proposal);
                if (reason != InventoryReproductionReason.None) log?.Record(input.Record(reason, proposal));
                return proposal;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                log?.Record(input.Record(InventoryReproductionReason.SolverException, exception: exception));
                throw;
            }
        }

        private void RecordReproduction(InventoryReproductionReason reason, InventorySnapshot actual = null,
            InventorySettlementDifferentialReport differential = null, Exception exception = null)
        {
            if (reproductionCase != null)
                reproductionLog?.Record(reproductionCase.Record(reason, result, actual, differential, exception, nextSwap, nextRotation));
        }
    }
}
#endif
