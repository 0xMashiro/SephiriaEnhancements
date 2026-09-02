using SephiriaEnhancements.Runtime.GameBridge;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class ServerParticipantCountReader
    {
        internal static int Read()
        {
            int count = ConnectedServerHumanParticipantReader.Count();
            if (count < 1) return 1;
            return count;
        }
    }
}
