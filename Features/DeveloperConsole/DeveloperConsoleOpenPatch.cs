using System;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.DeveloperConsole
{
    [HarmonyPatch(typeof(PlayerInputController),
        nameof(PlayerInputController.HandleOnOpenDevCommandPanel))]
    internal static class DeveloperConsoleOpenPatch
    {
        private static bool Prefix(PlayerInputController __instance,
            InputAction.CallbackContext input)
        {
            if (!EnhancementsSettings.Enabled || !DeveloperConsoleSettings.Enabled)
            {
                return true;
            }

            if (__instance == null || !__instance.enabled || !input.performed ||
                !__instance.HasAvatar)
            {
                return false;
            }

            UIManager manager = UIManager.Instance;
            if (manager == null || manager.CurrentControlStack != null)
            {
                return false;
            }

            ScreenFader fader = ScreenFader.Instance;
            if (fader != null &&
                (fader.FadingState != ScreenFader.EFadingState.None ||
                 fader.currentLoadingScreenType != -1))
            {
                return false;
            }

            try
            {
                manager.GetElement<UI_DevCommandlinePanel>()?.Open();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SephiriaEnhancements] Native developer console " +
                    "could not be opened: " + ex.Message);
            }

            return false;
        }
    }
}
