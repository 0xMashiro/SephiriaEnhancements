using Mirror;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesLobbySnapshotCoordinator
    {
        private const string LobbyMetadataKey =
            "sephiria_enhancements_multiplayer_rules";

        internal static void Publish(ActiveExplorationMultiplayerRules rules)
        {
            if (!NetworkServer.active || rules == null) return;
            NativeLobbyAccess.TryWriteOwnedSteamMetadata(LobbyMetadataKey,
                ActiveExplorationRulesPayloadCodec.Encode(rules));
        }

        internal static void ClearPublishedSnapshot()
        {
            NativeLobbyAccess.TryWriteOwnedSteamMetadata(LobbyMetadataKey,
                string.Empty);
        }

        internal static void ReadHostSnapshot()
        {
            if (NetworkServer.active) return;
            if (!NativeLobbyAccess.TryReadSteamMetadata(
                    LobbyMetadataKey, out string payload))
            {
                MultiplayerRulesController.ClearHostRulesForClientDisplay();
                return;
            }
            if (string.IsNullOrEmpty(payload))
            {
                MultiplayerRulesController.ClearHostRulesForClientDisplay();
                return;
            }
            if (ActiveExplorationRulesPayloadCodec.TryDecode(payload,
                    out ActiveExplorationMultiplayerRules rules))
                MultiplayerRulesController.ApplyHostRulesForClientDisplay(rules);
            else
            {
                MultiplayerRulesController.ClearHostRulesForClientDisplay();
                Debug.LogWarning("[SephiriaEnhancements] Ignored invalid host " +
                    "multiplayer-rules lobby snapshot.");
            }
        }
    }
}
