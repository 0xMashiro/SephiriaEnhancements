#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizer
    {
        internal static InventoryOptimizationProposal Solve(
            InventorySnapshot snapshot,
            ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget = null,
            CancellationToken cancellationToken = default)
        {
            var elapsed = Stopwatch.StartNew();
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot == null)
            {
                return Failure("SnapshotUnavailable", elapsed.ElapsedMilliseconds);
            }
            if (!snapshot.SettlementValidation.LayoutProjectionReady)
            {
                return new InventoryOptimizationProposal(false, null, null, null,
                    0, snapshot.SettlementValidation?.Issues.ToArray(),
                    elapsedMilliseconds: elapsed.ElapsedMilliseconds);
            }
            if (policy == null)
            {
                return Failure("OptimizationPolicyUnavailable",
                    elapsed.ElapsedMilliseconds);
            }

            budget ??= InventorySearchBudget.ForEffort(policy.SearchEffort);
            var scorer = new InventoryOptimizationScorer(snapshot, policy);
            InventoryLayoutProjection current = InventoryLayoutProjection.Current(
                snapshot);
            ProjectedInventorySettlement currentSettlement =
                InventorySettlementProjector.Evaluate(snapshot, current);
            if (!currentSettlement.Succeeded)
            {
                return new InventoryOptimizationProposal(false, null, null, null,
                    1, currentSettlement.Issues.ToArray(),
                    elapsedMilliseconds: elapsed.ElapsedMilliseconds);
            }

            int candidateEvaluations = 1;
            InventoryOptimizationScore initialScore = scorer.Score(current,
                currentSettlement);
            var evaluatedLayouts = new EvaluatedLayoutSet(snapshot, current);
            InventoryOptimizationScore currentScore = initialScore;
            InventorySearchTerminationReason terminationReason =
                InventorySearchTerminationReason.ImprovementRoundLimit;
            bool searchStopped = false;
            InventoryLayoutProjection bestLayout = current;
            InventoryOptimizationScore bestScore = currentScore;

            for (int round = 0; round < budget.MaximumImprovementRounds;
                round++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bestLayout = current;
                bestScore = currentScore;

                for (int first = 0; first < snapshot.Storage &&
                    !searchStopped; first++)
                {
                    for (int second = first + 1; second < snapshot.Storage;
                        second++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        InventoryLayoutProjection candidate =
                            current.WithCellsSwapped(first, second);
                        if (candidate.ContentEquals(current))
                        {
                            continue;
                        }
                        if (!TryPromote(snapshot, scorer, candidate,
                                evaluatedLayouts, budget,
                                elapsed,
                                cancellationToken, ref candidateEvaluations,
                                ref bestLayout, ref bestScore,
                                out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                    }
                }

                for (int itemIndex = 0; policy.AllowStoneTabletRotation &&
                    itemIndex < snapshot.Items.Count && !searchStopped;
                    itemIndex++)
                {
                    StoneTabletSnapshot stoneTablet =
                        snapshot.Items[itemIndex].StoneTablet;
                    if (stoneTablet == null || !stoneTablet.Rotatable)
                    {
                        continue;
                    }
                    for (int rotation = 0; rotation < 4; rotation++)
                    {
                        if (rotation == current.GetRotation(itemIndex))
                        {
                            continue;
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        InventoryLayoutProjection candidate =
                            current.WithRotation(itemIndex, rotation);
                        if (!TryPromote(snapshot, scorer, candidate,
                                evaluatedLayouts, budget,
                                elapsed,
                                cancellationToken,
                                ref candidateEvaluations, ref bestLayout,
                                ref bestScore, out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                    }
                }

                if (searchStopped)
                {
                    break;
                }
                if (bestScore.CompareTo(currentScore) <= 0)
                {
                    if (!TrySearchSwapAndStoneTabletRotationNeighborhood(
                            snapshot, scorer,
                            current, policy.AllowStoneTabletRotation,
                            evaluatedLayouts, budget,
                            elapsed, cancellationToken,
                            ref candidateEvaluations, ref bestLayout,
                            ref bestScore, out terminationReason))
                    {
                        searchStopped = true;
                        break;
                    }
                }
                if (bestScore.CompareTo(currentScore) <= 0)
                {
                    if (!TrySearchTwoSwapNeighborhood(snapshot, scorer,
                            current, evaluatedLayouts, budget, elapsed,
                            cancellationToken,
                            ref candidateEvaluations, ref bestLayout,
                            ref bestScore, out terminationReason))
                    {
                        searchStopped = true;
                        break;
                    }
                    int neighborhoodComparison = bestScore.CompareTo(
                        currentScore);
                    if (neighborhoodComparison < 0 ||
                        neighborhoodComparison == 0 &&
                        bestLayout.ContentEquals(current))
                    {
                        if (!TrySearchTwoItemRelocationAndTabletRotation(
                                snapshot, scorer, current,
                                policy.AllowStoneTabletRotation,
                                evaluatedLayouts, budget, elapsed,
                                cancellationToken, ref candidateEvaluations,
                                ref bestLayout, ref bestScore,
                                out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                        neighborhoodComparison = bestScore.CompareTo(
                            currentScore);
                    }
                    if (neighborhoodComparison < 0 ||
                        neighborhoodComparison == 0 &&
                        bestLayout.ContentEquals(current))
                    {
                        if (!TrySearchThreeItemRelocationNeighborhood(snapshot,
                                scorer, current, evaluatedLayouts, budget,
                                elapsed, cancellationToken,
                                ref candidateEvaluations, ref bestLayout,
                                ref bestScore, out terminationReason))
                        {
                            searchStopped = true;
                            break;
                        }
                        neighborhoodComparison = bestScore.CompareTo(
                            currentScore);
                        if (neighborhoodComparison < 0 ||
                            neighborhoodComparison == 0 &&
                            bestLayout.ContentEquals(current))
                        {
                            terminationReason = InventorySearchTerminationReason.
                                NeighborhoodLocalOptimum;
                            break;
                        }
                    }

                    // Equal-score layouts can expose a better neighborhood on
                    // the next round. Stable ordering makes this plateau walk
                    // strictly monotonic, while the shared evaluation and time
                    // budgets keep it bounded.
                }
                current = bestLayout;
                currentScore = bestScore;
            }

            // A budget can expire partway through any neighborhood. Keep the
            // best verified candidate even when that round could not finish.
            if (bestScore.CompareTo(currentScore) > 0)
            {
                current = bestLayout;
                currentScore = bestScore;
            }

            ProjectedInventorySettlement bestSettlement =
                InventorySettlementProjector.Evaluate(snapshot, current);
            InventoryOptimizationOutcome outcome =
                InventoryOptimizationOutcomeBuilder.Build(snapshot,
                    currentSettlement, bestSettlement, initialScore,
                    currentScore);
            return new InventoryOptimizationProposal(true, current, initialScore,
                currentScore, candidateEvaluations, Array.Empty<string>(), policy,
                scorer.EvaluateTargets(currentSettlement, bestSettlement),
                terminationReason, elapsed.ElapsedMilliseconds,
                duplicateLayoutsSkipped:
                    evaluatedLayouts.DuplicateLayoutsSkipped,
                outcome: outcome);
        }

        private static bool TrySearchSwapAndStoneTabletRotationNeighborhood(
            InventorySnapshot snapshot, InventoryOptimizationScorer scorer,
            InventoryLayoutProjection current, bool allowStoneTabletRotation,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget, Stopwatch elapsed,
            CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            // Score the combined result because a placement projection can make
            // both its setup move and its rotation neutral when tried alone.
            // TryPromote keeps this larger neighborhood inside the shared wall-
            // clock and candidate-evaluation budget.
            terminationReason = InventorySearchTerminationReason.
                ImprovementRoundLimit;
            if (!allowStoneTabletRotation)
            {
                return true;
            }

            for (int firstCell = 0; firstCell < snapshot.Storage; firstCell++)
            {
                for (int secondCell = firstCell + 1;
                    secondCell < snapshot.Storage; secondCell++)
                {
                    InventoryLayoutProjection afterSwap =
                        current.WithCellsSwapped(firstCell, secondCell);
                    if (afterSwap.ContentEquals(current))
                    {
                        continue;
                    }
                    for (int itemIndex = 0;
                        itemIndex < snapshot.Items.Count; itemIndex++)
                    {
                        StoneTabletSnapshot stoneTablet =
                            snapshot.Items[itemIndex].StoneTablet;
                        if (stoneTablet == null || !stoneTablet.Rotatable)
                        {
                            continue;
                        }
                        for (int rotation = 0; rotation < 4; rotation++)
                        {
                            if (rotation == afterSwap.GetRotation(itemIndex))
                            {
                                continue;
                            }
                            InventoryLayoutProjection candidate =
                                afterSwap.WithRotation(itemIndex, rotation);
                            if (!TryPromote(snapshot, scorer, candidate,
                                    evaluatedLayouts, budget,
                                    elapsed, cancellationToken,
                                    ref candidateEvaluations, ref bestLayout,
                                    ref bestScore, out terminationReason))
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool TrySearchTwoItemRelocationAndTabletRotation(
            InventorySnapshot snapshot, InventoryOptimizationScorer scorer,
            InventoryLayoutProjection current, bool allowStoneTabletRotation,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget,
            Stopwatch elapsed, CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            terminationReason = InventorySearchTerminationReason.
                ImprovementRoundLimit;
            if (!allowStoneTabletRotation || current.ItemCount < 2)
            {
                return true;
            }

            var occupiedByUnselectedItem = new bool[snapshot.Storage];
            for (int firstItem = 0; firstItem < current.ItemCount - 1;
                firstItem++)
            {
                for (int secondItem = firstItem + 1;
                    secondItem < current.ItemCount; secondItem++)
                {
                    Array.Clear(occupiedByUnselectedItem, 0,
                        occupiedByUnselectedItem.Length);
                    for (int itemIndex = 0; itemIndex < current.ItemCount;
                        itemIndex++)
                    {
                        if (itemIndex != firstItem && itemIndex != secondItem)
                        {
                            occupiedByUnselectedItem[
                                current.GetCell(itemIndex)] = true;
                        }
                    }

                    for (int firstCell = 0; firstCell < snapshot.Storage;
                        firstCell++)
                    {
                        if (occupiedByUnselectedItem[firstCell] ||
                            firstCell == current.GetCell(firstItem))
                        {
                            continue;
                        }
                        for (int secondCell = 0;
                            secondCell < snapshot.Storage; secondCell++)
                        {
                            if (secondCell == firstCell ||
                                occupiedByUnselectedItem[secondCell] ||
                                secondCell == current.GetCell(secondItem))
                            {
                                continue;
                            }
                            InventoryLayoutProjection relocated = current.
                                WithTwoItemCells(firstItem, firstCell,
                                    secondItem, secondCell);
                            for (int tabletItem = 0;
                                tabletItem < current.ItemCount; tabletItem++)
                            {
                                StoneTabletSnapshot tablet = snapshot.Items[
                                    tabletItem].StoneTablet;
                                if (tablet == null || !tablet.Rotatable)
                                {
                                    continue;
                                }
                                for (int rotation = 0; rotation < 4;
                                    rotation++)
                                {
                                    if (rotation == current.GetRotation(
                                            tabletItem))
                                    {
                                        continue;
                                    }
                                    InventoryLayoutProjection candidate =
                                        relocated.WithRotation(tabletItem,
                                            rotation);
                                    if (!TryPromote(snapshot, scorer, candidate,
                                            evaluatedLayouts, budget, elapsed,
                                            cancellationToken,
                                            ref candidateEvaluations,
                                            ref bestLayout, ref bestScore,
                                            out terminationReason))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool TrySearchThreeItemRelocationNeighborhood(
            InventorySnapshot snapshot, InventoryOptimizationScorer scorer,
            InventoryLayoutProjection current,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget,
            Stopwatch elapsed, CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            // Some placement conditions require three artifacts to move as one
            // unit. Search those relocations only after the smaller
            // neighborhoods stall; the shared budgets cap the worst case.
            terminationReason = InventorySearchTerminationReason.
                ImprovementRoundLimit;
            if (current.ItemCount < 3)
            {
                return true;
            }

            var occupiedByUnselectedItem = new bool[snapshot.Storage];
            for (int firstItem = 0; firstItem < current.ItemCount - 2;
                firstItem++)
            {
                for (int secondItem = firstItem + 1;
                    secondItem < current.ItemCount - 1; secondItem++)
                {
                    for (int thirdItem = secondItem + 1;
                        thirdItem < current.ItemCount; thirdItem++)
                    {
                        Array.Clear(occupiedByUnselectedItem, 0,
                            occupiedByUnselectedItem.Length);
                        for (int itemIndex = 0;
                            itemIndex < current.ItemCount; itemIndex++)
                        {
                            if (itemIndex != firstItem &&
                                itemIndex != secondItem &&
                                itemIndex != thirdItem)
                            {
                                occupiedByUnselectedItem[
                                    current.GetCell(itemIndex)] = true;
                            }
                        }

                        for (int firstCell = 0;
                            firstCell < snapshot.Storage; firstCell++)
                        {
                            if (occupiedByUnselectedItem[firstCell] ||
                                firstCell == current.GetCell(firstItem))
                            {
                                continue;
                            }
                            for (int secondCell = 0;
                                secondCell < snapshot.Storage; secondCell++)
                            {
                                if (secondCell == firstCell ||
                                    occupiedByUnselectedItem[secondCell] ||
                                    secondCell == current.GetCell(secondItem))
                                {
                                    continue;
                                }
                                for (int thirdCell = 0;
                                    thirdCell < snapshot.Storage; thirdCell++)
                                {
                                    if (thirdCell == firstCell ||
                                        thirdCell == secondCell ||
                                        occupiedByUnselectedItem[thirdCell] ||
                                        thirdCell == current.GetCell(thirdItem))
                                    {
                                        continue;
                                    }
                                    InventoryLayoutProjection candidate = current.
                                        WithThreeItemCells(firstItem, firstCell,
                                            secondItem, secondCell, thirdItem,
                                            thirdCell);
                                    if (!TryPromote(snapshot, scorer, candidate,
                                            evaluatedLayouts, budget, elapsed,
                                            cancellationToken,
                                            ref candidateEvaluations,
                                            ref bestLayout, ref bestScore,
                                            out terminationReason))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool TrySearchTwoSwapNeighborhood(
            InventorySnapshot snapshot, InventoryOptimizationScorer scorer,
            InventoryLayoutProjection current,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget,
            Stopwatch elapsed, CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            // Two swaps produce either a three-cell cycle or two disjoint
            // transpositions. Enumerating those final permutations directly
            // avoids evaluating the same layout from multiple swap orders.
            // Three-cell cycles come first because they cover adjacency setup
            // moves with the smaller neighborhood.
            terminationReason = InventorySearchTerminationReason.
                ImprovementRoundLimit;
            var occupiedCells = new bool[snapshot.Storage];
            for (int itemIndex = 0; itemIndex < current.ItemCount; itemIndex++)
            {
                occupiedCells[current.GetCell(itemIndex)] = true;
            }
            for (int first = 0; first < snapshot.Storage; first++)
            {
                for (int second = first + 1;
                    second < snapshot.Storage; second++)
                {
                    for (int third = second + 1;
                        third < snapshot.Storage; third++)
                    {
                        int occupiedCount = (occupiedCells[first] ? 1 : 0) +
                            (occupiedCells[second] ? 1 : 0) +
                            (occupiedCells[third] ? 1 : 0);
                        if (occupiedCount < 2)
                        {
                            continue;
                        }
                        InventoryLayoutProjection forward = current
                            .WithCellsSwapped(first, second)
                            .WithCellsSwapped(second, third);
                        if (!TryPromoteDistinctLayout(snapshot, scorer, current,
                                forward, evaluatedLayouts, budget, elapsed,
                                cancellationToken,
                                ref candidateEvaluations, ref bestLayout,
                                ref bestScore, out terminationReason))
                        {
                            return false;
                        }

                        InventoryLayoutProjection reverse = current
                            .WithCellsSwapped(first, third)
                            .WithCellsSwapped(second, third);
                        if (!TryPromoteDistinctLayout(snapshot, scorer, current,
                                reverse, evaluatedLayouts, budget, elapsed,
                                cancellationToken,
                                ref candidateEvaluations, ref bestLayout,
                                ref bestScore, out terminationReason))
                        {
                            return false;
                        }
                    }
                }
            }

            for (int first = 0; first < snapshot.Storage; first++)
            {
                for (int second = first + 1;
                    second < snapshot.Storage; second++)
                {
                    for (int third = second + 1;
                        third < snapshot.Storage; third++)
                    {
                        for (int fourth = third + 1;
                            fourth < snapshot.Storage; fourth++)
                        {
                            if ((occupiedCells[first] || occupiedCells[second]) &&
                                (occupiedCells[third] || occupiedCells[fourth]))
                            {
                                InventoryLayoutProjection adjacentPairs = current
                                    .WithCellsSwapped(first, second)
                                    .WithCellsSwapped(third, fourth);
                                if (!TryPromoteDistinctLayout(snapshot, scorer,
                                        current, adjacentPairs,
                                        evaluatedLayouts, budget, elapsed,
                                        cancellationToken,
                                        ref candidateEvaluations,
                                        ref bestLayout, ref bestScore,
                                        out terminationReason))
                                {
                                    return false;
                                }
                            }

                            if ((occupiedCells[first] || occupiedCells[third]) &&
                                (occupiedCells[second] || occupiedCells[fourth]))
                            {
                                InventoryLayoutProjection outerPairs = current
                                    .WithCellsSwapped(first, third)
                                    .WithCellsSwapped(second, fourth);
                                if (!TryPromoteDistinctLayout(snapshot, scorer,
                                        current, outerPairs, evaluatedLayouts,
                                        budget, elapsed,
                                        cancellationToken,
                                        ref candidateEvaluations,
                                        ref bestLayout, ref bestScore,
                                        out terminationReason))
                                {
                                    return false;
                                }
                            }

                            if ((occupiedCells[first] || occupiedCells[fourth]) &&
                                (occupiedCells[second] || occupiedCells[third]))
                            {
                                InventoryLayoutProjection crossedPairs = current
                                    .WithCellsSwapped(first, fourth)
                                    .WithCellsSwapped(second, third);
                                if (!TryPromoteDistinctLayout(snapshot, scorer,
                                        current, crossedPairs, evaluatedLayouts,
                                        budget, elapsed,
                                        cancellationToken,
                                        ref candidateEvaluations,
                                        ref bestLayout, ref bestScore,
                                        out terminationReason))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private static bool TryPromoteDistinctLayout(
            InventorySnapshot snapshot, InventoryOptimizationScorer scorer,
            InventoryLayoutProjection current,
            InventoryLayoutProjection candidate,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget,
            Stopwatch elapsed, CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            if (candidate.ContentEquals(current))
            {
                terminationReason = InventorySearchTerminationReason.
                    ImprovementRoundLimit;
                return true;
            }
            return TryPromote(snapshot, scorer, candidate, evaluatedLayouts,
                budget, elapsed, cancellationToken, ref candidateEvaluations,
                ref bestLayout, ref bestScore, out terminationReason);
        }

        private static bool TryPromote(InventorySnapshot snapshot,
            InventoryOptimizationScorer scorer,
            InventoryLayoutProjection candidate,
            EvaluatedLayoutSet evaluatedLayouts,
            InventorySearchBudget budget,
            Stopwatch elapsed,
            CancellationToken cancellationToken,
            ref int candidateEvaluations,
            ref InventoryLayoutProjection bestLayout,
            ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason terminationReason)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (candidateEvaluations >= budget.MaximumCandidateEvaluations)
            {
                terminationReason = InventorySearchTerminationReason.
                    CandidateEvaluationLimit;
                return false;
            }
            if (budget.UseElapsedTimeLimit && elapsed.ElapsedMilliseconds >= budget.MaximumElapsedMilliseconds)
            {
                terminationReason = InventorySearchTerminationReason.
                    ElapsedTimeLimit;
                return false;
            }
            if (!evaluatedLayouts.TryAdd(candidate))
            {
                terminationReason = InventorySearchTerminationReason.
                    ImprovementRoundLimit;
                return true;
            }

            terminationReason = InventorySearchTerminationReason.
                ImprovementRoundLimit;
            ProjectedInventorySettlement settlement =
                InventorySettlementProjector.EvaluateForScoring(
                    snapshot, candidate, evaluatedLayouts.EvaluationWorkspace);
            candidateEvaluations++;
            if (!settlement.Succeeded)
            {
                return true;
            }

            InventoryOptimizationScore score = scorer.Score(candidate, settlement);
            int comparison = score.CompareTo(bestScore);
            if (comparison > 0 || comparison == 0 &&
                candidate.CompareStableTo(bestLayout) < 0)
            {
                bestLayout = candidate;
                bestScore = score;
            }
            return true;
        }

        private sealed class EvaluatedLayoutSet
        {
            private readonly HashSet<InventoryLayoutProjection> layouts = new(
                CandidateLayoutContentComparer.Instance);

            internal EvaluatedLayoutSet(InventorySnapshot snapshot,
                InventoryLayoutProjection current)
            {
                EvaluationWorkspace = new InventorySettlementProjectionWorkspace(
                    snapshot.Storage);
                layouts.Add(current);
            }

            internal int DuplicateLayoutsSkipped { get; private set; }
            internal InventorySettlementProjectionWorkspace EvaluationWorkspace
            { get; }

            internal bool TryAdd(InventoryLayoutProjection layout)
            {
                if (layouts.Add(layout))
                {
                    return true;
                }
                DuplicateLayoutsSkipped++;
                return false;
            }
        }

        private sealed class CandidateLayoutContentComparer :
            IEqualityComparer<InventoryLayoutProjection>
        {
            internal static readonly CandidateLayoutContentComparer Instance =
                new();

            public bool Equals(InventoryLayoutProjection first,
                InventoryLayoutProjection second)
            {
                return ReferenceEquals(first, second) ||
                    first != null && first.ContentEquals(second);
            }

            public int GetHashCode(InventoryLayoutProjection layout)
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + layout.ItemCount;
                    for (int index = 0; index < layout.ItemCount; index++)
                    {
                        hash = hash * 31 + layout.GetCell(index);
                        hash = hash * 31 + layout.GetRotation(index);
                    }
                    return hash;
                }
            }
        }

        private static InventoryOptimizationProposal Failure(string issue,
            long elapsedMilliseconds)
        {
            return new InventoryOptimizationProposal(false, null, null, null, 0,
                new[] { issue }, elapsedMilliseconds: elapsedMilliseconds);
        }
    }

}
