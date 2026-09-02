[assembly: System.Reflection.AssemblyMetadata("BuildFlavor", SephiriaEnhancements.Diagnostics.BuildIdentity.Flavor)]

namespace SephiriaEnhancements.Diagnostics
{
    internal static class BuildIdentity
    {
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
        internal const string Flavor = "Development";
#else
        internal const string Flavor = "Release";
#endif
    }
}
