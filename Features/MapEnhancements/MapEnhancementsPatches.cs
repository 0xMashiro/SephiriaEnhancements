using HarmonyLib;
using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.MapEnhancements
{
    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.Show))]
    internal static class MapPanelShowPatch
    {
        private static void Prefix(UI_MapPanel __instance, string floorGuid) =>
            MapEnhancementsController.PrepareFixedFloorMap(__instance, floorGuid);

        private static void Postfix(UI_MapPanel __instance, string floorGuid)
        {
            MapEnhancementsController.ShowHiddenRooms(__instance, floorGuid);
            MapEnhancementsController.ShowFixedFloorMapMarkers(__instance, floorGuid);
            MapEnhancementsController.InitializeKeyboardRoomNavigation(__instance,
                floorGuid);
        }
    }

    [HarmonyPatch(typeof(UI_MapPanel), "Update")]
    internal static class FixedFloorMapUpdatePatch
    {
        private static void Postfix() => MapEnhancementsController.RefreshFixedFloorMap();
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
