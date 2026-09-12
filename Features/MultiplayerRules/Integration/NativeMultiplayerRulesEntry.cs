using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    [HarmonyPatch]
    internal static class NativeMultiplayerRulesEntryPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(UI_MultiplayerPanel), nameof(UIBase.OnOpened));
            yield return AccessTools.Method(typeof(UI_MultiplayerPanel_E), nameof(UIBase.OnOpened));
        }

        private static void Postfix(UIBase __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerRules)) return;
            try { PostfixCore(__instance); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.MultiplayerRules, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UIBase __instance)
        {
                Button template = __instance is UI_MultiplayerPanel steam ? steam.enterMultiZoneButton
                    : ((UI_MultiplayerPanel_E)__instance).enterMultiZoneButton;
                var existing = __instance.GetComponentInChildren<NativeMultiplayerRulesEntry>(true);
                if (existing != null) { existing.Refresh(); return; }
                if (template == null) return;
                var button = UnityEngine.Object.Instantiate(template, template.transform.parent, false);
                button.name = "Team Rules";
                button.interactable = true;
                button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                if (button is UI_HorayButton native)
                {
                    native.SetForceNavUp(null);
                    native.SetForceNavDown(null);
                    native.SetForceNavLeft(null);
                    native.SetForceNavRight(null);
                }
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() => NativeMultiplayerRulesPanel.Show());
                foreach (var label in button.GetComponentsInChildren<UI_LocalizationStringText>(true))
                    label.UpdateKey(MultiplayerRulesLocalization.PanelTitle);
                NativeOptionsLifetime.Track(button.gameObject);
                button.gameObject.AddComponent<NativeMultiplayerRulesEntry>().Refresh();
        }
    }

    internal sealed class NativeMultiplayerRulesEntry : MonoBehaviour
    {
        internal void Refresh() => gameObject.SetActive(MultiplayerRulesContext.CanInspect);
        private void Update() => Refresh();
    }
}
