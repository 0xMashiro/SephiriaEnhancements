#nullable disable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class NativeInventoryArtifactTooltip : MonoBehaviour,
        IUITooltipOpener, ISelectHandler, IDeselectHandler
    {
        private NewItemOwnInstance item;
        private Func<bool> hasPickup;

        internal void Configure(Func<bool> pickupActive) => hasPickup = pickupActive;

        public bool Showing { get; set; }
        public UI_BaseTooltip LastTooltip { get; set; }

        internal void SetItem(NewItemOwnInstance value)
        {
            if (ReferenceEquals(item, value))
            {
                return;
            }
            Hide();
            item = value;
            if (EventSystem.current?.currentSelectedGameObject == gameObject)
            {
                Show();
            }
        }

        public void OnSelect(BaseEventData eventData) => Show();

        public void OnDeselect(BaseEventData eventData) => Hide();

        private void OnDisable() => Hide();

        private void Show()
        {
            if (item?.Charm == null || NativeInventoryIntentDrop.HasHeldItem ||
                hasPickup?.Invoke() == true)
            {
                return;
            }
            RectTransform rect = transform as RectTransform;
            // Use the actual artifact instance and native tooltip renderer.
            // Do not enable the native throw/sell guides on a preference slot.
            UIManager.Instance?.GetElement<UI_CharmTooltip>()?.Open(
                this, rect, rect.rect.size * 0.5f, item);
        }

        internal void Hide()
        {
            Showing = false;
            if (LastTooltip != null && ReferenceEquals(LastTooltip.Target, this))
            {
                LastTooltip.Close();
            }
        }
    }
}
