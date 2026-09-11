using SephiriaEnhancements.Runtime;
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
        private static bool PrefixCore(UI_NewInventoryIcon __instance)
        {
            return controller?.TryHandleArtifactIntentClick(__instance) != true;
        }

        internal static void PrepareInput(UI_CharacterStatusPanel panel) =>
            controller?.PrepareArtifactPickupInput(panel);

        internal static void EndPickup(UI_CharacterStatusPanel panel) =>
            controller?.EndArtifactPickupForPanel(panel);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), "Update")]
    internal static class InventoryArtifactIntentInputPatch
    {
        // Repair focus before native throw/rotate actions inspect it. A held
        // goal reference must never turn those actions into real item changes.
        private static void Prefix(UI_CharacterStatusPanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        // Repair focus before native throw/rotate actions inspect it. A held
        // goal reference must never turn those actions into real item changes.
        private static void PrefixCore(UI_CharacterStatusPanel __instance) => InventoryArtifactIntentClickPatch.PrepareInput(__instance);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.OnClosed))]
    internal static class InventoryArtifactIntentClosedPatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_CharacterStatusPanel __instance) => InventoryArtifactIntentClickPatch.EndPickup(__instance);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.SetInventoryMode))]
    internal static class InventoryArtifactIntentModePatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.Inventory))
            {
                return;
            }

            try
            {
                PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.Inventory, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_CharacterStatusPanel __instance) => InventoryArtifactIntentClickPatch.EndPickup(__instance);
    }
}
