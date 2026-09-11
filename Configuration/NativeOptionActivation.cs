using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Configuration
{
    internal sealed class NativeOptionActivation : MonoBehaviour, IPointerClickHandler, ISubmitHandler
    {
        private Action activate;
        internal void Configure(Action action) => activate = action;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit != null && hit.GetComponentInParent<UI_HorizontalSelectionBox_Arrow>() != null) return;
            Activate();
        }

        public void OnSubmit(BaseEventData eventData) => Activate();

        private void Activate()
        {
            var box = GetComponent<UI_HorizontalSelectionBox>();
            if (box != null && box.IsInteractable()) activate?.Invoke();
        }
    }
}
