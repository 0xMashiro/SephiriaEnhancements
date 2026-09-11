using SephiriaEnhancements.Runtime;
using HarmonyLib;

namespace SephiriaEnhancements.Inventory
{
    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.CloseFromEsc))]
    internal static class InventoryPanelCancelPatch
    {
        private static bool Prefix(UIBase __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(UIBase __instance) => !InventoryOptimizationController.TryCancelPanel(__instance);
    }

    [HarmonyPatch(typeof(UI_BaseTooltip), "SetPositionToTarget")]
    internal static class InventoryPanelTooltipPlacementPatch
    {
        private static void Postfix(UI_BaseTooltip __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_BaseTooltip __instance) => InventoryOptimizationController.PositionTooltip(__instance);
    }
}
