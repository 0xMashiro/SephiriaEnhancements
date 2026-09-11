using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using HarmonyLib;
using System;
using SephiriaEnhancements.CombatRelationOutlines;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.CombatTargeting;
using SephiriaEnhancements.ViewDistance;
using SephiriaEnhancements.NativeCompanion;
using SephiriaEnhancements.MapEnhancements;
using SephiriaEnhancements.ModJournal.Integration;
using SephiriaEnhancements.ResourceBarValues.Integration;
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
using SephiriaEnhancements.AutoCasting.Integration;
using SephiriaEnhancements.Runtime.GameBridge;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using SephiriaEnhancements.DeveloperTools;
#endif
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SephiriaEnhancements
{
    public sealed partial class SephiriaEnhancementsMod : HorayModBase
    {
        private const string HarmonyId = "io.github.0xmashiro.sephiria-enhancements";
        private static SephiriaEnhancementsMod activeInstance;
        private static readonly Type[] MultiplayerRuleBehaviorPatchTypes =
        {
            typeof(EnemySpawnRoutineOriginPatch),
            typeof(AvatarSpawnOriginCapturePatch),
            typeof(NetworkSpawnOriginCapturePatch),
            typeof(EnemyHealthInitializationPatch),
            typeof(KrazBossSpawnOriginPatch),
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
            typeof(MidRunServerCleanupPatch),
            typeof(JoiningSupplyInitializationPatch),
            typeof(JoiningSupplyExplorationPatch),
            typeof(JoiningSupplyLoadPatch),
            typeof(JoiningSupplySpawnPatch),
            typeof(JoiningSupplyLevelRewardPatch),
            typeof(JoiningSupplyLevelFeedbackPatch),
            typeof(JoiningSupplyFinishPatch),
            typeof(JoiningSupplyTabletPatch),
            typeof(JoiningSupplyCheckpointPatch),
            typeof(JoiningSupplySavePatch),
            typeof(JoiningSupplyDisconnectPatch),
            typeof(JoiningSupplyCommandOwnershipPatch),
            typeof(JoiningSupplyRewardOwnershipPatch),
            typeof(JoiningSupplyWriteSavePatch),
            typeof(JoiningSupplyDespawnPatch),
            typeof(JoiningSupplyFacilityDicePatch),
            typeof(JoiningSupplyInteractionOwnershipPatch),
            typeof(JoiningSupplyEmbeddedMiraclePatch),
            typeof(JoiningSupplyEmbeddedAnvilPatch),
            typeof(JoiningSupplyMiracleConfirmationPatch),
            typeof(JoiningSupplyAnvilSelectionPatch),
            typeof(JoiningSupplyAnvilConfirmationPatch),
            typeof(JoiningSupplyEnchantConfirmationPatch),
            typeof(NativeJoiningSupplyPauseEntry)
        };

        private GameObject controllerObject;
        private CombatRelationOutlinesController combatRelationOutlines;
        private CombatInsightsController combatInsights;
        private CombatTargetingController combatTargeting;
        private NativeCompanionController nativeCompanion;
        private KeyboardUiNavigationController keyboardUiNavigation;
        private NativeAutoCasting autoCasting;
        private MapEnhancementsController mapEnhancements;
        private RuntimeKernel runtimeKernel;
        private InventoryOptimizationController inventoryOptimization;
        private MultiplayerRulesController multiplayerRules;

        private bool multiplayerRulesCompatibilityAvailable;
        private bool multiplayerRuleBehaviorPatchesAttempted;
        private bool multiplayerRuleBehaviorPatchesInstalled;

        protected override void OnModLoaded()
        {
            try { Load(); }
            catch (Exception exception)
            {
                SupportLogger.Failure("mod_startup_failed", exception);
                try { OnModUnloaded(); }
                catch (Exception cleanup) { SupportLogger.Failure("mod_cleanup_failed", cleanup); }
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Load()
        {
            // The native loader can load again without unloading its previous instance.
            activeInstance?.OnModUnloaded();
            activeInstance = this;
            FeatureFailure.Reset();
            FeatureFailure.Report = ReportFeatureFailure;
            SupportLogger.Initialize();
            Application.quitting += OnModUnloaded;
            StartupProfiler.Begin();
            GameLoadProfiler.Reset();
            long loadStartedAt = Stopwatch.GetTimestamp();
            long phaseStartedAt = loadStartedAt;
            FeatureFailure.Run(FeatureId.DeveloperTools, SephiriaEnhancements.Integration.CompatibilityProbe.Report);
            FeatureFailure.Run(FeatureId.MapEnhancements, CompatibilityProbe.ValidateMapText);
            FeatureFailure.Run(FeatureId.Inventory, CompatibilityProbe.ValidateCustomStats);
            FeatureFailure.Run(FeatureId.AutoCasting, CompatibilityProbe.ValidateCustomStats);
            float compatibilityMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            HorayModAPI.OnLocalizationReady += RegisterLocalization;
            FeatureFailure.Run(FeatureId.Settings, () => SephiriaEnhancements.Configuration.ModLocalization.RegisterCurrent());
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
                FeatureFailure.Disable(FeatureId.KeyboardUiNavigation, ex);
                SupportLogger.Warning("controls_initialization_failed", "[SephiriaEnhancements] Native control bindings " +
                    "could not be initialized: " + ex.Message);
            }
            float controlsMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();
            controllerObject = new GameObject("Sephiria Enhancements");
            UnityEngine.Object.DontDestroyOnLoad(controllerObject);
            controllerObject.AddComponent<FeatureFailurePump>().Process = ProcessFeatureFailures;
            InitializeFeature(FeatureId.Gameplay, () =>
            {
                runtimeKernel = AddController<RuntimeKernel>(FeatureId.Gameplay);
                runtimeKernel.Initialize();
                runtimeKernel.GameplayContextChanged += OnLocalGameplayContextChanged;
            });
            InitializeFeature(FeatureId.ModInformation, () =>
                AddController<ModInformation.Integration.NativeModInformation>(FeatureId.ModInformation));
            InitializeFeature(FeatureId.Inventory, () =>
            {
                inventoryOptimization = AddController<InventoryOptimizationController>(FeatureId.Inventory);
                inventoryOptimization.Initialize(runtimeKernel);
            });
            InitializeFeature(FeatureId.MultiplayerRules, () =>
            {
                multiplayerRules = AddController<MultiplayerRulesController>(FeatureId.MultiplayerRules);
                EnemySpawnRoutineContext.SetRuleScopeFactory(EnemySpawnRoutineRuleScope.Enter);
            });
            InitializeFeature(FeatureId.CombatRelationOutlines, () =>
                combatRelationOutlines = AddController<CombatRelationOutlinesController>(FeatureId.CombatRelationOutlines));
            InitializeFeature(FeatureId.CombatInsights, () =>
            {
                combatInsights = AddController<CombatInsightsController>(FeatureId.CombatInsights);
                combatInsights.Initialize(runtimeKernel);
                NativeReportDismissal.SetController(combatInsights);
                NativeStatisticsPauseEntry.SetController(combatInsights);
                DamageFeedbackCapture.SetController(combatInsights);
                DamageDetailCapture.SetController(combatInsights);
                UnitDeathCapture.SetController(combatInsights);
                LocalFinalBlowCapture.SetController(combatInsights);
            });
            InitializeFeature(FeatureId.DefeatRetry, () =>
            {
                DefeatRetryBridge.Initialize(combatInsights);
                AddController<DefeatRetryRuntime>(FeatureId.DefeatRetry);
            });
            InitializeFeature(FeatureId.MultiplayerAccess, () =>
                AddController<NativeJoiningSupplies>(FeatureId.MultiplayerAccess));
            InitializeFeature(FeatureId.CombatTargeting, () =>
                combatTargeting = AddController<CombatTargetingController>(FeatureId.CombatTargeting));
            InitializeFeature(FeatureId.AutoCasting, () =>
                autoCasting = AddController<NativeAutoCasting>(FeatureId.AutoCasting));
            InitializeFeature(FeatureId.NativeCompanion, () =>
                nativeCompanion = AddController<NativeCompanionController>(FeatureId.NativeCompanion));
            InitializeFeature(FeatureId.KeyboardUiNavigation, () =>
                keyboardUiNavigation = AddController<KeyboardUiNavigationController>(FeatureId.KeyboardUiNavigation));
            InitializeFeature(FeatureId.MapEnhancements, () =>
                mapEnhancements = AddController<MapEnhancementsController>(FeatureId.MapEnhancements));
            float controllersMilliseconds = ElapsedMilliseconds(phaseStartedAt);

            phaseStartedAt = Stopwatch.GetTimestamp();

            int successfulPatchCount = 0;
            int failedPatchCount = 0;
            multiplayerRulesCompatibilityAvailable = ValidateFeature(FeatureId.MultiplayerRules,
                MultiplayerRulesCompatibilityProbe.Validate);
            bool multiplayerExtensionPresent =
                MultiplayerExtensionDiscovery.HasDetectedExtension;
            bool midRunAdmissionCompatibilityAvailable =
                !multiplayerExtensionPresent &&
                ValidateFeature(FeatureId.MultiplayerAccess, MidRunAdmissionCompatibilityProbe.Validate);
            string slowestPatchName = string.Empty;
            float slowestPatchMilliseconds = 0f;
            bool retryCompatibilityAvailable = ValidateFeature(FeatureId.DefeatRetry, () =>
                NativeRetryTravel.IsAvailable && NativeRetryBoss.IsAvailable && NativeRetryRestart.IsAvailable &&
                DefeatRetryClientRestore.IsAvailable && DefeatRetryPlayerRestorePatch.IsAvailable);
            foreach (Type patchType in new[]
            {
                typeof(DamageFeedbackCapture),
                typeof(DamageDetailCapture),
                typeof(UnitDeathCapture),
                typeof(LocalFinalBlowCapture),
                typeof(NativeReportDismissal),
                typeof(NativeStatisticsPauseEntry),
                typeof(ModJournalRefreshPatch),
                typeof(EffectStats.Integration.EffectStatsPanelPatch),
                typeof(ModJournalClearPatch),
                typeof(ModJournalCategoryPatch),
                typeof(ModJournalTutorialPatch),
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
                typeof(NativeBossBarValuesPatch),
                typeof(NativeUnitBarValuesPatch),
                typeof(NativePropBarValuesPatch),
                typeof(NativePlayerBarValuesPatch),
                typeof(NativeCompanionBarValuesPatch),
                typeof(NativeManaBarValuesPatch),
                typeof(SephiriaEnhancements.Configuration.OptionsPanelPatch),
                typeof(ModLanguageLoadPatch),
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
                typeof(NativePresetSavePatch),
                typeof(BossEncounterRetryCheckpointPatch),
                typeof(SeedBossEncounterRetryCheckpointPatch),
                typeof(RenderedCombatFloorRetryCheckpointPatch),
                typeof(ApplyDefeatRetryPlacementPatch),
                typeof(DefeatRetryPlayerRestorePatch),
                typeof(DefeatRetryClientNotificationPatch),
                typeof(DefeatRetryCutscenePatch),
                typeof(NativeRetryRestart),
                typeof(DefeatRetryTravelRequestPatch),
                typeof(BossRetryPropRecipePatch),
                typeof(BossRetryPreserveFloorPatch),
                typeof(GameOverDefeatRetryButtonPatch),
                typeof(PreserveDefeatRetrySaveDeletionPatch),
                typeof(PreserveDefeatRetrySaveCreationPatch),
                typeof(PreserveDefeatRetryLobbyPatch),
                typeof(PreserveDefeatRetryRejoinStatePatch),
                typeof(DefeatRetryNewGamePatch),
                typeof(CombatTargetingInputPatch),
                typeof(AutoCastingManualInputPatch),
                typeof(AutoCastingPanelPatch),
                typeof(AutoCastingOptionsPatch),
                typeof(AutoCastingSubmitPatch),
                typeof(SkillNavigationMovePatch),
                typeof(SkillNavigationTogglePatch),
                typeof(CombatTargetingCastPatch),
                typeof(CombatTargetingReleasePatch),
                typeof(CombatTargetingDashPatch),
                typeof(ViewDistancePatch),
                typeof(MenuKeyboardSelectionPatch),
                typeof(RewardKeyboardGeneratedSelectionPatch),
                typeof(RewardKeyboardControlSelectionPatch),
                typeof(RewardKeyboardInventoryOpenedSelectionPatch),
                typeof(RewardKeyboardToggleSelectionPatch),
                typeof(RewardKeyboardClosedSelectionPatch),
                typeof(RewardKeyboardCancelSelectionPatch),
                typeof(OptionsKeyboardTabSelectionPatch),
                typeof(OptionsKeyboardMovePatch),
                typeof(KeyboardPointerInputPatch),
                typeof(KeyboardPointerHoverPatch),
                typeof(KeyboardCursorVisibilityPatch),
                typeof(KeyboardCarriedItemPositionPatch),
                typeof(KeyboardMapSelectionPositionPatch),
                typeof(MessageBoxKeyboardInitialSelectionPatch),
                typeof(MessageBoxKeyboardRestoredSelectionPatch),
                typeof(OptionsKeyboardEmptyFocusPatch),
                typeof(KeyboardControlsChangedPatch),
                typeof(ItemIconKeyboardSubmitPatch),
                typeof(ItemBoxKeyboardSecondaryActionPatch),
                typeof(TreeShopKeyboardSecondaryActionPatch),
                typeof(MapPanelShowPatch),
                typeof(MapNavigationUpdatePatch),
                typeof(MapPanelOpenedPatch),
                typeof(MapPanelClosedPatch),
                typeof(NativeInventoryItemSelectionModePatch),
                typeof(InventoryArtifactIntentClickPatch),
                typeof(InventoryArtifactIntentInputPatch),
                typeof(InventoryPanelCancelPatch),
                typeof(InventoryPanelTooltipPlacementPatch),
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
                typeof(MultiplayerRulesLobbyDeparturePatch),
                typeof(NativeLobbyRulesEntryPatch),
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
                    if (patchType.Namespace == "SephiriaEnhancements.DefeatRetry" ||
                        patchType == typeof(NativeSaveCapturePatch)) retryCompatibilityAvailable = false;
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
            DefeatRetryBridge.SetIntegrationAvailable(retryCompatibilityAvailable);
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
            ProcessFeatureFailures();
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
            try { Unload(); }
            catch (Exception exception) { SupportLogger.Failure("mod_cleanup_failed", exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Unload()
        {
            if (activeInstance != this) return;
            activeInstance = null;
            CleanupFeature(FeatureId.Settings, () => NativeOptionsLifetime.DisposeAll());
            CleanupFeature(FeatureId.MultiplayerRules, () => multiplayerRules?.Shutdown());
            CleanupFeature(FeatureId.MultiplayerAccess, () => MidRunAdmissionRuntime.SetIntegrationAvailable(false));
            CleanupFeature(FeatureId.MultiplayerRules, () => EnemySpawnRoutineContext.SetRuleScopeFactory(null));
            CleanupFeature(FeatureId.Inventory, () => inventoryOptimization?.Shutdown());
            CleanupFeature(FeatureId.DefeatRetry, () => DefeatRetryBridge.Shutdown());
            CleanupFeature(FeatureId.CombatInsights, () => combatInsights?.Shutdown());
            CleanupFeature(FeatureId.CombatInsights, () => NativeReportDismissal.SetController(null));
            CleanupFeature(FeatureId.CombatInsights, () => NativeStatisticsPauseEntry.SetController(null));
            CleanupFeature(FeatureId.DeveloperTools, () => DeveloperLogger.Shutdown());
            if (runtimeKernel != null)
            {
                runtimeKernel.GameplayContextChanged -= OnLocalGameplayContextChanged;
                CleanupFeature(FeatureId.Gameplay, () => runtimeKernel.Dispose());
            }
            HorayModAPI.OnLocalizationReady -= RegisterLocalization;
            HorayModAPI.OnStartSessionClientside -= OnStartSessionClientside;
            HorayModAPI.OnFloorAllocatedClientside -= OnFloorAllocatedClientside;
            HorayModAPI.OnStartSessionServerside -= OnStartSessionServerside;
            HorayModAPI.OnFloorAllocatedServerside -= OnFloorAllocatedServerside;
            MultiplayerRulesExplorationStartPatch.StartingExploration -=
                OnStartingExploration;
            CleanupFeature(FeatureId.CombatInsights, () => DamageFeedbackCapture.SetController(null));
            CleanupFeature(FeatureId.CombatInsights, () => DamageDetailCapture.SetController(null));
            CleanupFeature(FeatureId.CombatInsights, () => UnitDeathCapture.SetController(null));
            CleanupFeature(FeatureId.CombatInsights, () => LocalFinalBlowCapture.SetController(null));
            CleanupFeature(FeatureId.ResourceBarValues, () => NativeResourceBarValueView.DisposeAll());
            CleanupFeature(FeatureId.AutoCasting, () => NativeAutoCastingUi.DisposeAll());
            UnpatchFeatures();
            CleanupFeature(FeatureId.ModJournal, () => NativeModJournal.DisposeAll());
            CleanupFeature(FeatureId.EffectStats, EffectStats.Integration.NativeEffectStatsView.DisposeAll);

            multiplayerRulesCompatibilityAvailable = false;
            multiplayerRuleBehaviorPatchesAttempted = false;
            multiplayerRuleBehaviorPatchesInstalled = false;

            if (controllerObject != null)
            {
                // Finish old controller callbacks before a replacement installs
                // its static observers and event subscriptions.
                CleanupFeature(FeatureId.Gameplay, () => UnityEngine.Object.DestroyImmediate(controllerObject));
                controllerObject = null;
            }

            combatInsights = null;
            autoCasting = null;
            combatRelationOutlines = null;
            combatTargeting = null;
            nativeCompanion = null;
            keyboardUiNavigation = null;
            mapEnhancements = null;
            inventoryOptimization = null;
            multiplayerRules = null;
            runtimeKernel = null;
            Application.quitting -= OnModUnloaded;
            FeatureFailure.Report = null;
            SupportLogger.Shutdown();
        }

        private void OnStartSessionClientside(bool isSavedSession)
        {
            FeatureFailure.Run(FeatureId.DefeatRetry, () => NativeRetryBoss.ObserveWorldSession(isSavedSession));
            FeatureFailure.Run(FeatureId.AutoCasting, () => autoCasting?.ResetWorld());
            FeatureFailure.Run(FeatureId.DefeatRetry, () => DefeatRetryClientRestore.ObserveWorldSession(isSavedSession));
            FeatureFailure.Run(FeatureId.DeveloperTools, () => GameLoadProfiler.ObserveClientSessionStarted(isSavedSession));
            FeatureFailure.Run(FeatureId.MultiplayerRules, MultiplayerRulesLobbySnapshotCoordinator.ReadHostSnapshot);
            FeatureFailure.Run(FeatureId.Inventory, () => inventoryOptimization?.ResetWorldSession());
            FeatureFailure.Run(FeatureId.Gameplay, () => runtimeKernel?.BeginWorldSession());
        }

        private void OnFloorAllocatedClientside(string guid, string floorName, FloorGenerator generator)
        {
            FeatureFailure.Run(FeatureId.DeveloperTools, () => GameLoadProfiler.ObserveFloorAllocated(guid, floorName));
            FeatureFailure.Run(FeatureId.MultiplayerRules, MultiplayerRulesLobbySnapshotCoordinator.ReadHostSnapshot);
        }

        private void OnLocalGameplayContextChanged(LocalGameplayContextChange change)
        {
            FeatureFailure.Run(FeatureId.AutoCasting, () => autoCasting?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.Inventory, () => inventoryOptimization?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.CombatRelationOutlines, () => combatRelationOutlines?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.CombatTargeting, () => combatTargeting?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.KeyboardUiNavigation, () => keyboardUiNavigation?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.MapEnhancements, () => mapEnhancements?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.NativeCompanion, () => nativeCompanion?.ResetGameplayContext());
            FeatureFailure.Run(FeatureId.DeveloperTools, GameLoadProfiler.ObserveGameplayContextReset);
        }

        private void OnStartSessionServerside(bool isSavedSession)
        {
            FeatureFailure.Run(FeatureId.DeveloperTools, () => GameLoadProfiler.ObserveServerSessionStarted(isSavedSession));
            FeatureFailure.Run(FeatureId.MultiplayerRules, () =>
            {
                if (MultiplayerRulesExplorationStartPatch.ExplorationStarted)
                    BeginServerExploration(isSavedSession);
                else MultiplayerRulesController.EndExploration();
            });
            FeatureFailure.Run(FeatureId.NativeCompanion, () => nativeCompanion?.ResetSession());
        }

        private void OnStartingExploration() =>
            FeatureFailure.Run(FeatureId.MultiplayerRules, () => BeginServerExploration(false));

        private void BeginServerExploration(bool isSavedSession)
        {
            if (RequiresMultiplayerRuleBehaviorPatches(isSavedSession))
                EnsureMultiplayerRuleBehaviorPatches();
            if (FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
                multiplayerRules?.BeginServerExploration(isSavedSession);
        }

        private void OnFloorAllocatedServerside(string guid, string floorName, FloorGenerator generator) =>
            FeatureFailure.Run(FeatureId.MultiplayerRules, () => multiplayerRules?.PublishActiveRulesForLobbyDisplay());

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }

        private static bool RequiresMultiplayerRuleBehaviorPatches(
            bool isSavedSession)
        {
            if (!isSavedSession && !EnhancementsSettings.Enabled) return false;
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
            FeatureId feature = FeaturePatchOwnership.Get(patchType);
            if (!FeatureFailure.IsAvailable(feature)) return false;
            try
            {
                preparationMilliseconds = ElapsedMilliseconds(startedAt);
                long applicationStartedAt = Stopwatch.GetTimestamp();
                if (!featurePatches.Install(feature, patchType)) return false;
                applicationMilliseconds =
                    ElapsedMilliseconds(applicationStartedAt);
                succeeded = true;
                return true;
            }
            catch (Exception ex)
            {
                FeatureFailure.Disable(feature, ex);
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
