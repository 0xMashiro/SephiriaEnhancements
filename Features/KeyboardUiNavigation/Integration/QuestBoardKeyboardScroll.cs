using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.KeyboardUiNavigation
{
    internal static class QuestBoardKeyboardScroll
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        internal static void Update()
        {
            GameObject selected = KeyboardUiPointer.SelectedTarget();
            if (selected == null) return;

            // Native board events are nested below a stage and the board's grid,
            // rather than being direct children of the world-map scroll content.
            var card = selected.GetComponentInParent<UI_NewWorldMapStageBoardEvent>();
            if (card == null) return;
            var panel = card.GetComponentInParent<UI_NewWorldMapPanel>();
            if (!KeyboardUiSelection.IsInPanel(panel, selected)) return;
            ScrollRect scroll = panel.scrollRect;
            if (scroll == null || !scroll.isActiveAndEnabled || !scroll.vertical ||
                scroll.content == null || !card.transform.IsChildOf(scroll.content)) return;
            RectTransform viewport = scroll.viewport != null
                ? scroll.viewport : (RectTransform)scroll.transform;

            GetVerticalBounds(card.rectTransform, viewport, out float bottom, out float top);
            float offset = KeyboardSelectionScroll.VerticalOffset(bottom, top,
                viewport.rect.yMin, viewport.rect.yMax);
            if (Mathf.Abs(offset) < 0.01f) return;

            GetVerticalBounds(scroll.content, viewport, out float contentBottom, out float contentTop);
            float scrollableHeight = contentTop - contentBottom - viewport.rect.height;
            if (scrollableHeight <= 0f) return;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01(
                scroll.verticalNormalizedPosition - offset / scrollableHeight);
        }

        private static void GetVerticalBounds(RectTransform target, RectTransform viewport,
            out float bottom, out float top)
        {
            target.GetWorldCorners(Corners);
            bottom = float.PositiveInfinity;
            top = float.NegativeInfinity;
            foreach (Vector3 corner in Corners)
            {
                float y = viewport.InverseTransformPoint(corner).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
        }
    }
}
