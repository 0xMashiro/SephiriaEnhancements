using System;
using System.Collections.Generic;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.AutoCasting.Integration;
using SephiriaEnhancements.ResourceBarValues.Integration;
using SephiriaEnhancements.ModJournal.Integration;
using UnityEngine;

namespace SephiriaEnhancements
{
    public sealed partial class SephiriaEnhancementsMod
    {
        private readonly FeaturePatchSet featurePatches = new(HarmonyId);
        private readonly FeatureCleanup featureCleanup = new((feature, exception) =>
            SupportLogger.Failure("feature_cleanup_failed." + feature, exception));
        private readonly Dictionary<FeatureId, List<MonoBehaviour>> featureControllers = new();

        private void InitializeFeature(FeatureId feature, Action initialize) =>
            FeatureFailure.Run(feature, initialize);

        private static void RegisterLocalization(HorayModLocalizationContext context) =>
            FeatureFailure.Run(FeatureId.Settings, () => Configuration.ModLocalization.Register(context));

        private static bool ValidateFeature(FeatureId feature, Func<bool> validate)
        {
            return FeatureFailure.Run(feature, () =>
            {
                if (!validate()) throw new MissingMemberException("Required feature contract is unavailable.");
            });
        }

        private T AddController<T>(FeatureId feature) where T : MonoBehaviour
        {
            T component = controllerObject.AddComponent<T>();
            if (!featureControllers.TryGetValue(feature, out var controllers))
                featureControllers.Add(feature, controllers = new List<MonoBehaviour>());
            controllers.Add(component);
            return component;
        }

        private void ReportFeatureFailure(FeatureId feature, Exception exception)
        {
            SupportLogger.Failure("feature_disabled." + feature, exception);
        }

        internal static void CleanupFeature(FeatureId feature, Action cleanup)
        {
            try { cleanup(); }
            catch (Exception exception)
            {
                SupportLogger.Failure("feature_cleanup_failed." + feature, exception);
            }
        }

        private void ProcessFeatureFailures()
        {
            // Run outside patch callbacks, after installation or on the next Unity frame.
            while (FeatureFailure.TryTakeFailure(out FeatureId feature))
            {
                CleanupFeature(feature, () => StopFeature(feature));
                CleanupFeature(feature, () => featurePatches.Remove(feature));
                if (featureControllers.TryGetValue(feature, out var controllers))
                {
                    foreach (MonoBehaviour component in controllers)
                        if (component != null)
                            CleanupFeature(feature, () => UnityEngine.Object.DestroyImmediate(component));
                    featureControllers.Remove(feature);
                }
            }
            NativeRetryFailure.Tick();
            NativeModNotifications.Tick();
            ShowFailureNotice();
        }

        private void ShowFailureNotice()
        {
            FeatureId[] features = FeatureFailure.PendingNotices;
            if (features.Length == 0) return;
            if (NativeModNotifications.Chat(() => FeatureFailureLocalization.Describe(features, Configuration.ModLocalization.Get)))
                foreach (FeatureId feature in features) FeatureFailure.AcknowledgeNotice(feature);
        }

        private void StopFeature(FeatureId feature)
        {
            CleanupFeature(feature, () => keyboardUiNavigation?.CancelFeatureSelection(feature));
            featureCleanup.Stop(feature);
        }

