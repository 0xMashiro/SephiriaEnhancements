namespace SephiriaEnhancements.NativeCompanion
{
    internal enum NativeCompanionMode
    {
        Disabled,
        SoloOnly,
        SmartFill,
        AlwaysHost
    }

    internal enum NativeCompanionSessionKind
    {
        Unknown,
        OfflineSolo,
        OnlineHost,
        OnlineClient
    }

    internal enum NativeCompanionPresence
    {
        Absent,
        Present,
        Hold
    }

    internal static class NativeCompanionPolicy
    {
        internal static NativeCompanionSessionKind ClassifySession(bool lobbyStateKnown,
            bool hasLobby, bool playerIsServer, int humanPlayerCount)
        {
            if (humanPlayerCount < 1)
            {
                return NativeCompanionSessionKind.Unknown;
            }

            if (hasLobby || humanPlayerCount > 1)
            {
                return playerIsServer
                    ? NativeCompanionSessionKind.OnlineHost
                    : NativeCompanionSessionKind.OnlineClient;
            }

            return lobbyStateKnown && playerIsServer
                ? NativeCompanionSessionKind.OfflineSolo
                : NativeCompanionSessionKind.Unknown;
        }

        internal static NativeCompanionPresence Evaluate(bool suiteEnabled,
            NativeCompanionMode mode,
            bool serverActive, bool sessionActive, bool playerAvailable,
            bool playerIsServer, bool playerIsAlive,
            NativeCompanionSessionKind sessionKind,
            int humanPlayerCount, bool companionPresent, bool playerInBattle)
        {
            if (!suiteEnabled || mode == NativeCompanionMode.Disabled || !serverActive ||
                !sessionActive || !playerAvailable || !playerIsServer || !playerIsAlive)
            {
                return NativeCompanionPresence.Absent;
            }

            if (sessionKind == NativeCompanionSessionKind.Unknown || humanPlayerCount < 1)
            {
                return NativeCompanionPresence.Hold;
            }

            bool shouldBePresent = sessionKind == NativeCompanionSessionKind.OfflineSolo ||
                (sessionKind == NativeCompanionSessionKind.OnlineHost &&
                    (mode == NativeCompanionMode.AlwaysHost ||
                     (mode == NativeCompanionMode.SmartFill && humanPlayerCount == 1)));
            if (mode == NativeCompanionMode.SoloOnly &&
                sessionKind != NativeCompanionSessionKind.OfflineSolo)
            {
                shouldBePresent = false;
            }

            if (shouldBePresent)
            {
                return NativeCompanionPresence.Present;
            }

            return companionPresent && playerInBattle
                ? NativeCompanionPresence.Hold
                : NativeCompanionPresence.Absent;
        }
    }
}
