using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapSelectionFrame : MaskableGraphic
    {
        internal static readonly Color Paper = new Color(1f, .94f, .77f, 1f);
        private bool cornersOnly;

        internal static MapSelectionFrame Create(Transform parent, Color ink, bool corners = false)
        {
            var frame = new GameObject("Selection Frame", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(MapSelectionFrame)).GetComponent<MapSelectionFrame>();
            frame.rectTransform.SetParent(parent, false);
            frame.rectTransform.anchorMin = Vector2.zero;
            frame.rectTransform.anchorMax = Vector2.one;
            frame.rectTransform.offsetMin = frame.rectTransform.offsetMax = Vector2.zero;
            frame.color = ink;
            frame.raycastTarget = false;
            frame.cornersOnly = corners;
            return frame;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            DrawFrame(mesh, rect, cornersOnly ? 3f : 1f, color);
            if (cornersOnly)
            {
                rect.xMin += 1; rect.xMax -= 1;
                rect.yMin += 1; rect.yMax -= 1;
                DrawFrame(mesh, rect, 1f, Paper);
            }
        }

        private void DrawFrame(VertexHelper mesh, Rect rect, float thickness, Color tint)
        {
            float horizontal = cornersOnly ? Mathf.Min(7, rect.width / 2) : rect.width / 2;
            float vertical = cornersOnly ? Mathf.Min(7, rect.height / 2) : rect.height / 2;
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            {
                float left = x == 0 ? rect.xMin : rect.xMax - horizontal;
                float bottom = y == 0 ? rect.yMin : rect.yMax - thickness;
                AddRect(mesh, new Rect(left, bottom, horizontal, thickness), tint);
                left = x == 0 ? rect.xMin : rect.xMax - thickness;
                bottom = y == 0 ? rect.yMin + thickness : rect.yMax - vertical;
                AddRect(mesh, new Rect(left, bottom, thickness, vertical - thickness), tint);
            }
        }

        private static void AddRect(VertexHelper mesh, Rect rect, Color tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
