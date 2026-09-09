using UnityEngine;
using UnityEngine.EventSystems;

namespace SephiriaEnhancements.Configuration
{
    // UI_HorizontalSelectionBox_Arrow is a native UI contract whose pointer
    // handler does not consult Selectable.interactable.
    internal sealed class NativeHorizontalSelectionOptionState : MonoBehaviour
    {
        private const float DisabledAlpha = 0.45f;
        private CanvasGroup canvasGroup;
        private float enabledAlpha = 1f;

        internal static void Apply(GameObject row,
            UI_HorizontalSelectionBox box, bool interactive)
        {
            if (row == null || box == null) return;
            NativeHorizontalSelectionOptionState state =
                row.GetComponent<NativeHorizontalSelectionOptionState>();
            if (state == null)
                state = row.AddComponent<NativeHorizontalSelectionOptionState>();
            state.Apply(box, interactive);
        }

        private void Apply(UI_HorizontalSelectionBox box, bool interactive)
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                enabledAlpha = canvasGroup.alpha;
            }

            box.interactable = interactive;
            canvasGroup.alpha = interactive
                ? enabledAlpha : enabledAlpha * DisabledAlpha;
            foreach (UI_HorizontalSelectionBox_Arrow arrow in
                box.GetComponentsInChildren<UI_HorizontalSelectionBox_Arrow>(true))
            {
                arrow.enabled = interactive;
            }
            if (!interactive && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject == box.gameObject)
            {
                var next = box.FindSelectableOnDown() ?? box.FindSelectableOnUp();
                if (next != null) EventSystem.current.SetSelectedGameObject(next.gameObject);
            }
        }
    }
}
