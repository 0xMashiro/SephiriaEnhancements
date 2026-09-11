using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesLobbyContext
    {
        internal static int ParticipantCount => NetworkServer.active
            ? ServerParticipantCountReader.Read() : PlayerSpawner.MultiplayerList?.Count ?? 0;
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
            if (NetworkServer.active)
                return MultiplayerRulesController.TryGetDisplayedActiveRules(out var active)
                    ? active : PreferredMultiplayerRulesStore.Read().Freeze();
            return MultiplayerRulesLobbySnapshotCoordinator.ReadLobbyRules();
        }
    }
}
