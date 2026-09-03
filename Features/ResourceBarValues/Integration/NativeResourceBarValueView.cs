using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    internal sealed class NativeResourceBarValueView : MonoBehaviour
    {
        private static readonly HashSet<NativeResourceBarValueView> Views =
            new HashSet<NativeResourceBarValueView>();
        private readonly List<Label> labels = new List<Label>();
        private float nextRefresh;
        internal Action<bool> RefreshLayout;
        internal Action RestoreLayout;

        internal static NativeResourceBarValueView GetOrAdd(Component owner)
        {
            var view = owner.GetComponent<NativeResourceBarValueView>();
            if (view == null) view = owner.gameObject.AddComponent<NativeResourceBarValueView>();
            Views.Add(view);
            return view;
        }

        internal void Clear()
        {
            foreach (Label label in labels) label.Dispose();
            labels.Clear();
            RestoreLayout?.Invoke();
            RestoreLayout = null;
            RefreshLayout = null;
            nextRefresh = 0f;
        }

        internal TextMeshProUGUI Add(RectTransform parent, TextMeshProUGUI template,
            Func<string> read, Func<float> size, string name)
        {
            var label = new Label(parent, template, read, size, name);
            labels.Add(label);
            return label.Text;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.1f;
            bool enabled = EnhancementsSettings.Enabled;
            foreach (Label label in labels) label.Refresh(enabled);
            RefreshLayout?.Invoke(enabled);
        }

        private void OnDestroy()
        {
            Views.Remove(this);
            Clear();
        }

        internal static void DisposeAll()
        {
            foreach (var view in new List<NativeResourceBarValueView>(Views))
            {
                if (view == null) continue;
                view.Clear();
                Destroy(view);
            }
            Views.Clear();
        }

        private sealed class Label
        {
            internal readonly TextMeshProUGUI Text;
            private readonly Func<string> read;
            private readonly Func<float> size;

            internal Label(RectTransform parent, TextMeshProUGUI template,
                Func<string> read, Func<float> size, string name)
            {
                this.read = read;
                this.size = size;
                var root = new GameObject(name, typeof(RectTransform),
                    typeof(LayoutElement), typeof(TextMeshProUGUI));
                root.SetActive(false);
                var rect = root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                root.GetComponent<LayoutElement>().ignoreLayout = true;
                Text = root.GetComponent<TextMeshProUGUI>();
                Text.font = template.font;
                Text.fontSharedMaterial = template.fontSharedMaterial;
                Text.fontStyle = FontStyles.Normal;
                Text.color = Color.white;
                Text.raycastTarget = false;
                // Some native bars are inside a six-pixel mask. The label uses the
                // full bar width and follows its visibility, without clipping glyphs.
                Text.maskable = false;
                Text.textWrappingMode = TextWrappingModes.NoWrap;
                Text.overflowMode = TextOverflowModes.Overflow;
                Text.alignment = TextAlignmentOptions.Center;
                NativeLocalizedText.BindFont(Text, template);
            }

            internal void Refresh(bool enabled)
            {
                if (Text == null) return;
                string value = enabled ? read() : string.Empty;
                bool visible = !string.IsNullOrEmpty(value);
                if (Text.gameObject.activeSelf != visible) Text.gameObject.SetActive(visible);
                if (!visible) return;
                float designSize = size();
                NativeLocalizedText.SetShrinkOnlySize(Text, designSize, designSize * 0.8f);
                if (Text.text != value) Text.text = value;
            }

            internal void Dispose()
            {
                if (Text == null) return;
                Text.gameObject.SetActive(false);
                Destroy(Text.gameObject);
            }
        }
    }
}
