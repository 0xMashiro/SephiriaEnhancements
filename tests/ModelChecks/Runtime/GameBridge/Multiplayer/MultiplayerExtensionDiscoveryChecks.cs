using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;
using System.Reflection;
using System.Reflection.Emit;

namespace SephiriaEnhancements.ModelChecks.Runtime.GameBridge.Multiplayer;

internal static class MultiplayerExtensionDiscoveryChecks
{
    internal static void Run()
    {
        var vanillaMultiplayerSession = new MultiplayerSessionSnapshot(4,
            MultiplayerExtensionProvider.None);
        var extendedMultiplayerSession = new MultiplayerSessionSnapshot(7,
            MultiplayerExtensionProvider.SephiriaTogether);
        if (vanillaMultiplayerSession.ConnectedHumanParticipantCount != 4 ||
            vanillaMultiplayerSession.HasMultiplayerExtension ||
            extendedMultiplayerSession.ConnectedHumanParticipantCount != 7 ||
            !extendedMultiplayerSession.HasMultiplayerExtension)
            throw new InvalidOperationException(
                "multiplayer runtime snapshot must preserve connected humans and provider");

        if (MultiplayerExtensionDiscovery.HasDetectedExtension)
            throw new InvalidOperationException(
                "multiplayer extension discovery must start empty in model checks");
        AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("SephiriaTogether"), AssemblyBuilderAccess.Run)
            .DefineDynamicModule("Main");
        if (!MultiplayerExtensionDiscovery.HasDetectedExtension ||
            MultiplayerExtensionDiscovery.DetectedProvider !=
                MultiplayerExtensionProvider.SephiriaTogether)
        {
            throw new InvalidOperationException(
                "multiplayer extension discovery must observe assemblies loaded later");
        }
        Console.WriteLine("MultiplayerExtensionDiscovery: initial and late-load detection passed");
    }
}
