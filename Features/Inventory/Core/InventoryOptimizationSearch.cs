#nullable disable
using System;
using System.Threading;
using System.Threading.Tasks;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryOptimizationSearch : IDisposable
    {
        private readonly CancellationTokenSource cancellation = new();

        internal InventoryOptimizationSearch(InventorySnapshot snapshot, RuntimeStateSnapshot runtime,
            Func<CancellationToken, InventoryOptimizationProposal> solve)
        {
            SourceSnapshot = snapshot;
            SourceRuntime = runtime;
            CancellationToken token = cancellation.Token;
            Task = System.Threading.Tasks.Task.Run(() => solve(token), token);
        }

        internal InventorySnapshot SourceSnapshot { get; }
        internal RuntimeStateSnapshot SourceRuntime { get; }
        internal Task<InventoryOptimizationProposal> Task { get; }

        public void Dispose()
        {
            if (!Task.IsCompleted) cancellation.Cancel();
            cancellation.Dispose();
        }
    }
}
