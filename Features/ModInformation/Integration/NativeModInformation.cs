using SephiriaEnhancements.Runtime;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using UnityEngine;

namespace SephiriaEnhancements.ModInformation.Integration
{
    internal static class ModInformationSettings
    {
        private const string WelcomeKey = "SephiriaEnhancements.ModInformation.ShowWelcome";
        private const string AutomaticKey = "SephiriaEnhancements.ModInformation.AutomaticUpdateCheck";
        internal static bool ShowWelcome
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(WelcomeKey, true) ?? true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(WelcomeKey, value);
        }
        internal static bool AutomaticUpdateCheck
        {
            get => OptionsBinding.Instance?.DeviceOptions?.GetBool(AutomaticKey, true) ?? true;
            set => OptionsBinding.Instance?.DeviceOptions?.SetBool(AutomaticKey, value);
        }
    }

    internal sealed class NativeModInformation : MonoBehaviour
    {
        // Keep first-entry/check deduplication across the native loader's Mod recreation.
        private static readonly ModInformationState ProcessState = new ModInformationState();
        // Native HUD ownership is private; do not infer it from the camera's observed player.
        private static readonly AccessTools.FieldRef<UI_HUDLogViewer, UnitAvatar> ViewerOwner =
            AccessTools.FieldRefAccess<UI_HUDLogViewer, UnitAvatar>("unitAvatar");
        internal static NativeModInformation Instance { get; private set; }
        internal static readonly string InstalledVersion = typeof(SephiriaEnhancementsMod).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        internal static ModUpdateResult Result => ProcessState.Result;
        internal static DateTime? LastCheckedUtc => ProcessState.LastCheckedUtc;
        private CancellationTokenSource cancellation;
        private Task<ModUpdateResult> check;
        private bool automaticRequest;
        private float nextPoll;
        private string lastReadiness;

        private void Awake()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModInformation))
            {
                return;
            }

            try
            {
                AwakeCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModInformation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void AwakeCore()
        {
            Instance = this;
            SupportLogger.Record("mod_information_initialized", "version=" + InstalledVersion + " game=" + Application.version);
        }

        private void Update()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.ModInformation))
            {
                return;
            }

            try
            {
                UpdateCore();
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.ModInformation, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void UpdateCore()
        {
            if (Time.unscaledTime < nextPoll)
                return;
            nextPoll = Time.unscaledTime + 0.25f;
            bool automaticEnabled = EnhancementsSettings.Enabled && ModInformationSettings.AutomaticUpdateCheck;
            if (check != null && automaticRequest && !automaticEnabled)
                CancelCheck();
            if (check != null && check.IsCompleted)
            {
                ProcessState.Result = check.Status == TaskStatus.RanToCompletion ? check.Result : new ModUpdateResult(ModUpdateStatus.Failed);
                // Observe unexpected task faults without letting networking interrupt the game loop.
                if (check.IsFaulted)
                    _ = check.Exception;
                ProcessState.LastCheckedUtc = DateTime.UtcNow;
                SupportLogger.Record("mod_update_check_completed", "status=" + ProcessState.Result.Status +
                    " httpStatus=" + ProcessState.Result.HttpStatus);
                check = null;
                cancellation.Dispose();
                cancellation = null;
            }

            UI_HUDLogViewer viewer = ReadyViewer(out string readiness);
            if (OptionsBinding.Instance?.DeviceOptions == null)
                readiness = "settings_unavailable";
            if (lastReadiness != readiness)
            {
                SupportLogger.Record("mod_information_readiness", "state=" + readiness);
                lastReadiness = readiness;
            }

            if (readiness != "ready")
                return;
            if (ProcessState.EnterGameplay(EnhancementsSettings.Enabled && ModInformationSettings.ShowWelcome))
            {
                WriteLines(viewer, string.Format(ModLocalization.Get(ModInformationLocalization.Welcome), "SEPHIRIA ENHANCEMENTS · by 0xMashiro", InstalledVersion, Application.version) + "\nNexus Mods: " + ModOfficialLinks.Nexus + "\nGitHub: " + ModOfficialLinks.GitHub);
                SupportLogger.Record("mod_welcome_written", "lines=5");
            }

            if (ProcessState.BeginAutomaticCheck(automaticEnabled))
                StartCheck(true);
            if (ProcessState.TakeUpdateNotice(automaticEnabled))
                WriteLines(viewer, string.Format(ModLocalization.Get(ModInformationLocalization.Update), Result.Version, InstalledVersion, ModOfficialLinks.Nexus, ModOfficialLinks.GitHub));
        }

        internal static UI_HUDLogViewer ReadyViewer() => ReadyViewer(out _);

        private static UI_HUDLogViewer ReadyViewer(out string reason)
        {
            reason = "local_player_unavailable";
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (player == null) return null;
            reason = "player_loading";
            if (player.loadingScreenType != -1 || string.IsNullOrEmpty(player.currentFloorGuid)) return null;
            reason = "hud_unavailable";
            UIManager ui = UIManager.Instance;
            UI_HUDLogViewer viewer = ui?.GetElement<UI_HUDLogViewer>();
            if (viewer == null || !viewer.isActiveAndEnabled || viewer.logPool.Count == 0) return null;
            reason = "hud_owner_mismatch";
            if (ViewerOwner(viewer) != player) return null;
            reason = "screen_transition";
            ScreenFader fader = ScreenFader.Instance;
            if (fader != null && (fader.loadingScreenImage.alpha > 0.01f ||
                fader.currentLoadingScreenType != -1 || fader.FadingState != ScreenFader.EFadingState.None)) return null;
            reason = "hud_hidden";
            if (ui.IsHidden) return null;
            reason = "menu_open";
            if (ui.CurrentControlStack != null) return null;
            reason = "ready";
            return viewer;
        }

        private static void WriteLines(UI_HUDLogViewer viewer, string message)
        {
            foreach (string line in message.Split('\n')) viewer.SpawnLog(line, Color.cyan);
        }

        internal void StartCheck(bool automatic = false)
        {
            if (check != null) return;
            SupportLogger.Record("mod_update_check_started", "automatic=" + automatic);
            automaticRequest = automatic;
            ProcessState.Result = new ModUpdateResult(ModUpdateStatus.Checking);
            cancellation = new CancellationTokenSource();
            // Keep HTTP and JSON work off Unity's main thread; Update consumes the result.
            CancellationToken token = cancellation.Token;
            check = Task.Run(() => ModUpdateCheck.FetchAsync(InstalledVersion, token));
        }

        private void CancelCheck()
        {
            if (check == null) return;
            SupportLogger.Record("mod_update_check_cancelled");
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = null;
            _ = check.ContinueWith(task => { _ = task.Exception; },
                TaskContinuationOptions.OnlyOnFaulted);
            check = null;
            ProcessState.Result = new ModUpdateResult(ModUpdateStatus.NotChecked);
        }

        private void OnDestroy()
        {
            CancelCheck();
            if (Instance == this) Instance = null;
        }
    }
}
