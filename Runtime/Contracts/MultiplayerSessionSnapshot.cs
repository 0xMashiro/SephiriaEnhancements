namespace SephiriaEnhancements.Runtime
{
    internal enum MultiplayerExtensionProvider
    {
        None,
        SephiriaTogether
    }

    internal sealed class MultiplayerSessionSnapshot
    {
        internal MultiplayerSessionSnapshot(int connectedHumanParticipantCount,
            MultiplayerExtensionProvider extensionProvider)
        {
            ConnectedHumanParticipantCount = connectedHumanParticipantCount < 0
                ? 0 : connectedHumanParticipantCount;
            ExtensionProvider = extensionProvider;
        }

        internal int ConnectedHumanParticipantCount { get; }

        internal MultiplayerExtensionProvider ExtensionProvider { get; }

        internal bool HasMultiplayerExtension =>
            ExtensionProvider != MultiplayerExtensionProvider.None;
    }
}
