using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class NpcTracking
    {
        private UnitAI_NewBasic target;
        private PlayerAvatar owner;
        private string floorGuid;
        private UI_EnemyViewerElement arrow;

        internal Transform Target => target != null ? target.transform : null;

        internal void Toggle(Transform selected)
        {
            if (Target == selected) { Clear(); return; }
            var npc = selected != null ? selected.GetComponent<UnitAI_NewBasic>() : null;
            var player = LocalPlayerResolver.Resolve();
            if (npc == null || npc.Avatar == null || npc.Avatar.IsDead ||
                !npc.gameObject.activeInHierarchy || string.IsNullOrEmpty(npc.socialID) || player == null) return;
            Clear();
            target = npc; owner = player; floorGuid = player.currentFloorGuid;
        }

        internal void Update()
        {
            if (floorGuid == null) return;
            var player = LocalPlayerResolver.Resolve();
            if (target == null || target.Avatar == null || target.Avatar.IsDead ||
                !target.gameObject.activeInHierarchy || player == null || player != owner ||
                player.currentFloorGuid != floorGuid || player.IsDead)
            {
                Clear(); return;
            }
            var camera = GameCamera.Instance;
            bool show = camera != null && camera.Observer == player && player.loadingScreenType == -1 &&
                !player.IsInBattle && UIManager.Instance != null && UIManager.Instance.CurrentControlStack == null &&
                ScreenFader.Instance != null && !ScreenFader.Instance.IsFading;
            if (!show) { if (arrow != null) arrow.gameObject.SetActive(false); return; }
            Vector3 position = target.transform.position;
            Vector3 center = camera.transform.position;
            show = NpcTrackingDirection.TryPlace(position.x, position.y, center.x, center.y,
                camera.Camera.orthographicSize * camera.Camera.aspect, camera.Camera.orthographicSize, out var edge);
            if (!show) { if (arrow != null) arrow.gameObject.SetActive(false); return; }
            if (arrow == null)
            {
                // Reuse the native enemy edge-indicator prefab, without registering
                // a friendly NPC in the enemy viewer or changing its faction flags.
                var viewer = UIManager.Instance.GetElement<UI_EnemyViewer>();
                if (viewer == null || viewer.enemyCursorPrefab == null) return;
                arrow = Object.Instantiate(viewer.enemyCursorPrefab, viewer.contentParent);
                arrow.image.raycastTarget = false;
            }
            arrow.transform.position = new Vector3(edge.X, edge.Y, position.z);
            arrow.image.transform.eulerAngles = new Vector3(0, 0,
                HorayUtility.GetAngle(position, player.transform.position));
            arrow.gameObject.SetActive(true);
        }

        internal void Clear()
        {
            if (arrow != null) { arrow.gameObject.SetActive(false); Object.Destroy(arrow.gameObject); }
            arrow = null; target = null; owner = null; floorGuid = null;
        }
    }
}
