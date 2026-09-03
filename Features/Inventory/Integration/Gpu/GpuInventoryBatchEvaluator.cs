#nullable disable
using System.Collections.Generic;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class GpuInventoryBatchEvaluator : IInventoryCandidateBatchEvaluator
{
    private readonly DirectComputeKernel kernel;
    private readonly InventorySnapshot snapshot;
    private readonly ResolvedInventoryOptimizationPolicy policy;
    private GpuInventorySnapshot packed;
    private GpuInventoryScorer scorer;
    private int[] layouts;
    private int[] results;
    internal int GpuCandidates { get; private set; }
    internal int Dispatches { get; private set; }
    public int BatchSize => 256;

    internal GpuInventoryBatchEvaluator(InventorySnapshot snapshot, ResolvedInventoryOptimizationPolicy policy, DirectComputeKernel kernel)
    {
        this.kernel = kernel;
        this.snapshot = snapshot;
        this.policy = policy;
    }

    private void PrepareBuffers()
    {
        packed = new GpuInventorySnapshot(snapshot);
        scorer = new GpuInventoryScorer(packed, policy);
        layouts = new int[4 + BatchSize * snapshot.Items.Count * 2];
        results = new int[BatchSize * packed.ResultStride];
        kernel.Configure(packed.Data, layouts.Length, results.Length);
    }

    public void Evaluate(IReadOnlyList<InventoryLayoutProjection> candidates, InventoryOptimizationScore[] scores,
        IDictionary<string, InventoryTargetSearchEvidence> evidence, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Proven bounds and expired searches return before allocating or uploading a GPU batch.
        if (packed == null) PrepareBuffers();
        layouts[0] = candidates.Count;
        int items = packed.Snapshot.Items.Count;
        for (int c = 0; c < candidates.Count; c++)
            for (int item = 0; item < items; item++)
            {
                layouts[4 + (c * items + item) * 2] = candidates[c].GetCell(item);
                layouts[5 + (c * items + item) * 2] = candidates[c].GetRotation(item);
            }
        kernel.Run(layouts, results, candidates.Count);
        Dispatches++;
        GpuCandidates += candidates.Count;
        for (int c = 0; c < candidates.Count; c++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scores[c] = scorer.Score(results, c, evidence);
        }
    }
}
