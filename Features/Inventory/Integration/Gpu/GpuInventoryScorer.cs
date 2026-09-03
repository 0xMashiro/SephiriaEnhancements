#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class GpuInventoryScorer
{
    private readonly GpuInventorySnapshot packed;
    private readonly InventoryOptimizationScorer scorer;
    private readonly int[] artifactIndexes;

    internal GpuInventoryScorer(GpuInventorySnapshot packed, ResolvedInventoryOptimizationPolicy policy)
    {
        this.packed = packed;
        scorer = new InventoryOptimizationScorer(packed.Snapshot, policy);
        artifactIndexes = Enumerable.Range(0, packed.Snapshot.Items.Count)
            .Where(index => packed.Snapshot.Items[index].Artifact != null).ToArray();
    }

    internal InventoryOptimizationScore Score(int[] output, int candidate, InventoryLayoutProjection layout,
        IDictionary<string, InventoryTargetSearchEvidence> evidence)
    {
        int start = candidate * packed.ResultStride;
        if (output[start] != 1) return null;
        var artifacts = new ProjectedInventoryArtifactSettlement[artifactIndexes.Length];
        for (int index = 0; index < artifactIndexes.Length; index++)
        {
            int itemIndex = artifactIndexes[index];
            int position = start + 1 + itemIndex * 3;
            // GPU output carries the activation and levels used by scoring.
            // Detailed penalty state is checked by the final CPU settlement.
            artifacts[index] = new ProjectedInventoryArtifactSettlement(
                packed.Snapshot.Items[itemIndex].ItemKey, output[position] != 0,
                false, output[position + 1], output[position + 2]);
        }
        int categoriesStart = start + 1 + packed.Snapshot.Items.Count * 3;
        var combos = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < packed.Categories.Length; index++)
            combos.Add(packed.Categories[index], output[categoriesStart + index]);
        var settlement = new ProjectedInventorySettlement(true, null, artifacts, combos, null);
        scorer.ObserveTargets(settlement, evidence);
        return scorer.Score(layout, settlement);
    }
}
