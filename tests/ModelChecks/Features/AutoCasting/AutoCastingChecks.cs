using SephiriaEnhancements.AutoCasting;
using SephiriaEnhancements.AutoCasting.Integration;

namespace SephiriaEnhancements.ModelChecks.Features.AutoCasting;

internal static class AutoCastingChecks
{
    internal static void Run()
    {
        for (int number = 1; number <= 8; number++)
        {
            Expect(NativeAutoCastingBindings.SlotFor("Magic_Keyboard", "QuickCast" + number) == number - 1,
                "Keyboard actions map to their native magic slots");
            Expect(NativeAutoCastingBindings.SlotFor("Magic_Joystick", "QuickCast" + number + "_J") == number - 1,
                "Gamepad actions retain their native suffix");
        }
        Expect(NativeAutoCastingBindings.SlotFor("Magic_Joystick", "QuickCast1") == -1,
            "An invented gamepad action cannot match a slot");
        Expect(NativeAutoCastingBindings.SlotFor("Magic_Keyboard", "QuickCast9") == -1 &&
            NativeAutoCastingBindings.SlotFor("UI", "QuickCast1") == -1 &&
            NativeAutoCastingBindings.SlotFor(null, null) == -1, "Only native gameplay bindings qualify");
        Expect(NativeAutoCastingBindings.SlotFor("Player", "Fire") == 8 &&
            NativeAutoCastingBindings.SlotFor("Player", "SubFire") == 9 &&
            NativeAutoCastingBindings.SlotFor("Player", "Reload") == 10,
            "The three weapon inputs remain distinct from normal magic slots");
        var selection = new AutoCastingSelection();
        selection.Toggle(17);
        Expect(selection.Contains(17) && !selection.Contains(18), "Selecting an artifact cannot select its replacement");
        selection.Retain([17, 18]);
        Expect(selection.Contains(17), "Rebinding the same owned artifact preserves its selection");
        selection.Retain([18]);
        selection.Retain([17, 18]);
        Expect(!selection.Contains(17), "Losing an artifact clears selection, including when it is later reacquired");
        selection.Toggle(18);
        selection.Toggle(18);
        Expect(!selection.Contains(18), "The same action toggles off");
        selection.Toggle(17);
        selection.Clear();
        Expect(!selection.Contains(17), "World reload and character replacement clear selection");

        var rotation = new AutoCastingRotation();
        Expect(rotation.Take(0, 11, i => i == 0 || i == 8, 0.35) == 0, "First ready magic is requested");
        Expect(rotation.Take(0.1, 11, _ => true, 0.35) == -1, "Requests are not burst together");
        Expect(rotation.Take(0.4, 11, i => i == 0 || i == 8, 0.35) == 8, "Weapon-bound magic gets a turn");
        Expect(rotation.Take(0.8, 11, i => i == 0 || i == 8, 0.35) == 0, "Rotation wraps without starving slots");
        rotation.YieldToManualInput(1.2);
        Expect(rotation.Take(1.3, 11, _ => true, 0.35) == -1, "Manual input takes priority");
        Expect(rotation.Take(2, 11, _ => false, 0.35) == -1, "Unready skills cannot enter a queue");
        Expect(rotation.Take(3, 11, i => i == 4, 2) == 4, "Readiness is re-evaluated after a menu");
        Expect(rotation.Take(4, 11, _ => true, 2) == -1, "Network pacing is respected");
        rotation.Reset();
        Expect(rotation.Take(0, 11, i => i == 1, 0.35) == 1, "A new world does not inherit the old timer");
        Console.WriteLine("Auto casting: artifact ownership, replacement, rotation, readiness and manual priority passed");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
