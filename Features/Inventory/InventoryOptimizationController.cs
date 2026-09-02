#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using UnityEngine;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryOptimizationController : MonoBehaviour
    {
        private const float RequestCooldown = 1.5f;
        private const float ApplyTimeout = 20f;
        private readonly InventoryOptimizationHud hud =
            new InventoryOptimizationHud();
        private readonly NativeInventoryItemSelectionView prioritySelectionView =
            new NativeInventoryItemSelectionView();
        private RuntimeKernel runtimeKernel;
        private CancellationTokenSource solveCancellation;
        private Task<InventoryOptimizationProposal> solveTask;
        private InventorySnapshot sourceSnapshot;
        private RuntimeStateSnapshot sourceRuntime;
        private InventoryOptimizationProposal result;
        private ProjectedInventorySettlement expectedSettlement;
        private InventoryApplicationPlan applicationPlan;
        private GridInventory applyingInventory;
        private int nextSwap;
        private int nextRotation;
        private float nextRequestAt;
        private float applyDeadline;
        private long pendingOperationRevision;
        private int pendingRotation;
        private bool awaitingNativeConfirmation;
        private bool awaitingSwapConfirmation;
        private bool compatible = true;
        private float nextPriorityVisualRefreshAt;

        internal InventoryOptimizationOutcome LastAppliedOutcome
        {
            get;
            private set;
        }

        internal void Initialize(RuntimeKernel kernel)
        {
            runtimeKernel = kernel;
            PersistentInventoryOptimizationPolicyPersistence.EnsureLoaded();
            InventoryArtifactIntentClickPatch.SetController(this);
        }

        internal void ResetExploration()
        {
            EndPriorityMarking();
            hud.CancelArtifactPickup();
            ExplorationInventoryIntentStore.Clear();
        }

        internal void ResetGameplayContext()
        {
            EndPriorityMarking();
            ResetOperationState();
            LastAppliedOutcome = null;
            hud.Reset();
            compatible = true;
            nextRequestAt = 0f;
            nextPriorityVisualRefreshAt = 0f;
        }

        internal void Shutdown()
        {
            InventoryArtifactIntentClickPatch.SetController(null);
            prioritySelectionView.Dispose();
            ExplorationInventoryIntentStore.Clear();
            ResetOperationState();
            LastAppliedOutcome = null;
            hud.Dispose();
            compatible = false;
            runtimeKernel = null;
            enabled = false;
        }

        private bool Busy => solveTask != null || applicationPlan != null;

        private InventoryOptimizationHudPhase HudPhase => solveTask != null
            ? InventoryOptimizationHudPhase.Searching
            : applicationPlan != null
                ? InventoryOptimizationHudPhase.Applying
                : InventoryOptimizationHudPhase.Ready;

        private void Update()
        {
            PersistentInventoryOptimizationPolicyPersistence.EnsureLoaded();
            InventorySnapshot hudSnapshot = null;
            runtimeKernel?.TryGetLatestInventorySnapshot(out hudSnapshot,
                out RuntimeStateSnapshot _);
            MaintainPriorityMarking();
            RefreshPriorityMarkVisuals();
            InventoryOptimizationPreferences explorationIntent =
                ExplorationInventoryIntentStore.Capture();
            hud.Update(EnhancementsSettings.Enabled && compatible,
                HudPhase, hudSnapshot, RequestOptimization,
                ReplacePreferences, prioritySelectionView.IsVisible,
                InventoryArtifactIntentEditor.Count(explorationIntent),
                TogglePriorityMarking, EndPriorityMarking);
            if (!EnhancementsSettings.Enabled)
            {
                EndPriorityMarking();
                if (Busy)
                {
                    ResetOperationState();
                }
                return;
            }
            if (!compatible)
            {
                EndPriorityMarking();
                return;
            }

            if (solveTask != null && CancelSearchIfContextInvalid())
            {
                return;
            }
            if (solveTask != null)
            {
                PollSolver();
            }
            if (applicationPlan != null)
            {
                ApplyNextStep();
            }

            PlayerInputController input = PlayerInputController.Instance;
            NativeControlCoordinator.PreparePlayerInput(input);
            if (!NativeInputActions.WasPressed(input?.playerInput?.actions,
                    ModShortcuts.OptimizeInventory,
                    rejectKeyboardModifiers: true))
            {
                return;
            }

            RequestOptimization();
        }

        private void RequestOptimization()
        {
            if (Time.unscaledTime < nextRequestAt)
            {
                return;
            }
            nextRequestAt = Time.unscaledTime + RequestCooldown;
            if (Busy)
            {
                ShowMessage(InventoryOptimizationLocalization.Busy);
                return;
            }
            EndPriorityMarking();
            TryStartOptimization();
        }

        private void ReplacePreferences(
            InventoryOptimizationPreferences preferences)
        {
            if (Busy)
            {
                return;
            }
            ExplorationInventoryIntentStore.Replace(preferences);
            RefreshPriorityMarkVisuals(force: true);
        }

        internal bool TryHandleArtifactIntentClick(UI_NewInventoryIcon icon)
        {
            if (hud.HasArtifactPickup)
            {
                hud.CancelArtifactPickup();
                return true;
            }
            if (Busy || !prioritySelectionView.IsVisible ||
                !StandardInventoryContext.TryGetOpenInventory(out GridInventory inventory) ||
                icon?.Inventory != inventory)
            {
                return false;
            }

            NewItemOwnInstance item = icon?.Item;
            if (item?.Charm != null)
            {
                InventoryOptimizationPreferences updated =
                    InventoryArtifactIntentEditor.Toggle(
                        ExplorationInventoryIntentStore.Capture(),
                        item.InstanceID, item.EntityID);
                ExplorationInventoryIntentStore.Replace(updated);
                prioritySelectionView.Refresh();
                RefreshPriorityMarkVisuals(force: true);
            }
            return true;
        }

        internal void PrepareArtifactPickupInput(UI_CharacterStatusPanel panel) =>
            hud.PrepareNativeInventoryInput(panel);

        internal void EndArtifactPickupForPanel(UI_CharacterStatusPanel panel) =>
            hud.SuspendForInventoryViewChange(panel);

        private void TogglePriorityMarking()
        {
            if (prioritySelectionView.IsVisible)
            {
                EndPriorityMarking();
                return;
            }
            if (Busy)
            {
                ShowMessage(InventoryOptimizationLocalization.Busy);
                return;
            }
            if (NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup)
            {
                ShowMessage(InventoryOptimizationLocalization.FinishMovingItem);
                return;
            }
            hud.CancelArtifactPickup();
            if (!StandardInventoryContext.TryGetOpenInventory(
                    out GridInventory _,
                    out UI_CharacterStatusPanel panel) ||
                !prioritySelectionView.TryShow(panel,
                    item => item?.Charm != null))
            {
                ShowMessage(InventoryOptimizationLocalization.Unavailable);
                return;
            }
            RefreshPriorityMarkVisuals(force: true);
        }

        private void EndPriorityMarking()
        {
            prioritySelectionView.Hide();
        }

        private void MaintainPriorityMarking()
        {
            if (!prioritySelectionView.IsVisible)
            {
                return;
            }
            if (!EnhancementsSettings.Enabled || !compatible || Busy ||
                !prioritySelectionView.Refresh())
            {
                EndPriorityMarking();
            }
        }

        private void RefreshPriorityMarkVisuals(bool force = false)
        {
            if (!force && Time.unscaledTime < nextPriorityVisualRefreshAt)
            {
                return;
            }
            nextPriorityVisualRefreshAt = Time.unscaledTime + 0.15f;
            if (!StandardInventoryContext.TryGetOpenInventory(
                    out GridInventory inventory,
                    out UI_CharacterStatusPanel panel))
            {
                return;
            }

            InventoryOptimizationPreferences current =
                ExplorationInventoryIntentStore.Capture();
            InventoryItemKey[] validItemKeys = Enumerable.Range(0,
                inventory.CurrentInventoryStorage).Select(index =>
                    GetItemKey(inventory, index)).Where(key => key.HasValue)
                .Select(key => key.Value).ToArray();
            InventoryOptimizationPreferences pruned =
                InventoryArtifactIntentEditor.Prune(current, validItemKeys);
            if (!ReferenceEquals(pruned, current))
            {
                ExplorationInventoryIntentStore.Replace(pruned);
                current = pruned;
            }
            InventoryIntentBadge.RefreshVisible(panel, current);
        }

        private void TryStartOptimization()
        {
            if (NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup)
            {
                ShowMessage(InventoryOptimizationLocalization.FinishMovingItem);
                return;
            }
            if (!TryGetOpenInventory(out GridInventory inventory))
            {
                ShowMessage(InventoryOptimizationLocalization.Unavailable);
                return;
            }

            if (runtimeKernel == null)
            {
                ShowMessage(InventoryOptimizationLocalization.RuntimeNotReady);
                return;
            }

            if (!runtimeKernel.TryGetProjectableInventorySnapshot(
                    out sourceSnapshot, out sourceRuntime))
            {
                RuntimeConsistencyState consistency = runtimeKernel.State?.Consistency ??
                    RuntimeConsistencyState.Unavailable;
                bool settledButUnsupported = runtimeKernel.State?.
                    HasSettledInventoryObservation == true;
                ShowMessage(settledButUnsupported ||
                    consistency == RuntimeConsistencyState.Degraded ||
                    consistency == RuntimeConsistencyState.Invalid
                        ? InventoryOptimizationLocalization.Unsupported
                        : InventoryOptimizationLocalization.RuntimeNotReady);
                return;
            }
            if (sourceSnapshot.Items.Count == 0)
            {
                ShowMessage(InventoryOptimizationLocalization.EmptyInventory);
                return;
            }

            if (!MatchesInventory(sourceSnapshot, inventory))
            {
                ShowMessage(InventoryOptimizationLocalization.Changed);
                return;
            }

            hud.SuspendEditing();
            solveCancellation = new CancellationTokenSource();
            CancellationToken token = solveCancellation.Token;
            InventorySearchEffort searchEffort =
                InventoryOptimizationTendencyPolicy.GetSearchEffort(
                    ModSettings.InventoryOptimizationTendency);
            InventoryOptimizationPreferences preferences =
                InventoryOptimizationPreferenceComposer.Compose(
                    PersistentInventoryOptimizationPolicyStore.Capture(),
                    ExplorationInventoryIntentStore.Capture(), searchEffort,
                    InventoryOptimizationPreferences.Default.
                        AllowStoneTabletRotation);
            ResolvedInventoryOptimizationPolicy policy =
                InventoryOptimizationPolicyResolver.Resolve(sourceSnapshot,
                    preferences);
            solveTask = Task.Run(() => InventoryOptimizerSelector.Solve(
                sourceSnapshot, policy, cancellationToken: token), token);
            ShowMessage(InventoryOptimizationLocalization.Analyzing);
        }

        private void PollSolver()
        {
            if (!solveTask.IsCompleted)
            {
                return;
            }

            Task<InventoryOptimizationProposal> completed = solveTask;
            solveTask = null;
            solveCancellation?.Dispose();
            solveCancellation = null;
            if (completed.IsCanceled)
            {
                ResetOperationState();
                return;
            }
            if (completed.IsFaulted)
            {
                Fail(completed.Exception?.GetBaseException());
                return;
            }

            result = completed.Result;
            if (!result.Succeeded)
            {
                DeveloperLogger.RecordInventoryOptimization(result, null,
                    runtimeKernel?.State);
                ShowMessage(InventoryOptimizationLocalization.Unsupported);
                ResetOperationState();
                return;
            }
            if (!result.Improved)
            {
                ShowMessage(InventoryOptimizationLocalization.NoImprovementFound);
                ResetOperationState();
                return;
            }
            if (!TryGetOpenInventory(out applyingInventory) ||
                !RuntimeStillMatches() ||
                !MatchesInventory(sourceSnapshot, applyingInventory))
            {
                ShowMessage(InventoryOptimizationLocalization.Changed);
                ResetOperationState();
                return;
            }
            if (!InventoryLayoutPlanner.TryCreate(sourceSnapshot, result.Layout,
                    out applicationPlan, out string _))
            {
                ShowMessage(InventoryOptimizationLocalization.Failed);
                ResetOperationState();
                return;
            }
            expectedSettlement = InventorySettlementProjector.Evaluate(
                sourceSnapshot, result.Layout);
            if (!expectedSettlement.Succeeded)
            {
                ShowMessage(InventoryOptimizationLocalization.Unsupported);
                ResetOperationState();
                return;
            }
            DeveloperLogger.RecordInventoryOptimization(result, applicationPlan,
                runtimeKernel?.State);

            nextSwap = 0;
            nextRotation = 0;
            awaitingNativeConfirmation = false;
            applyDeadline = Time.unscaledTime + ApplyTimeout;
            ShowMessage(InventoryOptimizationLocalization.Applying);
        }

        private void ApplyNextStep()
        {
            if (NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup)
            {
                ShowMessage(InventoryOptimizationLocalization.MovingItemInterrupted);
                ResetOperationState();
                return;
            }
            if (Time.unscaledTime > applyDeadline)
            {
                ShowMessage(InventoryOptimizationLocalization.ApplyTimedOut);
                ResetOperationState();
                return;
            }
            if (!TryGetOpenInventory(out GridInventory current))
            {
                ShowMessage(InventoryOptimizationLocalization.InventoryClosed);
                ResetOperationState();
                return;
            }
            if (!RuntimeGameplayContextStillMatches())
            {
                ShowMessage(InventoryOptimizationLocalization.GameplayContextChanged);
                ResetOperationState();
                return;
            }
            if (current != applyingInventory)
            {
                ShowMessage(InventoryOptimizationLocalization.Changed);
                ResetOperationState();
                return;
            }

            try
            {
                if (awaitingNativeConfirmation)
                {
                    ConfirmPendingNativeOperation();
                    return;
                }

                if (nextSwap < applicationPlan.Swaps.Count)
                {
                    InventorySwapOperation operation =
                        applicationPlan.Swaps[nextSwap];
                    if (GetInstanceId(current, operation.FirstCell) !=
                            operation.ExpectedFirstInstanceId ||
                        GetInstanceId(current, operation.SecondCell) !=
                            operation.ExpectedSecondInstanceId)
                    {
                        ShowMessage(InventoryOptimizationLocalization.Changed);
                        ResetOperationState();
                        return;
                    }

                    ItemPosition first = current.IdxToPos(operation.FirstCell);
                    ItemPosition second = current.IdxToPos(operation.SecondCell);
                    pendingOperationRevision = runtimeKernel.State.
                        InventoryRevision;
                    awaitingNativeConfirmation = true;
                    awaitingSwapConfirmation = true;
                    current.Swap(first.x, first.y, second.x, second.y);
                    return;
                }

                if (nextRotation < applicationPlan.Rotations.Count)
                {
                    InventoryRotationOperation operation =
                        applicationPlan.Rotations[nextRotation];
                    ItemPosition position = current.IdxToPos(operation.Cell);
                    NewItemOwnInstance item = current.FindItem(position);
                    if (item?.InstanceID != operation.InstanceId ||
                        item.StoneTablet == null)
                    {
                        ShowMessage(InventoryOptimizationLocalization.Changed);
                        ResetOperationState();
                        return;
                    }
                    if (item.StoneTablet.rotation == operation.TargetRotation)
                    {
                        nextRotation++;
                        return;
                    }

                    pendingOperationRevision = runtimeKernel.State.
                        InventoryRevision;
                    pendingRotation = item.StoneTablet.rotation;
                    awaitingNativeConfirmation = true;
                    awaitingSwapConfirmation = false;
                    current.DoClickAction(position);
                    return;
                }

                if (!runtimeKernel.TryGetSettledInventorySnapshot(
                        out InventorySnapshot actualSnapshot,
                        out RuntimeStateSnapshot actualRuntime) ||
                    actualRuntime.GameplayContextEpoch !=
                        sourceRuntime.GameplayContextEpoch ||
                    actualRuntime.PlayerNetId != sourceRuntime.PlayerNetId ||
                    !InventoryApplicationConfirmation.MatchesTarget(
                        actualSnapshot, sourceSnapshot, result.Layout))
                {
                    return;
                }

                bool layoutMatched = MatchesLayout(current, sourceSnapshot,
                    result.Layout);
                InventorySettlementDifferentialReport differential =
                    InventorySettlementDifferentialVerifier.Compare(
                        sourceSnapshot, result.Layout, expectedSettlement,
                        actualSnapshot);
                DeveloperLogger.RecordInventoryApplication(layoutMatched,
                    applicationPlan, nextSwap, nextRotation,
                    actualRuntime);
                DeveloperLogger.RecordInventorySettlementDifferential(
                    differential, actualRuntime);
                if (!layoutMatched)
                {
                    ShowMessage(InventoryOptimizationLocalization.Changed);
                }
                else if (!differential.Matched)
                {
                    LastAppliedOutcome = null;
                    ShowMessage(InventoryOptimizationLocalization.
                        VerificationFailed);
                }
                else
                {
                    LastAppliedOutcome = result.Outcome;
                    ShowMessage(InventoryOptimizationLocalization.Completed);
                }
                ResetOperationState();
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private void ConfirmPendingNativeOperation()
        {
            if (!runtimeKernel.TryGetSettledInventorySnapshot(
                    out InventorySnapshot snapshot,
                    out RuntimeStateSnapshot runtime) ||
                runtime.GameplayContextEpoch !=
                    sourceRuntime.GameplayContextEpoch ||
                runtime.PlayerNetId != sourceRuntime.PlayerNetId ||
                runtime.InventoryRevision <= pendingOperationRevision)
            {
                return;
            }

            if (awaitingSwapConfirmation)
            {
                if (!InventoryApplicationConfirmation.IsSwapObserved(snapshot,
                        applicationPlan.Swaps[nextSwap]))
                {
                    return;
                }
                nextSwap++;
            }
            else
            {
                InventoryRotationOperation operation =
                    applicationPlan.Rotations[nextRotation];
                if (!InventoryApplicationConfirmation.IsRotationStepObserved(
                        snapshot, operation, pendingRotation))
                {
                    return;
                }
                InventoryItemSnapshot item = snapshot.Items.First(value =>
                    value.InstanceId == operation.InstanceId);
                if (item.StoneTablet.Rotation == operation.TargetRotation)
                {
                    nextRotation++;
                }
            }

            awaitingNativeConfirmation = false;
        }

        private bool RuntimeStillMatches()
        {
            RuntimeStateSnapshot current = runtimeKernel?.State;
            return current != null && sourceRuntime != null &&
                current.GameplayContextEpoch ==
                    sourceRuntime.GameplayContextEpoch &&
                current.InventoryRevision == sourceRuntime.InventoryRevision &&
                current.PlayerNetId == sourceRuntime.PlayerNetId;
        }

        private bool CancelSearchIfContextInvalid()
        {
            if (NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup)
            {
                ShowMessage(InventoryOptimizationLocalization.MovingItemInterrupted);
                ResetOperationState();
                return true;
            }
            bool standardInventoryOpen = TryGetOpenInventory(
                out GridInventory currentInventory);
            RuntimeStateSnapshot currentRuntime = runtimeKernel?.State;
            bool gameplayContextMatches = currentRuntime != null &&
                sourceRuntime != null &&
                currentRuntime.GameplayContextEpoch ==
                    sourceRuntime.GameplayContextEpoch &&
                currentRuntime.PlayerNetId == sourceRuntime.PlayerNetId;
            bool inventoryRevisionMatches = gameplayContextMatches &&
                currentRuntime.InventoryRevision == sourceRuntime.InventoryRevision;
            bool sourceLayoutMatches = standardInventoryOpen &&
                sourceSnapshot != null &&
                MatchesInventory(sourceSnapshot, currentInventory);
            InventoryArrangementInvalidationReason reason =
                InventoryArrangementLifecyclePolicy.Evaluate(
                    InventoryArrangementOperationPhase.Searching,
                    EnhancementsSettings.Enabled, standardInventoryOpen,
                    gameplayContextMatches, inventoryRevisionMatches,
                    sourceLayoutMatches);
            if (reason == InventoryArrangementInvalidationReason.None)
            {
                return false;
            }

            if (reason == InventoryArrangementInvalidationReason.
                    GameplayContextChanged)
            {
                ShowMessage(InventoryOptimizationLocalization.
                    GameplayContextChanged);
            }
            else if (reason == InventoryArrangementInvalidationReason.
                    InventoryStateChanged ||
                reason == InventoryArrangementInvalidationReason.
                    InventoryLayoutChanged)
            {
                ShowMessage(InventoryOptimizationLocalization.Changed);
            }
            ResetOperationState();
            return true;
        }

        private bool RuntimeGameplayContextStillMatches()
        {
            RuntimeStateSnapshot current = runtimeKernel?.State;
            return current != null && sourceRuntime != null &&
                current.GameplayContextEpoch ==
                    sourceRuntime.GameplayContextEpoch &&
                current.PlayerNetId == sourceRuntime.PlayerNetId;
        }

        private static bool TryGetOpenInventory(out GridInventory inventory)
        {
            // Native UI state and contextual companion-panel checks stay in
            // StandardInventoryContext so this controller only operates on the
            // game's ordinary inventory layout.
            return StandardInventoryContext.TryGetOpenInventory(
                out inventory);
        }

        private static bool MatchesInventory(InventorySnapshot snapshot,
            GridInventory inventory)
        {
            if (snapshot == null || inventory == null)
            {
                return false;
            }
            if (!InventoryArrangementLifecyclePolicy.HasSameCapacity(
                    snapshot.Width, snapshot.Storage, inventory.Width,
                    inventory.CurrentInventoryStorage))
            {
                return false;
            }

            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot expected = snapshot.Items[index];
                if (GetInstanceId(inventory, expected.CellIndex) !=
                    expected.InstanceId)
                {
                    return false;
                }
                NewItemOwnInstance item = inventory.FindItem(
                    inventory.IdxToPos(expected.CellIndex));
                if (expected.StoneTablet != null && item?.StoneTablet?.rotation !=
                    expected.StoneTablet.Rotation)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool MatchesLayout(GridInventory inventory,
            InventorySnapshot snapshot, InventoryLayoutProjection layout)
        {
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                InventoryItemSnapshot expected = snapshot.Items[index];
                int cell = layout.GetCell(index);
                ItemPosition position = inventory.IdxToPos(cell);
                NewItemOwnInstance item = inventory.FindItem(position);
                if (item?.InstanceID != expected.InstanceId ||
                    expected.StoneTablet != null && item.StoneTablet?.rotation !=
                    layout.GetRotation(index))
                {
                    return false;
                }
            }
            return true;
        }

        private static InventoryItemKey? GetItemKey(GridInventory inventory, int cell)
        {
            NewItemOwnInstance item = inventory.FindItem(inventory.IdxToPos(cell));
            return item == null ? null : new InventoryItemKey(item.EntityID, item.InstanceID);
        }

        private static int GetInstanceId(GridInventory inventory, int cell)
        {
            return inventory.FindItem(inventory.IdxToPos(cell))?.InstanceID ?? -1;
        }

        private void Fail(Exception exception)
        {
            compatible = false;
            Debug.LogWarning("[SephiriaEnhancements] Inventory optimization " +
                "disabled for the current gameplay context: " +
                (exception?.Message ?? "unknown failure"));
            ShowMessage(InventoryOptimizationLocalization.
                DisabledForGameplayContext);
            ResetOperationState();
        }

        private void ResetOperationState()
        {
            solveCancellation?.Cancel();
            solveCancellation?.Dispose();
            solveCancellation = null;
            solveTask = null;
            sourceSnapshot = null;
            sourceRuntime = null;
            result = null;
            expectedSettlement = null;
            applicationPlan = null;
            applyingInventory = null;
            nextSwap = 0;
            nextRotation = 0;
            pendingOperationRevision = 0;
            pendingRotation = 0;
            awaitingNativeConfirmation = false;
            awaitingSwapConfirmation = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private static void ShowMessage(string key)
        {
            UI_SystemMessage message =
                UIManager.Instance?.GetElement<UI_SystemMessage>();
            message?.Open(Loc._(key), 2f);
        }
    }
}
