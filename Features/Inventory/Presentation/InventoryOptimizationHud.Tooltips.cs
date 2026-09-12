using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Inventory
{
    internal sealed partial class InventoryOptimizationHud
    {
        private readonly Vector3[] tooltipCorners = new Vector3[4];

        internal void PositionTooltip(UI_BaseTooltip tooltip)
        {
            if (!panelOpen || !NavigationAvailable || tooltip?.Target == null ||
                tooltip.tooltipRoot == null) return;
            var nativeOwner = tooltip.Target as UI_NewInventoryIcon;
            var goalOwner = tooltip.Target as NativeInventoryArtifactTooltip;
            var preferenceOwner = tooltip.Target as UI_CommonTooltipOpener;
            if (nativeOwner == null && goalOwner == null && preferenceOwner == null ||
                nativeOwner != null && !nativeOwner.transform.IsChildOf(attachedInventoryZone) ||
                goalOwner != null && !goalOwner.transform.IsChildOf(root.transform) ||
                preferenceOwner != null && !preferenceOwner.transform.IsChildOf(root.transform)) return;

            // Leaving an inventory item for a board button must not leave its
            // old tooltip over the controls. Goal slots own their own tooltip.
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (nativeOwner != null && IsCustomSelection(selected))
            {
                tooltip.Close();
                return;
            }
            RectTransform canvas = tooltip.tooltipRoot;
            Rect tip = Bounds(tooltip.rectTransform, canvas);
            Rect board = Bounds((RectTransform)root.transform, canvas);
            if (!tip.Overlaps(board)) return;
            Rect inventory = Bounds(attachedInventoryZone, canvas);
            float gap = 8f;
            float x = inventory.xMin - tip.width - gap;
            if (x < canvas.rect.xMin) x = board.xMin - tip.width - gap;
            if (x < canvas.rect.xMin) x = board.xMax + gap;
            x = Mathf.Clamp(x, canvas.rect.xMin, Mathf.Max(canvas.rect.xMin, canvas.rect.xMax - tip.width));
            tooltip.rectTransform.position += canvas.TransformVector(new Vector3(x - tip.xMin, 0f));
        }

        private Rect Bounds(RectTransform target, RectTransform canvas)
        {
            target.GetWorldCorners(tooltipCorners);
            Vector3 first = canvas.InverseTransformPoint(tooltipCorners[0]);
            Vector3 last = canvas.InverseTransformPoint(tooltipCorners[2]);
            return Rect.MinMaxRect(first.x, first.y, last.x, last.y);
        }
    }
}
