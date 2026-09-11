using SephiriaEnhancements.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    internal static class NativeJoiningSupplyMiracle
    {
        private static readonly MethodInfo Generate = AccessTools.Method(typeof(MiracleSelector2), "GenerateMiracles");
        private static readonly MethodInfo Acquired = AccessTools.Method(typeof(MiracleSelector2), "UserCode_CmdHandleMiracleAcquired__MiracleController");

        internal static bool Grant(MiracleSelector2 selector, PlayerSpawner player, string id, int instanceId)
        {
            var controller = player.GetComponent<MiracleController>();
            var choices = (MiracleMetadata[])Generate.Invoke(selector,
                new object[] { player.netIdentity, controller.UnitAvatar.RandomID, controller.UnitAvatar });
            if (!Array.Exists(choices, choice => choice.id == id && choice.instanceID == instanceId)) return false;
            var miracle = MiracleDatabase.FindMiracle(id);
            var items = miracle.GetItems(false, controller, instanceId);
            if (!HasRoom(controller.UnitAvatar.Inventory, items)) return false;
            // Keep the native reward operations together on the authoritative thread.
            // The native panel otherwise sends removal, addition and items as separate commands.
            if (controller.miracles.Count > 0) controller.RemoveMiracle(0);
            controller.AddMiracle(id);
            if (items != null) controller.UnitAvatar.Inventory.AddItemsWithGenerateInstanceID(instanceId, items);
            Acquired.Invoke(selector, new object[] { controller });
            return true;
        }

        private static bool HasRoom(GridInventory inventory, ItemMetadata[] items)
        {
            int space = inventory.GetEmptySlotCount(EItemType.Charm);
            int potions = inventory.GetEnptyPotionStorageCount();
            if (items == null) return true;
            foreach (var metadata in items)
            {
                var item = ItemDatabase.FindItemById(metadata.entityID);
                if (item.type == EItemType.Potion && potions > 0) { potions--; continue; }
                if (item.type == EItemType.Charm)
                {
                    if (inventory.uniquePairCount > 0 && inventory.HasItem(item, out _, out _, out _)) continue;
                    if (inventory.uniquePairCount <= 0 && inventory.TryGetUniqueEffect(item, out _)) continue;
                }
                space--;
            }
            return space >= 0;
        }
    }

    [HarmonyPatch]
    internal static class JoiningSupplyMiracleConfirmationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            // These are the native confirmation callbacks, after its space check and Yes/No dialog.
            Type closure = AccessTools.Inner(typeof(UI_MiracleElement), "<>c__DisplayClass17_0") ??
                throw new MissingMemberException("Native miracle confirmation changed.");
            foreach (int index in new[] { 0, 2, 3, 4 })
                yield return AccessTools.Method(closure, "<HandleClick>b__" + index) ??
                    throw new MissingMethodException("Native miracle confirmation changed.");
        }

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
            var element = (UI_MiracleElement)AccessTools.Field(__instance.GetType(), "<>4__this").GetValue(__instance);
            var selector = (MiracleSelector2)AccessTools.Field(typeof(UI_MiracleElement), "miracleSelector").GetValue(element);
            if (selector == null || !JoiningSupplyBridge.IsCurrent(selector.netId))
                return true;
            var choice = (MiracleMetadata)AccessTools.Field(typeof(UI_MiracleElement), "entity").GetValue(element);
            var actor = (MiracleController)AccessTools.Field(typeof(UI_MiracleElement), "actor").GetValue(element);
            actor.CloseMiraclePanel();
            JoiningSupplyBridge.SelectMiracle(selector.netId, choice.id, choice.instanceID);
            return false;
        }
    }
}
