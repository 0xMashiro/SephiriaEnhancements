namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class KeyboardSelectionScroll
    {
        internal static float VerticalOffset(float selectedMin, float selectedMax,
            float viewportMin, float viewportMax)
        {
            if (selectedMax - selectedMin > viewportMax - viewportMin ||
                selectedMax > viewportMax)
                return viewportMax - selectedMax;
            if (selectedMin < viewportMin)
                return viewportMin - selectedMin;
            return 0f;
        }
    }
}
