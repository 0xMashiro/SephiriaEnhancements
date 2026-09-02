#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System.Collections.Generic;
using System.Threading;
using HarmonyLib;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class InventoryEvaluationOrderTraceSignal
    {
        private static readonly object Gate = new object();
        private static readonly List<InventoryItemKey> CategoryOrder = new List<InventoryItemKey>();
        private static readonly List<InventoryItemKey> ArtifactOrder = new List<InventoryItemKey>();
        private static readonly List<UniqueEffectRegistrationSnapshot> Unique =
            new List<UniqueEffectRegistrationSnapshot>();
        private static GridInventory inventory;
        private static long revision;

        internal static void Begin(GridInventory target)
        {
            lock (Gate)
            {
                inventory = target;
                CategoryOrder.Clear();
                ArtifactOrder.Clear();
                Unique.Clear();
                Interlocked.Increment(ref revision);
            }
        }

        internal static void RecordCategory(Charm_Basic artifact)
        {
            lock (Gate)
            {
                if (artifact?.Inventory == inventory && artifact?.Item != null)
                {
                    CategoryOrder.Add(new InventoryItemKey(artifact.Item.EntityID, artifact.Item.InstanceID));
                }
            }
        }

        internal static void RecordArtifact(Charm_Basic artifact)
        {
            lock (Gate)
            {
                if (artifact?.Inventory == inventory && artifact?.Item != null)
                {
                    ArtifactOrder.Add(new InventoryItemKey(artifact.Item.EntityID, artifact.Item.InstanceID));
                }
            }
        }

        internal static void RecordUnique(Charm_Basic artifact, bool accepted)
        {
            lock (Gate)
            {
                if (artifact?.Inventory == inventory && artifact?.Item != null)
                {
                    Unique.Add(new UniqueEffectRegistrationSnapshot(
                        artifact.Item.InstanceID,
                        artifact.Item.EntityID, accepted));
                }
            }
        }

        internal static bool TryGet(GridInventory target,
            out InventoryEvaluationOrderSnapshot snapshot)
        {
            lock (Gate)
            {
                if (target == null || target != inventory)
                {
                    snapshot = null;
                    return false;
                }
                snapshot = new InventoryEvaluationOrderSnapshot(revision,
                    CategoryOrder.ToArray(), ArtifactOrder.ToArray(),
                    Unique.ToArray());
                return true;
            }
        }

        internal static void Clear(GridInventory target = null)
        {
            lock (Gate)
            {
                if (target != null && target != inventory)
                {
                    return;
                }
                inventory = null;
                CategoryOrder.Clear();
                ArtifactOrder.Clear();
                Unique.Clear();
            }
        }
    }

    [HarmonyPatch(typeof(GridInventory), "GetPermission")]
    internal static class InventoryEvaluationOrderTraceStartPatch
    {
        private static void Postfix(GridInventory __instance)
        {
            InventoryEvaluationOrderTraceSignal.Begin(__instance);
        }
    }

    [HarmonyPatch(typeof(Charm_Basic), nameof(Charm_Basic.OnPreSetEffectRefreshed))]
    internal static class ArtifactCategoryRefreshOrderPatch
    {
        private static void Prefix(Charm_Basic __instance)
        {
            InventoryEvaluationOrderTraceSignal.RecordCategory(__instance);
        }
    }

    [HarmonyPatch(typeof(Charm_Basic), nameof(Charm_Basic.RefreshCharm))]
    internal static class ArtifactRefreshOrderPatch
    {
        private static void Prefix(Charm_Basic __instance)
        {
            InventoryEvaluationOrderTraceSignal.RecordArtifact(__instance);
        }
    }

    [HarmonyPatch(typeof(GridInventory), nameof(GridInventory.RegisterUniqueEffect))]
    internal static class UniqueEffectRegistrationTracePatch
    {
        private static void Postfix(Charm_Basic charmInstance, bool __result)
        {
            InventoryEvaluationOrderTraceSignal.RecordUnique(charmInstance,
                __result);
        }
    }
}
