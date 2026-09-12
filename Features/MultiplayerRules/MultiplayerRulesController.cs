using UnityEngine;
using Mirror;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;

namespace SephiriaEnhancements.MultiplayerRules
{
    internal sealed class MultiplayerRulesController : MonoBehaviour
    {
        private static ActiveExplorationMultiplayerRules currentActiveRules;
        private static MultiplayerRulesController currentController;
        private static bool integrationAvailable;
        private static bool allowExternalRuleStackingForExploration;
        private readonly MultiplayerRulesSession session = new MultiplayerRulesSession();
        private bool announceExploration;
        private readonly NativeLobbyRulesPoint lobbyPoint = new();

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules)) return;
            try
            {
                lobbyPoint.Update();
                MultiplayerRulesBridge.Tick();
                if (announceExploration && NetworkServer.active && currentActiveRules != null)
                {
                    var player = SephiriaEnhancements.Integration.LocalPlayerResolver.Resolve();
                    var floor = player == null ? null : FloorGenerator.FindByGuid(player.currentFloorGuid);
                    if (player != null && player.loadingScreenType == -1 && floor?.DataOnServer != null &&
                        !string.IsNullOrEmpty(floor.DataOnServer.stageName) && !DungeonManager.Instance.IsInMultiZone(player))
                    {
                        announceExploration = false;
                        MultiplayerRulesBridge.Announce(MultiplayerRulesNotice.Started);
                    }
                }
            }
            catch (System.Exception exception)
            {
                lobbyPoint.Dispose();
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
            }
        }

        private void OnEnable()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                OnEnableCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void OnEnableCore()
        {
            currentController = this;
            MultiplayerRulesBridge.Initialize();
        }

        private void OnDisable()
        {
            lobbyPoint.Dispose();
            MultiplayerRulesBridge.Shutdown();
            if (currentController == this) currentController = null;
        }

        internal static bool TryGetActivePreset(out MultiplayerRulesPreset preset)
        {
            if (currentActiveRules == null)
            {
                preset = MultiplayerRulesPreset.Original;
                return false;
            }

            preset = currentActiveRules.Preset;
            return true;
        }

        internal static bool TryGetActiveOverride(MultiplayerRuleId id,
            int participantCount, out float value)
        {
            value = 0f;
            bool authoritative = CanApplyCurrentRules(participantCount);
            bool overridden = authoritative &&
                currentActiveRules.Rules.Get(id, participantCount)
                    .TryGetOverride(out value);
            DeveloperLogger.RecordMultiplayerRuleResolution(id,
                participantCount, currentActiveRules?.Preset, authoritative,
                overridden, value);
            return overridden;
        }

        internal static void SetIntegrationAvailable(bool available)
        {
            integrationAvailable = available;
            if (!available)
                EnemyHealthAdjustmentBridge.SetResolver(null);
        }

        internal static bool TryGetDisplayedActiveRules(
            out ActiveExplorationMultiplayerRules activeRules)
        {
            activeRules = currentActiveRules;
            return activeRules != null;
        }

        internal static bool TryGetAuthoritativeActiveRules(
            out ActiveExplorationMultiplayerRules activeRules)
        {
            activeRules = currentActiveRules;
            return CanApplyCurrentRules(ServerParticipantCountReader.Read());
        }

        internal static void ApplyHostRulesForClientDisplay(
            ActiveExplorationMultiplayerRules activeRules)
        {
            currentActiveRules = activeRules;
        }

        internal static void ClearHostRulesForClientDisplay()
        {
            if (!NetworkServer.active) currentActiveRules = null;
        }

        internal static MultiplayerRulesState ReadServerState()
        {
            var multiplayer = NativeMultiplayerSessionReader.Read();
            bool active = currentActiveRules != null;
            bool allowStacking = active ? allowExternalRuleStackingForExploration : PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking();
            int participants = ServerParticipantCountReader.Read();
            var availability = MultiplayerRulesLifecyclePolicy.ResolveAvailability(active || Configuration.EnhancementsSettings.Enabled,
                integrationAvailable && FeatureFailure.IsAvailable(FeatureId.MultiplayerRules), participants,
                multiplayer.HasMultiplayerExtension, allowStacking);
            return new MultiplayerRulesState(currentActiveRules ?? PreferredMultiplayerRulesStore.Read().Freeze(),
                participants, active, availability, allowStacking);
        }

        internal static void EndExploration()
        {
            DeveloperLogger.RecordMultiplayerRulesLifecycle("end",
                currentActiveRules);
            EnemyHealthAdjustmentBridge.SetResolver(null);
            currentController?.session.EndExploration();
            if (currentController != null) currentController.announceExploration = false;
            currentActiveRules = null;
            allowExternalRuleStackingForExploration = false;
        }

        internal void BeginServerExploration(bool isSavedExploration)
        {
            announceExploration = true;
            ActiveExplorationMultiplayerRules rules;
            if (isSavedExploration)
            {
                if (!ActiveExplorationRulesStore.TryRead(out rules, out allowExternalRuleStackingForExploration))
                {
                    rules = ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original);
                    allowExternalRuleStackingForExploration = false;
                }
                session.ResumeExploration(rules);
            }
            else
            {
                allowExternalRuleStackingForExploration = PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking();
                var multiplayer = NativeMultiplayerSessionReader.Read();
                var availability = MultiplayerRulesLifecyclePolicy.ResolveAvailability(Configuration.EnhancementsSettings.Enabled,
                    integrationAvailable, ServerParticipantCountReader.Read(), multiplayer.HasMultiplayerExtension,
                    allowExternalRuleStackingForExploration);
                rules = session.BeginNewExploration(PreferredMultiplayerRulesStore.Read(),
                    availability == MultiplayerRulesAvailability.Available);
            }

            currentActiveRules = rules;
            ActiveExplorationRulesStore.Write(rules, allowExternalRuleStackingForExploration);
            DeveloperLogger.RecordMultiplayerRulesLifecycle(isSavedExploration ? "resume" : "begin", rules);
            ConfigureHealthAdjustment(rules);
            MultiplayerRulesBridge.Publish();
        }

        internal void Shutdown()
        {
            lobbyPoint.Dispose();
            NativeMultiplayerRulesPanel.CloseCurrent();
            MultiplayerRulesBridge.Shutdown();
            EndExploration();
        }

        private void ConfigureHealthAdjustment(
            ActiveExplorationMultiplayerRules activeRules)
        {
            bool required = integrationAvailable && activeRules.Rules.HasAnyOverride(
                MultiplayerRuleId.RegularEnemyHealthMultiplier,
                MultiplayerRuleId.RegularEnemyDamageBonus,
                MultiplayerRuleId.EliteEnemyHealthMultiplier,
                MultiplayerRuleId.EliteEnemyDamageBonus,
                MultiplayerRuleId.RandomEncounterHealthMultiplier,
                MultiplayerRuleId.RandomEncounterDamageBonus,
                MultiplayerRuleId.KrazBossHealthMultiplier,
                MultiplayerRuleId.BossEncounterDamageBonus,
                MultiplayerRuleId.MindEaterRootSummonHealthMultiplier,
                MultiplayerRuleId.MindEaterRootSummonDamageBonus);
            EnemyHealthAdjustmentBridge.SetResolver(required
                ? ResolveEnemyHealthMultiplier : null);
        }

        private bool ResolveEnemyHealthMultiplier(EnemySpawnOrigin spawnOrigin,
            EnemyHealthCategory healthCategory, int participantCount,
            float otherModifierPercent, out float multiplier)
        {
            if (!session.TryGetActive(out var activeRules) ||
                !CanApplyCurrentRules(participantCount))
            {
                multiplier = 1f;
                return false;
            }

            bool resolved = EnemyHealthRuleResolver.TryResolveMultiplier(activeRules,
                spawnOrigin, healthCategory, participantCount, otherModifierPercent,
                out multiplier);
            DeveloperLogger.RecordMultiplayerEnemyHealthResolution(spawnOrigin,
                healthCategory, participantCount, otherModifierPercent,
                activeRules.HealthModifierCombination, resolved, multiplier);
            return resolved;
        }

        private static bool CanApplyCurrentRules(int participantCount)
        {
            MultiplayerSessionSnapshot multiplayer =
                NativeMultiplayerSessionReader.Read();
            return MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                NetworkServer.active, currentActiveRules != null,
                integrationAvailable, participantCount,
                multiplayer.HasMultiplayerExtension,
                allowExternalRuleStackingForExploration);
        }
    }
}
