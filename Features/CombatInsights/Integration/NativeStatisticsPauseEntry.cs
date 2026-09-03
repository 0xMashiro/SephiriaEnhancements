using HarmonyLib;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.Integration
{
    [HarmonyPatch(typeof(UI_PausePanel), nameof(UI_PausePanel.OnOpened))]
    internal static class NativeStatisticsPauseEntry
    {
        private static CombatInsightsController controller;

        internal static void SetController(CombatInsightsController value)
        {
            controller = value;
            if (value != null) return;
            foreach (var entry in Object.FindObjectsByType<StatisticsPauseButton>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.Destroy(entry);
        }

        private static void Postfix(UI_PausePanel __instance)
        {
            var entry = __instance.GetComponent<StatisticsPauseButton>();
            if (entry == null) entry = __instance.gameObject.AddComponent<StatisticsPauseButton>();
            entry.Refresh(__instance, controller);
        }
    }

    internal sealed class StatisticsPauseButton : MonoBehaviour
    {
        private UI_HorayButton button;
        private UI_PausePanel panel;
        private CombatInsightsController controller;

        internal void Refresh(UI_PausePanel owner, CombatInsightsController model)
        {
            panel = owner;
            controller = model;
            if (button == null)
            {
                UI_HorayButton template = owner.defaultSelectable?.GetComponent<UI_HorayButton>();
                if (template == null) return;
                button = Instantiate(template, template.transform.parent, false);
                button.name = "Sephiria Enhancements — View Statistics";
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => controller?.OpenStatisticsBrowser(panel));
                button.SetForceNavUp(null);
                button.SetForceNavDown(null);
                button.SetForceNavLeft(null);
                button.SetForceNavRight(null);
                button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
                var localization = button.GetComponentInChildren<UI_LocalizationStringText>(true);
                localization?.UpdateKey(ModLocalization.ViewStatistics);
            }
            button.gameObject.SetActive(controller != null && controller.CanBrowseStatistics);
        }

        private void Update()
        {
            if (button == null) return;
            button.gameObject.SetActive(controller != null && controller.CanBrowseStatistics);
        }

        private void OnDestroy()
        {
            if (button != null) Destroy(button.gameObject);
        }
    }
}
