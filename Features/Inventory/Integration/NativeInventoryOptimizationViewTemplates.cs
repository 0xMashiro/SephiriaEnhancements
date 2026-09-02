#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryOptimizationViewTemplates
    {
        internal GameObject LauncherButton;
        internal Image WindowBackground;
        internal Button ContentButton;
        internal Sprite PreferencesIcon;
        internal UI_NewInventoryIcon InventoryIcon;
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
        private const string DamageStatisticsWindowName = "Deal";
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

            Transform window = FindDescendant(damageStatisticsPanel,
                DamageStatisticsWindowName);
            Transform contentButton = FindDescendant(damageStatisticsPanel,
                DamageStatisticsTabButtonName);
            UI_NewInventoryIcon[] inventoryIcons =
                panel.GetComponentsInChildren<UI_NewInventoryIcon>(true);
            templates = new NativeInventoryOptimizationViewTemplates
            {
                LauncherButton = launcher.gameObject,
                WindowBackground = window?.GetComponent<Image>(),
                ContentButton = contentButton?.GetComponent<Button>(),
                PreferencesIcon = FindLoadedSprite(PreferencesSpriteName),
                InventoryIcon = inventoryIcons.FirstOrDefault(icon =>
                    icon?.Item == null && icon.bgImage?.sprite != null) ??
                    inventoryIcons.FirstOrDefault()
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
