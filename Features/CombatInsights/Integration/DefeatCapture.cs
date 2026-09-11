using SephiriaEnhancements.Runtime;
using System;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch(typeof(UnitAvatar), "DieClientside")]
    internal static class UnitDeathCapture
    {
        private static CombatInsightsController controller;

        internal static void SetController(CombatInsightsController value) => controller = value;

        private static void Postfix(UnitAvatar __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatInsights, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UnitAvatar __instance) => controller?.RecordEnemyDeath(__instance);
    }

    [HarmonyPatch]
    internal static class LocalFinalBlowCapture
    {
        // This is the game's native network handler name. Keep it confined to
        // the integration boundary; the Mod domain event is LocalFinalBlow.
        private const string HandlerName =
            "UserCode_TargetKillUnit__NetworkConnectionToClient__UnitKillData";
        private static CombatInsightsController controller;

        internal static void SetController(CombatInsightsController value) => controller = value;

        private static MethodBase TargetMethod()
        {
            foreach (MethodInfo method in typeof(UnitAvatar).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == HandlerName && parameters.Length == 2 &&
                    parameters[1].ParameterType == typeof(UnitKillData)) return method;
            }
            throw new MissingMethodException(typeof(UnitAvatar).FullName, HandlerName);
        }

        private static void Postfix(UnitAvatar __instance, UnitKillData __1)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CombatInsights))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, __1);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CombatInsights, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UnitAvatar __instance, UnitKillData __1)
        {
            if (__instance is PlayerAvatar player && LocalPlayerResolver.IsLocal(player))
                controller?.RecordLocalFinalBlow(__1);
        }
    }
}
