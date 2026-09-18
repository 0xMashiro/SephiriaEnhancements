using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SephiriaEnhancements.DefeatRetry
{
    // Slot numbers, native skill types and item IDs are confined to this boundary.
    // Current native active skills are Charm_Active items; the combo API returns none.
    internal sealed class NativeRetrySkillLayout
    {
        private struct Slot
        {
            internal QuickSlotType Type;
            internal int Artifact, Weapon;
        }

        private readonly Slot[] slots = new Slot[11];
        private readonly Dictionary<int, bool> automaticBindings = new Dictionary<int, bool>();

        internal NativeRetrySkillLayout(IntegratedActionController actions, GridInventory inventory)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                QuickSlotData slot = At(actions, i);
                Charm_Basic artifact = slot.magic != null ? (Charm_Basic)slot.magic : slot.active as Charm_Active;
                slots[i] = new Slot { Type = slot.Type, Weapon = slot.weaponButton,
                    Artifact = artifact?.Item != null ? artifact.Item.InstanceID : -1 };
            }
            foreach (Charm_Basic artifact in inventory.charms.Values)
            {
                if (artifact is Charm_Magic magic) automaticBindings[artifact.Item.InstanceID] = magic.autoBindQuickSlotClientside;
                else if (artifact is Charm_Active active) automaticBindings[artifact.Item.InstanceID] = active.autoBindQuickSlotClientside;
            }
        }

        internal static bool IsReady(GridInventory inventory, int[] expected)
        {
            var present = new HashSet<int>();
            foreach (Charm_Basic artifact in inventory.charms.Values)
                if ((artifact is Charm_Magic || artifact is Charm_Active) && artifact.Item != null)
                    present.Add(artifact.Item.InstanceID);
            foreach (int id in expected) if (!present.Contains(id)) return false;
            return true;
        }

        internal int Restore(IntegratedActionController actions, GridInventory inventory, int[] expected)
        {
            // Refresh the native lists before editing. A second refresh publishes the
            // finished layout through the native events without toggle-style setters.
            var refresh = AccessTools.Method(typeof(IntegratedActionController), "RefreshQuickSlot");
            if (refresh == null) throw new MissingMethodException("Quick slot refresh is unavailable.");
            refresh.Invoke(actions, null);
            var available = new Dictionary<int, Charm_Basic>();
            var expectedIds = new HashSet<int>(expected);
            foreach (Charm_Basic artifact in inventory.charms.Values)
                if ((artifact is Charm_Magic || artifact is Charm_Active) && artifact.Item != null)
                    available[artifact.Item.InstanceID] = artifact;

            int missing = 0;
            var assigned = new HashSet<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                Slot saved = slots[i];
                QuickSlotData target = At(actions, i);
                target.Clear();
                if (saved.Type == QuickSlotType.Magic || saved.Type == QuickSlotType.Active)
                {
                    // Removed checkpoint items are not a failure and never match by name.
                    if (!expectedIds.Contains(saved.Artifact)) continue;
                    if (available.TryGetValue(saved.Artifact, out Charm_Basic artifact) &&
                        ((saved.Type == QuickSlotType.Magic && artifact is Charm_Magic) ||
                         (saved.Type == QuickSlotType.Active && artifact is Charm_Active)))
                    {
                        Assign(target, artifact);
                        assigned.Add(saved.Artifact);
                    }
                    else missing++;
                }
                else if (saved.Type != QuickSlotType.Empty) target.SetWeapon(saved.Weapon);
            }

            foreach (var pair in available)
            {
                bool autoBind = !automaticBindings.TryGetValue(pair.Key, out bool saved) || saved;
                if (!assigned.Contains(pair.Key) && autoBind)
                {
                    // Only vacated skill slots are available. Preserve empty slots and
                    // the weapon buttons, including intentional unbinding.
                    for (int i = 0; i < 8; i++)
                        if (slots[i].Type != QuickSlotType.Empty && At(actions, i).IsEmpty)
                        {
                            Assign(At(actions, i), pair.Value);
                            assigned.Add(pair.Key);
                            break;
                        }
                }
                bool keepAutomaticBinding = assigned.Contains(pair.Key) && autoBind;
                if (pair.Value is Charm_Magic magic) magic.autoBindQuickSlotClientside = keepAutomaticBinding;
                else ((Charm_Active)pair.Value).autoBindQuickSlotClientside = keepAutomaticBinding;
            }
            refresh.Invoke(actions, null);
            return missing;
        }

        private static void Assign(QuickSlotData target, Charm_Basic artifact)
        {
            if (artifact is Charm_Magic magic) target.SetMagic(magic);
            else target.SetActive((Charm_Active)artifact);
        }

        private static QuickSlotData At(IntegratedActionController actions, int index) =>
            index < 8 ? actions.quickSlots[index] : actions.quickSlotsWeapon[index - 8];
    }
}
