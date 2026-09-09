using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SephiriaEnhancements.Configuration
{
    internal static class NativeOptionsLifetime
    {
        private static readonly List<GameObject> Objects = new List<GameObject>();
        private static readonly HashSet<UI_OptionsPanel> Panels = new HashSet<UI_OptionsPanel>();

        internal static void Track(GameObject value) => Objects.Add(value);
        internal static void Track(UI_OptionsPanel panel) => Panels.Add(panel);

        internal static void DisposeAll()
        {
            foreach (UI_OptionsPanel panel in Panels)
            {
                if (panel == null) continue;
                OptionsNavigationState navigation = panel.GetComponent<OptionsNavigationState>();
                if (navigation != null)
                {
                    navigation.Restore();
                    Object.DestroyImmediate(navigation);
                }
                OptionsCategoryController categories = panel.GetComponent<OptionsCategoryController>();
                if (categories != null) Object.DestroyImmediate(categories);
            }
            Panels.Clear();

            foreach (GameObject value in Objects)
            {
                if (value == null) continue;
                GameObject selected = EventSystem.current?.currentSelectedGameObject;
                if (selected != null &&
                    (selected == value || selected.transform.IsChildOf(value.transform)))
                    EventSystem.current.SetSelectedGameObject(null);
                value.SetActive(false);
                Object.DestroyImmediate(value);
            }
            Objects.Clear();
        }
    }

    internal sealed class OptionsNavigationState : MonoBehaviour
    {
        private UI_HorizontalSelectionBox source;
        private Selectable originalDownstreamUp;
        internal Selectable OriginalDown { get; private set; }

        internal void Capture(UI_HorizontalSelectionBox box)
        {
            source = box;
            OriginalDown = box.forceNavDown;
            if (OriginalDown is UI_HorizontalSelectionBox downstream)
                originalDownstreamUp = downstream.forceNavUp;
        }

        internal void Restore()
        {
            if (source != null) source.forceNavDown = OriginalDown;
            if (OriginalDown is UI_HorizontalSelectionBox downstream && downstream != null)
                downstream.forceNavUp = originalDownstreamUp;
        }
    }
}
