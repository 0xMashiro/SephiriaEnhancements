using HarmonyLib;
using SephiriaEnhancements.Runtime;
using Mirror;

namespace SephiriaEnhancements.CostumeAppearance.Integration
{
    [HarmonyPatch(typeof(PlayerAvatar), "LocalEquipCostume")]
    internal static class CostumeAppearanceEquipPatch
    {
        private static bool Prefix(PlayerAvatar __instance, string costumeID, string skinID)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return true;
            try { return PrefixCore(__instance, costumeID, skinID); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); return false; }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(PlayerAvatar player, string costumeID, string skinID)
        {
            if (!NativeCostumeAppearance.Available || !NetworkServer.active) return true;
            var costume = CostumeDatabase.FindCostumeByID(costumeID);
            var skin = CostumeDatabase.GetCostumeSkinByID(skinID);
            if (costume == null || skin == null || skin.relatedCostumeID == costumeID) return true;
            // Native synchronized setters retain effect teardown and per-peer outfit hooks.
            // Unlock ownership lives on each player's local save, as in the native costume UI.
            player.NetworkcurrentCostume = costumeID;
            player.NetworkcurrentCostumeSkin = skinID;
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.EquipCostume))]
    internal static class CostumeAppearancePreferencePatch
    {
        private static void Prefix(PlayerAvatar __instance, ref string skinID)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { PrefixCore(__instance, ref skinID); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerAvatar player, ref string skin) => skin = NativeCostumeAppearance.OverrideSkin(player, skin);
    }

    [HarmonyPatch(typeof(UI_CostumePanel), nameof(UI_CostumePanel.OnOpened))]
    internal static class CostumeAppearancePanelPatch
    {
        private static void Postfix(UI_CostumePanel __instance, PlayerAvatar ___playerAvatar)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CostumeAppearance)) return;
            try { PostfixCore(__instance, ___playerAvatar); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.CostumeAppearance, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_CostumePanel panel, PlayerAvatar player)
        {
            if (!NativeCostumeAppearance.Available || panel.ignoreSave || !SephiriaEnhancements.Integration.LocalPlayerResolver.IsLocal(player)) return;
            var view = panel.GetComponent<NativeCostumeAppearanceView>() ?? panel.gameObject.AddComponent<NativeCostumeAppearanceView>();
            view.Bind(panel, player);
        }
    }
}
