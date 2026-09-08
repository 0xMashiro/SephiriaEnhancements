#nullable disable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SephiriaEnhancements.KeyboardUiNavigation;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryIntentPickupView : IDisposable
    {
        private readonly GameObject cover;
        private readonly Image image;
        private readonly Canvas dragCanvas;
        private readonly Vector3[] corners = new Vector3[4];
        private GameObject pickupFocus;

        internal static bool UsesSelection => KeyboardUiPointer.OwnsFocus ||
            ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse == false;

        internal NativeInventoryIntentPickupView(Canvas panelCanvas, Canvas pickerCanvas,
            Action cancelPickup, Action<int> changePage)
        {
            dragCanvas = pickerCanvas;
            cover = new GameObject("InventoryIntentPickupCover", typeof(RectTransform),
                typeof(Canvas), typeof(GraphicRaycaster), typeof(Image),
                typeof(InventoryIntentPanelDropTarget));
            var rect = (RectTransform)cover.transform;
            rect.SetParent(panelCanvas.rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var canvas = cover.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = panelCanvas.sortingLayerID;
            canvas.sortingOrder = panelCanvas.sortingOrder - 1;
            cover.GetComponent<Image>().color = Color.clear;
            cover.GetComponent<InventoryIntentPanelDropTarget>().Configure(cancelPickup, changePage);

            var visual = new GameObject("InventoryIntentPickup", typeof(RectTransform), typeof(Image));
            visual.transform.SetParent(dragCanvas.transform, false);
            image = visual.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            Hide();
        }

        internal void Show(Sprite sprite, GameObject source)
        {
            pickupFocus = source;
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            image.SetNativeSize();
            image.transform.SetAsLastSibling();
            cover.SetActive(true);
            UpdatePosition();
        }

        internal void UpdateWithinPanel(Transform panel)
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(panel))
                pickupFocus = selected;
            else if (UsesSelection && pickupFocus != null && pickupFocus.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(pickupFocus);
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (!UsesSelection && Mouse.current != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragCanvas.transform as RectTransform, Mouse.current.position.ReadValue(),
                    dragCanvas.worldCamera, out Vector2 point);
                image.transform.position = dragCanvas.transform.TransformPoint(point);
            }
            else if (EventSystem.current?.currentSelectedGameObject?.transform is RectTransform selected)
            {
                var canvas = (RectTransform)dragCanvas.transform;
                selected.GetWorldCorners(corners);
                Vector3 first = canvas.InverseTransformPoint(corners[0]);
                Vector3 last = canvas.InverseTransformPoint(corners[2]);
                var position = InventoryPickupPlacement.BesideSelection(first.x, last.x, (first.y + last.y) * 0.5f,
                    image.rectTransform.rect.width, image.rectTransform.rect.height,
                    canvas.rect.xMin, canvas.rect.xMax, canvas.rect.yMin, canvas.rect.yMax);
                image.transform.position = canvas.TransformPoint(new Vector3(position.X, position.Y, 0f));
            }
        }

        internal void Hide()
        {
            pickupFocus = null;
            if (image != null)
            {
                image.gameObject.SetActive(false);
            }
            if (cover != null)
            {
                cover.SetActive(false);
            }
        }

        public void Dispose()
        {
            Hide();
            if (image != null)
            {
                UnityEngine.Object.Destroy(image.gameObject);
            }
            if (cover != null)
            {
                UnityEngine.Object.Destroy(cover);
            }
        }
    }
}
