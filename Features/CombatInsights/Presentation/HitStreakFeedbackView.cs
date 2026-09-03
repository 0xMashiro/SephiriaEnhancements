using System;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Integration;
using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.Presentation
{
    internal sealed class HitStreakFeedbackView : IDisposable
    {
        private static readonly Color Gold = new Color(1f, 0.76f, 0.24f, 1f);
        private static readonly Color HotGold = new Color(1f, 0.46f, 0.12f, 1f);
        private static readonly Color Execution = new Color(1f, 0.28f, 0.56f, 1f);
        private Transform worldRoot;
        private Counter counter;

        internal void Show(UnitAvatar target, Vector2 position, HitStreakUpdate update,
            HitStreakImpact impact, Color sourceColor)
        {
            if (!TryAttach())
            {
                return;
            }

            Color color = ResolveColor(update, impact, sourceColor);
            float scale = 0.84f + Mathf.Min(update.Tier, 4) * 0.04f;
            Vector2 offset = new Vector2(0.85f, 0.9f);
            counter.Show(update.Count, color, target, position + offset, Time.unscaledTime, scale,
                update.ShouldAnimate, update.IsMilestone);
        }

        internal void Update(bool visible)
        {
            if (worldRoot == null)
            {
                Hide();
                return;
            }

            counter?.Update(Time.unscaledTime, visible);
        }

        internal void Hide() => counter?.Hide();

        public void Dispose()
        {
            counter?.Dispose();
            counter = null;
            worldRoot = null;
        }

        private bool TryAttach()
        {
            UIManager manager = UIManager.Instance;
            Transform currentRoot = manager?.GetRootFromType(EUIObjectPoolingParent.World)?.transform;
            if (currentRoot == null)
            {
                return false;
            }

            if (worldRoot == currentRoot && counter != null)
            {
                return true;
            }

            Dispose();
            worldRoot = currentRoot;
            TextMeshProUGUI template = currentRoot
                .GetComponentInChildren<UI_DamageParticle>(true)
                ?.GetComponent<TextMeshProUGUI>();
            template ??= manager.GetElement<UI_PlayerMP>()?.mpBar?.valueText;
            if (template == null || template.font == null)
            {
                worldRoot = null;
                return false;
            }

            Vector2 size = template.rectTransform.sizeDelta;
            if (size.x < 1f || size.y < 1f)
            {
                size = new Vector2(180f, 42f);
            }
            counter = new Counter(worldRoot, template, size);
            return true;
        }

        private static Color ResolveColor(HitStreakUpdate update, HitStreakImpact impact,
            Color sourceColor)
        {
            if (impact == HitStreakImpact.Execution) return Execution;
            if (update.IsMilestone)
                return Color.Lerp(update.Tier >= 3 ? HotGold : Gold, Color.white, 0.2f);
            if (impact == HitStreakImpact.Critical) return Gold;
            if (update.Tier >= 3) return HotGold;
            sourceColor.a = 1f;
            return Color.Lerp(sourceColor, Color.white, 0.25f);
        }

        private sealed class Counter : IDisposable
        {
            private const float FadeDelay = 1.24f;
            private const float Lifetime = 1.6f;
            private const float FollowSmoothTime = 0.08f;
            private const float MinimumTargetHold = 0.24f;
            private const float RelocationFadeTime = 0.1f;
            private const float RelocationViewportDistance = 0.18f;
            private readonly GameObject root;
            private readonly RectTransform rect;
            private readonly CanvasGroup group;
            private readonly TextMeshProUGUI label;
            private readonly TextMeshProUGUI template;
            private readonly NativeHitStreakText animatedText;
            private UnitAvatar target;
            private Vector2 targetOffset;
            private Vector2 anchorPosition;
            private Vector2 moveVelocity;
            private Vector2 moveTo;
            private float lastUpdateAt;
            private float lastHitAt;
            private float targetSelectedAt;
            private float relocatedAt = -1f;
            private float emphasisUntil;
            private Color baseColor;
            private Color emphasisColor;

            internal Counter(Transform parent, TextMeshProUGUI template, Vector2 size)
            {
                this.template = template;
                root = new GameObject("Combat Insights — Hit Streak Counter",
                    typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
                rect = root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.sizeDelta = size;
                group = root.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                label = root.GetComponent<TextMeshProUGUI>();
                label.font = template.font;
                label.fontSharedMaterial = template.fontSharedMaterial;
                label.fontStyle = FontStyles.Bold;
                NativeLocalizedText.MatchFontSize(label, template);
                NativeLocalizedText.BindFont(label, template);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.alignment = TextAlignmentOptions.Left;
                rect.pivot = new Vector2(0f, 0.5f);
                label.raycastTarget = false;
                root.SetActive(false);
                animatedText = new NativeHitStreakText(root);
            }

            internal void Show(int count, Color color, UnitAvatar target, Vector2 position, float now,
                float scale, bool shouldPulse, bool milestone)
            {
                bool entering = !root.activeSelf;
                baseColor = color;
                if (milestone)
                {
                    emphasisColor = color;
                    emphasisUntil = now + 0.24f;
                }
                lastHitAt = now;
                rect.localScale = new Vector3(scale, scale, 1f);
                // Keep a stable point on the same target and coalesce rapid target changes.
                if (entering || (this.target != target && now - targetSelectedAt >= MinimumTargetHold))
                {
                    this.target = target;
                    targetOffset = position - (Vector2)target.transform.position;
                    moveTo = position;
                    targetSelectedAt = now;
                }

                if (entering)
                {
                    anchorPosition = position;
                    moveVelocity = Vector2.zero;
                    lastUpdateAt = now;
                    rect.position = new Vector3(position.x, position.y, 0f);
                    rect.localRotation = Quaternion.identity;
                    root.SetActive(true);
                    shouldPulse = true;
                }

                animatedText.Show(count, milestone, shouldPulse);

                Update(now, true);
            }

            internal void Update(float now, bool visible)
            {
                if (!root.activeSelf)
                {
                    return;
                }
                if (!visible)
                {
                    Hide();
                    return;
                }

                float age = now - lastHitAt;
                if (age >= Lifetime)
                {
                    Hide();
                    return;
                }

                float alpha = age <= FadeDelay ? 1f :
                    1f - Mathf.InverseLerp(FadeDelay, Lifetime, age);
                label.color = Color.Lerp(baseColor, emphasisColor,
                    Mathf.Clamp01((emphasisUntil - now) / 0.24f));

                if (template != null)
                    NativeLocalizedText.MatchFontSize(label, template);
                if (target != null && !target.IsDead && target.gameObject.activeInHierarchy)
                    moveTo = (Vector2)target.transform.position + targetOffset;
                else
                    target = null;
                Vector2 destination = ConstrainToView(moveTo);
                Camera camera = GameCamera.Instance?.Camera;
                if (camera != null)
                {
                    Vector3 from = camera.WorldToViewportPoint(anchorPosition);
                    Vector3 to = camera.WorldToViewportPoint(destination);
                    if (((Vector2)(to - from)).sqrMagnitude >
                        RelocationViewportDistance * RelocationViewportDistance)
                    {
                        anchorPosition = destination;
                        moveVelocity = Vector2.zero;
                        relocatedAt = now;
                    }
                }
                float delta = Mathf.Max(0f, now - lastUpdateAt);
                lastUpdateAt = now;
                if (delta > 0f)
                    anchorPosition = Vector2.SmoothDamp(anchorPosition, destination,
                        ref moveVelocity, FollowSmoothTime, Mathf.Infinity, delta);
                group.alpha = alpha * Mathf.Clamp01((now - relocatedAt) / RelocationFadeTime);
                float drift = Mathf.InverseLerp(FadeDelay, Lifetime, age) * 0.18f;
                Vector2 position = ConstrainToView(anchorPosition + Vector2.up * drift);
                rect.position = new Vector3(position.x, position.y, 0f);
            }

            private Vector2 ConstrainToView(Vector2 position)
            {
                Camera camera = GameCamera.Instance?.Camera;
                if (camera == null) return position;
                Vector3 point = camera.WorldToViewportPoint(position);
                // Keep the rendered text (including its largest pulse) inside screen-edge margins.
                Vector3 right = camera.WorldToViewportPoint((Vector3)position +
                    rect.TransformVector(new Vector3(label.textBounds.size.x * 1.24f, 0f, 0f)));
                float width = Mathf.Abs(right.x - point.x);
                point.x = Mathf.Clamp(point.x, 0.06f, Mathf.Max(0.06f, 0.94f - width));
                point.y = Mathf.Clamp(point.y, 0.12f, 0.84f);
                return camera.ViewportToWorldPoint(point);
            }

            internal void Hide()
            {
                target = null;
                emphasisUntil = 0f;
                relocatedAt = -1f;
                animatedText.Reset();
                if (root != null && root.activeSelf)
                {
                    root.SetActive(false);
                }
            }

            public void Dispose()
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }
            }
        }
    }
}
