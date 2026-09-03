#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryCandidateNeighborhoods
    {
        internal static IEnumerable<InventoryLayoutProjection> Simple(
            InventorySnapshot snapshot, InventoryLayoutProjection current, bool rotate)
        {
            for (int first = 0; first < snapshot.Storage; first++)
                for (int second = first + 1; second < snapshot.Storage; second++)
                {
                    var candidate = current.WithCellsSwapped(first, second);
                    if (!candidate.ContentEquals(current)) yield return candidate;
                }
            for (int item = 0; rotate && item < snapshot.Items.Count; item++)
            {
                var tablet = snapshot.Items[item].StoneTablet;
                if (tablet == null || !tablet.Rotatable) continue;
                for (int rotation = 0; rotation < 4; rotation++)
                    if (rotation != current.GetRotation(item)) yield return current.WithRotation(item, rotation);
            }
        }

        internal static IEnumerable<InventoryLayoutProjection> SwapAndRotation(InventorySnapshot snapshot, InventoryLayoutProjection current, bool allowStoneTabletRotation)
        {
            // Score the combined result because a placement projection can make
            // both its setup move and its rotation neutral when tried alone.
            if (!allowStoneTabletRotation)
            {
                yield break;
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
                            yield return candidate;

                        }
                    }
                }
            }
            yield break;
        }

        internal static IEnumerable<InventoryLayoutProjection> TwoItemRelocationAndRotation(InventorySnapshot snapshot, InventoryLayoutProjection current, bool allowStoneTabletRotation)
        {
            if (!allowStoneTabletRotation || current.ItemCount < 2)
            {
                yield break;
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
                                    yield return candidate;
                                }
                            }
                        }
                    }
                }
            }
            yield break;
        }

        internal static IEnumerable<InventoryLayoutProjection> ThreeItemRelocation(InventorySnapshot snapshot, InventoryLayoutProjection current)
        {
            // Some placement conditions require three artifacts to move as one
            // unit. Search those relocations only after the smaller
            // neighborhoods stall; the shared budgets cap the worst case.
            if (current.ItemCount < 3)
            {
                yield break;
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
                                    yield return candidate;
                                }
                            }
                        }
                    }
                }
            }
            yield break;
        }

        internal static IEnumerable<InventoryLayoutProjection> TwoSwaps(InventorySnapshot snapshot, InventoryLayoutProjection current)
        {
            // Two swaps produce either a three-cell cycle or two disjoint
            // transpositions. Enumerating those final permutations directly
            // avoids evaluating the same layout from multiple swap orders.
            // Three-cell cycles come first because they cover adjacency setup
            // moves with the smaller neighborhood.
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
                        if (!forward.ContentEquals(current)) yield return forward;


                        InventoryLayoutProjection reverse = current
                            .WithCellsSwapped(first, third)
                            .WithCellsSwapped(second, third);
                        if (!reverse.ContentEquals(current)) yield return reverse;

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
                                if (!adjacentPairs.ContentEquals(current)) yield return adjacentPairs;

                            }

                            if ((occupiedCells[first] || occupiedCells[third]) &&
                                (occupiedCells[second] || occupiedCells[fourth]))
                            {
                                InventoryLayoutProjection outerPairs = current
                                    .WithCellsSwapped(first, third)
                                    .WithCellsSwapped(second, fourth);
                                if (!outerPairs.ContentEquals(current)) yield return outerPairs;

                            }

                            if ((occupiedCells[first] || occupiedCells[fourth]) &&
                                (occupiedCells[second] || occupiedCells[third]))
                            {
                                InventoryLayoutProjection crossedPairs = current
                                    .WithCellsSwapped(first, fourth)
                                    .WithCellsSwapped(second, third);
                                if (!crossedPairs.ContentEquals(current)) yield return crossedPairs;

                            }
                        }
                    }
                }
            }
            yield break;
        }
    }
}
