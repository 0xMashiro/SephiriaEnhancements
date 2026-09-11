using SephiriaEnhancements.Runtime;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using UnityEngine;

namespace SephiriaEnhancements.ViewDistance
{
    [HarmonyPatch(typeof(TargetTracker), "LateUpdate")]
    internal static class ViewDistancePatch
    {
        private static void Postfix(TargetTracker __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ViewDistance))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ViewDistance, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(TargetTracker __instance)
        {
            if (!EnhancementsSettings.Enabled || __instance == null || GameCamera.Instance?.targetTracker != __instance)
            {
                return;
            }

            float multiplier = ViewDistanceSettings.Multiplier;
            if (Mathf.Approximately(multiplier, 1f))
            {
                return;
            }

            Camera camera = GameCamera.Instance.Camera;
            if (camera == null || !camera.orthographic)
            {
                return;
            }

            camera.orthographicSize = Mathf.Clamp(camera.orthographicSize * multiplier, 1f, 40f);
        }
    }
}
