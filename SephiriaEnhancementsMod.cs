using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using HarmonyLib;
using System;
using SephiriaEnhancements.CombatRelationOutlines;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.RangedControls;
using SephiriaEnhancements.ViewDistance;
using SephiriaEnhancements.NativeCompanion;
using SephiriaEnhancements.MapEnhancements;
using SephiriaEnhancements.BossHealthDisplay;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.DeveloperConsole;
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerAccess.Integration;
using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.KeyboardUiNavigation;
using SephiriaEnhancements.Runtime.GameBridge;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using SephiriaEnhancements.DeveloperTools;
#endif
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SephiriaEnhancements
{
    public sealed class SephiriaEnhancementsMod : HorayModBase
    {
        private const string HarmonyId = "io.github.0xmashiro.sephiria-enhancements";
        private static readonly Type[] MultiplayerRuleBehaviorPatchTypes =
        {
            typeof(EnemySpawnRoutineOriginPatch),
            typeof(AvatarSpawnOriginCapturePatch),
            typeof(NetworkSpawnOriginCapturePatch),
            typeof(EnemyHealthInitializationPatch),
            typeof(SeedEncounterBossSpawnOriginPatch),
            typeof(MindEaterRootSummonOriginPatch),
            typeof(MonsterSpawnEntryMultiplierPatch),
            typeof(TargetedExperienceOrbDivisorPatch),
            typeof(MoneyAwardRulePatch),
            typeof(PlayerMoneyAwardAmountPatch),
            typeof(StandardBossRulesPatch),
            typeof(QliphothSealRulePatch),
            typeof(QliphothFinalBattleGridRulePatch),
            typeof(QliphothFinalBattleEntryTrackingRulePatch),
            typeof(QliphothTempleTrioActiveCountRulePatch),
            typeof(MerchantGenerationRuleContextPatch),
            typeof(MerchantCandidateRulePatch),
            typeof(SafeMerchantInventoryRulePatch),
            typeof(DirectMerchantInventoryRulePatch),
            typeof(FestivalOfBloodHealingRulePatch),
            typeof(HiddenRoomBreakableRewardCountRulePatch),
            typeof(HiddenRoomNativeBreakableSuppressionPatch),
            typeof(FloorGenerationRuleContextPatch),
            typeof(EnemyGroupDifficultyOffsetRulePatch),
            typeof(LifeSupplyCreatePropRulePatch)
        };
        private static readonly Type[] MidRunAdmissionPatchTypes =
        {
            typeof(MidRunAuthenticationPatch),
            typeof(MidRunDungeonAccessPatch),
            typeof(MidRunReconnectSupportPatch),
            typeof(FreshPlayerStartingItemPatch),
            typeof(MidRunLobbyAvailabilityPatch),
            typeof(FreshPlayerSaveSlotPatch),
            typeof(MidRunDisconnectCleanupPatch),
            typeof(MidRunServerCleanupPatch)
        };

        private GameObject controllerObject;
        private CombatRelationOutlinesController combatRelationOutlines;
        private CombatInsightsController combatInsights;
        private RangedControlsController rangedControls;
        private NativeCompanionController nativeCompanion;
        private KeyboardUiNavigationController keyboardUiNavigation;
        private MapEnhancementsController mapEnhancements;
        private RuntimeKernel runtimeKernel;
        private InventoryOptimizationController inventoryOptimization;
        private MultiplayerRulesController multiplayerRules;
        private Harmony harmony;
        private bool multiplayerRulesCompatibilityAvailable;
        private bool multiplayerRuleBehaviorPatchesAttempted;
        private bool multiplayerRuleBehaviorPatchesInstalled;

        protected override void OnModLoaded()
        {
            SupportLogger.Initialize();
            Application.quitting += SupportLogger.Shutdown;
            StartupProfiler.Begin();
            GameLoadProfiler.Reset();
            long loadStartedAt = Stopwatch.GetTimestamp();
            long phaseStartedAt = loadStartedAt;
            SephiriaEnhancements.Integration.CompatibilityProbe.Report();
            float compatibilityMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            HorayModAPI.OnLocalizationReady +=
                SephiriaEnhancements.Configuration.ModLocalization.Register;
            SephiriaEnhancements.Configuration.ModLocalization.RegisterCurrent();
            HorayModAPI.OnStartSessionClientside += OnStartSessionClientside;
            HorayModAPI.OnFloorAllocatedClientside += OnFloorAllocatedClientside;
            HorayModAPI.OnStartSessionServerside += OnStartSessionServerside;
            HorayModAPI.OnFloorAllocatedServerside += OnFloorAllocatedServerside;
            MultiplayerRulesExplorationStartPatch.StartingExploration +=
                OnStartingExploration;
            float localizationMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            try
            {
                NativeControlCoordinator.Initialize();
                NativeControlCoordinator.PreparePlayerInput(PlayerInputController.Instance);
            }
            catch (Exception ex)
            {
                SupportLogger.Warning("controls_initialization_failed", "[SephiriaEnhancements] Native control bindings " +
                    "could not be initialized: " + ex.Message);
            }
            float controlsMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            controllerObject = new GameObject("Sephiria Enhancements");
            UnityEngine.Object.DontDestroyOnLoad(controllerObject);
            runtimeKernel = controllerObject.AddComponent<RuntimeKernel>();
            runtimeKernel.Initialize();
            runtimeKernel.GameplayContextChanged += OnLocalGameplayContextChanged;
            inventoryOptimization =
                controllerObject.AddComponent<InventoryOptimizationController>();
            inventoryOptimization.Initialize(runtimeKernel);
            multiplayerRules = controllerObject.AddComponent<MultiplayerRulesController>();
            EnemySpawnRoutineContext.SetRuleScopeFactory(
                EnemySpawnRoutineRuleScope.Enter);
            combatRelationOutlines =
                controllerObject.AddComponent<CombatRelationOutlinesController>();
            combatInsights = controllerObject.AddComponent<CombatInsightsController>();
            combatInsights.Initialize(runtimeKernel);
            NativeReportDismissal.SetController(combatInsights);
            rangedControls = controllerObject.AddComponent<RangedControlsController>();
            nativeCompanion = controllerObject.AddComponent<NativeCompanionController>();
            keyboardUiNavigation =
                controllerObject.AddComponent<KeyboardUiNavigationController>();
            mapEnhancements = controllerObject.AddComponent<MapEnhancementsController>();
            DamageFeedbackCapture.SetController(combatInsights);
            DamageDetailCapture.SetController(combatInsights);
            UnitDeathCapture.SetController(combatInsights);
            LocalFinalBlowCapture.SetController(combatInsights);
            float controllersMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            harmony = new Harmony(HarmonyId);
            int successfulPatchCount = 0;
            int failedPatchCount = 0;
            multiplayerRulesCompatibilityAvailable =
                MultiplayerRulesCompatibilityProbe.Validate();
            bool multiplayerExtensionPresent =
                MultiplayerExtensionDiscovery.HasDetectedExtension;
            bool midRunAdmissionCompatibilityAvailable =
                !multiplayerExtensionPresent &&
                MidRunAdmissionCompatibilityProbe.Validate();
            string slowestPatchName = string.Empty;
            float slowestPatchMilliseconds = 0f;
            foreach (Type patchType in new[]
            {
                typeof(DamageFeedbackCapture),
                typeof(DamageDetailCapture),
                typeof(UnitDeathCapture),
                typeof(LocalFinalBlowCapture),
                typeof(NativeReportDismissal),
                typeof(NativeOrdinaryEncounterClearedPatch),
                typeof(NativeBossEncounterStartedPatch),
                typeof(NativeBossEncounterDefeatedPatch),
                typeof(NativeBossEncounterCompletionStartedPatch),
                typeof(NativeBossEncounterCompletedPatch),
                typeof(NativeBossEncounterPausedPatch),
                typeof(NativeBossEncounterResumedPatch),
                typeof(NativeSeedBossEncounterStartedPatch),
                typeof(NativeSeedBossEncounterDefeatedPatch),
                typeof(NativeSeedBossEncounterCompletionStartedPatch),
                typeof(NativeSeedBossEncounterCompletedPatch),
                typeof(BossHealthValuePatch),
                typeof(SephiriaEnhancements.Configuration.OptionsPanelPatch),
                typeof(SephiriaEnhancements.Configuration.NativeControlOptionsClosedPatch),
                typeof(CombatVisualOptionReadPatch),
                typeof(CompanionBodyTransparencyPatch),
                typeof(CompanionBulletTransparencyPatch),
                typeof(CompanionAreaJudgementTransparencyPatch),
                typeof(CompanionSpreadAoeTransparencyPatch),
                typeof(CompanionMeleeTransparencyPatch),
                typeof(CompanionBulletTailTransparencyPatch),
                typeof(CompanionBulletHitTransparencyPatch),
                typeof(CompanionBulletDestroyTransparencyPatch),
                typeof(CompanionChainLightningTransparencyPatch),
                typeof(DeveloperConsoleOpenPatch),
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
                typeof(DeveloperPlayerDamagePatch),
                typeof(NativeStartupProfilingPatch),
                typeof(NativeLoadingOperationProfilingPatch),
                typeof(NativeLoadingStateProfilingPatch),
                typeof(NativeFloorRenderProfilingPatch),
#endif
                typeof(NativeSaveCapturePatch),
                typeof(BossEncounterRetryCheckpointPatch),
                typeof(SeedBossEncounterRetryCheckpointPatch),
                typeof(ApplyDefeatRetryPlacementPatch),
                typeof(GameOverDefeatRetryButtonPatch),
                typeof(PreserveDefeatRetrySaveDeletionPatch),
                typeof(PreserveDefeatRetrySaveCreationPatch),
                typeof(PreserveDefeatRetryLobbyPatch),
                typeof(PreserveDefeatRetryRejoinStatePatch),
                typeof(DefeatRetryNewGamePatch),
                typeof(KeyboardBasicAttackPatch),
                typeof(KeyboardSpecialAttackPatch),
                typeof(ViewDistancePatch),
                typeof(MessageBoxKeyboardInitialSelectionPatch),
                typeof(MessageBoxKeyboardRestoredSelectionPatch),
                typeof(OptionsKeyboardEmptyFocusPatch),
                typeof(KeyboardControlsChangedPatch),
                typeof(ItemIconKeyboardSubmitPatch),
                typeof(ItemBoxKeyboardSecondaryActionPatch),
                typeof(TreeShopKeyboardSecondaryActionPatch),
                typeof(MapPanelShowPatch),
                typeof(MapPanelOpenedPatch),
                typeof(MapPanelClosedPatch),
                typeof(NativeInventoryItemSelectionModePatch),
                typeof(InventoryArtifactIntentClickPatch),
                typeof(InventoryArtifactIntentInputPatch),
                typeof(InventoryArtifactIntentClosedPatch),
                typeof(InventoryArtifactIntentModePatch),
                typeof(InventoryTemporaryItemDropPatch),
                typeof(SephiriaEnhancements.Runtime.GameBridge.Inventory.
                    InventoryEvaluationOrderTraceStartPatch),
                typeof(SephiriaEnhancements.Runtime.GameBridge.Inventory.
                    ArtifactCategoryRefreshOrderPatch),
                typeof(SephiriaEnhancements.Runtime.GameBridge.Inventory.
                    ArtifactRefreshOrderPatch),
                typeof(SephiriaEnhancements.Runtime.GameBridge.Inventory.
                    UniqueEffectRegistrationTracePatch),
                typeof(MultiplayerRulesNetworkSessionEndPatch),
                typeof(MultiplayerRulesExplorationStartPatch)
            })
            {
                if (TryPatch(patchType, out float patchMilliseconds))
                {
                    successfulPatchCount++;
                    if (patchType == typeof(NativeReportDismissal))
                        NativeReportDismissal.IsAvailable = true;
                }
                else
                {
                    failedPatchCount++;
                    if (patchType.Namespace ==
                        "SephiriaEnhancements.MultiplayerRules.Integration")
                        multiplayerRulesCompatibilityAvailable = false;
                }
                if (patchMilliseconds > slowestPatchMilliseconds)
                {
                    slowestPatchName = patchType.Name;
                    slowestPatchMilliseconds = patchMilliseconds;
                }
            }
            if (midRunAdmissionCompatibilityAvailable)
            {
                foreach (Type patchType in MidRunAdmissionPatchTypes)
                {
                    if (TryPatch(patchType, out float patchMilliseconds))
                    {
                        successfulPatchCount++;
                    }
                    else
                    {
                        failedPatchCount++;
                        midRunAdmissionCompatibilityAvailable = false;
                    }
                    if (patchMilliseconds > slowestPatchMilliseconds)
                    {
                        slowestPatchName = patchType.Name;
                        slowestPatchMilliseconds = patchMilliseconds;
                    }
                }
            }
            MidRunAdmissionRuntime.SetIntegrationAvailable(
                midRunAdmissionCompatibilityAvailable);
            if (multiplayerExtensionPresent)
                SupportLogger.Info("mid_run_admission_delegated", "[SephiriaEnhancements] Mid-run admission is delegated " +
                    "to the detected multiplayer extension.");
            else if (!midRunAdmissionCompatibilityAvailable)
                SupportLogger.Warning("mid_run_admission_unavailable", "[SephiriaEnhancements] Mid-run admission is " +
                    "disabled because a required native hook failed.");
            MultiplayerRulesController.SetIntegrationAvailable(
                multiplayerRulesCompatibilityAvailable);
            if (!multiplayerRulesCompatibilityAvailable)
                SupportLogger.Warning("multiplayer_rules_unavailable", "[SephiriaEnhancements] Multiplayer Rules " +
                    "are pass-through because at least one required native hook failed.");
            float patchesMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            float loadMilliseconds = ElapsedMilliseconds(loadStartedAt);
            SupportLogger.Record("mod_load_completed", "successfulHooks=" + successfulPatchCount +
                " failedHooks=" + failedPatchCount + " elapsedMs=" +
                loadMilliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            DeveloperLogger.RecordModLoadMetrics(loadMilliseconds,
                compatibilityMilliseconds, localizationMilliseconds,
                controlsMilliseconds, controllersMilliseconds,
                patchesMilliseconds, successfulPatchCount, failedPatchCount,
                slowestPatchName, slowestPatchMilliseconds);
            StartupProfiler.RecordMilestone("mod_initialized");
            SupportLogger.Info("mod_loaded", "[SephiriaEnhancements] Loaded in " + loadMilliseconds.ToString("F1") +
                " ms with " + successfulPatchCount + " compatibility hooks. " +
                "Configure features under Gameplay options.");
        }

        protected override void OnModUnloaded()
        {
            multiplayerRules?.Shutdown();
            MidRunAdmissionRuntime.SetIntegrationAvailable(false);
            EnemySpawnRoutineContext.SetRuleScopeFactory(null);
            inventoryOptimization?.Shutdown();
            combatInsights?.Shutdown();
            NativeReportDismissal.SetController(null);
            DeveloperLogger.Shutdown();
            if (runtimeKernel != null)
            {
                runtimeKernel.GameplayContextChanged -= OnLocalGameplayContextChanged;
                runtimeKernel.Dispose();
            }
            HorayModAPI.OnLocalizationReady -=
                SephiriaEnhancements.Configuration.ModLocalization.Register;
            HorayModAPI.OnStartSessionClientside -= OnStartSessionClientside;
            HorayModAPI.OnFloorAllocatedClientside -= OnFloorAllocatedClientside;
            HorayModAPI.OnStartSessionServerside -= OnStartSessionServerside;
            HorayModAPI.OnFloorAllocatedServerside -= OnFloorAllocatedServerside;
            MultiplayerRulesExplorationStartPatch.StartingExploration -=
                OnStartingExploration;
            DamageFeedbackCapture.SetController(null);
            DamageDetailCapture.SetController(null);
            UnitDeathCapture.SetController(null);
            LocalFinalBlowCapture.SetController(null);
            BossHealthValueFeature.DisposeAll();
            harmony?.UnpatchAll(HarmonyId);
            harmony = null;
            multiplayerRulesCompatibilityAvailable = false;
            multiplayerRuleBehaviorPatchesAttempted = false;
            multiplayerRuleBehaviorPatchesInstalled = false;

            if (controllerObject != null)
            {
                UnityEngine.Object.Destroy(controllerObject);
                controllerObject = null;
            }

            combatInsights = null;
            combatRelationOutlines = null;
            rangedControls = null;
            nativeCompanion = null;
            keyboardUiNavigation = null;
            mapEnhancements = null;
            inventoryOptimization = null;
            multiplayerRules = null;
            runtimeKernel = null;
            Application.quitting -= SupportLogger.Shutdown;
            SupportLogger.Shutdown();
        }

        private void OnStartSessionClientside(bool isSavedSession)
        {
            GameLoadProfiler.ObserveClientSessionStarted(isSavedSession);
            MultiplayerRulesLobbySnapshotCoordinator.ReadHostSnapshot();
            inventoryOptimization?.ResetExploration();
            runtimeKernel?.BeginWorldSession();
        }

        private void OnFloorAllocatedClientside(string guid, string floorName,
            FloorGenerator generator)
        {
            GameLoadProfiler.ObserveFloorAllocated(guid, floorName);
            MultiplayerRulesLobbySnapshotCoordinator.ReadHostSnapshot();
        }

        private void OnLocalGameplayContextChanged(LocalGameplayContextChange change)
        {
            inventoryOptimization?.ResetGameplayContext();
            combatRelationOutlines?.ResetGameplayContext();
            rangedControls?.ResetGameplayContext();
            keyboardUiNavigation?.ResetGameplayContext();
            mapEnhancements?.ResetGameplayContext();
            nativeCompanion?.ResetGameplayContext();
            GameLoadProfiler.ObserveGameplayContextReset();
        }

        private void OnStartSessionServerside(bool isSavedSession)
        {
            GameLoadProfiler.ObserveServerSessionStarted(isSavedSession);
            if (MultiplayerRulesExplorationStartPatch.ExplorationStarted)
                BeginServerExploration(isSavedSession);
            else
                MultiplayerRulesController.EndExploration();
            nativeCompanion?.ResetSession();
        }

        private void OnStartingExploration() => BeginServerExploration(false);

        private void BeginServerExploration(bool isSavedSession)
        {
            if (RequiresMultiplayerRuleBehaviorPatches(isSavedSession))
            {
                EnsureMultiplayerRuleBehaviorPatches();
            }
            multiplayerRules?.BeginServerExploration(isSavedSession);
        }

        private void OnFloorAllocatedServerside(string guid, string floorName,
            FloorGenerator generator)
        {
            multiplayerRules?.PublishActiveRulesForLobbyDisplay();
        }

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }

        private static bool RequiresMultiplayerRuleBehaviorPatches(
            bool isSavedSession)
        {
            MultiplayerRulesPreset preset;
            if (isSavedSession && ActiveExplorationRulesStore.TryRead(
                    out ActiveExplorationMultiplayerRules restoredRules))
            {
                preset = restoredRules.Preset;
            }
            else
            {
                preset = PreferredMultiplayerRulesStore.Read().Preset;
            }
            return MultiplayerRulesLifecyclePolicy.
                RequiresNativeBehaviorHooks(preset);
        }

        private void EnsureMultiplayerRuleBehaviorPatches()
        {
            if (!multiplayerRulesCompatibilityAvailable ||
                multiplayerRuleBehaviorPatchesInstalled ||
                multiplayerRuleBehaviorPatchesAttempted)
            {
                return;
            }

            multiplayerRuleBehaviorPatchesAttempted = true;
            bool succeeded = true;
            long startedAt = Stopwatch.GetTimestamp();
            foreach (Type patchType in MultiplayerRuleBehaviorPatchTypes)
            {
                if (!TryPatch(patchType, out _))
                {
                    succeeded = false;
                }
            }

            multiplayerRuleBehaviorPatchesInstalled = succeeded;
            if (!succeeded)
            {
                multiplayerRulesCompatibilityAvailable = false;
                MultiplayerRulesController.SetIntegrationAvailable(false);
                SupportLogger.Warning("multiplayer_rules_deferred_hooks_failed", "[SephiriaEnhancements] Multiplayer Rules " +
                    "are pass-through because at least one deferred native hook failed.");
            }
            SupportLogger.Info("multiplayer_rules_hooks_completed", "[SephiriaEnhancements] Multiplayer Rules behavior hooks " +
                (succeeded ? "installed" : "failed") + " in " +
                ElapsedMilliseconds(startedAt).ToString("F1") + " ms.");
        }

        private bool TryPatch(Type patchType, out float elapsedMilliseconds)
        {
            long startedAt = Stopwatch.GetTimestamp();
            elapsedMilliseconds = 0f;
            float preparationMilliseconds = 0f;
            float applicationMilliseconds = 0f;
            bool succeeded = false;
            try
            {
                PatchClassProcessor processor =
                    harmony.CreateClassProcessor(patchType);
                preparationMilliseconds = ElapsedMilliseconds(startedAt);
                long applicationStartedAt = Stopwatch.GetTimestamp();
                processor.Patch();
                applicationMilliseconds =
                    ElapsedMilliseconds(applicationStartedAt);
                succeeded = true;
                return true;
            }
            catch (Exception ex)
            {
                SupportLogger.Failure("hook_failed." + patchType.Name, ex);
                SupportLogger.Warning("feature_hook_failed", "[SephiriaEnhancements] Feature disabled because hook failed: " +
                    patchType.Name + " — " + ex.Message);
                return false;
            }
            finally
            {
                elapsedMilliseconds = ElapsedMilliseconds(startedAt);
                DeveloperLogger.RecordModPatch(patchType.FullName, succeeded,
                    elapsedMilliseconds, preparationMilliseconds,
                    applicationMilliseconds);
            }
        }
    }
}
