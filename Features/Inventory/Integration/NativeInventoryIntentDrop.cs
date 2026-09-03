#nullable disable
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Inventory
{
    internal static class NativeInventoryIntentDrop
    {
        internal static UI_NewInventoryIcon ConfirmedPickup => UIManager.Instance?
            .GetElement<UI_NewItemPicker_Controller>()?.CurrentPickedUp;

        internal static void ConsumeConfirmedPickup(UI_NewInventoryIcon source)
        {
            UI_NewItemPicker_Controller picker = UIManager.Instance?
                .GetElement<UI_NewItemPicker_Controller>();
            if (source != null && picker?.CurrentPickedUp == source)
            {
                picker.PickItemIcon(null, playSound: true);
            }
        }

        internal static bool WasRemovePressed => UIInputModule.current?
            .throwItemControlAction?.action?.WasPressedThisFrame() == true;

        internal static string RemoveBindingLabel => UIInputModule.current?
            .throwItemControlAction?.action?.GetBindingDisplayString(
                group: PlayerInputController.Instance?.playerInput?.currentControlScheme) ?? string.Empty;

        internal static void Consume(PointerEventData eventData)
        {
            UI_NewInventoryIcon source = eventData?.pointerDrag?
                .GetComponentInParent<UI_NewInventoryIcon>();
            UI_NewItemPicker picker = UIManager.Instance?
                .GetElement<UI_NewItemPicker>();
            if (source != null && picker?.CurrentPickedUp == source)
            {
                // Clear only the drag visual. Native OnEndDrag still owns its
                // callbacks; no inventory operation is needed to set a preference.
                picker.PickItemIcon(null, playSound: false);
            }
            ConsumeConfirmedPickup(source);
        }

        internal static bool HasHeldItem => UIManager.Instance?
            .GetElement<UI_NewItemPicker>()?.CurrentAny == true ||
            UIManager.Instance?.GetElement<UI_NewItemPicker_Controller>()?
                .CurrentAny == true;
    }
}
