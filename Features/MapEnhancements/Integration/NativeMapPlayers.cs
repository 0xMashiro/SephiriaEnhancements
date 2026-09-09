using System.Collections.Generic;
using UnityEngine;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal static class NativeMapPlayers
    {
        internal static void Refresh(UI_MapPanel panel, NativeMapGeometry geometry,
            Transform parent, Dictionary<PlayerSpawner, RectTransform> icons, bool showCursor)
        {
            var removed = new List<PlayerSpawner>();
            foreach (var pair in icons)
                if (pair.Key == null || !PlayerSpawner.MultiplayerList.Contains(pair.Key))
                {
                    if (pair.Value != null) Object.Destroy(pair.Value.gameObject);
                    removed.Add(pair.Key);
                }
            foreach (var player in removed) icons.Remove(player);

            foreach (PlayerSpawner spawner in PlayerSpawner.MultiplayerList)
            {
                if (spawner == null) continue;
                if (!icons.TryGetValue(spawner, out RectTransform icon) || icon == null)
                {
                    if (panel.playerIconPrefab == null) continue;
                    icon = Object.Instantiate(panel.playerIconPrefab, parent).rectTransform;
                    icons[spawner] = icon;
                }
                var view = icon.GetComponent<UI_MapPanelPlayerIcon>();
                bool multiplayer = PlayerSpawner.MultiplayerList.Count > 1;
                if (multiplayer) view.SetPlayerIdx(spawner.currentPlayerIdx);
                else
                {
                    view.defaultImage.enabled = true;
                    view.multiplayImage.enabled = false;
                }
                PlayerAvatar player = spawner.PlayerAvatar;
                bool visible = showCursor && player != null &&
                    player.currentFloorGuid == geometry.Floor.guid &&
                    geometry.Contains(player.transform.position);
                icon.gameObject.SetActive(visible);
                if (!visible) continue;
                icon.position = geometry.Map.contentsChild.TransformPoint(
                    geometry.Project(player.transform.position));
                icon.SetAsLastSibling();
            }
            foreach (var pair in icons)
                if (pair.Key != null && LocalPlayerResolver.IsLocal(pair.Key.PlayerAvatar) && pair.Value != null)
                    pair.Value.SetAsLastSibling();
        }
    }
}
