using HarmonyLib;

namespace SephiriaEnhancements.Inventory
{
    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.CloseFromEsc))]
    internal static class InventoryPanelCancelPatch
    {
        private static bool Prefix(UIBase __instance) =>
            !InventoryOptimizationController.TryCancelPanel(__instance);
    }

    [HarmonyPatch(typeof(UI_BaseTooltip), "SetPositionToTarget")]
    internal static class InventoryPanelTooltipPlacementPatch
    {
        private static void Postfix(UI_BaseTooltip __instance) =>
            InventoryOptimizationController.PositionTooltip(__instance);
    }
}
