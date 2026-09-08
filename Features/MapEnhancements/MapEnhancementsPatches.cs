using HarmonyLib;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements
{
    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.Show))]
    internal static class MapPanelShowPatch
    {
        private static void Prefix(UI_MapPanel __instance, string floorGuid) =>
            MapEnhancementsController.PrepareMapNavigation(__instance, floorGuid);

        private static void Postfix(UI_MapPanel __instance, string floorGuid)
        {
            MapEnhancementsController.ShowHiddenRooms(__instance, floorGuid);
            MapEnhancementsController.ShowMapNavigationMarkers(__instance, floorGuid);
            MapEnhancementsController.InitializeKeyboardRoomNavigation(__instance,
                floorGuid);
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), "Update")]
    internal static class MapNavigationUpdatePatch
    {
        private static void Prefix(UI_MapPanel __instance, out Vector2? __state)
        {
            // Native controller centering assumes an unscaled map and full viewport.
            __state = MapEnhancementsController.HasMapNavigation
                ? __instance.contentsParent.anchoredPosition : (Vector2?)null;
        }

        private static void Postfix(UI_MapPanel __instance, Vector2? __state)
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
            MapEnhancementsController.BeforeNativeMapOpened();
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.OnClosed))]
    internal static class MapPanelClosedPatch
    {
        private static void Postfix()
        {
            MapEnhancementsController.AfterNativeMapClosed();
        }
    }
}
