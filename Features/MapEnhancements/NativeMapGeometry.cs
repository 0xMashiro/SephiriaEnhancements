using UnityEngine;
using UnityEngine.UI;
using SephiriaEnhancements.MapEnhancements.Core;

namespace SephiriaEnhancements.MapEnhancements
{
    // Converts native world bounds to the layout owned by the current map.
    internal sealed class NativeMapGeometry
    {
        internal readonly FloorGenerator Floor;
        internal readonly UI_Map Map;
        internal FullyDesignedFloorGenerator Designed => Floor as FullyDesignedFloorGenerator;
        internal NativeMapGeometry(FloorGenerator floor, UI_Map map) { Floor = floor; Map = map; }

        internal UI_Map_Room RoomAt(Vector3 position)
        {
            foreach (var room in Map.rooms)
                if (room != null && room.gameObject.activeSelf &&
                    position.x >= room.bottomLeft.x && position.x <= room.topRight.x &&
                    position.y >= room.bottomLeft.y && position.y <= room.topRight.y)
                    return room;
            return null;
        }

        internal bool Contains(Vector3 position)
        {
            if (Designed == null) return RoomAt(position) != null;
            Vector3 origin = Floor.transform.position;
            return FixedFloorMapProjection.Contains(position.x, position.y, origin.x, origin.y,
                Designed.bottomLeft.x, Designed.bottomLeft.y, Designed.topRight.x, Designed.topRight.y);
        }

        internal Vector2 Project(Vector3 position)
        {
            if (Designed != null)
            {
                Vector3 origin = Floor.transform.position;
                var point = FixedFloorMapProjection.Project(position.x, position.y, origin.x, origin.y,
                    Designed.mapScale, Designed.mapOffset.x, Designed.mapOffset.y);
                return new Vector2(point.X, point.Y);
            }
            var room = RoomAt(position);
            if (room == null) return new Vector2(-9999, -9999);
            Vector2 size = room.topRight - room.bottomLeft;
            Vector2 relative = (Vector2)position - (room.bottomLeft + room.topRight) * 0.5f;
            Vector2 icon = room.GetRoomIconSize();
            // Library room textures include three pixels of padding on each side.
            if (room is UI_Map_LibraryProceduralDungeonRoom) icon -= new Vector2(6, 6);
            return room.GetIconCenterAnchoredPosition() + new Vector2(
                size.x > 0 ? relative.x / size.x * icon.x : 0,
                size.y > 0 ? relative.y / size.y * icon.y : 0);
        }

        internal bool TryGetRoomDestination(Vector3 target, out UI_Map_Room room)
        {
            room = null;
            if (Designed != null || !Contains(target)) return false;
            room = RoomAt(target);
            if (!(room is UI_Map_EnhancedProceduralDungeonRoom_Room) &&
                !(room is UI_Map_LibraryProceduralDungeonRoom)) return false;
            var button = room.GetSelectable()?.GetComponent<Selectable>();
            return button == null || button.isActiveAndEnabled && button.IsInteractable();
        }
    }
}
