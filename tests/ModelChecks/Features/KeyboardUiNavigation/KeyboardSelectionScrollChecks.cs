using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.ModelChecks.Features.KeyboardUiNavigation;

internal static class KeyboardSelectionScrollChecks
{
    internal static void Run()
    {
        Check(0, 20, 80, 0, 100, "Visible card stays still");
        Check(0, 0, 100, 0, 100, "Exact fit stays still");
        Check(30, -30, 10, 0, 100, "Bottom row scrolls up fully");
        Check(-30, 90, 130, 0, 100, "Returning to upper row scrolls down");
        Check(150, -150, -110, 0, 100, "Offscreen selection becomes visible");
        Check(-50, -150, 150, 0, 100, "Oversized card aligns its top");
        Check(0, -200, 100, 0, 100, "Oversized card remains stable");
        Check(20, -70, -10, -50, 50, "Centered viewport uses local bounds");
        Check(60, -60, 20, 0, 200, "Scaled canvas preserves geometry");
        Console.WriteLine("Keyboard selection scroll: visibility, return navigation, oversized cards and viewport coordinates passed.");
    }

    private static void Check(float expected, float bottom, float top,
        float viewportBottom, float viewportTop, string message)
    {
        float actual = KeyboardSelectionScroll.VerticalOffset(bottom, top,
            viewportBottom, viewportTop);
        if (actual != expected) throw new InvalidOperationException(message);
    }
}
