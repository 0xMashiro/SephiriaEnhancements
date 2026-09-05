#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal sealed class InventoryLayoutProjection
    {
        private readonly int[] cellsByItem;
        private readonly int[] rotationsByItem;

        internal InventoryLayoutProjection(int[] cellsByItem,
            int[] rotationsByItem)
            : this(cellsByItem, rotationsByItem, copyArrays: true)
        {
        }

        private InventoryLayoutProjection(int[] cellsByItem,
            int[] rotationsByItem, bool copyArrays)
        {
            this.cellsByItem = cellsByItem == null
                ? Array.Empty<int>()
                : copyArrays ? (int[])cellsByItem.Clone() : cellsByItem;
            this.rotationsByItem = rotationsByItem == null
                ? Array.Empty<int>()
                : copyArrays ? (int[])rotationsByItem.Clone() : rotationsByItem;
        }

        internal int ItemCount => cellsByItem.Length;
        internal int GetCell(int itemIndex) => cellsByItem[itemIndex];
        internal int GetRotation(int itemIndex) => rotationsByItem[itemIndex];
        internal int[] CopyCells() => (int[])cellsByItem.Clone();
        internal int[] CopyRotations() => (int[])rotationsByItem.Clone();

        internal InventoryLayoutProjection WithCellsSwapped(int firstCell,
            int secondCell)
        {
            int[] cells = CopyCells();
            for (int index = 0; index < cells.Length; index++)
            {
                if (cells[index] == firstCell)
                {
                    cells[index] = secondCell;
                }
                else if (cells[index] == secondCell)
                {
                    cells[index] = firstCell;
                }
            }
            return new InventoryLayoutProjection(cells, rotationsByItem,
                copyArrays: false);
        }

        internal InventoryLayoutProjection WithRotation(int itemIndex,
            int rotation)
        {
            int[] rotations = CopyRotations();
            rotations[itemIndex] = rotation;
            return new InventoryLayoutProjection(cellsByItem, rotations,
                copyArrays: false);
        }

        internal InventoryLayoutProjection WithThreeItemCells(int firstItem,
            int firstCell, int secondItem, int secondCell, int thirdItem,
            int thirdCell)
        {
            int[] cells = CopyCells();
            cells[firstItem] = firstCell;
            cells[secondItem] = secondCell;
            cells[thirdItem] = thirdCell;
            return new InventoryLayoutProjection(cells, rotationsByItem,
                copyArrays: false);
        }

        internal InventoryLayoutProjection WithTwoItemCells(int firstItem,
            int firstCell, int secondItem, int secondCell)
        {
            int[] cells = CopyCells();
            cells[firstItem] = firstCell;
            cells[secondItem] = secondCell;
            return new InventoryLayoutProjection(cells, rotationsByItem,
                copyArrays: false);
        }

        internal bool ContentEquals(InventoryLayoutProjection other) =>
            CompareStableTo(other) == 0;

        internal int CompareStableTo(InventoryLayoutProjection other)
        {
            if (other == null)
            {
                return 1;
            }
            int comparison = cellsByItem.Length.CompareTo(
                other.cellsByItem.Length);
            if (comparison != 0)
            {
                return comparison;
            }
            for (int index = 0; index < cellsByItem.Length; index++)
            {
                comparison = cellsByItem[index].CompareTo(
                    other.cellsByItem[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            comparison = rotationsByItem.Length.CompareTo(
                other.rotationsByItem.Length);
            if (comparison != 0)
            {
                return comparison;
            }
            for (int index = 0; index < rotationsByItem.Length; index++)
            {
                comparison = rotationsByItem[index].CompareTo(
                    other.rotationsByItem[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }
            return 0;
        }

        internal static InventoryLayoutProjection Current(
            InventorySnapshot snapshot)
        {
            int count = snapshot?.Items.Count ?? 0;
            var cells = new int[count];
            var rotations = new int[count];
            for (int index = 0; index < count; index++)
            {
                cells[index] = snapshot.Items[index].CellIndex;
                rotations[index] = snapshot.Items[index].StoneTablet?.Rotation ??
                    0;
            }
            return new InventoryLayoutProjection(cells, rotations,
                copyArrays: false);
        }
    }

    internal sealed class ProjectedInventoryCellSettlement
    {
        internal ProjectedInventoryCellSettlement(int level, int maximumLevel,
            int temporaryLevel, int levelMultiplier, int disableCount,
            int criteriaBypassCount)
        {
            Level = level;
            MaximumLevel = maximumLevel;
            TemporaryLevel = temporaryLevel;
            LevelMultiplier = levelMultiplier;
            DisableCount = disableCount;
            CriteriaBypassCount = criteriaBypassCount;
        }

        internal int Level { get; }
        internal int MaximumLevel { get; }
        internal int TemporaryLevel { get; }
        internal int LevelMultiplier { get; }
        internal int DisableCount { get; }
        internal int CriteriaBypassCount { get; }
    }

    internal sealed class ProjectedInventoryTabletSettlement
    {
        internal ProjectedInventoryTabletSettlement(InventoryItemKey itemKey, bool fixedSource,
            bool applied, int cellIndex, int rotation)
        {
            ItemKey = itemKey;
            FixedSource = fixedSource;
            Applied = applied;
            CellIndex = cellIndex;
            Rotation = rotation;
        }

        internal InventoryItemKey ItemKey { get; }
        internal bool FixedSource { get; }
        internal bool Applied { get; }
        internal int CellIndex { get; }
        internal int Rotation { get; }
    }

    internal sealed class ProjectedInventoryArtifactSettlement
    {
        internal ProjectedInventoryArtifactSettlement(InventoryItemKey itemKey, bool enabled,
            bool penaltyEnabled, int displayedLevel,
            int cappedEffectiveLevel)
        {
            ItemKey = itemKey;
            Enabled = enabled;
            PenaltyEnabled = penaltyEnabled;
            DisplayedLevel = displayedLevel;
            CappedEffectiveLevel = cappedEffectiveLevel;
        }

        internal InventoryItemKey ItemKey { get; }
        internal bool Enabled { get; }
        internal bool PenaltyEnabled { get; }
        internal int DisplayedLevel { get; }
        internal int CappedEffectiveLevel { get; }
    }

    internal sealed class ProjectedInventorySettlement
    {
        internal ProjectedInventorySettlement(bool succeeded,
            ProjectedInventoryCellSettlement[] cells,
            ProjectedInventoryArtifactSettlement[] artifacts,
            IDictionary<string, int> comboCounts, string[] issues,
            ProjectedInventoryTabletSettlement[] tablets = null,
            InventoryPositionEffectValue[] positionEffects = null)
        {
            Succeeded = succeeded;
            Cells = Array.AsReadOnly(cells ??
                Array.Empty<ProjectedInventoryCellSettlement>());
            Artifacts = Array.AsReadOnly(artifacts ??
                Array.Empty<ProjectedInventoryArtifactSettlement>());
            ComboCounts = new System.Collections.ObjectModel.
                ReadOnlyDictionary<string, int>(
                    new Dictionary<string, int>(comboCounts ??
                        new Dictionary<string, int>(), StringComparer.Ordinal));
            Tablets = Array.AsReadOnly(tablets ??
                Array.Empty<ProjectedInventoryTabletSettlement>());
            Issues = Array.AsReadOnly(issues ?? Array.Empty<string>());
            PositionEffects = Array.AsReadOnly(positionEffects ?? Array.Empty<InventoryPositionEffectValue>());
        }

        internal bool Succeeded { get; }
        internal IReadOnlyList<ProjectedInventoryCellSettlement> Cells { get; }
        internal IReadOnlyList<ProjectedInventoryArtifactSettlement> Artifacts { get; }
        internal IReadOnlyDictionary<string, int> ComboCounts { get; }
        internal IReadOnlyList<ProjectedInventoryTabletSettlement> Tablets { get; }
        internal IReadOnlyList<InventoryPositionEffectValue> PositionEffects { get; }
        internal IReadOnlyList<string> Issues { get; }
    }

    internal sealed class InventorySettlementProjectionWorkspace
    {
        internal InventorySettlementProjectionWorkspace(InventorySnapshot snapshot)
        {
            int storage = snapshot.Storage;
            ItemAtCell = new int[storage];
            AdditiveLevels = new int[storage];
            Multipliers = new int[storage];
            Disables = new int[storage];
            Bypasses = new int[storage];
            MaximumLevels = new int[storage];
            TemporaryLevels = new int[storage];
            ProjectedCategories = new IReadOnlyList<string>[snapshot.Items.Count];
            RowCategoryChoices = new IReadOnlyList<string>[snapshot.Items.Count][];
            for (int index = 0; index < snapshot.Items.Count; index++)
            {
                ArtifactCategoryRuleSnapshot rule = snapshot.Items[index].Artifact?.CategoryRule;
                if (rule?.Kind != ArtifactCategoryRuleKind.RowModulo) continue;
                var choices = new IReadOnlyList<string>[rule.RowCategories.Count];
                for (int row = 0; row < choices.Length; row++) choices[row] = new[] { rule.RowCategories[row] };
                RowCategoryChoices[index] = choices;
            }
            if (snapshot.PositionEffects.Rules.Count != 0)
                PositionEffectProjector = new InventoryPositionEffectProjector(snapshot);
            // Static categories count inventory membership, not activation or level.
            // Keep this cache local to the immutable input owned by this workspace.
            if (snapshot.Items.All(item => item.Artifact == null ||
                    item.Artifact.CategoryRule.Kind == ArtifactCategoryRuleKind.Static))
                StaticComboCounts = InventorySettlementProjector.CountCombos(snapshot,
                    InventoryLayoutProjection.Current(snapshot), ItemAtCell);
        }

        internal Dictionary<string, int> StaticComboCounts { get; }
        internal InventoryPositionEffectProjector PositionEffectProjector { get; }
        internal int[] ItemAtCell { get; }
        internal int[] AdditiveLevels { get; }
        internal int[] Multipliers { get; }
        internal int[] Disables { get; }
        internal int[] Bypasses { get; }
        internal int[] MaximumLevels { get; }
        internal int[] TemporaryLevels { get; }
        internal List<string> Issues { get; } = new();
        internal List<ProjectedInventoryArtifactSettlement> Artifacts { get; } = new();
        internal Dictionary<string, int> ComboCounts { get; } = new(
            StringComparer.Ordinal);
        internal HashSet<int> SeenComboEntities { get; } = new();
        internal IReadOnlyList<string>[] ProjectedCategories { get; }
        internal IReadOnlyList<string>[][] RowCategoryChoices { get; }
        internal HashSet<int> ResolvingCategories { get; } = new();
        internal Dictionary<string, int> NeighborCategoryCounts { get; } = new(StringComparer.Ordinal);
    }
}
