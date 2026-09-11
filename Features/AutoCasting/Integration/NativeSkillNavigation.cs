using SephiriaEnhancements.Runtime;
using System.Collections.Generic;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal static class NativeSkillNavigation
    {
        internal static bool TrySwitchRegion()
        {
            Keyboard keyboard = Keyboard.current;
            var stack = UIManager.Instance?.CurrentControlStack;
            if (!EnhancementsSettings.Enabled || keyboard == null || !keyboard.tabKey.wasPressedThisFrame ||
                stack == null || stack.Count != 1 || !(stack[0] is UI_CharacterStatusPanel panel) ||
                !NativeAutoCastingUi.IsEditable(panel)) return false;
            var regions = new List<List<Selectable>>();
            var inventory = new List<Selectable>();
            foreach (UI_InventoryIconMergedData cell in panel.inventoryZone.GetComponentsInChildren<UI_InventoryIconMergedData>())
                Add(inventory, cell.icon.GetComponent<Selectable>());
            regions.Add(inventory);
            var potions = new List<Selectable>();
            foreach (UI_NewInventoryIcon potion in panel.potionIcons) Add(potions, potion.GetComponent<Selectable>());
            regions.Add(potions);
            regions.Add(Controls(panel.subBagZone));
            regions.Add(Controls(panel.setEffectZone));
            regions.Add(Controls(panel.skillIconZone));
            var weaponInput = new List<Selectable>();
            Add(weaponInput, panel.showWeaponToggle);
            regions.Add(weaponInput);
            var memory = panel.GetComponent<NativeSkillNavigationMemory>() ??
                panel.gameObject.AddComponent<NativeSkillNavigationMemory>();
            Selectable selected = EventSystem.current?.currentSelectedGameObject?.GetComponent<Selectable>();
            int current = -1;
            for (int i = 0; i < regions.Count; i++)
                if (regions[i].Contains(selected)) current = i;
            if (current >= 0) memory.Selected[current] = selected;
            int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
            int next = current < 0 ? (direction > 0 ? -1 : 0) : current;
            for (int step = 0; step < regions.Count; step++)
            {
                next = (next + direction + regions.Count) % regions.Count;
                if (regions[next].Count == 0) continue;
                Selectable target = regions[next].Contains(memory.Selected[next]) ?
                    memory.Selected[next] : regions[next][0];
                EventSystem.current.SetSelectedGameObject(target.gameObject);
                return true;
            }
            return false;
        }

        private static void Add(List<Selectable> entries, Selectable entry)
        {
            if (entry != null && entry.IsActive() && entry.IsInteractable()) entries.Add(entry);
        }
        private static List<Selectable> Controls(Transform group)
        {
            var controls = new List<Selectable>();
            if (group != null)
                foreach (Selectable entry in group.GetComponentsInChildren<Selectable>()) Add(controls, entry);
            return controls;
        }

        internal static bool Move(Selectable source, AxisEventData data)
        {
            UI_CharacterStatusPanel panel = source.GetComponentInParent<UI_CharacterStatusPanel>();
            if (!NativeAutoCastingUi.IsEditable(panel)) return true;
            bool skill = source.GetComponent<UI_PlayerSkillIcon>() != null;
            bool toggle = source == panel.showWeaponToggle;
            if (!skill && !toggle) return true;
            Selectable target = null;
            if (data.moveDir == MoveDirection.Up || data.moveDir == MoveDirection.Down)
            {
                Vector3 direction = data.moveDir == MoveDirection.Up ? Vector3.up : Vector3.down;
                target = Closest(source, panel.skillIconZone, direction);
                if (target == null && data.moveDir == MoveDirection.Down && skill) target = panel.showWeaponToggle;
                if (target == null && data.moveDir == MoveDirection.Up) target = Closest(source, panel.setEffectZone, Vector3.up);
            }
            else return true;
            if (target == null) return true;
            data.selectedObject = target.gameObject;
            data.Use();
            return false;
        }

        private static Selectable Closest(Selectable source, Transform group, Vector3 direction)
        {
            if (group == null) return null;
            Selectable best = null;
            float score = float.PositiveInfinity;
            foreach (Selectable candidate in group.GetComponentsInChildren<Selectable>())
            {
                if (candidate == source || !candidate.IsActive() || !candidate.IsInteractable()) continue;
                Vector3 delta = candidate.transform.position - source.transform.position;
                float forward = Vector3.Dot(delta, direction);
                if (forward <= 0.01f) continue;
                float cross = Vector3.Cross(delta, direction).magnitude;
                float distance = forward + cross * 3f;
                if (distance < score) { score = distance; best = candidate; }
            }
            return best;
        }
    }

    internal sealed class NativeSkillNavigationMemory : MonoBehaviour
    {
        internal readonly Selectable[] Selected = new Selectable[6];
        internal void OnDisable() => System.Array.Clear(Selected, 0, Selected.Length);
    }

    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    internal static class SkillNavigationMovePatch
    {
        private static bool Prefix(Selectable __instance, AxisEventData eventData)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.AutoCasting))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, eventData);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.AutoCasting, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(Selectable __instance, AxisEventData eventData) => NativeSkillNavigation.Move(__instance, eventData);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.OnShowWeaponToggle))]
    internal static class SkillNavigationTogglePatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance, bool isOn)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.AutoCasting))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, isOn);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.AutoCasting, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_CharacterStatusPanel __instance, bool isOn)
        {
            if (isOn || !NativeAutoCastingUi.IsEditable(__instance))
                return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            foreach (UI_PlayerSkillIcon icon in __instance.weaponIcons)
                if (icon != null && selected == icon.gameObject)
                    EventSystem.current.SetSelectedGameObject(__instance.showWeaponToggle.gameObject);
        }
    }
}
