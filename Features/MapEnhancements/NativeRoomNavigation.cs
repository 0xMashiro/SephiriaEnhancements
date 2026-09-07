using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    // Keep room navigation inside the native room buttons, separate from map tools.
    internal sealed class NativeRoomNavigation
    {
        private static readonly AccessTools.FieldRef<UI_HorayButton, Selectable>[] Links = {
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavUp"),
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavDown"),
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavLeft"),
            AccessTools.FieldRefAccess<UI_HorayButton, Selectable>("forceNavRight")
        };
        private static readonly Vector2[] Directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        private readonly UI_Map map;
        private readonly Dictionary<Selectable, Navigation> original = new();
        private readonly Dictionary<UI_HorayButton, Selectable[]> originalLinks = new();
        internal NativeRoomNavigation(UI_Map map) { this.map = map; }

        internal GameObject FirstSelection(Vector3 position)
        {
            UI_Map_Room best = null;
            float distance = float.PositiveInfinity;
            foreach (var room in map.rooms)
            {
                if (Button(room) == null) continue;
                if (position.x >= room.bottomLeft.x && position.x <= room.topRight.x &&
                    position.y >= room.bottomLeft.y && position.y <= room.topRight.y)
                    return room.GetSelectable();
                float d = (((Vector2)position) - (room.bottomLeft + room.topRight) / 2).sqrMagnitude;
                if (d < distance) { distance = d; best = room; }
            }
            return best != null ? best.GetSelectable() : null;
        }

        private static Selectable Button(UI_Map_Room room)
        {
            if (!(room is UI_Map_EnhancedProceduralDungeonRoom_Room) &&
                !(room is UI_Map_LibraryProceduralDungeonRoom)) return null;
            var button = room.GetSelectable()?.GetComponent<Selectable>();
            return button != null && button.isActiveAndEnabled && button.IsInteractable() ? button : null;
        }

        internal void Refresh()
        {
            foreach (var room in map.rooms)
            {
                var button = Button(room);
                if (button == null) continue;
                if (!original.ContainsKey(button)) original.Add(button, button.navigation);
                var horay = button as UI_HorayButton;
                if (horay != null && !originalLinks.ContainsKey(horay))
                    originalLinks.Add(horay, new[] { Links[0](horay), Links[1](horay), Links[2](horay), Links[3](horay) });
                var targets = new Selectable[4];
                for (int i = 0; i < 4; i++)
                {
                    targets[i] = button;
                    float best = float.PositiveInfinity;
                    foreach (var other in map.rooms)
                    {
                        var candidate = Button(other);
                        if (candidate == null || candidate == button) continue;
                        Vector2 delta = other.GetIconCenterAnchoredPosition() - room.GetIconCenterAnchoredPosition();
                        float forward = Vector2.Dot(delta, Directions[i]);
                        if (forward <= 0) continue;
                        float score = delta.sqrMagnitude / forward;
                        if (score < best) { best = score; targets[i] = candidate; }
                    }
                    if (horay != null) Links[i](horay) = targets[i];
                }
                button.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = targets[0], selectOnDown = targets[1], selectOnLeft = targets[2], selectOnRight = targets[3] };
            }
        }

        internal void Clear()
        {
            foreach (var pair in original) if (pair.Key != null) pair.Key.navigation = pair.Value;
            foreach (var pair in originalLinks)
                if (pair.Key != null) for (int i = 0; i < 4; i++) Links[i](pair.Key) = pair.Value[i];
            original.Clear(); originalLinks.Clear();
        }
    }
}
