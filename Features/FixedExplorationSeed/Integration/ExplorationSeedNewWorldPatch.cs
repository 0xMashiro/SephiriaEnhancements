using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.FixedExplorationSeed.Integration
{
    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.NewGame))]
    internal static class ExplorationSeedNewWorldPatch
    {
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.FixedExplorationSeed)) return;
            try
            {
                PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.FixedExplorationSeed, exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore()
        {
            if (!NetworkServer.active || !EnhancementsSettings.Enabled) return;
            var run = SaveManager.CurrentRun;
            // Native world generation consumes this value before the town and players exist.
            // A saved town, continued exploration or restored checkpoint already owns its seed.
            if (run == null || run.ContainsKey("Seed")) return;
            if (ExplorationSeedInput.TryParse(ExplorationSeedSettings.Text, out var seed) && seed.HasValue)
                run.SetInt("Seed", seed.Value);
        }
    }
}
