using Mirror;

namespace SephiriaEnhancements.Integration
{
    internal static class LocalPlayerResolver
    {
        internal static PlayerAvatar Resolve()
        {
            NetworkIdentity identity = NetworkClient.localPlayer;
            return identity != null ? identity.GetComponent<PlayerAvatar>() : null;
        }

        internal static bool IsLocal(PlayerSpawner spawner, PlayerAvatar avatar)
        {
            if (spawner == null || avatar == null)
            {
                return false;
            }

            return IsLocal(avatar);
        }

        internal static bool IsLocal(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return false;
            }

            NetworkIdentity identity = NetworkClient.localPlayer;
            return identity != null && avatar.netIdentity == identity;
        }
    }
}
