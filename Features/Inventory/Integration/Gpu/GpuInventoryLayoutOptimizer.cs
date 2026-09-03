using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory.Integration.Gpu;

internal sealed class GpuInventoryLayoutOptimizer : IInventoryLayoutOptimizer, IDisposable
{
    private static readonly InventoryOptimizerMetadata OptimizerMetadata = new("builtin.gpu", 50,
        InventoryOptimizerCapabilities.ArtifactTargets | InventoryOptimizerCapabilities.ComboTargets |
        InventoryOptimizerCapabilities.InstanceTargets | InventoryOptimizerCapabilities.StoneTabletRotation |
        InventoryOptimizerCapabilities.FullInventory);
    private readonly object gate = new();
    private readonly Task<DirectComputeKernel> initialization;
    private int stopped;
    private bool failed;
    public InventoryOptimizerMetadata Metadata => OptimizerMetadata;

    internal GpuInventoryLayoutOptimizer()
    {
        initialization = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.Is64BitProcess
            ? Task.Run(InitializeDevice) : Task.FromResult<DirectComputeKernel>(null);
    }

    private static DirectComputeKernel InitializeDevice()
    {
        try
        {
            var kernel = new DirectComputeKernel(DirectComputeKernel.Compile(InventorySettlementShader.Source));
            SupportLogger.Record("inventory_gpu_ready");
            return kernel;
        }
        catch (Exception exception)
        {
            SupportLogger.Failure("inventory_gpu_unavailable", exception);
            return null;
        }
    }

    public bool CanOptimize(InventoryOptimizationRequest request) => Volatile.Read(ref stopped) == 0 && !failed &&
        initialization.Status == TaskStatus.RanToCompletion && initialization.Result != null &&
        request?.Snapshot?.SettlementValidation.LayoutProjectionReady == true && request.Policy != null &&
        request.Budget.MaximumCandidateEvaluations >= 5000 && request.Snapshot.Storage <= 64 &&
        request.Snapshot.Items.Count <= 64 && request.Snapshot.PositionEffects.Rules.Count == 0 &&
        request.Snapshot.PositionEffects.Observed.Count == 0;

    public bool TryOptimize(InventoryOptimizationRequest request, CancellationToken cancellationToken,
        out InventoryOptimizationProposal proposal)
    {
        proposal = null;
        if (!CanOptimize(request) || !Monitor.TryEnter(gate)) return false;
        try
        {
            if (!CanOptimize(request)) return false;
            cancellationToken.ThrowIfCancellationRequested();
            var elapsed = Stopwatch.StartNew();
            var evaluator = new GpuInventoryBatchEvaluator(request.Snapshot, request.Policy, initialization.Result);
            var budget = new InventorySearchBudget(request.Budget.MaximumImprovementRounds,
                request.Budget.MaximumCandidateEvaluations,
                Math.Max(0, request.Budget.MaximumElapsedMilliseconds - (int)elapsed.ElapsedMilliseconds));
            try { proposal = InventoryOptimizer.Solve(request.Snapshot, request.Policy, budget, cancellationToken, evaluator); }
            catch (NotSupportedException) { return false; }
            // Validate the selected result on the CPU before any game inventory operation.
            if (proposal.Succeeded && evaluator.GpuCandidates > 0)
            {
                var settlement = InventorySettlementProjector.Evaluate(request.Snapshot, proposal.Layout);
                if (!settlement.Succeeded || new InventoryOptimizationScorer(request.Snapshot, request.Policy)
                        .Score(proposal.Layout, settlement).CompareTo(proposal.BestScore) != 0)
                    throw new InvalidOperationException("GPU inventory result validation failed.");
            }
            if (evaluator.GpuCandidates > 0)
                SupportLogger.Record("inventory_gpu_solved", "gpuCandidates=" + evaluator.GpuCandidates +
                    " dispatches=" + evaluator.Dispatches + " elapsedMs=" + elapsed.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture));
            return true;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            failed = true;
            SupportLogger.Failure("inventory_gpu_failed", exception);
            throw;
        }
        finally { Monitor.Exit(gate); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref stopped, 1) != 0) return;
        // Device resources belong to this Mod controller, across floors and explorations.
        // Release after any background search; unloading never waits on the Unity thread.
        initialization.ContinueWith(task =>
        {
            lock (gate) task.Result?.Dispose();
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }
}