        private void RegisterFeatureCleanup()
        {
            featureCleanup.Add(FeatureId.Settings, NativeOptionsLifetime.DisposeAll);
            featureCleanup.Add(FeatureId.KeyboardUiNavigation, KeyboardUiNavigation.Integration.NativeTextInputKeyboard.Reset);
            featureCleanup.Add(FeatureId.CharacterPanelNavigation, () => KeyboardUiNavigation.CharacterPanelNavigationMemory.ResetAll(destroy: true));
            featureCleanup.Add(FeatureId.RewardNavigation, KeyboardUiNavigation.RewardKeyboardNavigation.Reset);
            featureCleanup.Add(FeatureId.MultiplayerRules, () => MultiplayerRulesController.SetIntegrationAvailable(false));
            featureCleanup.Add(FeatureId.MultiplayerRules, () => multiplayerRules?.Shutdown());
            featureCleanup.Add(FeatureId.MultiplayerAccess, null, MultiplayerAccess.Integration.JoiningSupplyBridge.NotifyHostUnavailable);
            featureCleanup.Add(FeatureId.MultiplayerAccess, () => MidRunAdmissionRuntime.SetIntegrationAvailable(false));
            featureCleanup.Add(FeatureId.MultiplayerRules, () => EnemySpawnRoutineContext.SetRuleScopeFactory(null));
            featureCleanup.Add(FeatureId.Inventory, () => inventoryOptimization?.Shutdown(), () => inventoryOptimization?.StopAfterFeatureFailure());
            featureCleanup.Add(FeatureId.DefeatRetry, DefeatRetryBridge.Shutdown, DefeatRetryBridge.StopAfterFeatureFailure);
            featureCleanup.Add(FeatureId.DefeatRetry, () =>
            {
                var button = UIManager.Instance?.GetElement<UI_GameOverLabel>()?.GetComponent<DefeatRetryButton>();
                if (button != null) UnityEngine.Object.DestroyImmediate(button);
            });
            featureCleanup.Add(FeatureId.CombatInsights, () =>
            {
                if (combatInsights != null) DefeatRetryBridge.TeamDefeated -= combatInsights.FinishDefeatedEncounter;
            });
            featureCleanup.Add(FeatureId.CombatInsights, () => DamageFeedbackCapture.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => DamageDetailCapture.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => UnitDeathCapture.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => LocalFinalBlowCapture.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => NativeReportDismissal.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => NativeStatisticsPauseEntry.SetController(null));
            featureCleanup.Add(FeatureId.CombatInsights, () => combatInsights?.Shutdown());
            featureCleanup.Add(FeatureId.CombatInsights, () => TrainingDamageStatistics.Instance?.Shutdown());
            featureCleanup.Add(FeatureId.Gameplay, NativeLocalPlayerData.Shutdown);
            featureCleanup.Add(FeatureId.Gameplay, () =>
            {
                if (runtimeKernel != null) runtimeKernel.GameplayContextChanged -= OnLocalGameplayContextChanged;
            });
            featureCleanup.Add(FeatureId.Gameplay, () => runtimeKernel?.Dispose());
            featureCleanup.Add(FeatureId.DeveloperTools, DeveloperLogger.Shutdown);
            featureCleanup.Add(FeatureId.ResourceBarValues, NativeResourceBarValueView.DisposeAll);
            featureCleanup.Add(FeatureId.CostumeAppearance, () => CostumeAppearance.Integration.NativeCostumeAppearance.Instance?.Shutdown());
            featureCleanup.Add(FeatureId.AutoCasting, NativeAutoCastingUi.DisposeAll);
            featureCleanup.Add(FeatureId.ModJournal, NativeModJournal.DisposeAll);
            featureCleanup.Add(FeatureId.EffectStats, EffectStats.Integration.NativeEffectStatsView.DisposeAll);
        }

        private void UnpatchFeatures()
        {
            foreach (FeatureId feature in Enum.GetValues(typeof(FeatureId)))
                CleanupFeature(feature, () => featurePatches.Remove(feature));
            featureControllers.Clear();
        }
    }
}

namespace SephiriaEnhancements.Runtime
{
    internal sealed class FeatureFailurePump : MonoBehaviour
    {
        internal Action Process;
        private bool failureReported;
        private void LateUpdate()
        {
            try { Process?.Invoke(); }
            catch (Exception exception)
            {
                if (failureReported) return;
                failureReported = true;
                SupportLogger.Failure("feature_cleanup_pump_failed", exception);
            }
        }
    }
}
