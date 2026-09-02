#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventoryExhaustiveSearchTerminationReason
    {
        SearchSpaceExhausted,
        CandidateLayoutLimit,
        ElapsedTimeLimit,
        InputRejected
    }

    internal sealed class InventoryExhaustiveSearchLimits
    {
        internal InventoryExhaustiveSearchLimits(
            int maximumCandidateLayouts = 100000,
            int maximumElapsedMilliseconds = 1000)
        {
            MaximumCandidateLayouts = Math.Max(1, maximumCandidateLayouts);
            MaximumElapsedMilliseconds = Math.Max(0,
                maximumElapsedMilliseconds);
        }

        internal int MaximumCandidateLayouts { get; }
        internal int MaximumElapsedMilliseconds { get; }
    }

    internal sealed class InventoryExhaustiveSearchResult
    {
        internal InventoryExhaustiveSearchResult(bool searchStarted,
            InventoryLayoutProjection bestLayout,
            InventoryOptimizationScore currentScore,
            InventoryOptimizationScore bestScore,
            long estimatedCandidateLayouts, int candidateLayoutsEvaluated,
            long elapsedMilliseconds,
            InventoryExhaustiveSearchTerminationReason terminationReason,
            IDictionary<string, InventoryTargetSearchEvidence>
                targetSearchEvidence = null,
            string[] issues = null)
        {
            SearchStarted = searchStarted;
            BestLayout = bestLayout;
            CurrentScore = currentScore;
            BestScore = bestScore;
            EstimatedCandidateLayouts = Math.Max(0,
                estimatedCandidateLayouts);
            CandidateLayoutsEvaluated = Math.Max(0,
                candidateLayoutsEvaluated);
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds);
            TerminationReason = terminationReason;
            TargetSearchEvidence = new ReadOnlyDictionary<string,
                InventoryTargetSearchEvidence>(new Dictionary<string,
                    InventoryTargetSearchEvidence>(targetSearchEvidence ??
                        new Dictionary<string,
                            InventoryTargetSearchEvidence>(),
                    StringComparer.Ordinal));
            Issues = Array.AsReadOnly(issues ?? Array.Empty<string>());
        }

        internal bool SearchStarted { get; }
        internal bool ProvenOptimal => SearchStarted &&
            TerminationReason ==
                InventoryExhaustiveSearchTerminationReason.SearchSpaceExhausted;
        internal InventoryLayoutProjection BestLayout { get; }
        internal InventoryOptimizationScore CurrentScore { get; }
        internal InventoryOptimizationScore BestScore { get; }
        internal long EstimatedCandidateLayouts { get; }
        internal int CandidateLayoutsEvaluated { get; }
        internal long ElapsedMilliseconds { get; }
        internal InventoryExhaustiveSearchTerminationReason TerminationReason
        { get; }
        internal IReadOnlyDictionary<string, InventoryTargetSearchEvidence>
            TargetSearchEvidence
        { get; }
        internal IReadOnlyList<string> Issues { get; }
    }

    internal static class InventoryExhaustiveSearchOracle
    {
        internal static InventoryExhaustiveSearchResult Solve(
            InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy,
            InventoryExhaustiveSearchLimits limits = null,
            CancellationToken cancellationToken = default)
        {
            var elapsed = Stopwatch.StartNew();
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot == null || !snapshot.SettlementValidation.LayoutProjectionReady)
            {
                return Rejected(snapshot?.SettlementValidation?.Issues,
                    elapsed.ElapsedMilliseconds);
            }
            if (policy == null)
            {
                return Rejected(new[] { "OptimizationPolicyUnavailable" },
                    elapsed.ElapsedMilliseconds);
            }

            limits ??= new InventoryExhaustiveSearchLimits();
            long estimatedCandidateLayouts = EstimateCandidateLayouts(
                snapshot, limits.MaximumCandidateLayouts);
            if (estimatedCandidateLayouts > limits.MaximumCandidateLayouts)
            {
                return new InventoryExhaustiveSearchResult(false, null, null,
                    null, estimatedCandidateLayouts, 0,
                    elapsed.ElapsedMilliseconds,
                    InventoryExhaustiveSearchTerminationReason.
                        CandidateLayoutLimit);
            }

            InventoryLayoutProjection current = InventoryLayoutProjection.Current(
                snapshot);
            ProjectedInventorySettlement currentSettlement =
                InventorySettlementProjector.Evaluate(snapshot, current);
            if (!currentSettlement.Succeeded)
            {
                return Rejected(currentSettlement.Issues,
                    elapsed.ElapsedMilliseconds);
            }

            var scorer = new InventoryOptimizationScorer(snapshot, policy);
            var search = new SearchState(snapshot, scorer, limits, elapsed,
                cancellationToken, current,
                scorer.Score(current, currentSettlement));
            search.VisitItem(0);
            return new InventoryExhaustiveSearchResult(true,
                search.BestLayout, search.CurrentScore, search.BestScore,
                estimatedCandidateLayouts, search.CandidateLayoutsEvaluated,
                elapsed.ElapsedMilliseconds,
                search.ElapsedTimeLimitReached
                    ? InventoryExhaustiveSearchTerminationReason.ElapsedTimeLimit
                    : InventoryExhaustiveSearchTerminationReason.
                        SearchSpaceExhausted,
                search.TargetSearchEvidence);
        }

        internal static long EstimateCandidateLayouts(
            InventorySnapshot snapshot, long maximumExactValue = long.MaxValue)
        {
            if (snapshot == null || snapshot.Items.Count > snapshot.Storage)
            {
                return 0;
            }

            long cap = maximumExactValue >= long.MaxValue - 1
                ? long.MaxValue
                : Math.Max(1, maximumExactValue + 1);
            long result = 1;
            for (int itemIndex = 0; itemIndex < snapshot.Items.Count;
                itemIndex++)
            {
                result = MultiplyCapped(result,
                    snapshot.Storage - itemIndex, cap);
                if (result >= cap)
                {
                    return cap;
                }
            }
            foreach (InventoryItemSnapshot item in snapshot.Items)
            {
                if (item.StoneTablet?.Rotatable == true)
                {
                    result = MultiplyCapped(result, 4, cap);
                    if (result >= cap)
                    {
                        return cap;
                    }
                }
            }
            return result;
        }

        private static long MultiplyCapped(long value, long factor, long cap)
        {
            if (value == 0 || factor == 0)
            {
                return 0;
            }
            return value > cap / factor ? cap : value * factor;
        }

        private static InventoryExhaustiveSearchResult Rejected(
            IReadOnlyList<string> issues, long elapsedMilliseconds)
        {
            return new InventoryExhaustiveSearchResult(false, null, null, null,
                0, 0, elapsedMilliseconds,
                InventoryExhaustiveSearchTerminationReason.InputRejected,
                issues: issues?.ToArray());
        }

        private sealed class SearchState
        {
            private readonly InventorySnapshot snapshot;
            private readonly InventoryOptimizationScorer scorer;
            private readonly InventoryExhaustiveSearchLimits limits;
            private readonly Stopwatch elapsed;
            private readonly CancellationToken cancellationToken;
            private readonly int[] cellsByItem;
            private readonly int[] rotationsByItem;
            private readonly bool[] occupiedCells;

            internal SearchState(InventorySnapshot snapshot,
                InventoryOptimizationScorer scorer,
                InventoryExhaustiveSearchLimits limits, Stopwatch elapsed,
                CancellationToken cancellationToken,
                InventoryLayoutProjection current,
                InventoryOptimizationScore currentScore)
            {
                this.snapshot = snapshot;
                this.scorer = scorer;
                this.limits = limits;
                this.elapsed = elapsed;
                this.cancellationToken = cancellationToken;
                cellsByItem = new int[snapshot.Items.Count];
                rotationsByItem = new int[snapshot.Items.Count];
                occupiedCells = new bool[snapshot.Storage];
                CurrentScore = currentScore;
                BestLayout = current;
                BestScore = currentScore;
                TargetSearchEvidence = new Dictionary<string,
                    InventoryTargetSearchEvidence>(StringComparer.Ordinal);
            }

            internal InventoryLayoutProjection BestLayout { get; private set; }
            internal InventoryOptimizationScore CurrentScore { get; }
            internal InventoryOptimizationScore BestScore { get; private set; }
            internal int CandidateLayoutsEvaluated { get; private set; }
            internal bool ElapsedTimeLimitReached { get; private set; }
            internal IDictionary<string, InventoryTargetSearchEvidence>
                TargetSearchEvidence
            { get; }

            internal void VisitItem(int itemIndex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ElapsedTimeLimitReached ||
                    elapsed.ElapsedMilliseconds >=
                        limits.MaximumElapsedMilliseconds)
                {
                    ElapsedTimeLimitReached = true;
                    return;
                }
                if (itemIndex == snapshot.Items.Count)
                {
                    EvaluateCurrentAssignment();
                    return;
                }

                InventoryItemSnapshot item = snapshot.Items[itemIndex];
                int firstRotation = item.StoneTablet?.Rotation ?? 0;
                int rotationCount = item.StoneTablet?.Rotatable == true ? 4 : 1;
                for (int cell = 0; cell < snapshot.Storage &&
                    !ElapsedTimeLimitReached; cell++)
                {
                    if (occupiedCells[cell])
                    {
                        continue;
                    }
                    occupiedCells[cell] = true;
                    cellsByItem[itemIndex] = cell;
                    for (int rotationIndex = 0;
                        rotationIndex < rotationCount &&
                        !ElapsedTimeLimitReached; rotationIndex++)
                    {
                        rotationsByItem[itemIndex] = rotationCount == 1
                            ? firstRotation
                            : rotationIndex;
                        VisitItem(itemIndex + 1);
                    }
                    occupiedCells[cell] = false;
                }
            }

            private void EvaluateCurrentAssignment()
            {
                InventoryLayoutProjection candidate = new(
                    cellsByItem, rotationsByItem);
                ProjectedInventorySettlement settlement =
                    InventorySettlementProjector.Evaluate(snapshot,
                        candidate);
                CandidateLayoutsEvaluated++;
                if (!settlement.Succeeded)
                {
                    return;
                }

                InventoryOptimizationScore score = scorer.Score(candidate,
                    settlement);
                scorer.ObserveTargets(settlement, TargetSearchEvidence);
                int comparison = score.CompareTo(BestScore);
                if (comparison > 0 || comparison == 0 &&
                    candidate.CompareStableTo(BestLayout) < 0)
                {
                    BestLayout = candidate;
                    BestScore = score;
                }
            }
        }
    }
}
