using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SephiriaEnhancements.MapEnhancements
{
    internal sealed class MapButtonView : MonoBehaviour, ISelectHandler
    {
        internal Action Selected;
        private UI_HorayButton button;
        private Image background, selectionMark;
        private MapSelectionFrame focusFrame;

        internal void Initialize(UI_HorayButton owner, Color ink)
        {
            button = owner;
            background = owner.GetComponent<Image>();
            focusFrame = MapSelectionFrame.Create(transform, ink);
            var mark = new GameObject("Selection Mark", typeof(RectTransform), typeof(Image));
            selectionMark = mark.GetComponent<Image>();
            selectionMark.raycastTarget = false;
            var rect = selectionMark.rectTransform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, .5f);
            rect.offsetMin = new Vector2(2, 4);
            rect.offsetMax = new Vector2(4, -4);
            Refresh(ink, false, false);
        }

        internal void Refresh(Color ink, bool selected, bool focused)
        {
            Color tint = ink;
            tint.a = selected ? .85f : focused ? .16f : .06f;
            background.color = tint;
            button.text.color = selected ? MapSelectionFrame.Paper : ink;
            if (!button.interactable)
            {
                tint = ink;
                tint.a = .45f;
                button.text.color = tint;
            }
            selectionMark.color = MapSelectionFrame.Paper;
            selectionMark.gameObject.SetActive(selected);
            focusFrame.color = selected ? MapSelectionFrame.Paper : ink;
            focusFrame.gameObject.SetActive(focused);
        }

        public void OnSelect(BaseEventData eventData) => Selected?.Invoke();
    }
}
