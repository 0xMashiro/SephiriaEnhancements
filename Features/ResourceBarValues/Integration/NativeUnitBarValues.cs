using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    internal static class NativeUnitBarValues
    {
        private static readonly AccessTools.FieldRef<UI_UnitHPBar, UnitAvatar> Target =
            AccessTools.FieldRefAccess<UI_UnitHPBar, UnitAvatar>("target");

        internal static void Configure(UI_UnitHPBar owner, TextMeshProUGUI template)
        {
            var view = NativeResourceBarValueView.GetOrAdd(owner);
            view.Clear();
            bool world = owner.IsWorldUI;
            RectTransform frame = owner.frameImage.rectTransform;
            var health = view.Add(frame, template, () =>
            {
                UnitAvatar target = Target(owner);
                return target != null && owner.valueImage.gameObject.activeInHierarchy
                    ? ResourceBarValueFormatter.HealthWithShield(target.Networkhp,
                        target.MaxHp, target.Shield) : string.Empty;
            }, () => template.fontSize * (world ? 0.8f : 0.65f),
                "Resource Bar Values — Unit Health");

            RectTransform rect = health.rectTransform;
            if (world)
            {
                // AppWorldUI uses 1/16 world units per canvas unit. Its ten-unit
                // tracks cannot hold a full HP ratio. Use a four-unit readout above
                // the track, scaled with the native five-unit MP numeric reference.
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 1f);
                rect.sizeDelta = new Vector2(60f, 6f);
            }
            else
            {
                RectTransform bar = owner.valueImage.rectTransform;
                rect.anchorMin = bar.anchorMin;
                rect.anchorMax = bar.anchorMax;
                rect.pivot = bar.pivot;
                rect.anchoredPosition = bar.anchoredPosition;
                rect.sizeDelta = bar.sizeDelta;
            }

            var armor = view.Add(frame, template, () =>
            {
                UnitAvatar target = Target(owner);
                if (target == null || target.InitializedMaxSuperArmor <= 0f ||
                    !owner.superArmorValueImage.gameObject.activeInHierarchy) return string.Empty;
                return Loc._("Status_SuperArmor_Name") + " " + ResourceBarValueFormatter.Ratio(
                    target.remainingSuperArmor, target.InitializedMaxSuperArmor);
            }, () => template.fontSize * (world ? 0.7f : 0.65f),
                "Resource Bar Values — Super Armor");
            armor.color = new Color(1f, 0.87f, 0.48f);
            rect = armor.rectTransform;
            if (world)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -1f);
                rect.sizeDelta = new Vector2(70f, 6f);
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(12f, 0f);
                rect.sizeDelta = new Vector2(110f, 8f);
                armor.alignment = TextAlignmentOptions.Left;
            }
        }
    }

    // The miniboss override calls this base update too. Attach when native UI is
    // actually updating, including bars created before mod loading or UI readiness.
    [HarmonyPatch(typeof(UI_UnitHPBar), "LateUpdate")]
    internal static class NativeUnitBarValuesPatch
    {
        private static void Postfix(UI_UnitHPBar __instance)
        {
            if (__instance.GetComponent<NativeResourceBarValueView>() != null) return;
            TextMeshProUGUI template = __instance is UI_MiniBossHPBar mini
                ? mini.nameText : UIManager.Instance?.GetElement<UI_PlayerMP>()?.mpBar?.valueText;
            if (template == null) return;
            NativeUnitBarValues.Configure(__instance, template);
        }
    }
}
