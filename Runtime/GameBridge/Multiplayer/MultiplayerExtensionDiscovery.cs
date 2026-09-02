#nullable enable

using System;
using System.Reflection;
using System.Threading;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.Runtime.GameBridge
{
    internal static class MultiplayerExtensionDiscovery
    {
        private const string SephiriaTogetherAssemblyName = "SephiriaTogether";
        private static int detectedProvider;

        static MultiplayerExtensionDiscovery()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            MultiplayerExtensionProvider loadedProvider = DetectLoadedProvider();
            Interlocked.CompareExchange(ref detectedProvider, (int)loadedProvider,
                (int)MultiplayerExtensionProvider.None);
        }

        internal static MultiplayerExtensionProvider DetectedProvider =>
            (MultiplayerExtensionProvider)Volatile.Read(ref detectedProvider);

        internal static bool HasDetectedExtension =>
            DetectedProvider != MultiplayerExtensionProvider.None;

        private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
        {
            MultiplayerExtensionProvider provider = DetectProvider(args.LoadedAssembly);
            if (provider != MultiplayerExtensionProvider.None)
                Interlocked.CompareExchange(ref detectedProvider, (int)provider,
                    (int)MultiplayerExtensionProvider.None);
        }

        private static MultiplayerExtensionProvider DetectLoadedProvider()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                MultiplayerExtensionProvider provider = DetectProvider(assembly);
                if (provider != MultiplayerExtensionProvider.None) return provider;
            }

            return MultiplayerExtensionProvider.None;
        }

        private static MultiplayerExtensionProvider DetectProvider(Assembly assembly)
        {
            return string.Equals(assembly?.GetName().Name,
                    SephiriaTogetherAssemblyName,
                    StringComparison.OrdinalIgnoreCase)
                ? MultiplayerExtensionProvider.SephiriaTogether
                : MultiplayerExtensionProvider.None;
        }
    }
}
