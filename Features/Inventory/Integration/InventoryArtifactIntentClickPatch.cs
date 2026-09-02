#nullable disable
using HarmonyLib;

namespace SephiriaEnhancements.Inventory
{
    [HarmonyPatch(typeof(UI_NewInventoryIcon),
        nameof(UI_NewInventoryIcon.OnButtonClick))]
    internal static class InventoryArtifactIntentClickPatch
    {
        private static InventoryOptimizationController controller;

        internal static void SetController(
            InventoryOptimizationController value)
        {
            controller = value;
        }

        private static bool Prefix(UI_NewInventoryIcon __instance)
        {
            return controller?.TryHandleArtifactIntentClick(__instance) != true;
        }
    }
}
