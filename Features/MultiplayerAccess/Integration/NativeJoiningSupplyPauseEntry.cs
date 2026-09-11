using SephiriaEnhancements.Runtime;
using HarmonyLib;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SephiriaEnhancements.MultiplayerAccess.Integration
{
    [HarmonyPatch(typeof(UI_PausePanel), nameof(UI_PausePanel.OnOpened))]
    internal static class NativeJoiningSupplyPauseEntry
    {
        private static void Postfix(UI_PausePanel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_PausePanel __instance)
        {
            var entry = __instance.GetComponent<JoiningSupplyPauseButton>();
            if (entry == null)
                entry = __instance.gameObject.AddComponent<JoiningSupplyPauseButton>();
            entry.Initialize(__instance);
        }
    }

    internal sealed class JoiningSupplyPauseButton : MonoBehaviour
    {
        private UI_HorayButton button;
        internal static void DisposeAll()
        {
            foreach (var entry in Resources.FindObjectsOfTypeAll<JoiningSupplyPauseButton>())
            {
                if (entry.button != null) DestroyImmediate(entry.button.gameObject);
                DestroyImmediate(entry);
            }
        }
        internal void Initialize(UI_PausePanel panel)
        {
            if (button != null) return;
            var template = panel.defaultSelectable?.GetComponent<UI_HorayButton>();
            if (template == null) return;
            button = Instantiate(template, template.transform.parent, false);
            button.name = "Sephiria Enhancements Joining Supplies";
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { panel.Close(); JoiningSupplyBridge.Claim(); });
            button.SetForceNavUp(null); button.SetForceNavDown(null);
            button.SetForceNavLeft(null); button.SetForceNavRight(null);
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            button.transform.SetSiblingIndex(template.transform.GetSiblingIndex() + 1);
            button.GetComponentInChildren<UI_LocalizationStringText>(true)?.UpdateKey(JoiningSupplyLocalization.Claim);
            Refresh();
        }
        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.MultiplayerAccess))
            {
                return;
            }

            try
            {
                UpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.MultiplayerAccess, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore() => Refresh();
        private void Refresh()
        {
            if (button != null) button.gameObject.SetActive(NativeJoiningSupplies.Instance != null && JoiningSupplyBridge.Available);
        }
        private void OnDestroy() { if (button != null) Destroy(button.gameObject); }
    }
}
