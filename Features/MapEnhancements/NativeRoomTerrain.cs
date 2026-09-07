using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    // Borrows generated HUD data, but owns only the recolored display textures.
    internal sealed class NativeRoomTerrain
    {
        private readonly List<(UI_Map_Room Room, RawImage View, Texture2D Texture)> tiles = new();

        internal NativeRoomTerrain(NativeMapGeometry geometry, RectTransform root, Color ink)
        {
            // Other room generators already have their own topology/terrain presentation.
            if (!(geometry.Floor is FixedFloorGenerator)) return;
            var data = geometry.Floor.CreateHUDMapUI();
            if (data == null) return;
            foreach (var tile in data)
            {
                if (tile.sprite == null) continue;
                UI_Map_Room owner = null;
                Vector2 center = (tile.bottomLeft + tile.topRight) * 0.5f;
                foreach (var room in geometry.Map.rooms)
                    if (room != null && center.x >= room.bottomLeft.x && center.x <= room.topRight.x &&
                        center.y >= room.bottomLeft.y && center.y <= room.topRight.y)
                    { owner = room; break; }
                if (owner == null) continue;
                Color[] pixels = tile.sprite.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color source = pixels[i];
                    pixels[i] = new Color(ink.r, ink.g, ink.b,
                        ink.a * source.a * (source.r > 0.9f ? 0.6f : 0.14f));
                }
                var texture = new Texture2D(tile.sprite.width, tile.sprite.height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.SetPixels(pixels);
                texture.Apply();
                var view = new GameObject("Room Terrain", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                view.rectTransform.SetParent(root, false);
                view.rectTransform.anchoredPosition = owner.GetIconCenterAnchoredPosition();
                view.rectTransform.sizeDelta = owner.GetRoomIconSize();
                view.texture = texture;
                view.raycastTarget = false;
                view.transform.SetAsFirstSibling();
                tiles.Add((owner, view, texture));
            }
            Refresh();
        }

        internal void Refresh()
        {
            foreach (var tile in tiles)
                tile.View.gameObject.SetActive(tile.Room != null && tile.Room.gameObject.activeSelf);
        }

        internal void Clear()
        {
            foreach (var tile in tiles)
            {
                Object.Destroy(tile.View.gameObject);
                Object.Destroy(tile.Texture);
            }
            tiles.Clear();
        }
    }
}
