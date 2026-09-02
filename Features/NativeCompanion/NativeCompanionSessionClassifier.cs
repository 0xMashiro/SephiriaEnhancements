using SephiriaEnhancements.Runtime.GameBridge;

namespace SephiriaEnhancements.NativeCompanion
{
    internal static class NativeCompanionSessionClassifier
    {
        internal static NativeCompanionSessionKind Classify(PlayerAvatar player,
            out int humanPlayerCount)
        {
            humanPlayerCount = CountHumanPlayers(player);
            bool lobbyStateKnown = NativeLobbyAccess.TryReadPresence(
                out bool hasLobby);
            return NativeCompanionPolicy.ClassifySession(lobbyStateKnown, hasLobby,
                player != null && player.isServer, humanPlayerCount);
        }

        private static int CountHumanPlayers(PlayerAvatar localPlayer)
        {
            System.Collections.Generic.IReadOnlyList<PlayerSpawner> players =
                PlayerSpawner.MultiplayerList;
            int count = 0;
            bool containsLocal = false;
            if (players != null)
            {
                for (int index = 0; index < players.Count; index++)
                {
                    PlayerAvatar avatar = players[index]?.PlayerAvatar;
                    if (avatar == null || IsDuplicate(players, index, avatar))
                    {
                        continue;
                    }

                    count++;
                    if (avatar == localPlayer)
                    {
                        containsLocal = true;
                    }
                }
            }

            if (localPlayer != null && !containsLocal)
            {
                count++;
            }

            return count;
        }

        private static bool IsDuplicate(
            System.Collections.Generic.IReadOnlyList<PlayerSpawner> players,
            int beforeIndex, PlayerAvatar avatar)
        {
            for (int index = 0; index < beforeIndex; index++)
            {
                if (players[index]?.PlayerAvatar == avatar)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
