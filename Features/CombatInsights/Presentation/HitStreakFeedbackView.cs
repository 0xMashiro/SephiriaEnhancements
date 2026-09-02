using SephiriaEnhancements.Diagnostics;
using System;
using SephiriaEnhancements.Core;
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
        private bool hitFxAvailable = true;
        private bool cameraShakeAvailable = true;
        private bool flashAvailable = true;

        internal void Show(Vector2 position, HitStreakUpdate update, HitStreakImpact impact,
            Color sourceColor, bool ownedContribution)
        {
            PlayMilestoneEffects(position, update);
            if (!TryAttach())
            {
                return;
            }

            string prefix = ownedContribution ? "◆ " : string.Empty;
            string text = prefix + update.Count + " <size=68%>HITS</size>" +
                (update.IsMilestone ? "!" : string.Empty);
            Color color = ResolveColor(update, impact, sourceColor);
            float scale = 0.84f + update.Tier * 0.08f + (update.IsMilestone ? 0.05f : 0f);
            Vector2 offset = new Vector2(0f, 0.68f + update.Tier * 0.04f);
            counter.Show(text, color, position + offset, Time.unscaledTime, scale,
                update.ShouldRender, update.IsMilestone);
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
            counter = new Counter(worldRoot, template.font, template.fontSharedMaterial,
                template.fontSize, size);
            return true;
        }

        private void PlayMilestoneEffects(Vector2 position, HitStreakUpdate update)
        {
            if (!update.IsMilestone || update.Count < 25)
            {
                return;
            }

            if (hitFxAvailable)
            {
                try
                {
                    SpriteFx.Pool?.Spawn("CommonHitFx",
                        new Vector3(position.x, position.y + 0.0001f, 0f));
                }
                catch (Exception exception)
                {
                    DisableNativeLayer(ref hitFxAvailable, "hit effect", exception);
                }
            }

            int strength = update.Count >= 100 ? 3 : update.Count >= 50 ? 2 : 1;
            if (cameraShakeAvailable)
            {
                try
                {
                    TargetTracker tracker = GameCamera.Instance?.targetTracker;
                    if (tracker != null)
                    {
                        float power = strength == 3 ? 0.1f : strength == 2 ? 0.075f : 0.055f;
                        float duration = strength == 3 ? 0.14f : strength == 2 ? 0.12f : 0.1f;
                        tracker.CreateCameraShaking(position, EShakeCameraType.Impact,
                            Vector2.up * power, duration);
                    }
                }
                catch (Exception exception)
                {
                    DisableNativeLayer(ref cameraShakeAvailable, "camera shake", exception);
                }
            }

            if (strength < 2 || !flashAvailable)
            {
                return;
            }

            try
            {
                UI_FlashScreen flash = UIManager.Instance?.GetElement<UI_FlashScreen>();
                if (flash != null)
                {
                    float alpha = strength == 3 ? 0.06f : 0.035f;
                    float duration = strength == 3 ? 0.16f : 0.12f;
                    flash.Flash(new Color(1f, 0.72f, 0.2f, alpha), duration,
                        EFlashScreenTImeType.UNSCALED);
                }
            }
            catch (Exception exception)
            {
                DisableNativeLayer(ref flashAvailable, "screen flash", exception);
            }
        }

        private static void DisableNativeLayer(ref bool available, string layer,
            Exception exception)
        {
            available = false;
            SupportLogger.Warning("hit_streak_layer_failed", "[SephiriaEnhancements] Hit-streak milestone " + layer +
                " disabled until the Mod is reloaded: " +
                exception.Message);
        }

        private static Color ResolveColor(HitStreakUpdate update, HitStreakImpact impact,
            Color sourceColor)
        {
            if (impact == HitStreakImpact.Execution) return Execution;
            if (update.Tier >= 3) return HotGold;
            if (update.IsMilestone || impact == HitStreakImpact.Critical) return Gold;
            sourceColor.a = 1f;
            return Color.Lerp(sourceColor, Color.white, 0.25f);
        }

        private sealed class Counter : IDisposable
        {
            private const float FadeDelay = 1.24f;
            private const float Lifetime = 1.6f;
            private const float MoveDuration = 0.16f;
            private readonly GameObject root;
            private readonly RectTransform rect;
            private readonly CanvasGroup group;
            private readonly TextMeshProUGUI label;
            private Vector2 moveFrom;
            private Vector2 moveTo;
            private float moveStartedAt;
            private float lastHitAt;
            private float pulseStartedAt;
            private float pulseDuration;
            private float pulseFromScale;
            private float pulsePeakScale;
            private float targetScale;
            private float currentScale;
            private bool pulseActive;

            internal Counter(Transform parent, TMP_FontAsset font, Material material,
                float fontSize, Vector2 size)
            {
                root = new GameObject("Combat Insights — Hit Streak Counter",
                    typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
                rect = root.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.sizeDelta = size;
                group = root.GetComponent<CanvasGroup>();
                group.interactable = false;
                group.blocksRaycasts = false;
                label = root.GetComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSharedMaterial = material;
                label.fontStyle = FontStyles.Bold;
                label.fontSize = fontSize;
                label.enableAutoSizing = false;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                root.SetActive(false);
            }

            internal void Show(string text, Color color, Vector2 position, float now,
                float scale, bool shouldPulse, bool milestone)
            {
                bool entering = !root.activeSelf;
                label.text = text;
                label.color = color;
                lastHitAt = now;
                targetScale = scale;
                moveFrom = entering ? position : (Vector2)rect.position;
                moveTo = position;
                moveStartedAt = now;

                if (entering)
                {
                    currentScale = targetScale * 0.94f;
                    rect.position = new Vector3(position.x, position.y, 0f);
                    rect.localRotation = Quaternion.identity;
                    root.SetActive(true);
                    shouldPulse = true;
                }

                if (shouldPulse)
                {
                    pulseFromScale = currentScale;
                    pulsePeakScale = Mathf.Max(currentScale,
                        targetScale * (milestone ? 1.18f : 1.08f));
                    pulseStartedAt = now;
                    pulseDuration = milestone ? 0.22f : 0.16f;
                    pulseActive = true;
                }

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

                group.alpha = age <= FadeDelay ? 1f :
                    1f - Mathf.InverseLerp(FadeDelay, Lifetime, age);

                float moveProgress = Mathf.Clamp01((now - moveStartedAt) / MoveDuration);
                float moveEase = Mathf.SmoothStep(0f, 1f, moveProgress);
                Vector2 position = Vector2.LerpUnclamped(moveFrom, moveTo, moveEase);
                rect.position = new Vector3(position.x, position.y, 0f);

                if (pulseActive)
                {
                    float pulseProgress = Mathf.Clamp01((now - pulseStartedAt) / pulseDuration);
                    if (pulseProgress < 0.25f)
                    {
                        float rise = Mathf.SmoothStep(0f, 1f, pulseProgress / 0.25f);
                        currentScale = Mathf.LerpUnclamped(pulseFromScale, pulsePeakScale, rise);
                    }
                    else
                    {
                        float settle = Mathf.SmoothStep(0f, 1f,
                            Mathf.InverseLerp(0.25f, 1f, pulseProgress));
                        currentScale = Mathf.LerpUnclamped(pulsePeakScale, targetScale, settle);
                    }
                    if (pulseProgress >= 1f)
                    {
                        pulseActive = false;
                        currentScale = targetScale;
                    }
                }
                else
                {
                    currentScale = targetScale;
                }

                rect.localScale = new Vector3(currentScale, currentScale, 1f);
            }

            internal void Hide()
            {
                pulseActive = false;
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
