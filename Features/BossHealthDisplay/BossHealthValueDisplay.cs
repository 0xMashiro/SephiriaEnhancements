using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.BossHealthDisplay
{
    internal static class BossHealthValueFeature
    {
        private static readonly HashSet<BossHealthValueView> ActiveViews =
            new HashSet<BossHealthValueView>();

        internal static void Attach(UI_BossHPBar owner, UnitAvatar boss)
        {
            if (owner == null) return;
            BossHealthValueView view = owner.GetComponent<BossHealthValueView>();
            if (view == null) view = owner.gameObject.AddComponent<BossHealthValueView>();
            ActiveViews.Add(view);
            view.Configure(owner, boss);
        }

        internal static void Unregister(BossHealthValueView view)
        {
            if (view != null) ActiveViews.Remove(view);
        }

        internal static void DisposeAll()
        {
            List<BossHealthValueView> views = new List<BossHealthValueView>(ActiveViews);
            ActiveViews.Clear();
            for (int index = 0; index < views.Count; index++)
            {
                if (views[index] != null) UnityEngine.Object.Destroy(views[index]);
            }
        }
    }

    internal sealed class BossHealthValueView : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;
        private static readonly FieldInfo BarImageField = AccessTools.Field(
            typeof(UI_BossHPBar), "barImage");

        private UI_BossHPBar owner;
        private UnitAvatar boss;
        private GameObject textObject;
        private TextMeshProUGUI valueText;
        private float nextRefresh;
        private int lastCurrent = -1;
        private int lastMaximum = -1;

        internal void Configure(UI_BossHPBar value, UnitAvatar target)
        {
            owner = value;
            boss = target;
            EnsureText();
            lastCurrent = -1;
            lastMaximum = -1;
            nextRefresh = 0f;
            Refresh();
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (now < nextRefresh) return;
            nextRefresh = now + RefreshInterval;
            Refresh();
        }

        private void OnDestroy()
        {
            BossHealthValueFeature.Unregister(this);
            if (textObject != null) Destroy(textObject);
        }

        private void EnsureText()
        {
            if (valueText != null || owner == null) return;
            TextMeshProUGUI template = owner.GetComponentInChildren<TextMeshProUGUI>(true);
            Image barImage = BarImageField?.GetValue(owner) as Image;
            RectTransform parent = barImage?.rectTransform.parent as RectTransform;
            parent ??= owner.transform as RectTransform;
            if (template == null || parent == null) return;

            textObject = new GameObject("Sephiria Enhancements — Boss HP Value",
                typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            textObject.GetComponent<LayoutElement>().ignoreLayout = true;

            valueText = textObject.GetComponent<TextMeshProUGUI>();
            valueText.font = template.font;
            valueText.fontSharedMaterial = template.fontSharedMaterial;
            valueText.fontStyle = FontStyles.Bold;
            valueText.fontSize = Mathf.Max(8f, template.fontSize * 0.62f);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 7f;
            valueText.fontSizeMax = Mathf.Max(9f, template.fontSize * 0.62f);
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = Color.white;
            valueText.raycastTarget = false;
        }

        private void Refresh()
        {
            if (valueText == null)
            {
                EnsureText();
                if (valueText == null) return;
            }

            float maximumHealth = boss != null ? boss.MaxHp : 0f;
            bool allowed = EnhancementsSettings.Enabled && boss != null && maximumHealth > 0f;
            if (textObject.activeSelf != allowed) textObject.SetActive(allowed);
            if (!allowed) return;

            int current = Mathf.CeilToInt(Mathf.Max(0f, boss.Networkhp));
            int maximum = Mathf.CeilToInt(maximumHealth);
            if (current == lastCurrent && maximum == lastMaximum) return;

            lastCurrent = current;
            lastMaximum = maximum;
            valueText.text = current.ToString("N0", CultureInfo.InvariantCulture) + " / " +
                maximum.ToString("N0", CultureInfo.InvariantCulture);
        }
    }

    [HarmonyPatch]
    internal static class BossHealthValuePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            MethodInfo standard = AccessTools.DeclaredMethod(typeof(UI_BossHPBar),
                "SetBoss", new[] { typeof(UnitAvatar) });
            if (standard != null) yield return standard;

            MethodInfo qTemple = AccessTools.DeclaredMethod(typeof(UI_QTempleQliphothHpBar),
                "SetBoss", new[] { typeof(UnitAvatar) });
            if (qTemple != null) yield return qTemple;
        }

        private static void Postfix(UI_BossHPBar __instance, UnitAvatar __0)
        {
            try
            {
                BossHealthValueFeature.Attach(__instance, __0);
            }
            catch (Exception ex)
            {
                SupportLogger.Warning("boss_health_display_failed", "[SephiriaEnhancements] BOSS HP value display disabled: " +
                    ex.Message);
            }
        }
    }
}
