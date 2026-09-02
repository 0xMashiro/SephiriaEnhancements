using SephiriaEnhancements.CombatRelationOutlines;

namespace SephiriaEnhancements.ModelChecks.Features.CombatRelationOutlines;

internal static class CombatRelationOutlinePolicyChecks
{
    internal static void Run()
    {
        if (!CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, true,
                true, true, true) ||
            CombatRelationOutlinePolicy.ShouldShow(true, true, true, true, true,
                true, true, true) ||
            CombatRelationOutlinePolicy.ShouldShow(true, false, true, false, true,
                true, true, true) ||
            CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, false,
                true, true, true) ||
            CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, true,
                false, true, true))
            throw new InvalidOperationException("combat-relation outline visibility policy failed");
        Console.WriteLine("CombatRelationOutlinePolicy: relation and lifecycle checks passed");
    }
}
