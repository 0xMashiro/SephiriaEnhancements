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

        private void OnEnable() => currentController = this;

        private void OnDisable()
        {
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
            currentActiveRules = null;
        }

        internal static void EndExploration()
        {
            DeveloperLogger.RecordMultiplayerRulesLifecycle("end",
                currentActiveRules);
            MultiplayerRulesLobbySnapshotCoordinator.ClearPublishedSnapshot();
            EnemyHealthAdjustmentBridge.SetResolver(null);
            currentController?.session.EndExploration();
            currentActiveRules = null;
            allowExternalRuleStackingForExploration = false;
        }

        internal void BeginServerExploration(bool isSavedExploration)
        {
            allowExternalRuleStackingForExploration =
                PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking();
            MultiplayerSessionSnapshot multiplayer =
                NativeMultiplayerSessionReader.Read();
            bool compatibilityPassThrough =
                multiplayer.ConnectedHumanParticipantCount > 4 ||
                (multiplayer.HasMultiplayerExtension &&
                 !allowExternalRuleStackingForExploration);
            if (!integrationAvailable || compatibilityPassThrough)
            {
                ActiveExplorationMultiplayerRules passThroughRules =
                    ActiveExplorationMultiplayerRules.FromPreset(
                        MultiplayerRulesPreset.Original);
                session.ResumeExploration(passThroughRules);
                ActiveExplorationRulesStore.Write(passThroughRules);
                currentActiveRules = passThroughRules;
                DeveloperLogger.RecordMultiplayerRulesLifecycle(
                    integrationAvailable
                        ? "begin_external_multiplayer_pass_through"
                        : "begin_compatibility_pass_through", passThroughRules);
                MultiplayerRulesLobbySnapshotCoordinator.Publish(passThroughRules);
                return;
            }

            if (isSavedExploration)
            {
                ActiveExplorationMultiplayerRules restoredRules;
                if (!ActiveExplorationRulesStore.TryRead(out restoredRules))
                {
                    restoredRules = ActiveExplorationMultiplayerRules.FromPreset(
                        MultiplayerRulesPreset.Original);
                    ActiveExplorationRulesStore.Write(restoredRules);
                }

                session.ResumeExploration(restoredRules);
                currentActiveRules = restoredRules;
                DeveloperLogger.RecordMultiplayerRulesLifecycle("resume",
                    restoredRules);
                ConfigureHealthAdjustment(restoredRules);
                MultiplayerRulesLobbySnapshotCoordinator.Publish(restoredRules);
                return;
            }

            ActiveExplorationMultiplayerRules activeRules =
                session.BeginNewExploration(PreferredMultiplayerRulesStore.Read());
            ActiveExplorationRulesStore.Write(activeRules);
            currentActiveRules = activeRules;
            DeveloperLogger.RecordMultiplayerRulesLifecycle("begin", activeRules);
            ConfigureHealthAdjustment(activeRules);
            MultiplayerRulesLobbySnapshotCoordinator.Publish(activeRules);
        }

        internal void PublishActiveRulesForLobbyDisplay()
        {
            if (session.TryGetActive(out ActiveExplorationMultiplayerRules activeRules))
                MultiplayerRulesLobbySnapshotCoordinator.Publish(activeRules);
        }

        internal void Shutdown()
        {
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
                MultiplayerRuleId.SeedEncounterBossHealthMultiplier,
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
