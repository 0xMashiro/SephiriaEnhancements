using Mirror;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal static class ConnectedServerHumanParticipantReader
    {
        internal static int Count()
        {
            int count = 0;
            foreach (NetworkConnectionToClient connection in
                NetworkServer.connections.Values)
            {
                if (connection?.identity != null &&
                    connection.identity.TryGetComponent<PlayerSpawner>(out _))
                    count++;
            }

            return count;
        }
    }
}
