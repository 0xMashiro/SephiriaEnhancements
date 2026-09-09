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
            var entries = new List<Selectable>();
            UI_NewInventoryIcon inventory = panel.GetComponentInChildren<UI_NewInventoryIcon>();
            if (inventory != null) Add(entries, inventory.GetComponent<Selectable>());
            Add(entries, First(panel.setEffectZone));
            Add(entries, First(panel.skillIconZone));
            Add(entries, panel.showWeaponToggle);
            if (entries.Count == 0) return false;
            Transform selected = EventSystem.current?.currentSelectedGameObject?.transform;
            int current = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                Transform entry = entries[i].transform;
                if (selected == entry || (selected != null &&
                    ((entry.IsChildOf(panel.setEffectZone) && selected.IsChildOf(panel.setEffectZone)) ||
                     (entry.IsChildOf(panel.skillIconZone) && selected.IsChildOf(panel.skillIconZone)) ||
                     (entry.GetComponent<UI_NewInventoryIcon>() != null && selected.GetComponent<UI_NewInventoryIcon>() != null))))
                    current = i;
            }
            int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
            EventSystem.current.SetSelectedGameObject(entries[(current + direction + entries.Count) % entries.Count].gameObject);
            return true;
        }

        private static void Add(List<Selectable> entries, Selectable entry)
        {
            if (entry != null && entry.IsActive() && entry.IsInteractable()) entries.Add(entry);
        }
        private static Selectable First(Transform group)
        {
            if (group == null) return null;
            foreach (Selectable entry in group.GetComponentsInChildren<Selectable>())
                if (entry.IsActive() && entry.IsInteractable()) return entry;
            return null;
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
            else if (data.moveDir == MoveDirection.Right)
            {
                target = Closest(source, panel.skillIconZone, Vector3.right);
                if (target == null)
                {
                    float best = float.PositiveInfinity;
                    foreach (UI_NewInventoryIcon icon in panel.GetComponentsInChildren<UI_NewInventoryIcon>())
                    {
                        Selectable candidate = icon.GetComponent<Selectable>();
                        if (candidate == null || !candidate.IsActive() || !candidate.IsInteractable()) continue;
                        float distance = (candidate.transform.position - source.transform.position).sqrMagnitude;
                        if (distance < best) { best = distance; target = candidate; }
                    }
                }
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

    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    internal static class SkillNavigationMovePatch
    {
        private static bool Prefix(Selectable __instance, AxisEventData eventData) =>
            NativeSkillNavigation.Move(__instance, eventData);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.OnShowWeaponToggle))]
    internal static class SkillNavigationTogglePatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance, bool isOn)
        {
            if (isOn || !NativeAutoCastingUi.IsEditable(__instance)) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            foreach (UI_PlayerSkillIcon icon in __instance.weaponIcons)
                if (icon != null && selected == icon.gameObject)
                    EventSystem.current.SetSelectedGameObject(__instance.showWeaponToggle.gameObject);
        }
    }
}
