#nullable disable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryIntentDropTarget : MonoBehaviour,
        IDropHandler, IPointerClickHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        private Action<UI_NewInventoryIcon> dropped;
        private Action clicked;
        private Image background;
        private Color normalColor;

        internal void Configure(Image image,
            Action<UI_NewInventoryIcon> onDropped, Action onClicked)
        {
            background = image;
            dropped = onDropped;
            clicked = onClicked;
            normalColor = image != null ? image.color : Color.white;
        }

        public void OnDrop(PointerEventData eventData)
        {
            UI_NewInventoryIcon icon = eventData?.pointerDrag?
                .GetComponentInParent<UI_NewInventoryIcon>();
            if (icon?.Item != null)
            {
                dropped?.Invoke(icon);
            }
            RestoreColor();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData?.button == PointerEventData.InputButton.Right)
            {
                clicked?.Invoke();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background != null && eventData?.pointerDrag != null)
            {
                normalColor = background.color;
                background.color = Color.Lerp(normalColor, Color.white, 0.3f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreColor();
        }

        private void RestoreColor()
        {
            if (background != null)
            {
                background.color = normalColor;
            }
        }
    }
}
