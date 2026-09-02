using System.Collections.Generic;
using Mirror;

namespace SephiriaEnhancements.Integration
{
    internal static class LocalPlayerResolver
    {
        internal static bool IsLocal(PlayerSpawner spawner, PlayerAvatar avatar)
        {
            if (spawner == null || avatar == null)
            {
                return false;
            }

            if (spawner.isOwned || spawner.isLocalPlayer)
            {
                return true;
            }

            NetworkIdentity localIdentity = NetworkClient.localPlayer;
            if (localIdentity != null && spawner.netIdentity == localIdentity)
            {
                return true;
            }

            return IsCameraObserver(avatar);
        }

        internal static bool IsLocal(PlayerAvatar avatar)
        {
            if (avatar == null)
            {
                return false;
            }

            if (avatar.isOwned || avatar.isLocalPlayer || IsCameraObserver(avatar))
            {
                return true;
            }

            IReadOnlyList<PlayerSpawner> players = PlayerSpawner.MultiplayerList;
            if (players == null)
            {
                return false;
            }

            for (int index = 0; index < players.Count; index++)
            {
                PlayerSpawner spawner = players[index];
                if (spawner?.PlayerAvatar == avatar &&
                    (spawner.isOwned || spawner.isLocalPlayer))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCameraObserver(PlayerAvatar avatar)
        {
            PlayerAvatar observer = GameCamera.Instance?.Observer;
            return observer != null && (observer == avatar ||
                (observer.netId != 0 && observer.netId == avatar.netId));
        }
    }
}
