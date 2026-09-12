using System.Linq;
using SephiriaEnhancements.KeyboardUiNavigation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationHud
    {
        internal bool OwnsKeyboardTab()
        {
            if (!NavigationAvailable || EventSystem.current == null) return false;
            GameObject selected = EventSystem.current.currentSelectedGameObject;
            return interaction.HasPickup || selected == null || FindInventoryIcon(selected) != null ||
                IsCustomSelection(selected);
        }

        internal bool TryHandleKeyboardTab()
        {
            Keyboard keyboard = Keyboard.current;
            EventSystem eventSystem = EventSystem.current;
            if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame ||
                eventSystem == null || !NavigationAvailable ||
                !KeyboardUiNavigationController.IsKeyboardModeActive() ||
                !KeyboardUiPointer.OwnsFocus)
            {
                return false;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            bool inventorySelected = FindInventoryIcon(selected) != null;
            bool customSelected = IsCustomSelection(selected);
            if (selected != null && !inventorySelected && !customSelected)
            {
                return false;
            }

            // A goal reference cannot leave the board while being moved.
            if (interaction.HasPickup) return true;
            if (inventorySelected) lastInventorySelection = selected;
            if (customSelected) lastCustomSelection = selected;
            GameObject target = customSelected ? FindInventoryEntry()
                : IsCustomSelection(lastCustomSelection) ? lastCustomSelection : FindFirstCustomEntry();
            if (target == null || target == selected)
            {
                return false;
            }

            eventSystem.SetSelectedGameObject(target);
            return true;
        }

        private bool NavigationAvailable => root != null && root.activeInHierarchy &&
            KeyboardUiSelection.IsPanelReady(attachedPanel) &&
            UIManager.Instance?.CurrentControlStack?.Contains(attachedPanel) == true &&
            StandardInventoryContext.TryGetOpenInventory(out GridInventory _, out var panel) &&
            panel == attachedPanel;

        internal bool OwnsSelection(UIBase panel, GameObject candidate) =>
            panel == attachedPanel && NavigationAvailable && IsCustomSelection(candidate);

        private void RefreshNavigation()
        {
            if (!NavigationAvailable)
            {
                navigationBridge.Clear();
                return;
            }
            var entry = FindFirstCustomEntry()?.GetComponent<UI_HorayButton>();
            var returnTarget = FindInventoryEntry()?.GetComponent<UI_HorayButton>();
            navigationBridge.Refresh(attachedPanel, root.transform as RectTransform,
                entry, returnTarget, allowReturnFromEntry: !goalEditor.Visible);
            if (goalEditor.Visible) goalEditor.RefreshNavigation();
            if (!preferencesExpanded && undoArrangement != null && undoArrangement.IsInteractable())
            {
                (optimize as UI_HorayButton)?.SetForceNavLeft(undoArrangement);
                (undoArrangement as UI_HorayButton)?.SetForceNavLeft(returnTarget);
            }
        }

        private void TrackInventorySelection()
        {
            if (!NavigationAvailable) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (FindInventoryIcon(selected) != null)
            {
                lastInventorySelection = selected;
            }
        }

        internal bool TryCancelPanel(UIBase panel)
        {
            if (panel != attachedPanel || !NavigationAvailable) return false;
            if (interaction.HasPickup) CancelPickedUpMark();
            else if (goalEditor.Visible)
            {
                CloseLevelEditor();
            }
            else if (priorityMarking) endPriorityMarking?.Invoke();
            else if (preferencesExpanded) TogglePreferences();
            else if (panelOpen) ClosePanel();
            else return false;
            return true;
        }

        private UI_NewInventoryIcon FindInventoryIcon(GameObject candidate)
        {
            if (candidate == null || attachedPanel == null || attachedInventoryZone == null) return null;
            UI_NewInventoryIcon icon = candidate.GetComponent<UI_NewInventoryIcon>();
            if (icon == null) icon = candidate.GetComponentInParent<UI_NewInventoryIcon>();
            return icon != null && attachedPanel != null &&
                icon.Inventory == attachedPanel.PlayerAvatar?.Inventory &&
                icon.transform.IsChildOf(attachedInventoryZone) ? icon : null;
        }

        private bool IsCustomSelection(GameObject candidate)
        {
            if (candidate == null || root == null ||
                !candidate.transform.IsChildOf(root.transform))
            {
                return false;
            }
            Selectable selectable = candidate.GetComponent<Selectable>() ??
                candidate.GetComponentInParent<Selectable>();
            return selectable != null && selectable.IsActive() &&
                selectable.IsInteractable();
        }

        private GameObject FindInventoryEntry()
        {
            if (attachedPanel == null || attachedInventoryZone == null) return null;
            if (FindInventoryIcon(lastInventorySelection) != null)
            {
                var remembered = lastInventorySelection.GetComponent<Selectable>();
                if (remembered == null) remembered = lastInventorySelection.GetComponentInParent<Selectable>();
                if (remembered != null && remembered.gameObject.activeInHierarchy && remembered.IsInteractable())
                    return remembered.gameObject;
            }
            lastInventorySelection = null;
            foreach (var icon in attachedPanel.GetComponentsInChildren<UI_NewInventoryIcon>(true))
            {
                if (icon == null || FindInventoryIcon(icon.gameObject) == null) continue;
                var selectable = icon.GetComponent<Selectable>();
                if (selectable != null && selectable.gameObject.activeInHierarchy && selectable.IsInteractable())
                    return selectable.gameObject;
            }
            return null;
        }

        private GameObject FindFirstCustomEntry()
        {
            if (goalEditor?.ActiveInHierarchy == true)
                return IsCustomSelection(lastCustomSelection) && goalEditor.Contains(lastCustomSelection)
                    ? lastCustomSelection : goalEditor.Entry;
            if (!panelOpen && launcher != null && launcher.gameObject.activeInHierarchy &&
                launcher.IsInteractable())
            {
                return launcher.gameObject;
            }

            if (panelOpen && preferencesExpanded && !detailsExpanded && prioritySlots.Count > 0 &&
                KeyboardUiSelection.IsNavigable(prioritySlots[0].Root))
                return prioritySlots[0].Root;

            if (panelOpen && !preferencesExpanded && KeyboardUiSelection.IsNavigable(optimize?.gameObject)) return optimize.gameObject;

            Button[] preferred = { preferencesToggle, moveMark, close, markPriorities,
                optimize, previousPage, nextPage };
            return preferred.FirstOrDefault(button => button != null &&
                button.gameObject.activeInHierarchy && button.IsInteractable())?.gameObject;
        }

        private void SelectFirstCustomEntry()
        {
            GameObject entry = FindFirstCustomEntry();
            if (entry != null)
            {
                EventSystem.current?.SetSelectedGameObject(entry);
            }
        }

        private void CloseLevelEditor()
        {
            var key = interaction.LevelTarget;
            bool editorSelected = EventSystem.current?.currentSelectedGameObject != null &&
                goalEditor.Contains(EventSystem.current.currentSelectedGameObject);
            interaction.CancelLevelEdit();
            goalEditor.SetVisible(false);
            ProjectIntentBoard(WorldSessionInventoryIntentStore.Capture());
            RefreshPageNavigation();
            if (editorSelected)
            {
                var slot = prioritySlots.Concat(avoidSlots).FirstOrDefault(candidate =>
                    candidate.Preference != null && candidate.Preference.ItemKey == key && candidate.Root.activeInHierarchy);
                EventSystem.current?.SetSelectedGameObject(slot?.Root ?? FindFirstCustomEntry());
            }
            nextProjectionAt = 0f;
        }

    }
}
