using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Runtime;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.MultiplayerRules;
using UnityEngine;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class DeveloperLogger
    {
        private const int MaxEventsPerLog = 50000;
        private const int MaxPendingLines = 4096;
        private static BlockingCollection<string> pendingLines;
        private static bool initialized;
        private static bool enabled;
        private static volatile bool accepting;
        private static int eventCount;
        private static int droppedSincePump;
        private static string workerError;
        private static bool modLoadMetricsAvailable;
        private static bool modLoadMetricsWritten;
        private static float modLoadTotalMilliseconds;
        private static float compatibilityMilliseconds;
        private static float localizationMilliseconds;
        private static float controlsMilliseconds;
        private static float controllersMilliseconds;
        private static float patchesMilliseconds;
        private static int successfulPatchCount;
        private static int failedPatchCount;
        private static string slowestPatchName;
        private static float slowestPatchMilliseconds;

        internal static string CurrentPath { get; private set; }

        internal static bool IsEnabled => enabled && accepting && pendingLines != null;

        internal static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            string[] arguments = Environment.GetCommandLineArgs();
            bool requested = Array.Exists(arguments, argument => string.Equals(
                argument, "-sephiria-enhancements-devlog", StringComparison.OrdinalIgnoreCase));
            enabled = requested;
            if (requested)
            {
                Open();
                WritePendingModLoadMetrics();
            }
            else
            {
                Debug.Log("[SephiriaEnhancements] Developer diagnostics are compiled but " +
                    "inactive; use -sephiria-enhancements-devlog to enable them.");
            }
        }

        internal static void RecordModLoadMetrics(float totalMilliseconds,
            float compatibility, float localization, float controls,
            float controllers, float patches, int successfulPatches,
            int failedPatches, string slowestPatch,
            float slowestPatchElapsedMilliseconds)
        {
            modLoadTotalMilliseconds = totalMilliseconds;
            compatibilityMilliseconds = compatibility;
            localizationMilliseconds = localization;
            controlsMilliseconds = controls;
            controllersMilliseconds = controllers;
            patchesMilliseconds = patches;
            successfulPatchCount = successfulPatches;
            failedPatchCount = failedPatches;
            slowestPatchName = slowestPatch;
            slowestPatchMilliseconds = slowestPatchElapsedMilliseconds;
            modLoadMetricsAvailable = true;
            modLoadMetricsWritten = false;
            WritePendingModLoadMetrics();
        }

        internal static void RecordStartupMilestone(string milestone,
            float elapsedMilliseconds)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"startup_milestone\",\"time\":" +
                    TimeValue() + ",\"milestone\":" + Json(milestone) +
                    ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) + "}");
            }
        }

        internal static void RecordModPatch(string patchName, bool succeeded,
            float elapsedMilliseconds, float preparationMilliseconds,
            float applicationMilliseconds)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"mod_patch\",\"time\":" +
                    TimeValue() + ",\"name\":" + Json(patchName) +
                    ",\"succeeded\":" + Bool(succeeded) +
                    ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) +
                    ",\"preparationMilliseconds\":" +
                    Float(preparationMilliseconds) +
                    ",\"applicationMilliseconds\":" +
                    Float(applicationMilliseconds) + "}");
            }
        }

        internal static void RecordGameLoadingOperation(int loadAttemptId,
            string operation, float elapsedMilliseconds, bool completed)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"game_loading_operation\",\"time\":" +
                    TimeValue() + ",\"loadAttemptId\":" + loadAttemptId +
                    ",\"operation\":" + Json(operation) +
                    ",\"completed\":" + Bool(completed) +
                    ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) + "}");
            }
        }

        internal static void RecordGameStartupOperation(string operation,
            float elapsedMilliseconds, bool completed)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"game_startup_operation\",\"time\":" +
                    TimeValue() + ",\"operation\":" + Json(operation) +
                    ",\"completed\":" + Bool(completed) +
                    ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) + "}");
            }
        }

        internal static void RecordLocalGameplayContext(
            LocalGameplayContextChange change, long epoch, uint playerNetId,
            string floorGuid, bool traveling)
        {
            if (IsEnabled)
                WriteLine("{\"event\":\"local_gameplay_context\",\"time\":" +
                    TimeValue() + ",\"change\":" + Json(change.ToString()) +
                    ",\"gameplayContextEpoch\":" + epoch +
                    ",\"playerNetId\":" + playerNetId +
                    ",\"floorGuid\":" + Json(floorGuid) +
                    ",\"traveling\":" + Bool(traveling) + "}");
        }

        internal static void RecordLoadingMilestone(int loadAttemptId,
            string milestone, string trigger, string sessionLoadMode,
            bool serverObserved, bool clientObserved,
            float elapsedMilliseconds, string floorGuid, string floorName,
            string detail)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"loading_milestone\",\"time\":" +
                    TimeValue() + ",\"loadAttemptId\":" + loadAttemptId +
                    ",\"milestone\":" + Json(milestone) +
                    ",\"trigger\":" + Json(trigger) +
                    ",\"sessionLoadMode\":" +
                    Json(sessionLoadMode) +
                    ",\"serverObserved\":" + Bool(serverObserved) +
                    ",\"clientObserved\":" + Bool(clientObserved) +
                    ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) + ",\"floorGuid\":" +
                    Json(floorGuid) + ",\"floorName\":" + Json(floorName) +
                    ",\"detail\":" +
                    Json(detail) + "}");
            }
        }

        internal static void RecordRetryCheckpointCapture(
            float elapsedMilliseconds, string checkpointKind, string source,
            string floorGuid, string floorName, string stageName,
            string threatType, string generatorType, string bossName,
            int placementCount)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"retry_checkpoint_capture\",\"time\":" +
                    TimeValue() + ",\"elapsedMilliseconds\":" +
                    Float(elapsedMilliseconds) + ",\"checkpointKind\":" +
                    Json(checkpointKind) + ",\"source\":" + Json(source) +
                    ",\"floorGuid\":" + Json(floorGuid) +
                    ",\"floorName\":" + Json(floorName) +
                    ",\"stageName\":" + Json(stageName) +
                    ",\"threatType\":" + Json(threatType) +
                    ",\"generatorType\":" + Json(generatorType) +
                    ",\"bossName\":" + Json(bossName) +
                    ",\"placementCount\":" + placementCount + "}");
            }
        }

        internal static void RecordRetryFloorEvaluation(string floorGuid,
            string floorName, string stageName, string threatType,
            string generatorType, bool explorationActivated,
            string checkpointKind, bool checkpointMatchesFloor, bool captured)
        {
            if (!IsEnabled) return;
            WriteLine("{\"event\":\"retry_floor_evaluation\",\"time\":" +
                TimeValue() + ",\"floorGuid\":" + Json(floorGuid) +
                ",\"floorName\":" + Json(floorName) +
                ",\"stageName\":" + Json(stageName) +
                ",\"threatType\":" + Json(threatType) +
                ",\"generatorType\":" + Json(generatorType) +
                ",\"explorationActivated\":" + Bool(explorationActivated) +
                ",\"checkpointKind\":" + Json(checkpointKind) +
                ",\"checkpointMatchesFloor\":" + Bool(checkpointMatchesFloor) +
                ",\"captured\":" + Bool(captured) + "}");
        }

        internal static void RecordRetryOfferDecision(int nativeGameOverType,
            string conclusionKind, string checkpointKind, string floorGuid,
            bool hasCheckpoint, bool serverActive, bool runStarted,
            bool gaveUp, bool offered)
        {
            if (!IsEnabled) return;
            WriteLine("{\"event\":\"retry_offer_decision\",\"time\":" +
                TimeValue() + ",\"nativeGameOverType\":" + nativeGameOverType +
                ",\"conclusionKind\":" + Json(conclusionKind) +
                ",\"checkpointKind\":" + Json(checkpointKind) +
                ",\"floorGuid\":" + Json(floorGuid) +
                ",\"hasCheckpoint\":" + Bool(hasCheckpoint) +
                ",\"serverActive\":" + Bool(serverActive) +
                ",\"runStarted\":" + Bool(runStarted) +
                ",\"gaveUp\":" + Bool(gaveUp) +
                ",\"offered\":" + Bool(offered) + "}");
        }

        internal static void Pump()
        {
            string error = Interlocked.Exchange(ref workerError, null);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning("[SephiriaEnhancements] " + error);
            }

            int dropped = Interlocked.Exchange(ref droppedSincePump, 0);
            if (dropped > 0)
            {
                Debug.LogWarning("[SephiriaEnhancements] Developer log queue was full; " +
                    dropped + " diagnostic event(s) were dropped without blocking gameplay.");
            }
        }

        internal static void RecordDamageFeedback(DamageFeedback feedback, PlayerAvatar owner)
        {
            if (!IsEnabled || feedback == null)
            {
                return;
            }

            string messageType = Enum.IsDefined(typeof(DamageFeedback.EMsgType), (int)feedback.msgType)
                ? ((DamageFeedback.EMsgType)feedback.msgType).ToString()
                : "Unknown_" + feedback.msgType.ToString(CultureInfo.InvariantCulture);

            UnitAvatar attacker = feedback.attacker;
            UnitAvatar target = feedback.self;
            WriteLine("{\"event\":\"damage_feedback\",\"time\":" + TimeValue() +
                ",\"messageType\":" + Json(messageType) +
                ",\"messageTypeRaw\":" + feedback.msgType.ToString(CultureInfo.InvariantCulture) +
                ",\"damage\":" + feedback.damageValue.ToString(CultureInfo.InvariantCulture) +
                ",\"private\":" + Bool(feedback.isPrivate) +
                ",\"position\":{\"x\":" + Float(feedback.position.x) + ",\"y\":" + Float(feedback.position.y) + "}" +
                ",\"rgba\":{\"r\":" + feedback.r + ",\"g\":" + feedback.g +
                ",\"b\":" + feedback.b + ",\"a\":" + feedback.a + "}" +
                ",\"fontSize\":" + feedback.fontSize.ToString(CultureInfo.InvariantCulture) +
                ",\"attacker\":" + Avatar(attacker) +
                ",\"owner\":" + Avatar(owner) +
                ",\"target\":" + Avatar(target) + "}");
        }

        internal static void RecordEncounterLifecycle(
            EncounterLifecycleEvent lifecycleEvent)
        {
            if (IsEnabled && lifecycleEvent != null)
            {
                WriteLine("{\"event\":\"encounter_lifecycle\",\"time\":" +
                    TimeValue() +
                    ",\"gameplayContextEpoch\":" +
                    lifecycleEvent.GameplayContextEpoch +
                    ",\"lifecycleRevision\":" +
                    lifecycleEvent.LifecycleRevision +
                    ",\"kind\":" + Json(lifecycleEvent.Kind.ToString()) +
                    ",\"transition\":" +
                    Json(lifecycleEvent.Transition.ToString()) +
                    ",\"sourceInstanceId\":" +
                    lifecycleEvent.SourceInstanceId +
                    ",\"previousSourceInstanceId\":" +
                    lifecycleEvent.PreviousSourceInstanceId + "}");
            }
        }

        internal static void RecordCombatInsightsVisibility(string reason,
            string displayPolicy, string viewMode, bool encounterActive,
            bool bossActive, bool encounterReportOpen, bool bossReportOpen,
            bool encounterReportPaused, bool bossReportPaused,
            bool hiddenByUser, bool hudAttached, bool hudActiveInHierarchy,
            int controlCount, string controlType, bool levelUpIndicatorVisible,
            bool flashScreenVisible, bool screenFading, bool cutSceneActive,
            bool playerLoading, string reportState, string presentationBlock)
        {
            if (!IsEnabled) return;
            WriteLine("{\"event\":\"combat_insights_visibility\",\"time\":" +
                TimeValue() + ",\"reason\":" + Json(reason) +
                ",\"displayPolicy\":" + Json(displayPolicy) +
                ",\"viewMode\":" + Json(viewMode) +
                ",\"reportState\":" + Json(reportState) +
                ",\"presentationBlock\":" + Json(presentationBlock) +
                ",\"encounterActive\":" + Bool(encounterActive) +
                ",\"bossActive\":" + Bool(bossActive) +
                ",\"encounterReportOpen\":" + Bool(encounterReportOpen) +
                ",\"bossReportOpen\":" + Bool(bossReportOpen) +
                ",\"encounterReportPaused\":" +
                Bool(encounterReportPaused) +
                ",\"bossReportPaused\":" + Bool(bossReportPaused) +
                ",\"hiddenByUser\":" + Bool(hiddenByUser) +
                ",\"hudAttached\":" + Bool(hudAttached) +
                ",\"hudActiveInHierarchy\":" + Bool(hudActiveInHierarchy) +
                ",\"nativeControl\":{\"count\":" + controlCount +
                ",\"type\":" + Json(controlType) + "}" +
                ",\"overlays\":{\"levelUpIndicator\":" +
                Bool(levelUpIndicatorVisible) +
                ",\"flashScreen\":" + Bool(flashScreenVisible) +
                ",\"screenFading\":" + Bool(screenFading) +
                ",\"cutScene\":" + Bool(cutSceneActive) +
                ",\"playerLoading\":" + Bool(playerLoading) + "}}");
        }

        internal static void RecordRuntimeMetrics(RuntimeMetricSnapshot metrics,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || metrics == null || state == null)
            {
                return;
            }

            WriteLine("{\"event\":\"runtime_metrics\",\"time\":" + TimeValue() +
                ",\"gameplayContextEpoch\":" +
                state.GameplayContextEpoch +
                ",\"runtimeRevision\":" + state.RuntimeRevision +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"catalogRevision\":" + state.CatalogRevision +
                ",\"consistency\":" + Json(state.Consistency.ToString()) +
                ",\"captures\":" + metrics.Captures +
                ",\"failedCaptures\":" + metrics.FailedCaptures +
                ",\"captureMs\":{\"average\":" + Float(metrics.AverageCaptureMilliseconds) +
                ",\"p50\":" + Float(metrics.P50CaptureMilliseconds) +
                ",\"p95\":" + Float(metrics.P95CaptureMilliseconds) +
                ",\"max\":" + Float(metrics.MaximumCaptureMilliseconds) +
                "},\"catalog\":{\"captures\":" + metrics.CatalogCaptures +
                ",\"failed\":" + metrics.FailedCatalogCaptures +
                ",\"averageMs\":" + Float(metrics.AverageCatalogCaptureMilliseconds) +
                ",\"maxMs\":" + Float(metrics.MaximumCatalogCaptureMilliseconds) +
                "},\"tabletQueries\":{\"cacheHits\":" +
                metrics.TabletQueryCacheHits +
                ",\"cacheMisses\":" + metrics.TabletQueryCacheMisses +
                ",\"failed\":" + metrics.FailedTabletQueries +
                ",\"averageParseMs\":" +
                Float(metrics.AverageTabletQueryMilliseconds) +
                "},\"preset\":{\"captures\":" + metrics.PresetCaptures +
                ",\"failed\":" + metrics.FailedPresetCaptures +
                ",\"averageMs\":" +
                Float(metrics.AveragePresetCaptureMilliseconds) +
                ",\"maxMs\":" +
                Float(metrics.MaximumPresetCaptureMilliseconds) +
                "},\"eventCounts\":[" + string.Join(",", metrics.EventCounts) + "]}");
        }

        internal static void RecordInventorySettlementValidation(
            SephiriaEnhancements.Runtime.Inventory.InventorySettlementValidationSnapshot
                validation,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || validation == null || state == null)
            {
                return;
            }

            var issues = new string[validation.Issues.Count];
            for (int index = 0; index < issues.Length; index++)
            {
                issues[index] = Json(validation.Issues[index]);
            }
            WriteLine("{\"event\":\"inventory_settlement_validation\",\"time\":" +
                TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"capabilities\":" + Json(validation.Capabilities.ToString()) +
                ",\"currentLayoutVerified\":" +
                Bool(validation.CurrentLayoutVerified) +
                ",\"layoutProjectionReady\":" +
                Bool(validation.LayoutProjectionReady) +
                ",\"issues\":[" + string.Join(",", issues) + "]}");
        }

        internal static void RecordInventoryPositionEffects(
            InventoryPositionEffectsSnapshot effects, RuntimeStateSnapshot state)
        {
            if (!IsEnabled || effects == null || state == null) return;
            string rules = string.Join(",", effects.Rules.Select(rule =>
                "{\"source\":" + ItemKeyJson(rule.Source) +
                ",\"kind\":" + Json(rule.Kind.ToString()) +
                ",\"valuesByLevel\":[" + string.Join(",", rule.ValuesByLevel.Select(Number)) +
                "],\"secondaryValuesByLevel\":[" + string.Join(",", rule.SecondaryValuesByLevel.Select(Number)) +
                "],\"offsets\":[" + string.Join(",", rule.Offsets.Select(offset =>
                    "{\"x\":" + offset.X + ",\"y\":" + offset.Y + "}")) +
                "],\"boundary\":" + rule.Boundary +
                ",\"channels\":[" + string.Join(",", rule.Channels.Select(Json)) +
                "],\"targetCategory\":" + Json(rule.TargetCategory) +
                ",\"conditionalDamage\":" + Bool(rule.ConditionalDamage) +
                ",\"maximumRarity\":" + rule.MaximumRarity + "}"));
            string traits = string.Join(",", effects.Traits.Select(trait =>
                "{\"item\":" + ItemKeyJson(trait.Item) +
                ",\"planet\":" + Bool(trait.Planet) +
                ",\"companion\":" + Bool(trait.Companion) +
                ",\"networkReady\":" + Bool(trait.NetworkReady) +
                ",\"rarity\":" + trait.Rarity +
                ",\"magicArtifact\":" + Bool(trait.MagicArtifact) + "}"));
            WriteLine("{\"event\":\"inventory_position_effects\",\"time\":" + TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"rules\":[" + rules + "],\"traits\":[" + traits +
                "],\"observed\":" + PositionEffects(effects.Observed) +
                ",\"issues\":[" + string.Join(",", effects.Issues.Select(Json)) + "]}");
        }

        internal static void RecordInventoryStorageChanged(int width,
            int oldStorage, int newStorage)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"inventory_storage_changed\",\"time\":" +
                    TimeValue() + ",\"width\":" + width +
                    ",\"oldStorage\":" + oldStorage +
                    ",\"newStorage\":" + newStorage +
                    ",\"delta\":" + (newStorage - oldStorage) + "}");
            }
        }

        internal static void RecordInventoryHeightChanged(int oldHeight,
            int newHeight)
        {
            if (IsEnabled)
            {
                WriteLine("{\"event\":\"inventory_height_changed\",\"time\":" +
                    TimeValue() + ",\"oldHeight\":" + oldHeight +
                    ",\"newHeight\":" + newHeight +
                    ",\"delta\":" + (newHeight - oldHeight) + "}");
            }
        }

        internal static void RecordInventoryEvaluationOrder(
            SephiriaEnhancements.Runtime.Inventory.InventoryEvaluationOrderSnapshot order,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || order == null || state == null)
            {
                return;
            }

            var categoryOrder = new string[order.CategoryRefreshItemKeys.Count];
            for (int index = 0; index < categoryOrder.Length; index++)
            {
                categoryOrder[index] = ItemKeyJson(order.CategoryRefreshItemKeys[index]);
            }

            var artifactOrder = new string[order.ArtifactRefreshItemKeys.Count];
            for (int index = 0; index < artifactOrder.Length; index++)
            {
                artifactOrder[index] = ItemKeyJson(order.ArtifactRefreshItemKeys[index]);
            }

            var uniqueRegistrations = new string[order.UniqueRegistrations.Count];
            for (int index = 0; index < uniqueRegistrations.Length; index++)
            {
                SephiriaEnhancements.Runtime.Inventory.UniqueEffectRegistrationSnapshot
                    registration = order.UniqueRegistrations[index];
                uniqueRegistrations[index] =
                    "{\"itemKey\":" + ItemKeyJson(registration.ItemKey) +
                    ",\"accepted\":" + Bool(registration.Accepted) + "}";
            }

            WriteLine("{\"event\":\"inventory_evaluation_order\",\"time\":" +
                TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"traceRevision\":" + order.TraceRevision +
                ",\"categoryRefreshItemKeys\":[" +
                string.Join(",", categoryOrder) +
                "],\"artifactRefreshItemKeys\":[" +
                string.Join(",", artifactOrder) +
                "],\"uniqueRegistrations\":[" +
                string.Join(",", uniqueRegistrations) + "]}");
        }

        internal static void RecordInventoryOptimization(
            SephiriaEnhancements.Inventory.InventoryOptimizationProposal result,
            SephiriaEnhancements.Inventory.InventoryApplicationPlan plan,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || result == null || state == null)
            {
                return;
            }

            SephiriaEnhancements.Inventory.InventoryOptimizationScore current =
                result.CurrentScore;
            SephiriaEnhancements.Inventory.InventoryOptimizationScore best =
                result.BestScore;
            WriteLine("{\"event\":\"inventory_optimization\",\"time\":" +
                TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"succeeded\":" + Bool(result.Succeeded) +
                ",\"improved\":" + Bool(result.Improved) +
                ",\"candidateEvaluations\":" + result.CandidateEvaluations +
                ",\"elapsedMilliseconds\":" + result.ElapsedMilliseconds +
                ",\"terminationReason\":" +
                    Json(result.TerminationReason.ToString()) +
                ",\"searchMethod\":" + Json(result.SearchMethod.ToString()) +
                ",\"optimalityProven\":" + Bool(result.OptimalityProven) +
                ",\"duplicateLayoutsSkipped\":" +
                    result.DuplicateLayoutsSkipped +
                ",\"swapCount\":" + (plan?.Swaps.Count ?? 0) +
                ",\"rotationCount\":" + (plan?.Rotations.Count ?? 0) +
                ",\"current\":" + Score(current) +
                ",\"best\":" + Score(best) +
                ",\"targetEvaluations\":" +
                    TargetEvaluations(result.TargetEvaluations) +
                ",\"outcome\":" + Outcome(result.Outcome) + "}");
        }

        internal static void RecordInventoryApplication(bool matched,
            SephiriaEnhancements.Inventory.InventoryApplicationPlan plan,
            int swapsApplied, int rotationsApplied,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || plan == null || state == null)
            {
                return;
            }

            WriteLine("{\"event\":\"inventory_application\",\"time\":" +
                TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"runtimeRevision\":" + state.RuntimeRevision +
                ",\"matchedTargetLayout\":" + Bool(matched) +
                ",\"swapsApplied\":" + swapsApplied +
                ",\"rotationsApplied\":" + rotationsApplied +
                ",\"expectedSwapCount\":" + plan.Swaps.Count +
                ",\"expectedRotationCount\":" + plan.Rotations.Count + "}");
        }

        internal static void RecordInventorySettlementDifferential(
            SephiriaEnhancements.Runtime.Inventory.
                InventorySettlementDifferentialReport report,
            RuntimeStateSnapshot state)
        {
            if (!IsEnabled || report == null || state == null)
            {
                return;
            }

            string mismatches = string.Join(",", report.Mismatches.Select(Json));
            SephiriaEnhancements.Runtime.Inventory.InventoryMechanicCoverageSnapshot
                coverage = report.Coverage;
            string nativeTypes = string.Join(",",
                (coverage?.NativeItemTypes ?? Array.Empty<string>()).Select(Json));
            string activationConditions = string.Join(",",
                (coverage?.ActivationConditions ?? Array.Empty<string>()).Select(Json));
            string dynamicKinds = string.Join(",",
                (coverage?.DynamicCategoryKinds ?? Array.Empty<string>()).Select(Json));
            WriteLine("{\"event\":\"inventory_settlement_differential\",\"time\":" +
                TimeValue() +
                ",\"inventoryRevision\":" + state.InventoryRevision +
                ",\"matched\":" + Bool(report.Matched) +
                ",\"mismatchCount\":" + report.Mismatches.Count +
                ",\"mismatches\":[" + mismatches + "]" +
                ",\"coverage\":{\"artifacts\":" +
                    (coverage?.ArtifactCount ?? 0) +
                ",\"restrictedArtifacts\":" +
                    (coverage?.RestrictedArtifactCount ?? 0) +
                ",\"enchantedArtifacts\":" +
                    (coverage?.EnchantedArtifactCount ?? 0) +
                ",\"uniqueArtifacts\":" +
                    (coverage?.UniqueArtifactCount ?? 0) +
                ",\"weaponRestrictedArtifacts\":" +
                    (coverage?.WeaponRestrictedArtifactCount ?? 0) +
                ",\"dynamicCategoryArtifacts\":" +
                    (coverage?.DynamicCategoryArtifactCount ?? 0) +
                ",\"positionEffectSources\":" + (coverage?.PositionEffectSourceCount ?? 0) +
                ",\"positionEffectKinds\":[" + string.Join(",",
                    (coverage?.PositionEffectKinds ?? Array.Empty<string>()).Select(Json)) + "]" +
                ",\"tablets\":" + (coverage?.TabletCount ?? 0) +
                ",\"rotatableTablets\":" +
                    (coverage?.RotatableTabletCount ?? 0) +
                ",\"fixedTablets\":" +
                    (coverage?.FixedTabletCount ?? 0) +
                ",\"mysticCells\":" +
                    (coverage?.MysticCellCount ?? 0) +
                ",\"otherItems\":" + (coverage?.OtherItemCount ?? 0) +
                ",\"nativeItemTypes\":[" + nativeTypes + "]" +
                ",\"activationConditions\":[" + activationConditions + "]" +
                ",\"dynamicCategoryKinds\":[" + dynamicKinds + "]}}");
        }

        internal static void RecordMultiplayerRuleResolution(
            MultiplayerRuleId ruleId, int participantCount,
            MultiplayerRulesPreset? preset, bool authoritative,
            bool overridden, float overrideValue)
        {
            if (!IsEnabled) return;
            string source = !authoritative ? "inactive" :
                overridden ? "override" : "game_behavior";
            WriteLine("{\"event\":\"multiplayer_rule_resolution\",\"time\":" +
                TimeValue() + ",\"rule\":" + Json(ruleId.ToString()) +
                ",\"participantCount\":" + participantCount +
                ",\"preset\":" + (preset.HasValue
                    ? Json(preset.Value.ToString()) : "null") +
                ",\"authoritative\":" + Bool(authoritative) +
                ",\"source\":" + Json(source) +
                ",\"appliedValue\":" + (overridden
                    ? Float(overrideValue) : "null") + "}");
        }

        internal static void RecordMultiplayerEnemyHealthResolution(
            EnemySpawnOrigin spawnOrigin, EnemyHealthCategory healthCategory,
            int participantCount, float otherModifierPercent,
            EnemyHealthModifierCombination combination, bool resolved,
            float multiplier)
        {
            if (!IsEnabled) return;
            WriteLine("{\"event\":\"multiplayer_enemy_health_resolution\",\"time\":" +
                TimeValue() + ",\"spawnOrigin\":" +
                Json(spawnOrigin.ToString()) + ",\"healthCategory\":" +
                Json(healthCategory.ToString()) + ",\"participantCount\":" +
                participantCount + ",\"otherModifierPercent\":" +
                Float(otherModifierPercent) + ",\"combination\":" +
                Json(combination.ToString()) + ",\"resolved\":" +
                Bool(resolved) + ",\"appliedMultiplier\":" +
                (resolved ? Float(multiplier) : "null") + "}");
        }

        internal static void RecordMultiplayerRulesLifecycle(string transition,
            ActiveExplorationMultiplayerRules activeRules)
        {
            if (!IsEnabled) return;
            WriteLine("{\"event\":\"multiplayer_rules_lifecycle\",\"time\":" +
                TimeValue() + ",\"transition\":" + Json(transition) +
                ",\"preset\":" + (activeRules == null ? "null" :
                    Json(activeRules.Preset.ToString())) + "}");
        }

        internal static void Shutdown()
        {
            initialized = false;
            enabled = false;
            Close("mod_unloaded");
        }

        private static void Open()
        {
            try
            {
                string directory = Path.Combine(SaveData.CommonPath, "Mods", "SephiriaEnhancements", "Logs");
                Directory.CreateDirectory(directory);
                CurrentPath = Path.Combine(directory,
                    "diagnostics-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture) + ".jsonl");
                BlockingCollection<string> queue = new BlockingCollection<string>(
                    new ConcurrentQueue<string>(), MaxPendingLines);
                pendingLines = queue;
                eventCount = 0;
                accepting = true;
                workerError = null;
                Interlocked.Exchange(ref droppedSincePump, 0);
                string path = CurrentPath;
                Thread writer = new Thread(() => WriterLoop(queue, path))
                {
                    IsBackground = true,
                    Name = "Sephiria Enhancements diagnostic writer"
                };
                writer.Start();
                WriteLine("{\"event\":\"log_start\",\"time\":" + TimeValue() +
                    ",\"utc\":" + Json(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)) +
                    ",\"schemaVersion\":8,\"modVersion\":" +
                    Json(typeof(DeveloperLogger).Assembly.GetName().Version.ToString(3)) +
                    ",\"gameVersion\":" + Json(Application.version) +
                    ",\"resolution\":{\"width\":" + Screen.width + ",\"height\":" + Screen.height + "}}");
                Debug.Log("[SephiriaEnhancements] Developer log enabled: " + CurrentPath);
            }
            catch (Exception ex)
            {
                accepting = false;
                pendingLines = null;
                enabled = false;
                Debug.LogWarning("[SephiriaEnhancements] Could not open developer log: " + ex.Message);
            }
        }

        private static void WritePendingModLoadMetrics()
        {
            if (!IsEnabled || !modLoadMetricsAvailable || modLoadMetricsWritten)
            {
                return;
            }

            modLoadMetricsWritten = true;
            WriteLine("{\"event\":\"mod_load_metrics\",\"time\":" + TimeValue() +
                ",\"totalMilliseconds\":" + Float(modLoadTotalMilliseconds) +
                ",\"phases\":{\"compatibility\":" +
                Float(compatibilityMilliseconds) +
                ",\"localization\":" + Float(localizationMilliseconds) +
                ",\"controls\":" + Float(controlsMilliseconds) +
                ",\"controllers\":" + Float(controllersMilliseconds) +
                ",\"patches\":" + Float(patchesMilliseconds) + "}" +
                ",\"patchSummary\":{\"successful\":" + successfulPatchCount +
                ",\"failed\":" + failedPatchCount +
                ",\"slowestName\":" + Json(slowestPatchName) +
                ",\"slowestMilliseconds\":" +
                Float(slowestPatchMilliseconds) + "}}");
        }

        private static void Close(string reason)
        {
            BlockingCollection<string> queue = pendingLines;
            if (queue == null)
            {
                return;
            }

            try
            {
                if (accepting)
                {
                    queue.TryAdd("{\"event\":\"log_end\",\"time\":" + TimeValue() +
                        ",\"reason\":" + Json(reason) + ",\"events\":" + eventCount + "}");
                }
                accepting = false;
                pendingLines = null;
                queue.CompleteAdding();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SephiriaEnhancements] Could not finish developer log queue cleanly: " +
                    ex.Message);
            }
        }

        private static void WriteLine(string line)
        {
            BlockingCollection<string> queue = pendingLines;
            if (!accepting || queue == null)
            {
                return;
            }

            if (eventCount >= MaxEventsPerLog)
            {
                queue.TryAdd("{\"event\":\"log_limit_reached\",\"limit\":" +
                    MaxEventsPerLog + "}");
                accepting = false;
                pendingLines = null;
                queue.CompleteAdding();
                Debug.LogWarning("[SephiriaEnhancements] Developer log event limit " +
                    "reached; this log file is closed until logging is restarted.");
                return;
            }

            try
            {
                if (queue.TryAdd(line))
                {
                    eventCount++;
                }
                else
                {
                    Interlocked.Increment(ref droppedSincePump);
                }
            }
            catch (Exception ex)
            {
                accepting = false;
                pendingLines = null;
                Debug.LogWarning("[SephiriaEnhancements] Developer logging queue stopped: " + ex.Message);
            }
        }

        private static void WriterLoop(BlockingCollection<string> queue, string path)
        {
            try
            {
                using StreamWriter output = new StreamWriter(path, false,
                    new UTF8Encoding(false), 65536);
                System.Diagnostics.Stopwatch flushTimer =
                    System.Diagnostics.Stopwatch.StartNew();
                while (!queue.IsCompleted)
                {
                    if (queue.TryTake(out string line, 250))
                    {
                        output.WriteLine(line);
                    }
                    if (flushTimer.ElapsedMilliseconds >= 2000)
                    {
                        output.Flush();
                        flushTimer.Restart();
                    }
                }
                while (queue.TryTake(out string remaining))
                {
                    output.WriteLine(remaining);
                }
                output.Flush();
            }
            catch (Exception ex)
            {
                accepting = false;
                Interlocked.Exchange(ref workerError,
                    "Developer logging worker stopped after an I/O error: " + ex.Message);
            }
            finally
            {
                queue.Dispose();
            }
        }

        private static string Avatar(UnitAvatar avatar)
        {
            if (avatar == null)
            {
                return "null";
            }

            return "{\"instanceId\":" + avatar.GetInstanceID() +
                ",\"runtimeType\":" + Json(avatar.GetType().Name) +
                ",\"monsterType\":" + Json(avatar.monsterType.ToString()) + "}";
        }

        private static string TimeValue() => Float(Time.realtimeSinceStartup);

        private static string Float(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string PositionEffects(IEnumerable<InventoryPositionEffectValue> values) =>
            "[" + string.Join(",", values.Select(value =>
                "{\"source\":" + ItemKeyJson(value.Key.Source) +
                ",\"kind\":" + Json(value.Key.Kind.ToString()) +
                ",\"target\":" + (value.Key.Target.HasValue ? ItemKeyJson(value.Key.Target.Value) : "null") +
                ",\"channel\":" + Json(value.Key.Channel) +
                ",\"value\":" + Number(value.Value) +
                ",\"mode\":" + Bool(value.Mode) + "}")) + "]";

        private static string Bool(bool value) => value ? "true" : "false";

        private static string Score(
            SephiriaEnhancements.Inventory.InventoryOptimizationScore score)
        {
            if (score == null)
            {
                return "null";
            }
            return "{\"prioritySatisfied\":" + score.PriorityTargetsSatisfied +
                ",\"priorityTargetCompletionPoints\":" +
                score.PriorityTargetCompletionPoints +
                ",\"avoidedActive\":" + score.AvoidedTargetsActive +
                ",\"positionEffectRegressions\":" + score.PositionEffectRegressions +
                ",\"coreSatisfied\":" + score.CoreTargetsSatisfied +
                ",\"coreTargetCompletionPoints\":" +
                score.CoreTargetCompletionPoints +
                ",\"preferredTargetsSatisfied\":" +
                score.PreferredTargetsSatisfied +
                ",\"preferredTargetCompletionPoints\":" +
                score.PreferredTargetCompletionPoints +
                ",\"sourceEnabledArtifactsDeactivated\":" +
                score.SourceEnabledArtifactsDeactivated +
                ",\"enabledArtifactCount\":" + score.EnabledArtifactCount +
                ",\"comboBreakpointValue\":" + score.ComboBreakpointValue +
                ",\"cappedEffectiveArtifactLevelTotal\":" +
                score.CappedEffectiveArtifactLevelTotal +
                ",\"excessArtifactLevelTotal\":" +
                score.ExcessArtifactLevelTotal +
                ",\"movedItemCount\":" + score.MovedItemCount +
                ",\"rotatedTabletCount\":" + score.RotatedTabletCount + "}";
        }

        private static string TargetEvaluations(
            IEnumerable<SephiriaEnhancements.Inventory.
                InventoryOptimizationTargetEvaluation> evaluations)
        {
            if (evaluations == null)
            {
                return "[]";
            }

            return "[" + string.Join(",", evaluations.Select(evaluation =>
                "{\"target\":" + Json(evaluation.Target) +
                ",\"kind\":" + Json(evaluation.Kind.ToString()) +
                ",\"level\":" + Json(evaluation.Level.ToString()) +
                ",\"source\":" + Json(evaluation.Source.ToString()) +
                ",\"requiredValue\":" + evaluation.RequiredValue +
                ",\"beforeValue\":" + evaluation.BeforeValue +
                ",\"afterValue\":" + evaluation.AfterValue +
                ",\"beforeConditionReached\":" +
                    Bool(evaluation.BeforeConditionReached) +
                ",\"afterConditionReached\":" +
                    Bool(evaluation.AfterConditionReached) +
                ",\"beforeCompletionPoints\":" +
                    evaluation.BeforeCompletionPoints +
                ",\"afterCompletionPoints\":" +
                    evaluation.AfterCompletionPoints +
                ",\"maximumObservedValue\":" +
                    evaluation.MaximumObservedValue +
                ",\"maximumObservedCompletionPoints\":" +
                    evaluation.MaximumObservedCompletionPoints +
                ",\"reachability\":" +
                    Json(evaluation.Reachability.ToString()) + "}")) + "]";
        }

        private static string Outcome(
            SephiriaEnhancements.Inventory.InventoryOptimizationOutcome outcome)
        {
            if (outcome == null)
            {
                return "null";
            }

            string artifacts = string.Join(",", outcome.ArtifactChanges.Select(
                change => "{\"itemKey\":" + ItemKeyJson(change.ItemKey) +
                    ",\"nameKey\":" + Json(change.NameKey) +
                    ",\"beforeEnabled\":" + Bool(change.BeforeEnabled) +
                    ",\"afterEnabled\":" + Bool(change.AfterEnabled) +
                    ",\"beforeEffectiveLevel\":" +
                        change.BeforeEffectiveLevel +
                    ",\"afterEffectiveLevel\":" +
                        change.AfterEffectiveLevel + "}"));
            string categories = string.Join(",", outcome.CategoryChanges.Select(
                change => "{\"categoryId\":" + Json(change.CategoryId) +
                    ",\"beforeCount\":" + change.BeforeCount +
                    ",\"afterCount\":" + change.AfterCount +
                    ",\"beforeBreakpointValue\":" +
                        change.BeforeBreakpointValue +
                    ",\"afterBreakpointValue\":" +
                        change.AfterBreakpointValue + "}"));
            return "{\"movedItems\":" + outcome.MovedItems +
                ",\"rotatedTablets\":" + outcome.RotatedTablets +
                ",\"beforeArtifactsEnabled\":" +
                    outcome.BeforeArtifactsEnabled +
                ",\"afterArtifactsEnabled\":" +
                    outcome.AfterArtifactsEnabled +
                ",\"beforeEffectiveLevels\":" +
                    outcome.BeforeEffectiveLevels +
                ",\"afterEffectiveLevels\":" +
                    outcome.AfterEffectiveLevels +
                ",\"beforeBreakpointValue\":" +
                    outcome.BeforeBreakpointValue +
                ",\"afterBreakpointValue\":" +
                    outcome.AfterBreakpointValue +
                ",\"artifactChanges\":[" + artifacts +
                "],\"categoryChanges\":[" + categories +
                "],\"beforePositionEffects\":" + PositionEffects(outcome.BeforePositionEffects) +
                ",\"afterPositionEffects\":" + PositionEffects(outcome.AfterPositionEffects) + "}";
        }

        private static string ItemKeyJson(InventoryItemKey key) =>
            "{\"entityId\":" + key.EntityId +
            ",\"nativeInstanceId\":" + key.NativeInstanceId + "}";

        private static string Json(string value)
        {
            if (value == null)
            {
                return "null";
            }

            StringBuilder result = new StringBuilder(value.Length + 2);
            result.Append('"');
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    default:
                        if (character < 32)
                        {
                            result.Append("\\u");
                            result.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            result.Append(character);
                        }
                        break;
                }
            }
            result.Append('"');
            return result.ToString();
        }
    }
}
#else
namespace SephiriaEnhancements.Diagnostics
{
    internal static class DeveloperLogger
    {
        internal static bool IsEnabled => false;

