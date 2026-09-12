using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesContext
    {
        internal static int ParticipantCount => NetworkServer.active
            ? ServerParticipantCountReader.Read() : MultiplayerRulesBridge.Received?.Participants ?? PlayerSpawner.MultiplayerList?.Count ?? 0;
        internal static bool CanInspect => EnhancementsSettings.Enabled &&
            FeatureFailure.IsAvailable(FeatureId.MultiplayerRules) &&
            LocalPlayerResolver.Resolve() is PlayerAvatar player && player.loadingScreenType == -1 && DungeonManager.Instance != null;
        internal static bool IsInLobby => EnhancementsSettings.Enabled &&
            FeatureFailure.IsAvailable(FeatureId.MultiplayerRules) &&
            LocalPlayerResolver.Resolve() is PlayerAvatar player &&
            player.loadingScreenType == -1 && DungeonManager.Instance != null &&
            DungeonManager.Instance.IsInMultiZone(player);

        internal static bool CanEdit => IsInLobby && NetworkServer.active &&
            EnhancementsSettings.Enabled && FeatureFailure.IsAvailable(FeatureId.MultiplayerRules) &&
            !MultiplayerRulesExplorationStartPatch.ExplorationStarted &&
            !MultiplayerRulesController.TryGetActivePreset(out _);

        internal static ActiveExplorationMultiplayerRules ReadDisplayed()
        {
            return ReadState()?.Rules;
        }

        internal static MultiplayerRulesState ReadState() => NetworkServer.active
            ? MultiplayerRulesController.ReadServerState() : MultiplayerRulesBridge.Received;
    }
}
