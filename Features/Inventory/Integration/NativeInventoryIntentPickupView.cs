#nullable disable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryIntentPickupView : IDisposable
    {
        private readonly GameObject cover;
        private readonly Image image;
        private readonly Canvas dragCanvas;

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

        internal void Show(Sprite sprite)
        {
            image.sprite = sprite;
            image.gameObject.SetActive(true);
            image.SetNativeSize();
            image.transform.SetAsLastSibling();
            cover.SetActive(true);
            UpdatePosition();
        }

        internal void UpdatePosition()
        {
            // Match the native controller picker: pointer position for mouse,
            // selected control plus a 20-unit vertical offset for controller.
            if (ControlsChangeHandler.Current?.IsUsingKeyboardAndMouse != false && Mouse.current != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragCanvas.transform as RectTransform, Mouse.current.position.ReadValue(),
                    dragCanvas.worldCamera, out Vector2 point);
                image.transform.position = dragCanvas.transform.TransformPoint(point);
            }
            else if (EventSystem.current?.currentSelectedGameObject != null)
            {
                image.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
                image.rectTransform.anchoredPosition += Vector2.up * 20f;
            }
        }

        internal void Hide()
        {
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