        internal static void Initialize() { }

        internal static void RecordModLoadMetrics(float totalMilliseconds,
            float compatibility, float localization, float controls,
            float controllers, float patches, int successfulPatches,
            int failedPatches, string slowestPatch,
            float slowestPatchElapsedMilliseconds)
        { }

        internal static void RecordRetryCheckpointCapture(
            float elapsedMilliseconds, string checkpointKind, string source,
            string floorGuid, string floorName, string stageName,
            string threatType, string generatorType, string bossName,
            int placementCount)
        { }

        internal static void RecordRetryFloorEvaluation(string floorGuid,
            string floorName, string stageName, string threatType,
            string generatorType, bool explorationActivated,
            string checkpointKind, bool checkpointMatchesFloor, bool captured)
        { }

        internal static void RecordRetryOfferDecision(int nativeGameOverType,
            string conclusionKind, string checkpointKind, string floorGuid,
            bool hasCheckpoint, bool serverActive, bool runStarted,
            bool gaveUp, bool offered)
        { }

        internal static void RecordStartupMilestone(string milestone,
            float elapsedMilliseconds)
        { }

        internal static void RecordModPatch(string patchName, bool succeeded,
            float elapsedMilliseconds, float preparationMilliseconds,
            float applicationMilliseconds)
        { }

