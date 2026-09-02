using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.ModelChecks.Features.KeyboardUiNavigation;

internal static class KeyboardSelectionRecoveryPolicyChecks
{
    internal static string Run()
    {
        if (KeyboardSelectionRecoveryPolicy.ShouldRestore(
                keyboardMode: true, hasUsableSelection: false,
                navigationPressed: false) ||
            KeyboardSelectionRecoveryPolicy.ShouldRestore(
                keyboardMode: false, hasUsableSelection: false,
                navigationPressed: true) ||
            KeyboardSelectionRecoveryPolicy.ShouldRestore(
                keyboardMode: true, hasUsableSelection: true,
                navigationPressed: true) ||
            !KeyboardSelectionRecoveryPolicy.ShouldRestore(
                keyboardMode: true, hasUsableSelection: false,
                navigationPressed: true))
        {
            throw new InvalidOperationException(
                "missing keyboard focus must only recover on navigation intent");
        }

        return "pointer-owned empty focus and navigation-triggered recovery passed";
    }
}
