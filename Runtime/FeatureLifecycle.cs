using System;
using System.Collections.Generic;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Runtime;
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
            if (keyboardUiNavigation != null) keyboardUiNavigation.CancelFeatureSelection(feature);
            switch (feature)
            {
                case FeatureId.KeyboardUiNavigation:
                    CleanupFeature(feature, KeyboardUiNavigation.Integration.NativeTextInputKeyboard.Reset);
                    break;
                case FeatureId.CharacterPanelNavigation:
                    CleanupFeature(feature, () => KeyboardUiNavigation.CharacterPanelNavigationMemory.ResetAll(destroy: true));
                    break;
                case FeatureId.RewardNavigation:
                    CleanupFeature(feature, KeyboardUiNavigation.RewardKeyboardNavigation.Reset);
                    break;
                case FeatureId.EffectStats:
                    CleanupFeature(feature, EffectStats.Integration.NativeEffectStatsView.DisposeAll);
                    break;
                case FeatureId.Inventory:
                    CleanupFeature(feature, () => inventoryOptimization?.StopAfterFeatureFailure());
                    break;
                case FeatureId.DefeatRetry:
                    CleanupFeature(feature, DefeatRetryBridge.StopAfterFeatureFailure);
                    CleanupFeature(feature, () =>
                    {
                        var button = UIManager.Instance?.GetElement<UI_GameOverLabel>()?.GetComponent<DefeatRetryButton>();
                        if (button != null) UnityEngine.Object.DestroyImmediate(button);
                    });
                    break;
                case FeatureId.MultiplayerRules:
                    CleanupFeature(feature, () => MultiplayerRulesController.SetIntegrationAvailable(false));
                    CleanupFeature(feature, () => EnemySpawnRoutineContext.SetRuleScopeFactory(null));
                    CleanupFeature(feature, () => multiplayerRules?.Shutdown());
                    break;
                case FeatureId.MultiplayerAccess:
                    CleanupFeature(feature, MultiplayerAccess.Integration.JoiningSupplyBridge.NotifyHostUnavailable);
                    CleanupFeature(feature, () => MidRunAdmissionRuntime.SetIntegrationAvailable(false));
                    break;
                case FeatureId.Gameplay:
                    CleanupFeature(feature, () => runtimeKernel?.Dispose());
                    break;
                case FeatureId.CombatInsights:
                    CleanupFeature(feature, () => DamageFeedbackCapture.SetController(null));
                    CleanupFeature(feature, () => DamageDetailCapture.SetController(null));
                    CleanupFeature(feature, () => UnitDeathCapture.SetController(null));
                    CleanupFeature(feature, () => LocalFinalBlowCapture.SetController(null));
                    CleanupFeature(feature, () => NativeReportDismissal.SetController(null));
                    CleanupFeature(feature, () => NativeStatisticsPauseEntry.SetController(null));
                    CleanupFeature(feature, () => combatInsights?.Shutdown());
                    break;
                case FeatureId.ResourceBarValues:
                    CleanupFeature(feature, NativeResourceBarValueView.DisposeAll);
                    break;
                case FeatureId.AutoCasting:
                    CleanupFeature(feature, NativeAutoCastingUi.DisposeAll);
                    break;
                case FeatureId.ModJournal:
                    CleanupFeature(feature, NativeModJournal.DisposeAll);
                    break;
            }
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
