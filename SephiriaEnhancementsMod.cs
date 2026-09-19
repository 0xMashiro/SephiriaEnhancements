using SephiriaEnhancements.Runtime.GameBridge.Inventory;
using HarmonyLib;
using SephiriaEnhancements.StageRewardAutoClaim.Integration;
using System;
using SephiriaEnhancements.CostumeAppearance.Integration;
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
            RegisterFeatureCleanup();
            NativeModNotifications.Reset();
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
            InitializeFeature(FeatureId.StageRewardAutoClaim, () =>
                AddController<NativeStageRewardAutoClaim>(FeatureId.StageRewardAutoClaim).Initialize());
            InitializeFeature(FeatureId.CostumeAppearance, () =>
                AddController<NativeCostumeAppearance>(FeatureId.CostumeAppearance).Initialize());
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
                DefeatRetryBridge.TeamDefeated += combatInsights.FinishDefeatedEncounter;
                AddController<TrainingDamageStatistics>(FeatureId.CombatInsights);
                NativeReportDismissal.SetController(combatInsights);
                NativeStatisticsPauseEntry.SetController(combatInsights);
                DamageFeedbackCapture.SetController(combatInsights);
                DamageDetailCapture.SetController(combatInsights);
                UnitDeathCapture.SetController(combatInsights);
                LocalFinalBlowCapture.SetController(combatInsights);
            });
            InitializeFeature(FeatureId.DefeatRetry, () =>
            {
                DefeatRetryBridge.Initialize();
                AddController<DefeatRetryRuntime>(FeatureId.DefeatRetry);
            });
            InitializeFeature(FeatureId.MultiplayerAccess, () =>
                AddController<NativeJoiningSupplies>(FeatureId.MultiplayerAccess));
            InitializeFeature(FeatureId.CombatTargeting, () =>
                combatTargeting = AddController<CombatTargetingController>(FeatureId.CombatTargeting));
            InitializeFeature(FeatureId.AutoCasting, () =>
                autoCasting = AddController<NativeAutoCasting>(FeatureId.AutoCasting));
            InitializeFeature(FeatureId.ItemCommunication, () =>
                AddController<ItemCommunication.Integration.NativeItemCommunication>(FeatureId.ItemCommunication));
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
            foreach (var patch in StartupPatches())
            {
                if (TryPatch(patch.Feature, patch.Patch, out float patchMilliseconds))
                {
                    successfulPatchCount++;
                    if (patch.Patch == typeof(NativeReportDismissal))
                        NativeReportDismissal.IsAvailable = true;
                }
                else
                {
                    failedPatchCount++;
                    if (patch.Feature == FeatureId.DefeatRetry) retryCompatibilityAvailable = false;
                    if (patch.Feature == FeatureId.MultiplayerRules)
                        multiplayerRulesCompatibilityAvailable = false;
                }
                if (patchMilliseconds > slowestPatchMilliseconds)
                {
                    slowestPatchName = patch.Patch.Name;
                    slowestPatchMilliseconds = patchMilliseconds;
                }
            }
            DefeatRetryBridge.SetIntegrationAvailable(retryCompatibilityAvailable);
            if (midRunAdmissionCompatibilityAvailable)
            {
                foreach (var patch in MidRunAdmissionPatches())
                {
                    if (TryPatch(patch.Feature, patch.Patch, out float patchMilliseconds))
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
                        slowestPatchName = patch.Patch.Name;
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
            featureCleanup.Unload();
            HorayModAPI.OnLocalizationReady -= RegisterLocalization;
            HorayModAPI.OnStartSessionClientside -= OnStartSessionClientside;
            HorayModAPI.OnFloorAllocatedClientside -= OnFloorAllocatedClientside;
            HorayModAPI.OnStartSessionServerside -= OnStartSessionServerside;
            HorayModAPI.OnFloorAllocatedServerside -= OnFloorAllocatedServerside;
            MultiplayerRulesExplorationStartPatch.StartingExploration -=
                OnStartingExploration;
            UnpatchFeatures();

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
            NativeModNotifications.Reset();
            SupportLogger.Shutdown();
        }

        private void OnStartSessionClientside(bool isSavedSession)
        {
            NativeModNotifications.ClearContext();
            FeatureFailure.Run(FeatureId.DefeatRetry, NativeRetryConclusion.Reset);
            FeatureFailure.Run(FeatureId.DefeatRetry, () => NativeRetryBoss.ObserveWorldSession(isSavedSession));
            FeatureFailure.Run(FeatureId.Gameplay, () => NativeLocalPlayerData.ObserveWorldSession(isSavedSession));
            FeatureFailure.Run(FeatureId.DefeatRetry, () => DefeatRetryClientRestore.ObserveWorldSession(isSavedSession));
            FeatureFailure.Run(FeatureId.DeveloperTools, () => GameLoadProfiler.ObserveClientSessionStarted(isSavedSession));
            FeatureFailure.Run(FeatureId.MultiplayerRules, MultiplayerRulesBridge.ObserveWorld);
            FeatureFailure.Run(FeatureId.Inventory, () => inventoryOptimization?.ResetWorldSession());
            FeatureFailure.Run(FeatureId.Gameplay, () => runtimeKernel?.BeginWorldSession());
        }

        private void OnFloorAllocatedClientside(string guid, string floorName, FloorGenerator generator)
        {
            FeatureFailure.Run(FeatureId.DeveloperTools, () => GameLoadProfiler.ObserveFloorAllocated(guid, floorName));
        }

        private void OnLocalGameplayContextChanged(LocalGameplayContextChange change)
        {
            NativeModNotifications.ClearContext();
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
            FeatureFailure.Run(FeatureId.MultiplayerRules, MultiplayerRulesBridge.Publish);

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
            if (isSavedSession)
            {
                if (!ActiveExplorationRulesStore.TryRead(out ActiveExplorationMultiplayerRules restoredRules, out _)) return false;
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
            foreach (var patch in MultiplayerRuleBehaviorPatches())
            {
                if (!TryPatch(patch.Feature, patch.Patch, out _))
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

        private bool TryPatch(FeatureId feature, Type patchType, out float elapsedMilliseconds)
        {
            long startedAt = Stopwatch.GetTimestamp();
            elapsedMilliseconds = 0f;
            float preparationMilliseconds = 0f;
            float applicationMilliseconds = 0f;
            bool succeeded = false;
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
