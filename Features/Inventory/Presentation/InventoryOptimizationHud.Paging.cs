using System.Linq;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationHud
    {
        private void HandlePageShortcut()
        {
            if (!NavigationAvailable || !panelOpen || !preferencesExpanded || specialEffectsExpanded ||
                !interaction.Editable || goalEditor.Visible || NativeInventoryIntentDrop.HasHeldItem ||
                !IsCustomSelection(EventSystem.current?.currentSelectedGameObject)) return;

            var actions = PlayerInputController.Instance?.playerInput?.actions;
            bool previous = NativeInputActions.FindAction(actions, NativeUiActions.PrevTab)?.WasPressedThisFrame() == true;
            bool next = NativeInputActions.FindAction(actions, NativeUiActions.NextTab)?.WasPressedThisFrame() == true;
            if (previous == next) return;
            Button target = previous ? previousPage : nextPage;
            if (target.IsInteractable()) ChangePage(previous ? -1 : 1);
        }

        private void RefreshPageNavigation()
        {
            if (!panelOpen || !preferencesExpanded || specialEffectsExpanded || goalEditor.Visible) return;
            var previous = (UI_HorayButton)previousPage;
            var next = (UI_HorayButton)nextPage;
            Button entry = previous.IsInteractable() ? previous : next.IsInteractable() ? next : null;
            Button content = detailsExpanded
                ? rows.FirstOrDefault(row => row.Root.activeInHierarchy)?.Choice ?? optimize
                : moveMark.IsInteractable() ? moveMark : optimize;
            Button above = detailsExpanded ? preferencesToggle : avoidSlots[0].Button;

            previous.SetForceNavRight(next.IsInteractable() ? next : null);
            next.SetForceNavLeft(previous.IsInteractable() ? previous : null);
            previous.SetForceNavUp(above);
            next.SetForceNavUp(above);
            previous.SetForceNavDown(content);
            next.SetForceNavDown(content);
            if (detailsExpanded)
            {
                ((UI_HorayButton)preferencesToggle).SetForceNavDown(entry ?? content);
                ((UI_HorayButton)content).SetForceNavUp(entry ?? preferencesToggle);
            }
            else
            {
                foreach (var slot in avoidSlots) ((UI_HorayButton)slot.Button).SetForceNavDown(entry ?? content);
                ((UI_HorayButton)content).SetForceNavUp(entry ?? above);
            }

            // Paging can disable the focused arrow or hide a row on the last page.
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(root.transform) &&
                !IsCustomSelection(selected))
                EventSystem.current.SetSelectedGameObject((entry ?? content).gameObject);
        }
    }
}
