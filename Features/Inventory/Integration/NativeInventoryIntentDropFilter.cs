#nullable disable
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryIntentDropFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private RectTransform panel;

        internal void Bind(RectTransform value) => panel = value;

        internal bool Contains(Vector2 screenPoint, Camera eventCamera) =>
            panel != null && panel.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(panel, screenPoint, eventCamera);

        // The native red drop zone must not receive pointer-down or drop
        // events inside the goal editor, even while its raycasts are enabled.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) =>
            !Contains(screenPoint, eventCamera);
    }

    [HarmonyPatch(typeof(UI_DraggableTempItemIcon), nameof(UI_DraggableTempItemIcon.OnEndDrag))]
    internal static class InventoryTemporaryItemDropPatch
    {
        private static void Prefix(UI_DraggableTempItemIcon __instance, PointerEventData eventData)
        {
            NativeInventoryIntentDropFilter filter = __instance.Panel?.itemDropZone?
                .GetComponent<NativeInventoryIntentDropFilter>();
            if (filter != null && filter.Contains(eventData.position, eventData.pressEventCamera))
            {
                // Native OnEndDrag restores its captured position on cancel;
                // do not move or remove the underlying temporary-storage item.
                __instance.cancelDrag = true;
            }
        }
    }
}
