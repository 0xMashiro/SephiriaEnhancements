using SephiriaEnhancements.CombatTargeting;

namespace SephiriaEnhancements.ModelChecks.Features.CombatTargeting;

internal static class CombatTargetingChecks
{
    internal static void Run()
    {
        var selection = new TargetSelection<string>();
        selection.Refresh(["near", "side", "far"], allowAutomatic: false);
        Expect(selection.Target == null, "Melee must not acquire a distant target automatically");
        selection.Refresh(["near", "side", "far"], allowAutomatic: true);
        Expect(selection.Target == "near" && !selection.IsManual, "Initial automatic acquisition");
        selection.Refresh(["far", "side", "near"], allowAutomatic: true);
        Expect(selection.Target == "near", "Retreating or crossing enemies must not steal the target");
        selection.Switch(null);
        Expect(selection.Target == "side" && selection.IsManual, "Switch follows the stable cycle");
        selection.Refresh(["side", "near", "far"], allowAutomatic: false);
        selection.Switch(null);
        Expect(selection.Target == "far", "Distance changes must not reorder the cycle");
        selection.Refresh(["near", "far"], allowAutomatic: false);
        Expect(selection.Target == "far" && selection.IsManual, "Manual lock survives weapon policy changes");
        selection.Refresh(["near"], allowAutomatic: false);
        Expect(selection.Target == null && !selection.IsManual, "Invalid melee lock releases without auto-acquisition");
        selection.Refresh(["near", "new"], allowAutomatic: true);
        selection.Refresh(["new"], allowAutomatic: true);
        Expect(selection.Target == "new", "Invalid ranged target reacquires");
        selection.Unlock();
        selection.Refresh(["new"], allowAutomatic: true);
        Expect(selection.Target == "new" && !selection.IsManual, "Unlock returns ranged attacks to automatic aim");
        selection.Clear();
        selection.Refresh(["far", "near"], allowAutomatic: false);
        selection.Switch("far");
        Expect(selection.Target == "near", "New context rebuilds cycle and can start from native target");
        selection.Clear();
        selection.Switch(null);
        Expect(selection.Target == null, "Empty candidate set");

        var gesture = new TargetSwitchGesture();
        Expect(gesture.Update(true, true, false, 0) == TargetSwitchCommand.None, "Press alone must not switch");
        Expect(gesture.IsPending, "Pending keyboard press enters combat before choosing a target");
        Expect(gesture.Update(false, false, true, 0.1f) == TargetSwitchCommand.Switch, "Tap switches on release");
        Expect(!gesture.IsPending, "Tap completes the gesture");
        gesture.Update(true, true, false, 1);
        Expect(gesture.Update(false, true, false, 1.5f) == TargetSwitchCommand.Unlock, "Hold unlocks without switching");
        Expect(gesture.Update(false, false, true, 1.6f) == TargetSwitchCommand.None, "Release after hold must not switch");
        gesture.Update(true, true, false, 2);
        Expect(gesture.Update(false, false, true, 2.5f) == TargetSwitchCommand.Unlock, "Slow frame release still recognizes hold");
        gesture.Update(true, true, false, 3);
        gesture.Clear();
        Expect(!gesture.IsPending, "Suspending combat cancels the pending gesture");
        Expect(gesture.Update(false, false, true, 3.1f) == TargetSwitchCommand.None, "Context loss cancels pending tap");
        Console.WriteLine("CombatTargeting: target retention, stable switching and tap/hold checks passed");
    }

    private static void Expect(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
}
