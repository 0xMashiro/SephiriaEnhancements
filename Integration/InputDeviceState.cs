using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Integration
{
    internal static class InputDeviceState
    {
        internal static bool HasKeyboardModifierPressed =>
            Keyboard.current != null && HasModifier(Keyboard.current);

        internal static bool TryGetPointerPosition(out Vector2 position)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                position = Vector2.zero;
                return false;
            }

            position = mouse.position.ReadValue();
            return true;
        }

        internal static bool HasPointerMoved(float minimumDistancePixels = 2f)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            return mouse.delta.ReadValue().sqrMagnitude >=
                minimumDistancePixels * minimumDistancePixels;
        }

        private static bool HasModifier(Keyboard keyboard) =>
            keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed ||
            keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed ||
            keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    }
}
