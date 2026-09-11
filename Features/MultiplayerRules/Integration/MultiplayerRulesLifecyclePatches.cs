using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch(typeof(DungeonManager), nameof(DungeonManager.LoadStageAndMove))]
    internal static class MultiplayerRulesExplorationStartPatch
    {
        internal static event Action StartingExploration;

        // Native sessions also load towns. isRunStarted is restored by LoadDungeon;
        // LoadStageAndMove starts a new exploration before generating its first stage.
        internal static bool ExplorationStarted =>
            DungeonManager.Instance != null && DungeonManager.Instance.isRunStarted;

        private static void Prefix(DungeonManager __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(DungeonManager __instance)
        {
            if (MultiplayerRulesLifecyclePolicy.ShouldBeginNewExploration(__instance.isServer, __instance.isRunStarted))
                StartingExploration?.Invoke();
        }
    }

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

        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerRules, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore() => MultiplayerRulesController.EndExploration();
    }
}
