using SephiriaEnhancements.Runtime;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SephiriaEnhancements.ResourceBarValues.Integration
{
    // Combat props such as golem totems have their own native health-bar owner.
    [HarmonyPatch(typeof(UI_PropHPBar), "LateUpdate")]
    internal static class NativePropBarValuesPatch
    {
        private static readonly AccessTools.FieldRef<UI_PropHPBar, BreakableProp> Target =
            AccessTools.FieldRefAccess<UI_PropHPBar, BreakableProp>("target");

        private static void Postfix(UI_PropHPBar __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ResourceBarValues))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ResourceBarValues, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_PropHPBar __instance)
        {
            var owner = __instance;
            if (owner.GetComponent<NativeResourceBarValueView>() != null)
                return;
            TextMeshProUGUI template = UIManager.Instance?.GetElement<UI_PlayerMP>()?.mpBar?.valueText;
            if (template == null)
                return;
            var view = NativeResourceBarValueView.GetOrAdd(owner);
            var text = view.Add(owner.frameImage.rectTransform, template, () =>
            {
                if (!ResourceBarValueSettings.Get(ResourceBarValueSetting.PropHealthNumbers))
                    return string.Empty;
                BreakableProp target = Target(owner);
                return target != null && owner.valueImage.gameObject.activeInHierarchy ? ResourceBarValueFormatter.Ratio(target.hp, target.MaxHP) : string.Empty;
            }, () => template.fontSize * 0.8f, "Resource Bar Values — Prop Health");
            // Same AppWorldUI scale and placement as ordinary creature bars.
            RectTransform rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(60f, 6f);
        }
    }
}
