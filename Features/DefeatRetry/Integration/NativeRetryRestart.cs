using SephiriaEnhancements.Runtime;
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    // Cancel only the native restart coroutine owned by this retry, never all
    // coroutines on the network manager. Failure, cancellation or a newer retry
    // must stop late continuations after the old placement/save guards are gone.
    [HarmonyPatch]
    internal static class NativeRetryRestart
    {
        private static readonly FieldInfo Restarting = AccessTools.Field(typeof(HorayNetworkManager), "restarting");
        private static MethodBase TargetMethod() => AccessTools.Method(typeof(HorayNetworkManager), "RestartGameCoroutine");
        internal static bool IsAvailable => Restarting != null && TargetMethod() != null;

        private static void Postfix(HorayNetworkManager __instance, ref IEnumerator __result)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            var original___result = __result;
            try
            {
                PostfixCore(__instance, ref __result);
            }
            catch (System.Exception exception)
            {
                __result = original___result;
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(HorayNetworkManager __instance, ref IEnumerator __result)
        {
            if (DefeatRetryFeature.IsRetrying)
                __result = Observe(__instance, __result, DefeatRetryBridge.CurrentRecoveryId);
        }

        private static IEnumerator Observe(HorayNetworkManager manager, IEnumerator restart, long id)
        {
            try
            {
                while (DefeatRetryBridge.CanContinueRestart(id))
                {
                    bool next;
                    try { next = restart.MoveNext(); }
                    catch
                    {
                        if (DefeatRetryBridge.CanContinueRestart(id)) DefeatRetryBridge.FailRecovery();
                        throw;
                    }
                    if (!next) break;
                    yield return restart.Current;
                }
            }
            finally
            {
                (restart as IDisposable)?.Dispose();
                if (!DefeatRetryBridge.CanContinueRestart(id) &&
                    id == DefeatRetryBridge.CurrentRecoveryId && manager != null)
                    Restarting.SetValue(manager, false);
            }
        }
    }
}
