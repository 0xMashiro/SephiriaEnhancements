using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal static class NativeMultiplayerSessionReader
    {
        internal static MultiplayerSessionSnapshot Read()
        {
            return new MultiplayerSessionSnapshot(
                ConnectedServerHumanParticipantReader.Count(),
                MultiplayerExtensionDiscovery.DetectedProvider);
        }
    }
}
