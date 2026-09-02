#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryOptimizationViewTemplates
    {
        internal GameObject LauncherButton;
        internal Image WindowBackground;
        internal Button ContentButton;
        internal Sprite PreferencesIcon;
        internal UI_SubBagIcon Slot;
        internal Canvas DragCanvas;
    }

    internal static class NativeInventoryOptimizationControls
    {
        internal static Button AddButton(GameObject owner, Button template)
        {
            // Keep native pointer selection, submit and directional navigation.
            UI_HorayButton button = owner.AddComponent<UI_HorayButton>();
            if (template != null)
            {
                button.transition = template.transition;
                button.colors = template.colors;
                button.spriteState = template.spriteState;
                button.animationTriggers = template.animationTriggers;
            }
            if (template is UI_HorayButton native)
            {
                button.disabledColor = native.disabledColor;
                button.useAABBNav = native.useAABBNav;
            }
            return button;
        }

        internal static void SetLabel(Button button, TextMeshProUGUI label)
        {
            ((UI_HorayButton)button).text = label;
        }
    }

    internal static class NativeInventoryOptimizationViewTemplateResolver
    {
        // Native scene names are intentionally confined to this integration
        // boundary. They identify the game's serialized Character/Stats UI;
        // optimizer presentation code consumes only the resolved templates.
        private const string StatisticsPanelName = "StatsPanel";
        private const string StatisticsLauncherName =
            "DealStatisticsButton";
        private const string DamageStatisticsPanelName = "DamageStatUI";
        private const string DamageStatisticsTabButtonName = "CurrentButton";
        private const string PreferencesSpriteName = "SettingButton";

        internal static bool TryResolve(UI_CharacterStatusPanel panel,
            out NativeInventoryOptimizationViewTemplates templates)
        {
            templates = null;
            Transform sceneRoot = panel?.transform?.root;
            if (sceneRoot == null)
            {
                return false;
            }

            Transform[] transforms =
                sceneRoot.GetComponentsInChildren<Transform>(true);
            Transform launcher = null;
            Transform damageStatisticsPanel = null;
            foreach (Transform candidate in transforms)
            {
                if (candidate == null)
                {
                    continue;
                }
                if (candidate.name == StatisticsLauncherName &&
                    candidate.parent?.name == StatisticsPanelName)
                {
                    launcher = candidate;
                }
                else if (candidate.name == DamageStatisticsPanelName)
                {
                    damageStatisticsPanel = candidate;
                }
            }

            if (launcher?.GetComponent<Button>() == null)
            {
                return false;
            }

            Transform contentButton = FindDescendant(damageStatisticsPanel,
                DamageStatisticsTabButtonName);
            Image window = panel.subBagZone?.GetComponent<Image>();
            UI_SubBagIcon slot = panel.subBagIconPrefab;
            Canvas dragCanvas = UIManager.Instance?
                .GetElement<UI_NewItemPicker>()?.parentCanvas;
            if (window?.sprite == null ||
                contentButton?.GetComponent<Button>() == null ||
                slot?.defaultBGSprite == null || dragCanvas == null)
            {
                return false;
            }
            templates = new NativeInventoryOptimizationViewTemplates
            {
                LauncherButton = launcher.gameObject,
                WindowBackground = window,
                ContentButton = contentButton?.GetComponent<Button>(),
                PreferencesIcon = FindLoadedSprite(PreferencesSpriteName),
                Slot = slot,
                DragCanvas = dragCanvas
            };
            return true;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }
            Transform[] descendants =
                root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant != null && descendant.name == name)
                {
                    return descendant;
                }
            }
            return null;
        }

        private static Sprite FindLoadedSprite(string name)
        {
            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (Sprite sprite in sprites)
            {
                if (sprite != null && sprite.name == name)
                {
                    return sprite;
                }
            }
            return null;
        }
    }
}
