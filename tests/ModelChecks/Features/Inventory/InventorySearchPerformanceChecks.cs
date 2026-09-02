using SephiriaEnhancements.Runtime.Inventory;
using System.Diagnostics;
using SephiriaEnhancements.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventorySearchPerformanceChecks
{
    private const long MaximumAllocatedBytesPerCase = 12L * 1024 * 1024;

    internal static string Run()
    {
        var results = new List<string>();
        foreach ((int storage, int itemCount) in new[]
        {
            (30, 1), (30, 3), (30, 6),
            (32, 1), (32, 3), (32, 6),
            (36, 1), (36, 3), (36, 6),
            (42, 1), (42, 3), (42, 6)
        })
        {
            InventorySnapshot snapshot = CreateFlatMainInventorySnapshot(
                storage, itemCount);
            ResolvedInventoryOptimizationPolicy policy =
                InventoryOptimizationPolicyResolver.Resolve(snapshot,
                    InventoryOptimizationPreferences.Default);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var elapsed = Stopwatch.StartNew();
            InventoryOptimizationProposal result = InventoryOptimizer.Solve(
                snapshot, policy,
                new InventorySearchBudget(maximumImprovementRounds: 8,
                    maximumCandidateEvaluations: 1500,
                    maximumElapsedMilliseconds: 5000));
            elapsed.Stop();
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() -
                allocatedBefore;

            if (!result.Succeeded || result.Improved ||
                result.CandidateEvaluations > 1500 ||
                result.ElapsedMilliseconds > elapsed.ElapsedMilliseconds ||
                allocatedBytes < 0 ||
                allocatedBytes > MaximumAllocatedBytesPerCase ||
                itemCount == 1 &&
                    result.TerminationReason !=
                        InventorySearchTerminationReason.NeighborhoodLocalOptimum ||
                itemCount > 1 &&
                    result.TerminationReason !=
                        InventorySearchTerminationReason.CandidateEvaluationLimit)
            {
                throw new InvalidOperationException(
                    "inventory performance budget contract failed for itemCount=" +
                    itemCount);
            }

            results.Add("storage=" + storage + "/" + itemCount + "items:" +
                result.CandidateEvaluations + "eval/" +
                elapsed.ElapsedMilliseconds + "ms/" + allocatedBytes + "B");
        }
        return string.Join(';', results);
    }

    private static InventorySnapshot CreateFlatMainInventorySnapshot(int storage,
        int itemCount)
    {
        const int width = 6;
        var cells = new InventoryCellSnapshot[storage];
        for (int cell = 0; cell < storage; cell++)
        {
            var settlement = new InventoryCellSettlementSnapshot(true,
                baselineLevel: 1, baselineMaximumLevel: -1,
                baselineTemporaryLevel: 0, baselineLevelMultiplier: 0,
                baselineDisableCount: 0, baselineCriteriaBypassCount: 0,
                enchantLevel: 0, fixedLevel: 0, fixedDisableCount: 0,
                fixedCriteriaBypassCount: 0, fixedLevelMultiplier: 0,
                tabletLevel: 0, tabletDisableCount: 0,
                tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
            cells[cell] = new InventoryCellSnapshot(cell, cell % width,
                cell / width, level: 1,
                maxLevel: cell < itemCount ? 1 : -1, temporaryLevel: 0,
                levelMultiplier: 0, disableCount: 0,
                ignoreCriteriaCount: 0, mystic: false, settlement);
        }

        var items = new InventoryItemSnapshot[itemCount];
        for (int index = 0; index < itemCount; index++)
        {
            var artifact = new ArtifactSnapshot(displayedLevel: 1, maxLevel: 1,
                enchant: 0, effectEnabledLevel: 1,
                limitedEffectEnabledLevel: 1, effectEnabled: true,
                penaltyEnabled: false, weaponRestricted: false,
                requiredWeapon: string.Empty, weaponCompatible: true,
                uniqueEffect: false, uniqueEffectRegistered: false,
                calculationOrder: "Pre",
                new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                    CriteriaEvaluationState.NotApplicable,
                    CriteriaEvaluationState.NotApplicable),
                Array.Empty<string>(), Array.Empty<string>(),
                attackable: false, magic: null);
            items[index] = new InventoryItemSnapshot(1000 + index,
                2000 + index, quantity: 1, cellIndex: index, x: index,
                y: 0, "Artifact " + index, "Artifact_" + index, "Charm",
                "Normal", Array.Empty<string>(), InventoryItemKind.Artifact,
                artifact, stoneTablet: null);
        }
        return new InventorySnapshot(width, storage, cells, items);
    }
}
