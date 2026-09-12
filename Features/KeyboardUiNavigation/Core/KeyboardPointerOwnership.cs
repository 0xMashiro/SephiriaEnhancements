namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal sealed class KeyboardPointerOwnership
    {
        private bool hasPointerPosition;
        private float pointerX;
        private float pointerY;

        internal bool KeyboardOwnsFocus { get; private set; }

        internal void Update(bool available, bool keyboardInput,
            bool pointerAction, bool hasPointer, float x, float y)
        {
            float dx = x - pointerX;
            float dy = y - pointerY;
            // Six screen pixels distinguish intentional motion from minor jitter;
            // slow movement still accumulates from the last pointer-owned position.
            bool moved = hasPointer && hasPointerPosition && dx * dx + dy * dy >= 36f;
            if (!available || pointerAction || moved)
                KeyboardOwnsFocus = false;
            else if (keyboardInput)
                KeyboardOwnsFocus = true;

            if (!KeyboardOwnsFocus || !hasPointerPosition)
            {
                pointerX = x;
                pointerY = y;
            }
            hasPointerPosition = hasPointer;
        }

        internal void Reset()
        {
            KeyboardOwnsFocus = false;
            hasPointerPosition = false;
        }
    }
}
