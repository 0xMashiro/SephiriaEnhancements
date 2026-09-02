using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch]
    internal static class MultiplayerRulesNetworkSessionEndPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // These native network lifecycle names are integration contracts.
            yield return AccessTools.DeclaredMethod(typeof(HorayNetworkManager),
                nameof(HorayNetworkManager.OnStopServer));
            yield return AccessTools.DeclaredMethod(typeof(HorayNetworkManager),
                nameof(HorayNetworkManager.OnStopClient));
        }

        private static void Postfix() => MultiplayerRulesController.EndExploration();
    }
}
