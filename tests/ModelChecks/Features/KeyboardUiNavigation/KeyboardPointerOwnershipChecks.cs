using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.ModelChecks.Features.KeyboardUiNavigation;

internal static class KeyboardPointerOwnershipChecks
{
    internal static void Run()
    {
        var state = new KeyboardPointerOwnership();
        void Check(bool expected, string message)
        {
            if (state.KeyboardOwnsFocus != expected)
                throw new InvalidOperationException(message);
        }

        state.Update(true, false, false, true, 10, 10);
        Check(false, "Opening UI alone must not claim keyboard ownership.");
        state.Update(true, true, false, true, 10, 10);
        Check(true, "Keyboard input takes focus.");
        state.Update(true, false, false, true, 10, 10);
        Check(true, "Idle frames preserve keyboard focus.");
        state.Update(true, false, false, true, 11, 10);
        Check(true, "One pixel of jitter does not take focus.");
        state.Update(true, false, false, true, 12, 10);
        Check(false, "Slow accumulated mouse movement must take focus.");
        state.Update(true, true, false, true, 12, 10);
        Check(true, "Keyboard can reclaim focus after mouse movement.");
        state.Update(true, true, true, true, 12, 10);
        Check(false, "Mouse clicks, drag holds, releases and wheel input take priority.");
        state.Update(true, true, false, true, 30, 30);
        Check(false, "Simultaneous real movement wins over a key press.");
        state.Update(true, true, false, true, 30, 30);
        state.Update(false, true, false, true, 30, 30);
        Check(false, "Disabled, unfocused, gamepad or no-menu contexts release ownership.");
        state.Update(true, false, false, true, 30, 30);
        Check(false, "Returning to a menu does not reuse stale keyboard ownership.");
        state.Update(true, true, false, false, 0, 0);
        Check(true, "A physical mouse is not required for keyboard navigation.");
        state.Reset();
        Check(false, "Context reset and unload release ownership.");
        Console.WriteLine("KeyboardPointerOwnership: input priority, movement threshold, transitions and reset passed.");
    }
}
