namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class KeyboardSelectionRecoveryPolicy
    {
        internal static bool ShouldRestore(bool keyboardMode,
            bool hasUsableSelection, bool navigationPressed) =>
            keyboardMode && !hasUsableSelection && navigationPressed;
    }
}