        internal static void RecordGameLoadingOperation(int loadAttemptId,
            string operation, float elapsedMilliseconds, bool completed)
        { }

        internal static void RecordGameStartupOperation(string operation,
            float elapsedMilliseconds, bool completed)
        { }

        internal static void RecordLocalGameplayContext(
            LocalGameplayContextChange change, long epoch, uint playerNetId,
            string floorGuid, bool traveling)
        { }

        internal static void RecordLoadingMilestone(int loadAttemptId,
            string milestone, string trigger, string sessionLoadMode,
            bool serverObserved, bool clientObserved,
            float elapsedMilliseconds, string floorGuid, string floorName,
            string detail)
        { }

        internal static void Pump() { }

        internal static void RecordDamageFeedback(DamageFeedback feedback, PlayerAvatar owner) { }

        internal static void RecordEncounterLifecycle(
            SephiriaEnhancements.Runtime.EncounterLifecycleEvent lifecycleEvent)
        { }

        internal static void RecordCombatInsightsVisibility(string reason,
            string displayPolicy, string viewMode, bool encounterActive,
            bool bossActive, bool encounterReportOpen, bool bossReportOpen,
            bool encounterReportPaused, bool bossReportPaused,
            bool hiddenByUser, bool hudAttached, bool hudActiveInHierarchy,
            int controlCount, string controlType, bool levelUpIndicatorVisible,
            bool flashScreenVisible, bool screenFading, bool cutSceneActive,
            bool playerLoading, string reportState, string presentationBlock)
        { }

