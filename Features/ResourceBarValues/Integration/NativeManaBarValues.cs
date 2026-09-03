using HarmonyLib;
using UnityEngine;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    [HarmonyPatch(typeof(UI_MpBar), nameof(UI_MpBar.SetTarget))]
    internal static class NativeManaBarValuesPatch
    {
        private static void Postfix(UI_MpBar __instance, UnitAvatar __0)
        {
            var owner = __instance;
            var target = __0;
            var view = NativeResourceBarValueView.GetOrAdd(owner);
            view.Clear();
            var original = owner.valueText;
            bool originalEnabled = original.enabled;
            var value = view.Add(owner.valueImage.rectTransform.parent as RectTransform,
                original, () => target != null
                    ? ResourceBarValueFormatter.Mana(target.mp, target.MaxMp,
                        target.reservedMp, Loc._("Keyword_ReservedMP_Name")) : string.Empty,
                () => original.fontSize, "Resource Bar Values — Mana");
            value.rectTransform.offsetMin = new Vector2(2f, 0f);
            value.rectTransform.offsetMax = new Vector2(-2f, 0f);
            view.RefreshLayout = enabled =>
            {
                if (original != null) original.enabled = enabled ? false : originalEnabled;
            };
            view.RestoreLayout = () =>
            {
                if (original != null) original.enabled = originalEnabled;
            };
        }
    }
}
