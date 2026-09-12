using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SephiriaEnhancements.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    // Native character-panel boundary. Hierarchy containment is not region ownership.
    internal static class CharacterPanelNavigation
    {
        private static readonly AccessTools.FieldRef<UI_CharacterStatusPanel, List<UI_SubBagIcon>> SubBagIcons =
            AccessTools.FieldRefAccess<UI_CharacterStatusPanel, List<UI_SubBagIcon>>("subBagIcons");
        private static readonly AccessTools.FieldRef<UI_CharacterStatusPanel, List<UI_PlayerSkillIcon>> SkillIcons =
            AccessTools.FieldRefAccess<UI_CharacterStatusPanel, List<UI_PlayerSkillIcon>>("skillIcons");
        private static readonly AccessTools.FieldRef<UI_CharacterStatusPanel, Dictionary<string, UI_SetEffectElement>> SetEffects =
            AccessTools.FieldRefAccess<UI_CharacterStatusPanel, Dictionary<string, UI_SetEffectElement>>("setEffectElements");

        internal static bool IsReady(UI_CharacterStatusPanel panel) =>
            Configuration.EnhancementsSettings.Enabled && KeyboardUiSelection.IsPanelReady(panel) &&
            panel.PlayerAvatar != null && panel.PlayerAvatar.Inventory != null &&
            UIManager.Instance != null && UIManager.Instance.CurrentControlStack != null &&
            UIManager.Instance.CurrentControlStack.Contains(panel);

        internal static void SwitchRegion(UI_CharacterStatusPanel panel, bool reverse)
        {
            if (!IsReady(panel) || EventSystem.current == null) return;
            List<List<Selectable>> regions = ReadRegions(panel);
            var memory = panel.GetComponent<CharacterPanelNavigationMemory>();
            if (memory == null) memory = panel.gameObject.AddComponent<CharacterPanelNavigationMemory>();
            memory.Bind(panel.PlayerAvatar);
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            Selectable selected = selectedObject != null ? selectedObject.GetComponent<Selectable>() : null;
            int current = regions.FindIndex(region => region.Contains(selected));
            if (current >= 0) memory.Selected[current] = selected;
            int direction = reverse ? -1 : 1;
            int next = current < 0 ? (reverse ? 0 : -1) : current;
            for (int step = 0; step < regions.Count; step++)
            {
                next = (next + direction + regions.Count) % regions.Count;
                if (regions[next].Count == 0) continue;
                Selectable remembered = memory.Selected[next];
                Selectable target = remembered != null && regions[next].Contains(remembered)
                    ? remembered : regions[next][0];
                EventSystem.current.SetSelectedGameObject(target.gameObject);
                return;
            }
        }

        internal static List<List<Selectable>> ReadRegions(UI_CharacterStatusPanel panel)
        {
            var inventory = new List<Selectable>();
            GridInventory playerInventory = panel.PlayerAvatar.Inventory;
            for (int index = 0; index < playerInventory.CurrentInventoryStorage; index++)
            {
                UI_NewInventoryIcon icon = panel.GetItemIcon(playerInventory.IdxToPos(index));
                if (icon != null) Add(inventory, icon.button);
            }
            var potions = new List<Selectable>();
            foreach (UI_NewInventoryIcon icon in panel.potionIcons)
                if (icon != null) Add(potions, icon.button);
            var subBag = new List<Selectable>();
            foreach (UI_SubBagIcon icon in SubBagIcons(panel))
                if (icon != null) Add(subBag, icon.GetComponent<Selectable>());
            var weaponInput = new List<Selectable>();
            Add(weaponInput, panel.showWeaponToggle);
            return new List<List<Selectable>> { inventory, potions, subBag,
                ReadComboEffects(panel), ReadSkills(panel), weaponInput };
        }

        private static List<Selectable> ReadComboEffects(UI_CharacterStatusPanel panel)
        {
            var effects = new List<Selectable>();
            foreach (UI_SetEffectElement effect in SetEffects(panel).Values.Where(effect => effect != null)
                .OrderBy(effect => effect.transform.GetSiblingIndex()))
                Add(effects, effect.GetComponent<Selectable>());
            return effects;
        }

        private static List<Selectable> ReadSkills(UI_CharacterStatusPanel panel)
        {
            var skills = new List<Selectable>();
            foreach (UI_PlayerSkillIcon icon in SkillIcons(panel).Concat(panel.weaponIcons).Where(icon => icon != null)
                .OrderBy(icon => icon.transform.GetSiblingIndex()))
                Add(skills, icon.GetComponent<Selectable>());
            return skills;
        }

        private static void Add(List<Selectable> entries, Selectable entry)
        {
            if (entry != null && KeyboardUiSelection.IsNavigable(entry.gameObject)) entries.Add(entry);
        }

        internal static bool Move(Selectable source, AxisEventData data)
        {
            UI_CharacterStatusPanel panel = source.GetComponentInParent<UI_CharacterStatusPanel>();
            if (!IsReady(panel)) return true;
            bool skill = source.GetComponent<UI_PlayerSkillIcon>() != null;
            bool toggle = source == panel.showWeaponToggle;
            if (!skill && !toggle) return true;
            if (data.moveDir != MoveDirection.Up && data.moveDir != MoveDirection.Down) return true;
            Vector3 direction = data.moveDir == MoveDirection.Up ? Vector3.up : Vector3.down;
            Selectable target = Closest(source, ReadSkills(panel), direction);
            if (target == null && data.moveDir == MoveDirection.Down && skill &&
                panel.showWeaponToggle != null && KeyboardUiSelection.IsNavigable(panel.showWeaponToggle.gameObject))
                target = panel.showWeaponToggle;
            if (target == null && data.moveDir == MoveDirection.Up)
                target = Closest(source, ReadComboEffects(panel), Vector3.up);
            if (target == null) return true;
            data.selectedObject = target.gameObject;
            data.Use();
            return false;
        }

        private static Selectable Closest(Selectable source, List<Selectable> candidates, Vector3 direction)
        {
            Selectable best = null;
            float score = float.PositiveInfinity;
            foreach (Selectable candidate in candidates)
            {
                if (candidate == source) continue;
                Vector3 delta = candidate.transform.position - source.transform.position;
                float forward = Vector3.Dot(delta, direction);
                if (forward <= 0.01f) continue;
                float distance = forward + Vector3.Cross(delta, direction).magnitude * 3f;
                if (distance < score) { score = distance; best = candidate; }
            }
            return best;
        }
    }

    internal sealed class CharacterPanelNavigationMemory : MonoBehaviour
    {
        internal readonly Selectable[] Selected = new Selectable[6];
        private PlayerAvatar player;

        internal void Bind(PlayerAvatar owner)
        {
            if (player == owner) return;
            OnDisable();
            player = owner;
        }

        internal void OnDisable()
        {
            System.Array.Clear(Selected, 0, Selected.Length);
            player = null;
        }

        internal static void ResetAll(bool destroy = false)
        {
            foreach (var memory in Object.FindObjectsByType<CharacterPanelNavigationMemory>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                memory.OnDisable();
                if (destroy) Object.Destroy(memory);
            }
        }
    }

    [HarmonyPatch(typeof(Selectable), nameof(Selectable.OnMove))]
    internal static class CharacterPanelNavigationMovePatch
    {
        private static bool Prefix(Selectable __instance, AxisEventData eventData)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CharacterPanelNavigation))
            {
                return true;
            }

            try
            {
                return PrefixCore(__instance, eventData);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CharacterPanelNavigation, exception);
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static bool PrefixCore(Selectable __instance, AxisEventData eventData) => CharacterPanelNavigation.Move(__instance, eventData);
    }

    [HarmonyPatch(typeof(UI_CharacterStatusPanel), nameof(UI_CharacterStatusPanel.OnShowWeaponToggle))]
    internal static class CharacterPanelNavigationTogglePatch
    {
        private static void Prefix(UI_CharacterStatusPanel __instance, bool isOn)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.CharacterPanelNavigation))
            {
                return;
            }

            try
            {
                PrefixCore(__instance, isOn);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.CharacterPanelNavigation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(UI_CharacterStatusPanel __instance, bool isOn)
        {
            if (isOn || !CharacterPanelNavigation.IsReady(__instance))
                return;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach (UI_PlayerSkillIcon icon in __instance.weaponIcons)
                if (icon != null && selected == icon.gameObject)
                    EventSystem.current.SetSelectedGameObject(__instance.showWeaponToggle.gameObject);
        }
    }
}
