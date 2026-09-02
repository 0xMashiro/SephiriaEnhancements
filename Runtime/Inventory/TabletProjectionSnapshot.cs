#nullable disable

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal enum TabletCriteriaKind
    {
        Unknown,
        AnyItem,
        Artifact,
        Placed
    }

    internal enum TabletEffectKind
    {
        Unknown,
        IncreaseLevel,
        Disable,
        IgnoreCriteria,
        MultiplyLevel
    }

    internal sealed class TabletAdditionSnapshot
    {
        internal TabletAdditionSnapshot(int x, int y, string nativeValue,
            bool validCell, bool xWorldPosition, bool yWorldPosition,
            bool borderTop, bool borderRight, bool borderBottom, bool borderLeft,
            TabletCriteriaKind criteriaKind = TabletCriteriaKind.Unknown,
            TabletEffectKind effectKind = TabletEffectKind.Unknown,
            int levelParameter = 0)
        {
            X = x;
            Y = y;
            NativeValue = nativeValue ?? string.Empty;
            ValidCell = validCell;
            XWorldPosition = xWorldPosition;
            YWorldPosition = yWorldPosition;
            BorderTop = borderTop;
            BorderRight = borderRight;
            BorderBottom = borderBottom;
            BorderLeft = borderLeft;
            CriteriaKind = criteriaKind;
            EffectKind = effectKind;
            LevelParameter = levelParameter;
        }

        internal int X { get; }
        internal int Y { get; }
        internal string NativeValue { get; }
        internal bool ValidCell { get; }
        internal bool XWorldPosition { get; }
        internal bool YWorldPosition { get; }
        internal bool BorderTop { get; }
        internal bool BorderRight { get; }
        internal bool BorderBottom { get; }
        internal bool BorderLeft { get; }
        internal TabletCriteriaKind CriteriaKind { get; }
        internal TabletEffectKind EffectKind { get; }
        internal int LevelParameter { get; }
    }

    internal sealed class TabletRotationProjectionSnapshot
    {
        internal TabletRotationProjectionSnapshot(int rotation,
            TabletAdditionSnapshot[] criteria, TabletAdditionSnapshot[] effects,
            bool parseSucceeded, string parseIssue = null)
        {
            Rotation = rotation;
            Criteria = Array.AsReadOnly(criteria == null
                ? Array.Empty<TabletAdditionSnapshot>()
                : (TabletAdditionSnapshot[])criteria.Clone());
            Effects = Array.AsReadOnly(effects == null
                ? Array.Empty<TabletAdditionSnapshot>()
                : (TabletAdditionSnapshot[])effects.Clone());
            ParseSucceeded = parseSucceeded;
            ParseIssue = parseIssue ?? string.Empty;
        }

        internal int Rotation { get; }
        internal IReadOnlyList<TabletAdditionSnapshot> Criteria { get; }
        internal IReadOnlyList<TabletAdditionSnapshot> Effects { get; }
        internal bool ParseSucceeded { get; }
        internal string ParseIssue { get; }
    }

    internal sealed class TabletPlacementProjectionSnapshot
    {
        internal TabletPlacementProjectionSnapshot(int cellIndex, int x, int y,
            TabletRotationProjectionSnapshot[] rotations)
        {
            CellIndex = cellIndex;
            X = x;
            Y = y;
            Rotations = Array.AsReadOnly(rotations == null
                ? Array.Empty<TabletRotationProjectionSnapshot>()
                : (TabletRotationProjectionSnapshot[])rotations.Clone());
        }

        internal int CellIndex { get; }
        internal int X { get; }
        internal int Y { get; }
        internal IReadOnlyList<TabletRotationProjectionSnapshot> Rotations
        { get; }

        internal TabletRotationProjectionSnapshot FindRotation(int rotation)
        {
            for (int index = 0; index < Rotations.Count; index++)
            {
                if (Rotations[index].Rotation == rotation)
                {
                    return Rotations[index];
                }
            }
            return null;
        }
    }

    internal sealed class FixedTabletSourceSnapshot
    {
        internal FixedTabletSourceSnapshot(int instanceId, int entityId,
            int cellIndex, int rotation, bool applied,
            TabletRotationProjectionSnapshot projection)
        {
            InstanceId = instanceId;
            EntityId = entityId;
            CellIndex = cellIndex;
            Rotation = rotation;
            Applied = applied;
            Projection = projection;
        }

        internal InventoryItemKey ItemKey => new(EntityId, InstanceId);
        internal int InstanceId { get; }
        internal int EntityId { get; }
        internal int CellIndex { get; }
        internal int Rotation { get; }
        internal bool Applied { get; }
        internal TabletRotationProjectionSnapshot Projection { get; }
    }
}
