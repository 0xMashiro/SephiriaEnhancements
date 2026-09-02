using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class StoneTabletSnapshotChecks
{
    internal static void Run()
    {
        var tabletAdditions = new[]
        {
            new TabletAdditionSnapshot(1, 0, "CHARM", true, false, false,
                true, false, false, true, TabletCriteriaKind.Artifact),
            new TabletAdditionSnapshot(2, 0, "MUL/2", false, true, false,
                false, true, false, false, effectKind: TabletEffectKind.MultiplyLevel,
                levelParameter: 2)
        };
        var tabletProjection = new TabletRotationProjectionSnapshot(0,
            new[] { tabletAdditions[0] }, new[] { tabletAdditions[1] }, true);
        var stoneTabletSnapshot = new StoneTabletSnapshot(0, true, false, true, true,
            "R:CHARM", "R:MUL/2", new[] { tabletProjection });
        tabletAdditions[0] = tabletAdditions[1];
        if (stoneTabletSnapshot.RotationProjections.Count != 1 ||
            stoneTabletSnapshot.RotationProjections[0].Criteria[0].CriteriaKind !=
                TabletCriteriaKind.Artifact ||
            stoneTabletSnapshot.RotationProjections[0].Effects[0].EffectKind !=
                TabletEffectKind.MultiplyLevel ||
            stoneTabletSnapshot.RotationProjections[0].Effects[0].LevelParameter != 2 ||
            stoneTabletSnapshot.RotationProjections[0].Effects[0].ValidCell)
            throw new InvalidOperationException("tablet projection semantics or immutability failed");
        Console.WriteLine("StoneTabletSnapshot: native semantics, validity and immutability checks passed");
    }
}
