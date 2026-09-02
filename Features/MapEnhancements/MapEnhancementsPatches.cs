using HarmonyLib;
using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.MapEnhancements
{
    [HarmonyPatch(typeof(UI_MapPanel), nameof(UI_MapPanel.Show))]
    internal static class MapPanelShowPatch
    {
        private static void Postfix(UI_MapPanel __instance, string floorGuid)
        {
            MapEnhancementsController.ShowHiddenRooms(__instance, floorGuid);
            MapEnhancementsController.ShowTownNpcMapMarkers(__instance, floorGuid);
            MapEnhancementsController.InitializeKeyboardRoomNavigation(__instance,
                floorGuid);
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
