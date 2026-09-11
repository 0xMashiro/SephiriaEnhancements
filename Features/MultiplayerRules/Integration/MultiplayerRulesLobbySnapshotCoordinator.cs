using SephiriaEnhancements.Diagnostics;
using Mirror;
using SephiriaEnhancements.Runtime.GameBridge;
using UnityEngine;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class MultiplayerRulesLobbySnapshotCoordinator
    {
        private const string PreferredMetadataKey = "sephiria_enhancements_lobby_rules";

        internal static void PublishLobbyRules()
        {
            if (!NetworkServer.active) return;
            var rules = MultiplayerRulesLobbyContext.ReadDisplayed();
            var multiplayer = NativeMultiplayerSessionReader.Read();
            if (!Configuration.EnhancementsSettings.Enabled ||
                multiplayer.ConnectedHumanParticipantCount > 4 ||
                (multiplayer.HasMultiplayerExtension && !PreferredMultiplayerRulesStore.ReadAllowExternalRuleStacking()))
                rules = ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original);
            NativeLobbyAccess.TryWriteOwnedSteamMetadata(PreferredMetadataKey,
                ActiveExplorationRulesPayloadCodec.Encode(rules));
        }

        internal static ActiveExplorationMultiplayerRules ReadLobbyRules()
        {
            if (!NativeLobbyAccess.TryReadSteamMetadata(PreferredMetadataKey, out var payload) ||
                string.IsNullOrEmpty(payload)) return null;
            return ActiveExplorationRulesPayloadCodec.TryDecode(payload, out var rules) ? rules : null;
        }
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
                SupportLogger.Warning("multiplayer_rules_snapshot_invalid", "[SephiriaEnhancements] Ignored invalid host " +
                    "multiplayer-rules lobby snapshot.");
            }
        }
    }
}
