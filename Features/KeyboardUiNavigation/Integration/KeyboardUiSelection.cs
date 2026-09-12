using SephiriaEnhancements.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    // Native UI boundary: resolve actual navigable controls, not list containers.
    internal static class KeyboardUiSelection
    {
        internal static bool IsPanelReady(UIBase panel) =>
            panel != null && panel.isActiveAndEnabled && panel.IsOpened &&
            (!panel.hasControl || panel.IsControlEnabled) &&
            (panel.CanvasGroup == null || panel.CanvasGroup.interactable) &&
            (!(panel is UI_SephiriteRewardPanel reward) || reward.rewardsGroupInteractable);

        internal static bool IsNavigable(GameObject candidate)
        {
            if (candidate == null || !candidate.activeInHierarchy) return false;
            Selectable selectable = candidate.GetComponent<Selectable>();
            return selectable != null && selectable.IsActive() &&
                selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None;
        }

        internal static bool IsInPanel(UIBase panel, GameObject candidate) =>
            IsPanelReady(panel) && IsNavigable(candidate) &&
            (candidate.transform.IsChildOf(panel.transform) ||
                OwnsCustomSelection(panel, candidate));

        private static bool OwnsCustomSelection(UIBase panel, GameObject candidate)
        {
            if (!(panel is UI_CharacterStatusPanel)) return false;
            bool owned = false;
            FeatureFailure.Run(FeatureId.CharacterPanelNavigation, () =>
                owned = Inventory.InventoryOptimizationController.OwnsSelection(panel, candidate));
            return owned;
        }

        internal static bool IsInControlStack(GameObject candidate)
        {
            var stack = UIManager.Instance?.CurrentControlStack;
            if (stack == null) return false;
            foreach (UIBase panel in stack)
                if (IsInPanel(panel, candidate)) return true;
            return false;
        }

        internal static GameObject FindPanelEntry(UIBase panel, GameObject preferred = null)
        {
            if (!IsPanelReady(panel)) return null;
            if (IsInPanel(panel, preferred)) return preferred;

            // The holder owns control, but each message box owns its default
            // choice (including dialogs that deliberately default to Cancel).
            if (panel is UI_MessageBoxHolder)
            {
                foreach (UI_MessageBox box in panel.GetComponentsInChildren<UI_MessageBox>())
                    if (IsPanelReady(box)) return FindPanelEntry(box);
                return null;
            }

            // Settings navigate within the current tab; rewards use their own
            // partition while sharing control with the inventory.
            if (panel is UI_OptionsPanel options)
            {
                GameObject entry = null;
                FeatureFailure.Run(FeatureId.OptionsNavigation, () => entry = OptionsKeyboardNavigation.FindEntry(options));
                if (entry != null) return entry;
            }
            if (IsInPanel(panel, panel.defaultSelectable)) return panel.defaultSelectable;

            Transform contents = panel is UI_SephiriteRewardPanel reward
                ? reward.rewardZone : panel.transform;
            if (contents == null) return null;
            foreach (Selectable candidate in contents.GetComponentsInChildren<Selectable>())
            {
                if (!(candidate is Scrollbar) && IsInPanel(panel, candidate.gameObject))
                    return candidate.gameObject;
            }
            return null;
        }
    }
}
