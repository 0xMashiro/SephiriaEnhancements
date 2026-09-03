#nullable disable
using System.Collections.Generic;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal interface IInventoryCandidateBatchEvaluator
    {
        int BatchSize { get; }
        void Evaluate(IReadOnlyList<InventoryLayoutProjection> candidates, InventoryOptimizationScore[] scores,
            IDictionary<string, InventoryTargetSearchEvidence> evidence, CancellationToken cancellationToken);
    }
}
