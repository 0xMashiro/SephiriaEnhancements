using SephiriaEnhancements.Runtime;
using HarmonyLib;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements
{
    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.Show))]
    internal static class MapPanelShowPatch
    {
        private static void Prefix(UI_MapPanel __instance, string floorGuid)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, floorGuid);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_MapPanel __instance, string floorGuid) => MapEnhancementsController.PrepareMapNavigation(__instance, floorGuid);

        private static void Postfix(UI_MapPanel __instance, string floorGuid)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements))
            {
                return;
            }

            try
            {
                PostfixCore(__instance, floorGuid);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_MapPanel __instance, string floorGuid)
        {
            MapEnhancementsController.ShowHiddenRooms(__instance, floorGuid);
            MapEnhancementsController.ShowMapNavigationMarkers(__instance, floorGuid);
            MapEnhancementsController.InitializeKeyboardRoomNavigation(__instance, floorGuid);
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), "Update")]
    internal static class MapNavigationUpdatePatch
    {
        private static void Prefix(UI_MapPanel __instance, out Vector2? __state)
        {
            __state = default;
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, out __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_MapPanel __instance, out Vector2? __state)
        {
            // Native controller centering assumes an unscaled map and full viewport.
            __state = MapEnhancementsController.HasMapNavigation ? __instance.contentsParent.anchoredPosition : (Vector2? )null;
        }

        private static void Postfix(UI_MapPanel __instance, Vector2? __state)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements)) return;
            try
            {
                PostfixCore(__instance, __state);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_MapPanel __instance, Vector2? __state)
        {
            if (__state.HasValue && MapEnhancementsController.HasMapNavigation)
                __instance.contentsParent.anchoredPosition = __state.Value;
            MapEnhancementsController.RefreshMapNavigation();
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.OnOpened))]
    internal static class MapPanelOpenedPatch
    {
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements))
            {
                return;
            }

            try
            {
                PrefixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore()
        {
            MapEnhancementsController.BeforeNativeMapOpened();
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.OnClosed))]
    internal static class MapPanelClosedPatch
    {
        private static void Postfix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MapEnhancements))
            {
                return;
            }

            try
            {
                PostfixCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MapEnhancements, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore()
        {
            MapEnhancementsController.AfterNativeMapClosed();
        }
    }
}
