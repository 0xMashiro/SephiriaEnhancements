using System;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.RangedControls
{
    internal sealed class TargetLockFeedbackView : IDisposable
    {
        private const float MinimumSize = 36f;
        private const float CornerInset = 4f;
        private const float CornerLength = 17f;
        private const float StrokeWidth = 4f;
        private const float ShadowWidth = 8f;
        private const float ConfirmDuration = 0.32f;
        private static readonly Color ManualColor =
            new Color(1f, 0.27f, 0.12f, 1f);
        private static readonly Color AutomaticColor =
            new Color(1f, 0.76f, 0.3f, 1f);
        private static readonly Color ShadowColor =
            new Color(0.08f, 0.055f, 0.035f, 0.9f);

        private readonly RectTransform[] shadowSegments = new RectTransform[8];
        private readonly RectTransform[] colorSegments = new RectTransform[8];
        private readonly Image[] coloredImages = new Image[9];
        private RectTransform marker;
        private RectTransform centerDot;
        private RectTransform centerShadow;
        private CanvasGroup markerGroup;
        private GameObject canvasObject;
        private Sprite pixelSprite;
        private UnitAvatar target;
        private bool manual;
        private float confirmedAt;

        internal void Show(UnitAvatar nextTarget, Camera camera, bool isManual,
            bool forceConfirm)
        {
            if (nextTarget == null || camera == null)
            {
                Hide();
                return;
            }

            bool stateChanged = target != nextTarget || manual != isManual;
            target = nextTarget;
            manual = isManual;
            if (stateChanged || forceConfirm)
            {
                confirmedAt = Time.unscaledTime;
            }

            float elapsed = Time.unscaledTime - confirmedAt;
            if (!TryProjectBodyBounds(nextTarget, camera, out Vector2 center,
                    out Vector2 targetSize))
            {
                Deactivate();
                return;
            }

            EnsureCreated();
            if (!canvasObject.activeSelf)
            {
                canvasObject.SetActive(true);
            }

            // Automatic acquisition stays quiet; explicit locks confirm once,
            // then retain their marker until the controller clears the target.
            float progress = isManual ? Mathf.Clamp01(elapsed / ConfirmDuration) : 1f;
            float settle = 1f - Mathf.Pow(1f - progress, 3f);
            float padding = Mathf.Lerp(24f, 12f, settle);
            Vector2 size = new Vector2(
                Mathf.Round(Mathf.Max(MinimumSize, targetSize.x) + padding),
                Mathf.Round(Mathf.Max(MinimumSize, targetSize.y) + padding));
            marker.anchoredPosition = new Vector2(Mathf.Round(center.x),
                Mathf.Round(center.y));
            LayoutCorners(size);
            markerGroup.alpha = isManual ? Mathf.Lerp(1f, 0.85f, settle) : 0.5f;
            Color color = isManual ? ManualColor : AutomaticColor;
            for (int index = 0; index < coloredImages.Length; index++)
            {
                coloredImages[index].color = color;
            }
            float dotSize = isManual ? Mathf.Round(Mathf.Lerp(10f, 6f, settle)) : 4f;
            SetSegment(centerDot, Vector2.zero, Vector2.one * dotSize);
            SetSegment(centerShadow, Vector2.zero, Vector2.one * (dotSize + 4f));
        }

        internal void Hide()
        {
            target = null;
            manual = false;
            Deactivate();
        }

        public void Dispose()
        {
            if (canvasObject != null)
            {
                UnityEngine.Object.Destroy(canvasObject);
                canvasObject = null;
            }
            if (pixelSprite != null)
            {
                UnityEngine.Object.Destroy(pixelSprite);
                pixelSprite = null;
            }
            marker = null;
            centerDot = null;
            centerShadow = null;
            markerGroup = null;
            target = null;
        }

        private void Deactivate()
        {
            if (canvasObject != null && canvasObject.activeSelf)
            {
                canvasObject.SetActive(false);
            }
        }

        private void EnsureCreated()
        {
            if (canvasObject != null)
            {
                return;
            }

            pixelSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            pixelSprite.name = "Sephiria Enhancements — Target Feedback Pixel";
            canvasObject = new GameObject("Sephiria Enhancements — Target Feedback Canvas",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            UnityEngine.Object.DontDestroyOnLoad(canvasObject);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30000;
            CanvasGroup canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject markerObject = new GameObject("Target Feedback",
                typeof(RectTransform), typeof(CanvasGroup));
            marker = markerObject.GetComponent<RectTransform>();
            marker.SetParent(canvasObject.transform, false);
            marker.anchorMin = Vector2.zero;
            marker.anchorMax = Vector2.zero;
            marker.pivot = new Vector2(0.5f, 0.5f);
            markerGroup = markerObject.GetComponent<CanvasGroup>();
            markerGroup.interactable = false;
            markerGroup.blocksRaycasts = false;

            for (int index = 0; index < shadowSegments.Length; index++)
            {
                shadowSegments[index] = CreatePixel("Target Shadow " + index,
                    marker, ShadowColor);
            }
            for (int index = 0; index < colorSegments.Length; index++)
            {
                colorSegments[index] = CreatePixel("Target Color " + index,
                    marker, ManualColor);
                coloredImages[index] = colorSegments[index].GetComponent<Image>();
            }
            centerShadow = CreatePixel("Target Center Shadow", marker, ShadowColor);
            centerDot = CreatePixel("Target Center", marker, ManualColor);
            coloredImages[8] = centerDot.GetComponent<Image>();

            LayoutCorners(new Vector2(MinimumSize, MinimumSize));
            canvasObject.SetActive(false);
        }

        private RectTransform CreatePixel(string name, Transform parent, Color color)
        {
            GameObject pixel = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = pixel.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = pixel.GetComponent<Image>();
            image.sprite = pixelSprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static bool TryProjectBodyBounds(UnitAvatar nextTarget, Camera camera,
            out Vector2 center, out Vector2 size)
        {
            Vector3 worldCenter = nextTarget.transform.position + Vector3.up * 0.25f;
            Vector2 worldSize = new Vector2(0.5f, 0.5f);
            if (nextTarget.TopdownActor != null)
            {
                // Native rendering metadata describes the visible body, not a hitbox.
                worldCenter = nextTarget.TopdownActor.Position_Center;
                worldSize = nextTarget.TopdownActor.size;
            }

            float halfWidth = Mathf.Max(0.05f, Mathf.Abs(worldSize.x) * 0.5f);
            float halfHeight = Mathf.Max(0.05f, Mathf.Abs(worldSize.y) * 0.5f);
            Vector3 screenCenter = camera.WorldToScreenPoint(worldCenter);
            if (screenCenter.z <= 0f)
            {
                center = Vector2.zero;
                size = Vector2.zero;
                return false;
            }

            Vector3 projectedX = camera.WorldToScreenPoint(worldCenter +
                Vector3.right * halfWidth) - screenCenter;
            Vector3 projectedY = camera.WorldToScreenPoint(worldCenter +
                Vector3.up * halfHeight) - screenCenter;
            center = screenCenter;
            size = new Vector2(
                2f * (Mathf.Abs(projectedX.x) + Mathf.Abs(projectedY.x)),
                2f * (Mathf.Abs(projectedX.y) + Mathf.Abs(projectedY.y)));
            return true;
        }

        private void LayoutCorners(Vector2 size)
        {
            marker.sizeDelta = size;
            float edgeX = size.x * 0.5f - CornerInset;
            float edgeY = size.y * 0.5f - CornerInset;
            float horizontalLength = Mathf.Min(CornerLength,
                Mathf.Max(8f, size.x * 0.3f));
            float verticalLength = Mathf.Min(CornerLength,
                Mathf.Max(8f, size.y * 0.3f));
            LayoutCorner(0, -edgeX, edgeY, left: true, top: true,
                horizontalLength, verticalLength);
            LayoutCorner(2, edgeX, edgeY, left: false, top: true,
                horizontalLength, verticalLength);
            LayoutCorner(4, -edgeX, -edgeY, left: true, top: false,
                horizontalLength, verticalLength);
            LayoutCorner(6, edgeX, -edgeY, left: false, top: false,
                horizontalLength, verticalLength);
        }

        private void LayoutCorner(int index, float x, float y, bool left, bool top,
            float horizontalLength, float verticalLength)
        {
            float horizontalX = x + (left ? horizontalLength * 0.5f :
                -horizontalLength * 0.5f);
            float verticalY = y + (top ? -verticalLength * 0.5f :
                verticalLength * 0.5f);
            SetSegment(shadowSegments[index], new Vector2(horizontalX, y),
                new Vector2(horizontalLength + 4f, ShadowWidth));
            SetSegment(shadowSegments[index + 1], new Vector2(x, verticalY),
                new Vector2(ShadowWidth, verticalLength + 4f));
            SetSegment(colorSegments[index], new Vector2(horizontalX, y),
                new Vector2(horizontalLength, StrokeWidth));
            SetSegment(colorSegments[index + 1], new Vector2(x, verticalY),
                new Vector2(StrokeWidth, verticalLength));
        }

        private static void SetSegment(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
