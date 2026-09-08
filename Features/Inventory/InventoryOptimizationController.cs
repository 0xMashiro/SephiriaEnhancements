#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Linq;
using System.Threading.Tasks;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using UnityEngine;
using SephiriaEnhancements.Inventory.Integration;
using static SephiriaEnhancements.Inventory.Integration.NativeInventoryLayoutApplication;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationController : MonoBehaviour
    {
        internal static InventoryOptimizationController Current { get; private set; }
        private const float RequestCooldown = 1.5f;
        private const float ApplyTimeout = 20f;
        private readonly InventoryOptimizationHud hud =
            new InventoryOptimizationHud();
        private readonly NativeInventoryItemSelectionView prioritySelectionView =
            new NativeInventoryItemSelectionView();
        private readonly Integration.NativeRewardComboHighlightView rewardComboHighlights =
            new Integration.NativeRewardComboHighlightView();
        private RuntimeKernel runtimeKernel;
        private Integration.Gpu.GpuInventoryLayoutOptimizer gpuOptimizer;
        private InventoryOptimizationSearch search;
        private NativeInventoryLayoutApplication application;
        private float nextRequestAt;
        private bool compatible = true;
        private float nextPriorityVisualRefreshAt;
        private InventoryIntentResultFeedback intentFeedback;

        internal InventoryOptimizationOutcome LastAppliedOutcome
        {
            get;
            private set;
        }

        internal void Initialize(RuntimeKernel kernel)
        {
            Current = this;
            runtimeKernel = kernel;
            gpuOptimizer = new Integration.Gpu.GpuInventoryLayoutOptimizer();
            InventoryOptimizerRegistry.Register(gpuOptimizer);
            PersistentInventoryOptimizationPolicyPersistence.EnsureLoaded();
            InventoryArtifactIntentClickPatch.SetController(this);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            InitializeReproductionLog();
#endif
        }

        internal void ResetExploration()
        {
            undo = null;
            intentFeedback = null;
            EndPriorityMarking();
            hud.CancelArtifactPickup();
            ExplorationInventoryIntentStore.Clear();
        }

        internal void ResetGameplayContext()
        {
            undo = null;
            rewardComboHighlights.Clear();
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            ResetOptimizationFrameMetrics();
#endif
            EndPriorityMarking();
            ResetOperationState();
            LastAppliedOutcome = null;
            intentFeedback = null;
            hud.Reset();
            compatible = true;
            nextRequestAt = 0f;
            nextPriorityVisualRefreshAt = 0f;
        }

        internal void Shutdown()
        {
            undo = null;
            rewardComboHighlights.Clear();
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            ResetOptimizationFrameMetrics();
#endif
            InventoryOptimizerRegistry.Unregister(gpuOptimizer);
            gpuOptimizer?.Dispose();
            gpuOptimizer = null;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            reproductionLog?.Dispose();
            PumpReproductionLog();
            reproductionLog = null;
#endif
            InventoryArtifactIntentClickPatch.SetController(null);
            prioritySelectionView.Dispose();
            ExplorationInventoryIntentStore.Clear();
            ResetOperationState();
            LastAppliedOutcome = null;
            hud.Dispose();
            compatible = false;
            runtimeKernel = null;
            enabled = false;
            if (Current == this)
            {
                Current = null;
            }
        }

        internal static bool TryHandleKeyboardTab() =>
            Current?.hud.TryHandleKeyboardTab() == true;

        internal static bool OwnsSelection(UIBase panel, GameObject candidate) =>
            Current != null && Current.hud.OwnsSelection(panel, candidate);

        internal static bool TryCancelPanel(UIBase panel) =>
            Current != null && Current.hud.TryCancelPanel(panel);

        internal static void PositionTooltip(UI_BaseTooltip tooltip) =>
            Current?.hud.PositionTooltip(tooltip);

        private bool Busy => search != null || application != null;

        private InventoryOptimizationHudPhase HudPhase => search != null
            ? InventoryOptimizationHudPhase.Searching
            : application != null
                ? InventoryOptimizationHudPhase.Applying
                : InventoryOptimizationHudPhase.Ready;

        private void Update()
        {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            SampleOptimizationFrame();
            PumpReproductionLog();
            HandleReproductionCapture();
#endif
            PersistentInventoryOptimizationPolicyPersistence.EnsureLoaded();
            InventorySnapshot hudSnapshot = null;
            runtimeKernel?.TryGetLatestInventorySnapshot(out hudSnapshot,
                out RuntimeStateSnapshot _);
            rewardComboHighlights.Update(EnhancementsSettings.Enabled, hudSnapshot);
            MaintainPriorityMarking();
            MaintainArrangementHistory();
            RefreshPriorityMarkVisuals();
            InventoryOptimizationPreferences explorationIntent =
                ExplorationInventoryIntentStore.Capture();
            if (intentFeedback?.IsCurrent(runtimeKernel?.State, explorationIntent) != true)
                intentFeedback = null;
            hud.ConfigureArrangementActions(RequestUndo, CanUndo);
            hud.Update(EnhancementsSettings.Enabled && compatible,
                HudPhase, hudSnapshot, RequestOptimization,
                ReplacePreferences, prioritySelectionView.IsVisible,
                InventoryArtifactIntentEditor.Count(explorationIntent),
                TogglePriorityMarking, EndPriorityMarking, intentFeedback);
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

            if (search != null && CancelSearchIfContextInvalid())
            {
                return;
            }
            if (search != null)
            {
                PollSolver();
            }
            if (application != null)
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
            // The HUD edits the complete visible category policy. Persist even
            // removal (Automatic), so a hidden old rule cannot reappear in Solve.
            if (InventoryOptimizationPreferencesCodec.Encode(preferences) !=
                InventoryOptimizationPreferencesCodec.Encode(PersistentInventoryOptimizationPolicyStore.Capture()))
            {
                var persistent = new InventoryOptimizationPreferences(preferences.SearchEffort,
                    preferences.AllowStoneTabletRotation, Array.Empty<ArtifactOptimizationPreference>(),
                    preferences.ComboPreferences.ToArray(), preferences.PositionEffectPreference, preferences.AllowAdditionalMagicCost);
                PersistentInventoryOptimizationPolicyStore.Replace(persistent);
                try
                {
                    if (!PersistentInventoryOptimizationPolicyPersistence.Save(persistent))
                        SupportLogger.Record("inventory_preferences_not_saved", "Device options unavailable", "WARN");
                }
                catch (Exception ex)
                {
                    SupportLogger.Failure("inventory_preferences_save_failed", ex);
                }
            }
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
            if (item?.Entity?.type == EItemType.Charm)
            {
                InventoryOptimizationPreferences updated =
                    InventoryArtifactIntentEditor.Toggle(
                        ExplorationInventoryIntentStore.Capture(),
                        item.InstanceID, item.EntityID);
                ExplorationInventoryIntentStore.Replace(updated);
                hud.PreviewArtifact(new InventoryItemKey(item.EntityID, item.InstanceID));
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
            bool wasMarkingPriorities = prioritySelectionView.IsVisible;
            EndPriorityMarking();
            if (wasMarkingPriorities)
            {
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
                    item => item?.Entity?.type == EItemType.Charm))
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
                    out InventorySnapshot sourceSnapshot, out RuntimeStateSnapshot sourceRuntime))
            {
                runtimeKernel.TryGetLatestInventorySnapshot(out InventorySnapshot latest, out _);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                if (latest != null && !latest.SettlementValidation.LayoutProjectionReady)
                    RecordRejectedReproduction(latest);
#endif
                SupportLogger.Record("inventory_projection_unavailable",
                    "consistency=" + runtimeKernel.State?.Consistency + " issues=" +
                    string.Join(",", (latest?.SettlementValidation.Issues ?? Array.Empty<string>())
                        .Select(issue => issue.Split(':')[0]).Distinct()), "WARN");
                if (latest != null &&
                    latest.SettlementValidation.HasItemIdentityConflict)
                {
                    ShowMessage(InventoryOptimizationLocalization.ItemIdentityConflict);
                    return;
                }
                if (latest?.SettlementValidation.HasPositionEffectIssue == true)
                {
                    ShowMessage(InventoryOptimizationLocalization.PositionEffectsUnavailable);
                    return;
                }
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
            intentFeedback = null;
            LastAppliedOutcome = null;
            InventorySearchEffort searchEffort =
                InventorySearchModePolicy.GetSearchEffort(
                    ModSettings.InventorySearchMode);
            InventoryOptimizationPreferences preferences =
                InventoryOptimizationPreferenceComposer.Compose(
                    PersistentInventoryOptimizationPolicyStore.Capture(),
                    ExplorationInventoryIntentStore.Capture(), searchEffort,
                    InventoryOptimizationPreferences.Default.
                        AllowStoneTabletRotation);
            ResolvedInventoryOptimizationPolicy policy =
                InventoryOptimizationPolicyResolver.Resolve(sourceSnapshot,
                    preferences);
            SupportLogger.Record("inventory_search_started", "effort=" + searchEffort +
                " items=" + sourceSnapshot.Items.Count + " artifactGoals=" + preferences.ArtifactPreferences.Count +
                " comboGoals=" + preferences.ComboPreferences.Count +
                " allowTabletRotation=" + preferences.AllowStoneTabletRotation);
            // Capture immutable inputs: resetting the controller must not change
            // the snapshot read by a task that has not started yet.
            InventorySnapshot snapshot = sourceSnapshot;
            InventorySearchBudget budget = InventorySearchBudget.ForEffort(searchEffort);
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            reproductionCase = new InventoryReproductionCase(snapshot, preferences, policy, budget,
                DeveloperTools.InventoryReproductionSettings.RecordAllResults);
            InventoryReproductionCase capturedCase = reproductionCase;
            InventoryReproductionLog capturedLog = reproductionLog;
            search = new InventoryOptimizationSearch(snapshot, sourceRuntime,
                token => SolveWithReproduction(capturedCase, capturedLog, token));
#else
            search = new InventoryOptimizationSearch(snapshot, sourceRuntime,
                token => InventoryOptimizerSelector.Solve(snapshot, policy, budget, token));
#endif
            ShowMessage(InventoryOptimizationLocalization.Analyzing);
        }

        private void PollSolver()
        {
            if (!search.Task.IsCompleted)
            {
                return;
            }

            Task<InventoryOptimizationProposal> completed = search.Task;
            InventorySnapshot sourceSnapshot = search.SourceSnapshot;
            RuntimeStateSnapshot sourceRuntime = search.SourceRuntime;
            search.Dispose();
            search = null;
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

            InventoryOptimizationProposal result = completed.Result;
            SupportLogger.Record("inventory_search_completed", "succeeded=" + result.Succeeded +
                " improved=" + result.Improved + " reason=" + result.TerminationReason +
                " candidates=" + result.CandidateEvaluations + " elapsedMs=" + result.ElapsedMilliseconds);
            if (!result.Succeeded)
            {
                DeveloperLogger.RecordInventoryOptimization(result, null,
                    runtimeKernel?.State);
                bool hardFailure = result.HardConstraintStatus == InventoryHardConstraintStatus.ProvenInfeasible ||
                    result.HardConstraintStatus == InventoryHardConstraintStatus.NotFound;
                if (hardFailure && RuntimeStillMatches(sourceRuntime) && TryGetOpenInventory(out var unchangedInventory) &&
                    MatchesInventory(sourceSnapshot, unchangedInventory))
                    intentFeedback = new InventoryIntentResultFeedback(sourceSnapshot, result.Policy,
                        ExplorationInventoryIntentStore.Capture(), sourceRuntime);
                ShowMessage(result.HardConstraintStatus == InventoryHardConstraintStatus.ProvenInfeasible
                    ? InventoryOptimizationLocalization.HardInfeasible
                    : hardFailure ? InventoryOptimizationLocalization.HardNotFound : InventoryOptimizationLocalization.Unsupported);
                ResetOperationState();
                return;
            }
            if (!result.Improved)
            {
                if (RuntimeStillMatches(sourceRuntime) && TryGetOpenInventory(out var unchangedInventory) &&
                    MatchesInventory(sourceSnapshot, unchangedInventory))
                {
                    intentFeedback = new InventoryIntentResultFeedback(sourceSnapshot, result.Policy,
                        ExplorationInventoryIntentStore.Capture(), sourceRuntime);
                }
                ShowMessage(InventoryOptimizationLocalization.NoImprovementFound);
                ResetOperationState();
                return;
            }
            if (!TryGetOpenInventory(out GridInventory inventory) ||
                !RuntimeStillMatches(sourceRuntime) ||
                !MatchesInventory(sourceSnapshot, inventory))
            {
                ShowMessage(InventoryOptimizationLocalization.Changed);
                ResetOperationState();
                return;
            }
            if (!InventoryLayoutPlanner.TryCreate(sourceSnapshot, result.Layout,
                    out InventoryApplicationPlan applicationPlan, out string _))
            {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                RecordReproduction(InventoryReproductionReason.ApplicationPlanRejected, proposal: result);
#endif
                ShowMessage(InventoryOptimizationLocalization.Failed);
                ResetOperationState();
                return;
            }
            ProjectedInventorySettlement expectedSettlement = InventorySettlementProjector.Evaluate(
                sourceSnapshot, result.Layout);
            if (!expectedSettlement.Succeeded)
            {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                RecordReproduction(InventoryReproductionReason.ProjectionRejected, proposal: result);
#endif
                ShowMessage(InventoryOptimizationLocalization.Unsupported);
                ResetOperationState();
                return;
            }
            // Recheck at the application boundary, including proposals supplied
            // by registered optimizers. Only the final settled layout is constrained.
            if (!new InventoryOptimizationScorer(sourceSnapshot, result.Policy)
                    .Score(result.Layout, expectedSettlement).HardConstraintsSatisfied)
            {
                ShowMessage(InventoryOptimizationLocalization.HardNotFound);
                ResetOperationState();
                return;
            }
            DeveloperLogger.RecordInventoryOptimization(result, applicationPlan,
                runtimeKernel?.State);

            undo = null;
            application = new NativeInventoryLayoutApplication(inventory,
                new InventoryLayoutApplication(sourceSnapshot, sourceRuntime, result,
                    applicationPlan, expectedSettlement, Time.unscaledTime + ApplyTimeout));
            ShowMessage(InventoryOptimizationLocalization.Applying);
        }

        private void ApplyNextStep()
        {
            try
            {
                InventoryApplicationProgress progress = application.Advance(runtimeKernel, Time.unscaledTime,
                    NativeInventoryIntentDrop.HasHeldItem || hud.HasArtifactPickup);
                if (progress == InventoryApplicationProgress.Pending) return;
                if (progress == InventoryApplicationProgress.Completed)
                {
                    CompleteApplication();
                }
                else
                {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                    if (progress == InventoryApplicationProgress.TimedOut)
                    {
                        runtimeKernel.TryGetLatestInventorySnapshot(out InventorySnapshot latest, out _);
                        RecordReproduction(InventoryReproductionReason.ApplicationTimedOut, latest);
                    }
                    if (progress == InventoryApplicationProgress.PositionEffectsChanged)
                        RecordReproduction(InventoryReproductionReason.PositionEffectsChanged, application.ObservedSnapshot);
#endif
                    if (progress == InventoryApplicationProgress.StepRejected)
                    {
                        DeveloperLogger.RecordInventorySettlementDifferential(application.Verification, application.ObservedRuntime);
                        SupportLogger.Record("inventory_application_step_rejected",
                            "mismatches=" + application.Verification.Mismatches.Count, "WARN");
                    }
                    ShowMessage(progress switch
                    {
                        InventoryApplicationProgress.MovingItemInterrupted => InventoryOptimizationLocalization.MovingItemInterrupted,
                        InventoryApplicationProgress.TimedOut => InventoryOptimizationLocalization.ApplyTimedOut,
                        InventoryApplicationProgress.InventoryUnavailable => InventoryOptimizationLocalization.OptimizationUnavailable,
                        InventoryApplicationProgress.GameplayContextChanged => InventoryOptimizationLocalization.GameplayContextChanged,
                        InventoryApplicationProgress.InventoryChanged => InventoryOptimizationLocalization.Changed,
                        InventoryApplicationProgress.PositionEffectsChanged => InventoryOptimizationLocalization.PositionEffectsUnavailable,
                        InventoryApplicationProgress.StepRejected => InventoryOptimizationLocalization.VerificationFailed,
                        _ => throw new InvalidOperationException("Unexpected inventory application progress.")
                    });
                }
                ResetOperationState();
            }
            catch (Exception ex)
            {
                Fail(ex);
            }
        }

        private void CompleteApplication()
        {
            InventorySnapshot actualSnapshot = application.ObservedSnapshot;
            RuntimeStateSnapshot actualRuntime = application.ObservedRuntime;
            bool layoutMatched = application.LayoutMatched;
            InventorySettlementDifferentialReport differential = application.Verification;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            InventoryReproductionReason reason = reproductionCase?.ApplicationReason(layoutMatched, differential.Matched)
                ?? InventoryReproductionReason.None;
            if (reason != InventoryReproductionReason.None)
                RecordReproduction(reason, actualSnapshot, differential);
#endif
            DeveloperLogger.RecordInventoryApplication(layoutMatched,
                application.State.Plan, application.State.NextSwap, application.State.NextRotation,
                actualRuntime);
            DeveloperLogger.RecordInventorySettlementDifferential(
                differential, actualRuntime);
            SupportLogger.Record("inventory_application_completed", "layoutMatched=" + layoutMatched +
                " settlementMatched=" + differential.Matched + " mismatches=" + differential.Mismatches.Count +
                " swaps=" + application.State.NextSwap + " rotations=" + application.State.NextRotation,
                layoutMatched && differential.Matched ? "INFO" : "WARN");
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
            else if (application.State.IsUndo)
            {
                LastAppliedOutcome = null;
                intentFeedback = null;
                ShowMessage(InventoryArrangementLocalization.Undone);
            }
            else
            {
                undo = new InventoryArrangementUndo(application.State.SourceSnapshot, actualRuntime);
                LastAppliedOutcome = application.State.Proposal.Outcome;
                intentFeedback = new InventoryIntentResultFeedback(actualSnapshot, application.State.Proposal.Policy,
                    ExplorationInventoryIntentStore.Capture(), actualRuntime);
                ShowMessage(InventoryOptimizationLocalization.Completed);
            }
        }

        private bool RuntimeStillMatches(RuntimeStateSnapshot sourceRuntime)
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
            bool inventoryOptimizationAvailable = TryGetOpenInventory(
                out GridInventory currentInventory);
            RuntimeStateSnapshot currentRuntime = runtimeKernel?.State;
            bool gameplayContextMatches = currentRuntime != null &&
                search.SourceRuntime != null &&
                currentRuntime.GameplayContextEpoch ==
                    search.SourceRuntime.GameplayContextEpoch &&
                currentRuntime.PlayerNetId == search.SourceRuntime.PlayerNetId;
            bool inventoryRevisionMatches = gameplayContextMatches &&
                currentRuntime.InventoryRevision == search.SourceRuntime.InventoryRevision;
            bool sourceLayoutMatches = inventoryOptimizationAvailable &&
                search.SourceSnapshot != null &&
                MatchesInventory(search.SourceSnapshot, currentInventory);
            InventoryArrangementInvalidationReason reason =
                InventoryArrangementLifecyclePolicy.Evaluate(
                    InventoryArrangementOperationPhase.Searching,
                    EnhancementsSettings.Enabled, inventoryOptimizationAvailable,
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

        private static bool TryGetOpenInventory(out GridInventory inventory)
        {
            return NativeInventoryOptimizationContext.TryGetOpenInventory(
                out inventory);
        }

        private static bool MatchesInventory(InventorySnapshot snapshot,
            GridInventory inventory)
        {
            return snapshot != null && MatchesLayout(inventory, snapshot,
                InventoryLayoutProjection.Current(snapshot));
        }

        private void Fail(Exception exception)
        {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            if (application != null)
                RecordReproduction(InventoryReproductionReason.ApplicationException, exception: exception);
#endif
            compatible = false;
            SupportLogger.Failure("inventory_operation_failed", exception);
            SupportLogger.Warning("inventory_context_disabled", "[SephiriaEnhancements] Inventory optimization " +
                "disabled for the current gameplay context: " +
                (exception?.Message ?? "unknown failure"));
            ShowMessage(InventoryOptimizationLocalization.
                DisabledForGameplayContext);
            ResetOperationState();
        }

        private void ResetOperationState()
        {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
            reproductionCase = null;
#endif
            search?.Dispose();
            search = null;
            application = null;
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        private static void ShowMessage(string key)
        {
            SupportLogger.Record("inventory_message", "code=" + key);
            UI_SystemMessage message =
                UIManager.Instance?.GetElement<UI_SystemMessage>();
            message?.Open(Loc._(key), 2f);
        }
    }
}
