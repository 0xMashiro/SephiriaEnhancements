#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal enum ArtifactCategoryRuleKind
    {
        Static,
        RowModulo,
        DependencyTarget,
        NeighborMatch
    }

    internal sealed class InventoryOffsetSnapshot
    {
        internal InventoryOffsetSnapshot(int x, int y)
        {
            X = x;
            Y = y;
        }

        internal int X { get; }
        internal int Y { get; }
    }

    internal sealed class ArtifactCategoryRuleSnapshot
    {
        internal ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind kind,
            string[] rowCategories = null, int targetX = 0, int targetY = 0,
            InventoryOffsetSnapshot[] neighborOffsets = null, int match = 0)
        {
            Kind = kind;
            RowCategories = Array.AsReadOnly(rowCategories == null
                ? Array.Empty<string>()
                : (string[])rowCategories.Clone());
            TargetX = targetX;
            TargetY = targetY;
            NeighborOffsets = Array.AsReadOnly(neighborOffsets == null
                ? Array.Empty<InventoryOffsetSnapshot>()
                : (InventoryOffsetSnapshot[])neighborOffsets.Clone());
            Match = match;
        }

        internal ArtifactCategoryRuleKind Kind { get; }
        internal IReadOnlyList<string> RowCategories { get; }
        internal int TargetX { get; }
        internal int TargetY { get; }
        internal IReadOnlyList<InventoryOffsetSnapshot> NeighborOffsets { get; }
        internal int Match { get; }

        internal static ArtifactCategoryRuleSnapshot Static { get; } =
            new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.Static);
    }
}
