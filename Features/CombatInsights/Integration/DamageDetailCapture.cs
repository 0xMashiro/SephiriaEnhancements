using System;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Combat;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch]
    internal static class DamageDetailCapture
    {
        private const string RpcHandlerPrefix = "UserCode_RpcApplyDamage__DamageData";
        private static CombatInsightsController controller;

        internal static void SetController(CombatInsightsController value) => controller = value;

        private static MethodBase TargetMethod()
        {
            foreach (MethodInfo method in typeof(UnitAvatar).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name.StartsWith(RpcHandlerPrefix, StringComparison.Ordinal) &&
                    parameters.Length == 1 && parameters[0].ParameterType == typeof(DamageData))
                {
                    return method;
                }
            }

            throw new MissingMethodException(typeof(UnitAvatar).FullName, RpcHandlerPrefix);
        }

        private static void Postfix(UnitAvatar __instance, DamageData __0)
        {
            controller?.RecordDamageDetail(__instance, __0);
        }
    }
}
