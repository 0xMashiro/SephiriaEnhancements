using SephiriaEnhancements.DeveloperTools.Core;

namespace SephiriaEnhancements.ModelChecks.Features.DeveloperTools;

internal static class DeveloperPlayerDamagePolicyChecks
{
    internal static void Run()
    {
        if (DeveloperPlayerDamagePolicy.MultiplierCount != 5 ||
            DeveloperPlayerDamagePolicy.NormalizeIndex(-1) != 0 ||
            DeveloperPlayerDamagePolicy.NormalizeIndex(5) != 4 ||
            Math.Abs(DeveloperPlayerDamagePolicy.GetMultiplier(0) - 1f) > 0.001f ||
            Math.Abs(DeveloperPlayerDamagePolicy.GetMultiplier(4) - 100f) > 0.001f ||
            Math.Abs(DeveloperPlayerDamagePolicy.Apply(12.5f, 2) - 62.5f) > 0.001f)
        {
            throw new InvalidOperationException(
                "developer player damage multiplier mapping failed");
        }
    }
}
