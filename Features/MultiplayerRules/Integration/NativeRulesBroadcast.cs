using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules.Presentation;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class NativeRulesBroadcast
    {
        internal static string Describe(MultiplayerRulesState state, MultiplayerRulesNotice notice, int changes) =>
            MultiplayerRulesSummary.Format(state, notice, changes, ModLocalization.Get);

        internal static void SendNative(NetworkConnectionToClient peer, MultiplayerRulesState state,
            MultiplayerRulesNotice notice, int changes)
        {
            var world = DungeonManager.Instance;
            if (!NetworkServer.active || world == null || peer == null || !peer.isReady) return;
            foreach (string line in MultiplayerRulesSummary.SplitForNativeChat(Describe(state, notice, changes)))
            {
                using var writer = NetworkWriterPool.Get();
                writer.WriteNetworkBehaviour((PlayerAvatar)null);
                writer.WriteString("[Sephiria Enhancements]");
                writer.WriteString(line);
                // Native DungeonManager.RpcChat(PlayerAvatar, string, string), sent to
                // this peer only. RPC hashes use the low 16 bits, not GetStableHashCode16.
                peer.Send(new RpcMessage { netId = world.netId, componentIndex = world.ComponentIndex,
                    functionHash = unchecked((ushort)"System.Void DungeonManager::RpcChat(PlayerAvatar,System.String,System.String)".GetStableHashCode()),
                    payload = writer.ToArraySegment() });
            }
        }

    }
}
