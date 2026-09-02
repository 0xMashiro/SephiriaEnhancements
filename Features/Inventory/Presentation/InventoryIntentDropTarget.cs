#nullable disable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryIntentDropTarget : MonoBehaviour,
        IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler,
        IEndDragHandler
    {
        private InventoryIntentInteractionState interaction;
        private Action<UI_NewInventoryIcon> inventoryDropped;
        private Action intentDropped;
        private Action beginDrag;
        private Action endDrag;
        private Action removed;

        internal void Configure(InventoryIntentInteractionState state,
            Action<UI_NewInventoryIcon> onInventoryDropped, Action onIntentDropped,
            Action onBeginDrag, Action onEndDrag, Action onRemoved)
        {
            interaction = state;
            inventoryDropped = onInventoryDropped;
            intentDropped = onIntentDropped;
            beginDrag = onBeginDrag;
            endDrag = onEndDrag;
            removed = onRemoved;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData?.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            if (interaction?.Editable == true)
            {
                var source = eventData.pointerDrag?.GetComponent<InventoryIntentDropTarget>();
                if (source != null && ReferenceEquals(source.interaction, interaction) &&
                    interaction.IsDragging)
                {
                    intentDropped?.Invoke();
                }
                else
                {
                    UI_NewInventoryIcon native = eventData.pointerDrag?
                        .GetComponentInParent<UI_NewInventoryIcon>();
                    if (native?.Item != null)
                    {
                        inventoryDropped?.Invoke(native);
                    }
                }
            }
            NativeInventoryIntentDrop.Consume(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (interaction?.Editable == true &&
                eventData?.button == PointerEventData.InputButton.Right)
            {
                removed?.Invoke();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData?.button == PointerEventData.InputButton.Left)
            {
                beginDrag?.Invoke();
            }
        }

        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData) => endDrag?.Invoke();
    }

    internal sealed class InventoryIntentPanelDropTarget : MonoBehaviour,
        IDropHandler, IPointerClickHandler, IScrollHandler
    {
        private Action cancelPickup;
        private Action<int> changePage;
        private bool cancelOnLeftClick;

        internal void Configure(Action cancel, Action<int> page, bool cancelOnLeft = true)
        {
            cancelPickup = cancel;
            changePage = page;
            cancelOnLeftClick = cancelOnLeft;
        }

        public void OnDrop(PointerEventData eventData)
        {
            cancelPickup?.Invoke();
            NativeInventoryIntentDrop.Consume(eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData?.button == PointerEventData.InputButton.Right ||
                cancelOnLeftClick && eventData?.button == PointerEventData.InputButton.Left)
            {
                cancelPickup?.Invoke();
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData.scrollDelta.y != 0)
            {
                changePage?.Invoke(eventData.scrollDelta.y > 0 ? -1 : 1);
            }
        }
    }
}