        internal static void RecordRuntimeMetrics(
            SephiriaEnhancements.Runtime.RuntimeMetricSnapshot metrics,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventorySettlementValidation(
            SephiriaEnhancements.Runtime.Inventory.InventorySettlementValidationSnapshot
                validation,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventoryStorageChanged(int width,
            int oldStorage, int newStorage)
        { }

        internal static void RecordInventoryHeightChanged(int oldHeight,
            int newHeight)
        { }

        internal static void RecordInventoryEvaluationOrder(
            SephiriaEnhancements.Runtime.Inventory.InventoryEvaluationOrderSnapshot order,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventoryPositionEffects(
            SephiriaEnhancements.Runtime.Inventory.InventoryPositionEffectsSnapshot effects,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventoryOptimization(
            SephiriaEnhancements.Inventory.InventoryOptimizationProposal result,
            SephiriaEnhancements.Inventory.InventoryApplicationPlan plan,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventoryApplication(bool matched,
            SephiriaEnhancements.Inventory.InventoryApplicationPlan plan,
            int swapsApplied, int rotationsApplied,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordInventorySettlementDifferential(
            SephiriaEnhancements.Runtime.Inventory.
                InventorySettlementDifferentialReport report,
            SephiriaEnhancements.Runtime.RuntimeStateSnapshot state)
        { }

        internal static void RecordMultiplayerRuleResolution(
            SephiriaEnhancements.MultiplayerRules.MultiplayerRuleId ruleId,
            int participantCount,
            SephiriaEnhancements.MultiplayerRules.MultiplayerRulesPreset? preset,
            bool authoritative, bool overridden, float overrideValue)
        { }

        internal static void RecordMultiplayerEnemyHealthResolution(
            SephiriaEnhancements.MultiplayerRules.EnemySpawnOrigin spawnOrigin,
            SephiriaEnhancements.MultiplayerRules.EnemyHealthCategory healthCategory,
            int participantCount, float otherModifierPercent,
            SephiriaEnhancements.MultiplayerRules.EnemyHealthModifierCombination
                combination,
            bool resolved, float multiplier)
        { }

        internal static void RecordMultiplayerRulesLifecycle(string transition,
            SephiriaEnhancements.MultiplayerRules.ActiveExplorationMultiplayerRules
                activeRules)
        { }

        internal static void Shutdown() { }
    }
}
#endif
