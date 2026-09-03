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

        // Strategies supply a layout and search evidence, never their own scoring
        // semantics. Build all player-facing results from the original request.
        internal InventoryOptimizationProposal CreateProposal(
            InventoryLayoutProjection layout, int candidateEvaluations,
            InventorySearchTerminationReason terminationReason,
            long elapsedMilliseconds,
            InventoryOptimizationSearchMethod searchMethod = InventoryOptimizationSearchMethod.Neighborhood,
            bool optimalityProven = false, int duplicateLayoutsSkipped = 0,
            IReadOnlyDictionary<string, InventoryTargetSearchEvidence> searchEvidence = null)
        {
            if (Snapshot == null || Policy == null || layout == null ||
                layout.CopyRotations().Length != layout.ItemCount)
                return Reject("OptimizationInputUnavailable");

            ProjectedInventorySettlement after = InventorySettlementProjector.Evaluate(Snapshot, layout);
            if (!after.Succeeded)
                return Reject(after.Issues.ToArray());
            for (int index = 0; index < Snapshot.Items.Count; index++)
            {
                StoneTabletSnapshot tablet = Snapshot.Items[index].StoneTablet;
                if (tablet != null && !Policy.AllowStoneTabletRotation &&
                    layout.GetRotation(index) != tablet.Rotation)
                    return Reject("StoneTabletRotationDisabled");
            }

            InventoryLayoutProjection original = InventoryLayoutProjection.Current(Snapshot);
            ProjectedInventorySettlement before = InventorySettlementProjector.Evaluate(Snapshot, original);
            if (!before.Succeeded)
                return Reject(before.Issues.ToArray());
            var scorer = new InventoryOptimizationScorer(Snapshot, Policy);
            InventoryOptimizationScore current = scorer.Score(original, before);
            InventoryOptimizationScore best = scorer.Score(layout, after);
            if (best.CompareTo(current) < 0)
            {
                layout = original;
                after = before;
                best = current;
                optimalityProven = false;
            }
            return new InventoryOptimizationProposal(true, layout, current, best,
                candidateEvaluations, Array.Empty<string>(), Policy,
                scorer.EvaluateTargets(before, after, searchEvidence, optimalityProven),
                terminationReason, elapsedMilliseconds, searchMethod, optimalityProven,
                duplicateLayoutsSkipped, InventoryOptimizationOutcomeBuilder.Build(
                    Snapshot, before, after, current, best));

            InventoryOptimizationProposal Reject(params string[] issues) => new(false,
                null, null, null, candidateEvaluations, issues, Policy,
                terminationReason: InventorySearchTerminationReason.InputRejected,
                elapsedMilliseconds: elapsedMilliseconds, searchMethod: searchMethod);
        }
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
                new MultiStartInventoryLayoutOptimizer(),
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
            return Solve(request, InventoryOptimizerRegistry.Capture(), cancellationToken);
        }

        // Explicit composition also lets contract checks exercise a contributed
        // strategy without changing the process-wide registry.
        internal static InventoryOptimizationProposal Solve(
            InventoryOptimizationRequest request,
            IEnumerable<IInventoryLayoutOptimizer> optimizers,
            CancellationToken cancellationToken = default)
        {
            foreach (IInventoryLayoutOptimizer optimizer in optimizers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (optimizer.CanOptimize(request) && optimizer.TryOptimize(
                        request, cancellationToken,
                        out InventoryOptimizationProposal proposal) && proposal != null)
                {
                    if (!proposal.Succeeded) return proposal;
                    var evidence = new Dictionary<string, InventoryTargetSearchEvidence>(StringComparer.Ordinal);
                    foreach (InventoryOptimizationTargetEvaluation target in proposal.TargetEvaluations)
                        evidence[target.Target] = new InventoryTargetSearchEvidence(
                            target.MaximumObservedValue, target.MaximumObservedCompletionPoints,
                            target.Reachability == InventoryTargetReachability.SelectedLayoutReachesCondition ||
                            target.Reachability == InventoryTargetReachability.ObservedReachable);
                    InventoryOptimizationProposal verified = request.CreateProposal(
                        proposal.Layout, proposal.CandidateEvaluations,
                        proposal.TerminationReason, proposal.ElapsedMilliseconds,
                        proposal.SearchMethod, proposal.OptimalityProven &&
                            optimizer.Metadata.Capabilities.HasFlag(InventoryOptimizerCapabilities.OptimalityProof),
                        proposal.DuplicateLayoutsSkipped, evidence);
                    if (verified.TerminationReason != InventorySearchTerminationReason.InputRejected)
                        return verified;
                }
            }

            return InventoryOptimizer.Solve(request.Snapshot, request.Policy, request.Budget,
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
                request.Budget.MaximumCandidateEvaluations, request.Policy.AllowStoneTabletRotation) <=
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
                        request.Budget.MaximumElapsedMilliseconds, request.Budget.UseElapsedTimeLimit),
                    cancellationToken);
            if (!exact.SearchStarted)
            {
                proposal = null;
                return false;
            }

            proposal = request.CreateProposal(exact.BestLayout,
                exact.CandidateLayoutsEvaluated, exact.SearchSpaceExhausted
                    ? InventorySearchTerminationReason.SearchSpaceExhausted
                    : InventorySearchTerminationReason.ElapsedTimeLimit,
                exact.ElapsedMilliseconds,
                InventoryOptimizationSearchMethod.Exhaustive,
                optimalityProven: exact.SearchSpaceExhausted,
                searchEvidence: exact.TargetSearchEvidence);
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
