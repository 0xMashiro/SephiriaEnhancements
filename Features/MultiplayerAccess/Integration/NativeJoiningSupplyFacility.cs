using SephiriaEnhancements.Runtime;
using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class NativeJoiningSupplyFacility
    {
        internal static bool Enhance(Anvil anvil, PlayerSpawner player, int weapon, string selection)
        {
            if (string.IsNullOrEmpty(selection)) return false;
            var choices = UnityEngine.JsonUtility.FromJson<NativeJoiningSupplyAnvil>(selection);
            if (!choices.Initialized || !choices.Choices.Contains(weapon)) return false;
            var controller = player.GetComponent<WeaponControllerSimple>();
            if (controller.currentWeapon == null || !(WeaponDatabase.GetWeaponEnhancements(controller.currentWeapon.entityId)?
                .Any(item => item != null && item.enhanced != null && item.enhanced.id == weapon) ?? false)) return false;
            controller.EquipWeapon(false, weapon);
            AccessTools.Method(typeof(Anvil), "UserCode_CmdMarkEnhanced__NetworkConnectionToClient")
                .Invoke(anvil, new object[] { player.connectionToClient });
            return true;
        }

        internal static bool Enchant(AltarOfEnchant altar, PlayerSpawner player, ItemPosition position, int instanceId)
        {
            if (NativeJoiningSupplyRecipes.Acquired(altar.gameObject, player)) return false;
            var inventory = player.GetComponent<GridInventory>();
            var item = inventory.FindItem(position);
            if (item == null || item.InstanceID != instanceId || item.Charm == null || item.Charm.maxLevel <= 0) return false;
            int.TryParse(DungeonManager.Instance.GetGlobalItemStatValue(instanceId, "Enchant"), out int level);
            if (level >= item.Charm.maxLevel) return false;
            inventory.Enchant(position);
            AccessTools.Method(typeof(AltarOfEnchant), "UserCode_CmdUse__NetworkConnectionToClient")
                .Invoke(altar, new object[] { player.connectionToClient });
            return true;
        }
    }

    [HarmonyPatch]
    internal static class JoiningSupplyAnvilConfirmationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.Inner(typeof(UI_WeaponEnhancementPanel), "<>c__DisplayClass34_0"), "<Enhance>b__0") ??
            throw new MissingMethodException("Native weapon enhancement confirmation changed.");

        private static bool Prefix(object __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(object __instance)
        {
            var panel = (UI_WeaponEnhancementPanel)AccessTools.Field(__instance.GetType(), "<>4__this").GetValue(__instance);
            var anvil = (Anvil)AccessTools.Field(typeof(UI_WeaponEnhancementPanel), "anvil").GetValue(panel);
            if (anvil == null || !JoiningSupplyBridge.IsCurrent(anvil.netId))
                return true;
            var weapon = (WeaponEntity)AccessTools.Field(__instance.GetType(), "weapon").GetValue(__instance);
            panel.Close();
            JoiningSupplyBridge.SelectFacility(anvil.netId, weapon.id, default, 0);
            return false;
        }
    }

    [HarmonyPatch]
    internal static class JoiningSupplyEnchantConfirmationPatch
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.Inner(typeof(UI_CharacterStatusPanel), "<>c__DisplayClass175_0"), "<OnItemClicked>b__0") ??
            throw new MissingMethodException("Native enchanting confirmation changed.");

        private static bool Prefix(object __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(object __instance)
        {
            var altar = (AltarOfEnchant)AccessTools.Field(__instance.GetType(), "savedEnchantBinding").GetValue(__instance);
            if (altar == null || !JoiningSupplyBridge.IsCurrent(altar.netId))
                return true;
            var icon = (UI_NewInventoryIcon)AccessTools.Field(__instance.GetType(), "icon").GetValue(__instance);
            var panel = (UI_CharacterStatusPanel)AccessTools.Field(__instance.GetType(), "<>4__this").GetValue(__instance);
            if (icon?.Item == null)
                return false;
            JoiningSupplyBridge.SelectFacility(altar.netId, 0, icon.Item.Position, icon.Item.InstanceID);
            panel.SetInventoryMode(UI_CharacterStatusPanel.EInventoryMode.None);
            panel.EnchantBinding = null;
            panel.Close();
            return false;
        }
    }
}
