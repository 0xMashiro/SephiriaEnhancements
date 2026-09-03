#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SephiriaEnhancements.Inventory
{
    [Flags]
    internal enum InventoryOptimizerCapabilities
    {
        None = 0,
        ArtifactTargets = 1 << 0,
        ComboTargets = 1 << 1,
        InstanceTargets = 1 << 2,
        StoneTabletRotation = 1 << 3,
        FullInventory = 1 << 4,
        OptimalityProof = 1 << 5
    }

    internal sealed class InventoryOptimizerMetadata
    {
        internal InventoryOptimizerMetadata(string id, int selectionPriority,
            InventoryOptimizerCapabilities capabilities)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Optimizer ID is required.",
                    nameof(id));
            }
            Id = id;
            SelectionPriority = selectionPriority;
            Capabilities = capabilities;
        }

        internal string Id { get; }
        internal int SelectionPriority { get; }
        internal InventoryOptimizerCapabilities Capabilities { get; }
    }

    internal sealed class InventoryOptimizationRequest
    {
        internal InventoryOptimizationRequest(InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget)
        {
            Snapshot = snapshot;
            Policy = policy;
            Budget = budget ?? InventorySearchBudget.ForEffort(
                policy?.SearchEffort ?? InventorySearchEffort.Balanced);
        }

        internal InventorySnapshot Snapshot { get; }
        internal ResolvedInventoryOptimizationPolicy Policy { get; }
        internal InventorySearchBudget Budget { get; }
    }

    internal interface IInventoryLayoutOptimizer
    {
        InventoryOptimizerMetadata Metadata { get; }

        bool CanOptimize(InventoryOptimizationRequest request);

        bool TryOptimize(InventoryOptimizationRequest request,
            CancellationToken cancellationToken,
            out InventoryOptimizationProposal proposal);
    }

    internal static class InventoryOptimizerRegistry
    {
        private static readonly object Gate = new();
        private static readonly List<IInventoryLayoutOptimizer> Optimizers =
            new()
            {
                new ExactInventoryLayoutOptimizer(),
                new BoundedInventoryLayoutOptimizer()
            };

        internal static void Register(IInventoryLayoutOptimizer optimizer)
        {
            if (optimizer?.Metadata == null)
            {
                throw new ArgumentNullException(nameof(optimizer));
            }
            lock (Gate)
            {
                if (Optimizers.Any(candidate => string.Equals(
                        candidate.Metadata.Id, optimizer.Metadata.Id,
                        StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        "An inventory optimizer with ID '" +
                        optimizer.Metadata.Id + "' is already registered.");
                }
                Optimizers.Add(optimizer);
            }
        }

        internal static void Unregister(IInventoryLayoutOptimizer optimizer)
        {
            lock (Gate) Optimizers.Remove(optimizer);
        }

        internal static IInventoryLayoutOptimizer[] Capture()
        {
            lock (Gate)
            {
                return Optimizers.OrderByDescending(optimizer =>
                    optimizer.Metadata.SelectionPriority).ToArray();
            }
        }
    }

    internal static class InventoryOptimizerSelector
    {
        internal static InventoryOptimizationProposal Solve(
            InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget = null,
            CancellationToken cancellationToken = default)
        {
            var request = new InventoryOptimizationRequest(snapshot, policy,
                budget);
            foreach (IInventoryLayoutOptimizer optimizer in
                InventoryOptimizerRegistry.Capture())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (optimizer.CanOptimize(request) && optimizer.TryOptimize(
                        request, cancellationToken,
                        out InventoryOptimizationProposal proposal))
                {
                    return proposal;
                }
            }

            return InventoryOptimizer.Solve(snapshot, policy, request.Budget,
                cancellationToken);
        }
    }

    internal sealed class ExactInventoryLayoutOptimizer :
        IInventoryLayoutOptimizer
    {
        private static readonly InventoryOptimizerMetadata OptimizerMetadata =
            new("builtin.exact", 100,
                InventoryOptimizerCapabilities.ArtifactTargets |
                InventoryOptimizerCapabilities.ComboTargets |
                InventoryOptimizerCapabilities.InstanceTargets |
                InventoryOptimizerCapabilities.StoneTabletRotation |
                InventoryOptimizerCapabilities.FullInventory |
                InventoryOptimizerCapabilities.OptimalityProof);

        public InventoryOptimizerMetadata Metadata => OptimizerMetadata;

        public bool CanOptimize(InventoryOptimizationRequest request)
        {
            if (request?.Snapshot == null || request.Policy == null)
            {
                return false;
            }
            return InventoryExhaustiveSearchOracle.EstimateCandidateLayouts(
                request.Snapshot,
                request.Budget.MaximumCandidateEvaluations) <=
                request.Budget.MaximumCandidateEvaluations;
        }

        public bool TryOptimize(InventoryOptimizationRequest request,
            CancellationToken cancellationToken,
            out InventoryOptimizationProposal proposal)
        {
            InventoryExhaustiveSearchResult exact =
                InventoryExhaustiveSearchOracle.Solve(request.Snapshot,
                    request.Policy, new InventoryExhaustiveSearchLimits(
                        request.Budget.MaximumCandidateEvaluations,
                        request.Budget.MaximumElapsedMilliseconds),
                    cancellationToken);
            if (!exact.SearchStarted)
            {
                proposal = null;
                return false;
            }

            InventoryLayoutProjection current = InventoryLayoutProjection.Current(
                request.Snapshot);
            ProjectedInventorySettlement currentSettlement =
                InventorySettlementProjector.Evaluate(
                    request.Snapshot, current);
            ProjectedInventorySettlement bestSettlement =
                InventorySettlementProjector.Evaluate(
                    request.Snapshot, exact.BestLayout);
            var scorer = new InventoryOptimizationScorer(request.Snapshot,
                request.Policy);
            InventoryOptimizationOutcome outcome =
                InventoryOptimizationOutcomeBuilder.Build(request.Snapshot,
                    currentSettlement, bestSettlement, exact.CurrentScore,
                    exact.BestScore);
            proposal = new InventoryOptimizationProposal(true,
                exact.BestLayout, exact.CurrentScore, exact.BestScore,
                exact.CandidateLayoutsEvaluated, exact.Issues.ToArray(),
                request.Policy, scorer.EvaluateTargets(currentSettlement,
                    bestSettlement, exact.TargetSearchEvidence,
                    exact.ProvenOptimal), exact.ProvenOptimal
                    ? InventorySearchTerminationReason.SearchSpaceExhausted
                    : InventorySearchTerminationReason.ElapsedTimeLimit,
                exact.ElapsedMilliseconds,
                InventoryOptimizationSearchMethod.Exhaustive,
                optimalityProven: exact.ProvenOptimal, outcome: outcome);
            return true;
        }
    }

    internal sealed class BoundedInventoryLayoutOptimizer :
        IInventoryLayoutOptimizer
    {
        private static readonly InventoryOptimizerMetadata OptimizerMetadata =
            new("builtin.bounded", 0,
                InventoryOptimizerCapabilities.ArtifactTargets |
                InventoryOptimizerCapabilities.ComboTargets |
                InventoryOptimizerCapabilities.InstanceTargets |
                InventoryOptimizerCapabilities.StoneTabletRotation |
                InventoryOptimizerCapabilities.FullInventory);

        public InventoryOptimizerMetadata Metadata => OptimizerMetadata;

        public bool CanOptimize(InventoryOptimizationRequest request) => true;

        public bool TryOptimize(InventoryOptimizationRequest request,
            CancellationToken cancellationToken,
            out InventoryOptimizationProposal proposal)
        {
            proposal = InventoryOptimizer.Solve(request.Snapshot,
                request.Policy, request.Budget, cancellationToken);
            return true;
        }
    }
}
