using System;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Presentation;
using SephiriaEnhancements.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SephiriaEnhancements.Combat
{
    internal enum CombatInsightsViewMode { Hidden, Pulse, Party, Boss, Report }
    internal enum CombatInsightsVisibilityReason
    {
        Visible,
        StatisticsDisabled,
        LocalPlayerUnavailable,
        NativeControlOpen,
        PresentationBlocked,
        HiddenByUser,
        HudUnavailable,
        HudSuppressedByHierarchy,
        BossOnlyOutsideBoss,
        SmartAwaitingContribution,
        SmartInitialDelay,
        ReportDeferred,
        ReportExpired,
        NoActiveCombatOrReport,
        RuntimeIncompatible,
        ControllerDisabled
    }

    internal sealed partial class CombatInsightsController : MonoBehaviour
    {
        private int bossSourceInstanceId;
        private const float SampleInterval = 0.2f;
        private const float EncounterFallbackQuietSeconds = 2.5f;
        private const float SmartPulseDelaySeconds = 1.5f;
        private readonly Dictionary<long, PlayerDamageState> states = new Dictionary<long, PlayerDamageState>(4);
        private readonly BossEncounterTracker bossEncounter = new BossEncounterTracker();
        private readonly EncounterDefeatTracker defeats = new EncounterDefeatTracker();
        private readonly DamageContextBuffer damageTypeContexts =
            new DamageContextBuffer();
        private readonly Dictionary<EncounterDamageType, float> damageByType =
            new Dictionary<EncounterDamageType, float>();
        private readonly List<PlayerDamageState> ordered = new List<PlayerDamageState>(4);
        private readonly HashSet<long> activeKeys = new HashSet<long>();
        private readonly List<long> staleKeys = new List<long>(4);
        private readonly EncounterAreaLocator encounterAreaLocator =
            new EncounterAreaLocator();
        private readonly CombatInsightsHud hud = new CombatInsightsHud();
        private readonly HitStreakFeedbackFeature hitStreakFeedback = new HitStreakFeedbackFeature();
        private RuntimeKernel runtimeKernel;
        private float nextSample;
        private readonly ReportDisplayWindow reportWindow =
            new ReportDisplayWindow();
        private EncounterReportSnapshot encounterReport;
        private readonly FloorCombatStatistics floorStatistics = new FloorCombatStatistics();
        private NativeStatisticsBrowser statisticsBrowser;
        internal bool PreferFloorStatistics { get; set; }
        internal CombatStatisticsSnapshot FloorStatistics => floorStatistics.Capture();
        internal bool StatisticsBrowserOpen => statisticsBrowser != null && statisticsBrowser.IsOpened;
        internal bool CanBrowseStatistics => isActiveAndEnabled && runtimeCompatible &&
            StatisticsCaptureEnabled && LocalPlayerResolver.Resolve() != null;
        private float encounterStartedAt = -1f, encounterEndedAt = -1f;
        private float encounterLastActivity = -1000f;
        private float encounterDamage;
        private string floorGuid;
        private EncounterScope encounterScope;
        private bool encounterActive, majorEncounter, runtimeCompatible = true;
        private bool hudHiddenByUser;
        private readonly CombatInsightsShortcut statisticsShortcut =
            new CombatInsightsShortcut();
        private bool localIdentityWarningLogged;
        private float localIdentityMissingSince = -1f;
        private bool runtimeSuspended;
        private bool statisticsWereEnabled = true;
        private string lastVisibilityDiagnostic;
        private (bool Enabled, bool LocalPlayerReady, bool MenuOpen, bool HiddenByUser,
            bool HudAttached, CombatInsightsViewMode View, ReportDisplayState Report,
            ReportPresentationBlock Block, CombatInsightsDisplayPolicy Policy)? lastSupportVisibility;
        private ReportPresentationBlock presentationBlock;

        private static bool StatisticsCaptureEnabled =>
            EnhancementsSettings.Enabled &&
            ModSettings.DisplayPolicy != CombatInsightsDisplayPolicy.Disabled;

        internal void Initialize(RuntimeKernel kernel)
        {
            if (runtimeKernel == kernel)
            {
                return;
            }

            if (runtimeKernel != null)
            {
                runtimeKernel.EncounterLifecycleChanged -=
                    OnEncounterLifecycleChanged;
                runtimeKernel.GameplayContextChanged -= OnStatisticsContextChanged;
            }
            runtimeKernel = kernel;
            if (runtimeKernel != null)
            {
                runtimeKernel.EncounterLifecycleChanged +=
                    OnEncounterLifecycleChanged;
                runtimeKernel.GameplayContextChanged += OnStatisticsContextChanged;
            }
        }

        internal void Shutdown()
        {
            retryStatistics.Clear();
            Initialize(null);
            ResetCombatState();
            floorStatistics.Clear();
            if (statisticsBrowser != null) Destroy(statisticsBrowser.gameObject);
            enabled = false;
        }

        internal IReadOnlyList<PlayerDamageState> Players => ordered;
        internal IReadOnlyDictionary<long, float> BossDamage => bossEncounter.Damage;
        internal EncounterDefeatTracker Defeats => defeats;
        internal EncounterReportSnapshot EncounterReport => encounterReport;
        internal float BossTotal => bossEncounter.Total;
        internal bool BossActive => bossEncounter.Active;
        internal float BossElapsed => bossEncounter.Elapsed(Time.time);
        internal bool IsSolo => ordered.Count <= 1;
        internal float EncounterElapsed => encounterActive
            ? Mathf.Max(0f, Time.time - encounterStartedAt)
            : Mathf.Max(0f, encounterEndedAt - encounterStartedAt);
        internal float LocalDps => FindLocal()?.RollingDps ?? 0f;

        internal CombatInsightsViewMode ViewMode
        {
            get
            {
                if (ModSettings.DisplayPolicy ==
                    CombatInsightsDisplayPolicy.Disabled)
                    return CombatInsightsViewMode.Hidden;
                if (bossEncounter.Active) return CombatInsightsViewMode.Boss;
                if (encounterReport != null &&
                    reportWindow.IsVisible(Time.unscaledTime))
                    return CombatInsightsViewMode.Report;
                if (ModSettings.DisplayPolicy ==
                    CombatInsightsDisplayPolicy.BossOnly)
                    return CombatInsightsViewMode.Hidden;
                if (encounterActive)
                {
                    if (ModSettings.DisplayPolicy ==
                        CombatInsightsDisplayPolicy.Smart &&
                        encounterDamage <= 0f && defeats.DefeatedCount == 0)
                        return CombatInsightsViewMode.Hidden;
                    if (ModSettings.DisplayPolicy ==
                        CombatInsightsDisplayPolicy.Smart &&
                        !majorEncounter && EncounterElapsed < SmartPulseDelaySeconds)
                        return CombatInsightsViewMode.Hidden;
                    bool expanded = !IsSolo &&
                        (majorEncounter || EncounterElapsed >= 6f);
                    return expanded ? CombatInsightsViewMode.Party : CombatInsightsViewMode.Pulse;
                }
                return CombatInsightsViewMode.Hidden;
            }
        }

        private void Update()
        {
            DeveloperLogger.Pump();
            if (!runtimeCompatible) return;
            try { Tick(); }
            catch (Exception ex)
            {
                runtimeCompatible = false;
                hud.Hide();
                DeveloperLogger.RecordCombatInsightsVisibility(
                    CombatInsightsVisibilityReason.RuntimeIncompatible.ToString(),
                    ModSettings.DisplayPolicy.ToString(),
                    CombatInsightsViewMode.Hidden.ToString(), encounterActive,
                    bossEncounter.Active, false, false, false, false,
                    hudHiddenByUser, hud.IsAttached, hud.IsActiveInHierarchy,
                    0, null, false, false, false, false, false,
                    reportWindow.State(Time.unscaledTime).ToString(),
                    presentationBlock.ToString());
                SupportLogger.Error("combat_insights_failed", "[SephiriaEnhancements] Runtime compatibility failure; " +
                    "Combat Insights disabled until the Mod is reloaded: " + ex);
            }
        }

        private void Tick()
        {
            TickStatisticsRetry();
            float now = Time.unscaledTime;
            bool suiteEnabled = EnhancementsSettings.Enabled;
            CombatInsightsDisplayPolicy displayPolicy = ModSettings.DisplayPolicy;
            bool statisticsEnabled = suiteEnabled &&
                displayPolicy != CombatInsightsDisplayPolicy.Disabled;
            bool hitStreakEnabled = suiteEnabled && ModSettings.HitStreakFeedback;
            if (!statisticsEnabled && statisticsWereEnabled)
            {
                retryStatistics.Clear();
                ResetCombatState();
                floorStatistics.Clear();
                encounterAreaLocator.Reset();
            }
            statisticsWereEnabled = statisticsEnabled;

            if (!statisticsEnabled && !hitStreakEnabled)
            {
                statisticsShortcut.Reset();
                if (!runtimeSuspended)
                {
                    runtimeSuspended = true;
                    hud.Hide();
                    hitStreakFeedback.Reset();
                }
                RecordVisibilityDiagnostic(statisticsEnabled, null, false, now);
                return;
            }
            if (runtimeSuspended)
            {
                runtimeSuspended = false;
                nextSample = 0f;
            }

            bool trackOrdinaryEncounters = statisticsEnabled && !retryStatistics.Pending && !encounterDefeated;
            if (now >= nextSample)
            {
                nextSample = now + SampleInterval;
                SamplePlayers(now, trackOrdinaryEncounters);
            }
            floorStatistics.UpdateClock(Time.time, StatisticsCaptureEnabled && !retryStatistics.Pending && !encounterDefeated &&
                (bossEncounter.Active ? bossEncounter.IsTiming : encounterActive));
            PlayerDamageState local = FindLocal();
            TrackLocalIdentity(local, now);
            bool menuOpen = UIManager.Instance != null && UIManager.Instance.CurrentControlStack != null;
            bool contextAllowed = local != null && !menuOpen;
            // Native spawner clear events are authoritative. This remains only
            // as recovery for missed hooks and non-structured combat activity.
            if (encounterActive && !bossEncounter.Active && local != null &&
                local.Avatar != null && !local.Avatar.IsDead && !AnyParticipantInBattle() &&
                now - encounterLastActivity >= EncounterFallbackQuietSeconds)
                EndEncounter(now);
            reportWindow.CloseForEncounter(bossEncounter.Active, encounterActive,
                encounterDamage > 0f || defeats.DefeatedCount > 0);
            presentationBlock = NativeReportPresentation.ReadBlock(
                UIManager.Instance, local?.Avatar);
            string notification = HandleStatisticsShortcut(contextAllowed, now);
            bool reportPresentationAvailable = contextAllowed &&
                !hudHiddenByUser && !bossEncounter.Active &&
                presentationBlock == ReportPresentationBlock.None;
            reportWindow.SetPresentationAvailable(reportPresentationAvailable, now);
            bool inCombat = ViewMode != CombatInsightsViewMode.Hidden ||
                hitStreakFeedback.IsRecent(now) || (local != null && local.IsInBattle);
            hud.Update(statisticsEnabled && contextAllowed && !hudHiddenByUser, this);
            hitStreakFeedback.Update(hitStreakEnabled && contextAllowed && inCombat);
            if (notification == ModLocalization.StatisticsOpened &&
                !StatisticsBrowserOpen)
                notification = CombatInsightsNotifications.BlockedMessage(presentationBlock)
                    ?? ModLocalization.EncounterReportHudUnavailable;
            CombatInsightsNotifications.Show(notification);
            RecordVisibilityDiagnostic(statisticsEnabled, local, menuOpen, now);
        }

        private void RecordVisibilityDiagnostic(bool statisticsEnabled,
            PlayerDamageState local, bool menuOpen, float now)
        {
            var summary = (statisticsEnabled, local != null, menuOpen, hudHiddenByUser,
                hud.IsAttached, ViewMode, reportWindow.State(now), presentationBlock, ModSettings.DisplayPolicy);
            if (lastSupportVisibility != summary)
            {
                lastSupportVisibility = summary;
                SupportLogger.Record("combat_insights_state", "enabled=" + statisticsEnabled +
                    " localPlayerReady=" + (local != null) + " menuOpen=" + menuOpen +
                    " hiddenByUser=" + hudHiddenByUser + " hudAttached=" + hud.IsAttached +
                    " view=" + ViewMode + " reportState=" + reportWindow.State(now) +
                    " presentationBlock=" + presentationBlock + " policy=" + ModSettings.DisplayPolicy);
            }
            if (!DeveloperLogger.IsEnabled) return;

            CombatInsightsViewMode viewMode = ViewMode;
            CombatInsightsVisibilityReason reason;
            if (!statisticsEnabled)
                reason = CombatInsightsVisibilityReason.StatisticsDisabled;
            else if (local == null)
                reason = CombatInsightsVisibilityReason.LocalPlayerUnavailable;
            else if (menuOpen)
                reason = CombatInsightsVisibilityReason.NativeControlOpen;
            else if (hudHiddenByUser)
                reason = CombatInsightsVisibilityReason.HiddenByUser;
            else if (encounterReport != null && reportWindow.IsPaused &&
                presentationBlock != ReportPresentationBlock.None)
                reason = CombatInsightsVisibilityReason.PresentationBlocked;
            else if (viewMode != CombatInsightsViewMode.Hidden && !hud.IsAttached)
                reason = CombatInsightsVisibilityReason.HudUnavailable;
            else if (viewMode != CombatInsightsViewMode.Hidden &&
                !hud.IsActiveInHierarchy)
                reason = CombatInsightsVisibilityReason.HudSuppressedByHierarchy;
            else if (viewMode != CombatInsightsViewMode.Hidden)
                reason = CombatInsightsVisibilityReason.Visible;
            else if (ModSettings.DisplayPolicy ==
                CombatInsightsDisplayPolicy.BossOnly)
                reason = CombatInsightsVisibilityReason.BossOnlyOutsideBoss;
            else if (encounterActive && ModSettings.DisplayPolicy ==
                CombatInsightsDisplayPolicy.Smart && encounterDamage <= 0f &&
                defeats.DefeatedCount == 0)
                reason = CombatInsightsVisibilityReason.SmartAwaitingContribution;
            else if (encounterActive && ModSettings.DisplayPolicy ==
                CombatInsightsDisplayPolicy.Smart && !majorEncounter &&
                EncounterElapsed < SmartPulseDelaySeconds)
                reason = CombatInsightsVisibilityReason.SmartInitialDelay;
            else if (encounterReport != null && reportWindow.IsPaused)
                reason = CombatInsightsVisibilityReason.ReportDeferred;
            else if (reportWindow.HasStarted)
                reason = CombatInsightsVisibilityReason.ReportExpired;
            else
                reason = CombatInsightsVisibilityReason.NoActiveCombatOrReport;

            UIManager manager = UIManager.Instance;
            var controlStack = manager?.CurrentControlStack;
            int controlCount = controlStack?.Count ?? 0;
            string controlType = controlCount > 0
                ? controlStack[0]?.GetType().Name
                : null;
            UI_LevelUpIndicator levelUpIndicator =
                manager?.GetElement<UI_LevelUpIndicator>();
            bool levelUpIndicatorVisible = levelUpIndicator?.levelUp != null &&
                levelUpIndicator.levelUp.gameObject.activeInHierarchy;
            UI_FlashScreen flashScreen = manager?.GetElement<UI_FlashScreen>();
            bool flashScreenVisible = flashScreen != null && flashScreen.IsOpened;
            bool screenFading = ScreenFader.Instance != null &&
                ScreenFader.Instance.IsFading;
            bool cutSceneActive = CutScenePlayer.Current != null;
            bool playerLoading = local?.Avatar != null &&
                local.Avatar.loadingScreenType != -1;
            bool reportOpen = encounterReport != null &&
                reportWindow.IsOpen(now);
            bool bossReportOpen = reportOpen && encounterReport.Kind ==
                EncounterReportKind.Boss;
            bool encounterReportOpen = reportOpen && encounterReport.Kind ==
                EncounterReportKind.Ordinary;
            bool bossReportPaused = bossReportOpen && reportWindow.IsPaused;
            bool encounterReportPaused = encounterReportOpen &&
                reportWindow.IsPaused;
            string signature = reason + "|" + viewMode + "|" + controlType +
                "|" + controlCount + "|" + levelUpIndicatorVisible + "|" +
                flashScreenVisible + "|" + screenFading + "|" +
                cutSceneActive + "|" + playerLoading + "|" +
                bossReportOpen + "|" + encounterReportOpen + "|" +
                bossReportPaused + "|" + encounterReportPaused +
                "|" + hud.IsActiveInHierarchy + "|" + reportWindow.State(now) +
                "|" + presentationBlock;
            if (string.Equals(signature, lastVisibilityDiagnostic,
                StringComparison.Ordinal)) return;
            lastVisibilityDiagnostic = signature;

            DeveloperLogger.RecordCombatInsightsVisibility(reason.ToString(),
                ModSettings.DisplayPolicy.ToString(), viewMode.ToString(),
                encounterActive, bossEncounter.Active, encounterReportOpen,
                bossReportOpen, encounterReportPaused, bossReportPaused,
                hudHiddenByUser, hud.IsAttached, hud.IsActiveInHierarchy,
                controlCount, controlType, levelUpIndicatorVisible,
                flashScreenVisible, screenFading, cutSceneActive, playerLoading,
                reportWindow.State(now).ToString(), presentationBlock.ToString());
        }

        private string HandleStatisticsShortcut(bool contextAllowed, float now)
        {
            PlayerInputController input = PlayerInputController.Instance;
            NativeControlCoordinator.PreparePlayerInput(input);
            InputAction action = NativeInputActions.FindShortcut(
                input?.playerInput?.actions, ModShortcuts.ToggleDamageStatistics);
            CombatInsightsShortcutAction triggered = statisticsShortcut.Update(
                StatisticsCaptureEnabled && (contextAllowed || statisticsBrowser != null && statisticsBrowser.IsControlEnabled) &&
                    !InputDeviceState.HasKeyboardModifierPressed &&
                    action != null && action.enabled,
                action?.WasPressedThisFrame() ?? false,
                action?.IsPressed() ?? false,
                action?.WasReleasedThisFrame() ?? false, now);
            if (triggered == CombatInsightsShortcutAction.None) return null;
            if (presentationBlock != ReportPresentationBlock.None &&
                (statisticsBrowser == null || !statisticsBrowser.IsControlEnabled))
                return CombatInsightsNotifications.BlockedMessage(presentationBlock);
            if (triggered == CombatInsightsShortcutAction.ToggleDisplay)
            {
                hudHiddenByUser = !hudHiddenByUser;
                if (hudHiddenByUser)
                {
                    if (statisticsBrowser != null) statisticsBrowser.Close();
                    hud.Hide();
                }
                return hudHiddenByUser ? ModLocalization.DamageStatisticsDisplayHidden
                    : ModLocalization.DamageStatisticsDisplayRestored;
            }
            if (triggered != CombatInsightsShortcutAction.ToggleStatistics) return null;
            if (statisticsBrowser != null && statisticsBrowser.IsControlEnabled)
            {
                statisticsBrowser.Close();
                return ModLocalization.StatisticsClosed;
            }
            if (!hudHiddenByUser && reportWindow.TryDismiss(now))
                return ModLocalization.StatisticsClosed;
            if (encounterActive || bossEncounter.Active || FindLocal()?.IsInBattle == true)
                return null;
            return OpenStatisticsBrowser() ? ModLocalization.StatisticsOpened
                : ModLocalization.EncounterReportHudUnavailable;
        }

        internal bool OpenStatisticsBrowser(UI_PausePanel pausePanel = null)
        {
            if (!CanBrowseStatistics) return false;
            UI_PausePanel pause = pausePanel ?? UIManager.Instance?.GetElement<UI_PausePanel>();
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            ReportPresentationBlock block = NativeReportPresentation.ReadBlock(UIManager.Instance, player);
            if (block != ReportPresentationBlock.None &&
                !(block == ReportPresentationBlock.Menu && pausePanel != null && pausePanel.IsControlEnabled))
                return false;
            if (pause == null || pause.ParentRoot == null) return false;
            if (statisticsBrowser == null)
                statisticsBrowser = NativeStatisticsBrowser.Create(pause, this);
            if (statisticsBrowser == null) return false;
            reportWindow.Clear(ReportDisplayState.Dismissed);
            hud.Hide();
            hudHiddenByUser = false;
            statisticsBrowser.Show(pausePanel == null);
            return true;
        }

        internal bool TryCloseStatisticsBrowser()
        {
            if (statisticsBrowser == null || !statisticsBrowser.IsControlEnabled) return false;
            statisticsBrowser.Close();
            return true;
        }

        private void OnStatisticsContextChanged(LocalGameplayContextChange change)
        {
            if (change == LocalGameplayContextChange.TravelStarted)
                retryStatistics.ObserveTravelStarted();
            if (change == LocalGameplayContextChange.WorldSessionLoaded)
            {
                retryStatistics.ObserveWorldLoaded();
                encounterDefeated = false;
            }
            else if (!retryStatistics.Pending &&
                (change == LocalGameplayContextChange.PlayerChanged || change == LocalGameplayContextChange.FloorChanged))
            {
                retryStatistics.Clear();
                encounterDefeated = false;
            }
            if (change == LocalGameplayContextChange.WorldSessionLoaded ||
                change == LocalGameplayContextChange.PlayerChanged ||
                change == LocalGameplayContextChange.FloorChanged)
                floorStatistics.Clear();
        }

        internal bool CanDismissPresentedReport
        {
            get
            {
                PlayerAvatar player = FindLocal()?.Avatar;
                return isActiveAndEnabled && runtimeCompatible && StatisticsCaptureEnabled &&
                    !hudHiddenByUser && hud.IsReportPresented &&
                    reportWindow.IsVisible(Time.unscaledTime) && player != null &&
                    !player.activeMagicCastModeClientside &&
                    NativeReportPresentation.ReadBlock(UIManager.Instance, player) ==
                        ReportPresentationBlock.None;
            }
        }

        internal bool TryDismissPresentedReport()
        {
            if (!CanDismissPresentedReport ||
                !reportWindow.TryDismiss(Time.unscaledTime)) return false;
            hud.Hide();
            return true;
        }

        private void SamplePlayers(float now, bool trackOrdinaryEncounters)
        {
            IReadOnlyList<PlayerSpawner> players = PlayerSpawner.MultiplayerList;
            activeKeys.Clear();
            PlayerAvatar localAvatar = LocalPlayerResolver.Resolve();
            string observedFloor = localAvatar?.NetworkcurrentFloorGuid;
            floorStatistics.ObserveFloor(observedFloor);
            if (players != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerSpawner spawner = players[i];
                    PlayerAvatar avatar = spawner?.PlayerAvatar;
                    if (avatar == null) continue;
                    UpsertPlayer(spawner, avatar, false, now);
                }
            }
            long localKey = localAvatar != null ? GetPlayerKey(localAvatar) : 0;
            if (localAvatar != null && !activeKeys.Contains(localKey))
            {
                UpsertPlayer(localAvatar.GetComponent<PlayerSpawner>(), localAvatar, true, now);
            }
            if (floorGuid == null) floorGuid = observedFloor;
            else if (!string.Equals(floorGuid, observedFloor, StringComparison.Ordinal))
            {
                floorGuid = observedFloor;
                ResetCombatState();
            }
            staleKeys.Clear();
            foreach (long key in states.Keys) if (!activeKeys.Contains(key)) staleKeys.Add(key);
            for (int i = 0; i < staleKeys.Count; i++) states.Remove(staleKeys[i]);
            ordered.Clear();
            foreach (PlayerDamageState state in states.Values)
                if (ShouldIncludePlayer(state)) ordered.Add(state);
            if (bossEncounter.Active) ordered.Sort(CompareBossDamage);
            else ordered.Sort(CompareRollingDps);
            PlayerDamageState local = FindLocal();
            if (trackOrdinaryEncounters && !bossEncounter.Active &&
                local?.Avatar != null &&
                local.IsInBattle &&
                encounterAreaLocator.TryLocate(local.Avatar,
                    out EncounterScope scope))
            {
                EnsureAreaEncounter(scope, now);
            }
        }

        private void UpsertPlayer(PlayerSpawner spawner, PlayerAvatar avatar, bool forceLocal, float now)
        {
            long key = GetPlayerKey(avatar);
            activeKeys.Add(key);
            if (!states.TryGetValue(key, out PlayerDamageState state))
            {
                state = new PlayerDamageState(key);
                states.Add(key, state);
            }
            state.Avatar = avatar;
            state.Spawner = spawner;
            state.IsLocal = forceLocal || LocalPlayerResolver.IsLocal(spawner, avatar);
            state.IsInBattle = avatar.IsInBattle;
            state.Name = string.IsNullOrWhiteSpace(avatar.Name) ? "Player" : avatar.Name;
            state.RollingDps = state.Window.Dps(now);
        }

        private bool EnsureAreaEncounter(EncounterScope scope, float now)
        {
            if (retryStatistics.Pending || encounterDefeated || scope == null || scope.Kind != EncounterScopeKind.Ordinary ||
                bossEncounter.Active) return false;
            if (runtimeKernel?.IsOrdinaryEncounterCleared(
                    scope.SourceInstanceId) == true)
                return false;
            if (encounterActive && encounterScope != null && encounterScope.IsSame(scope)) return true;
            // Area selection alone does not establish a new fight. Keep the
            // previous report until this encounter records damage or a defeat.
            encounterScope = scope;
            encounterActive = true;
            encounterStartedAt = Time.time;
            floorStatistics.UpdateClock(Time.time, true);
            encounterEndedAt = -1f;
            encounterLastActivity = now;
            encounterDamage = 0f;
            majorEncounter = false;
            defeats.Reset();
            foreach (PlayerDamageState state in states.Values)
            {
                state.EncounterDamage = 0f;
                state.Window.Reset();
                state.RollingDps = 0f;
            }
            damageByType.Clear();
            damageTypeContexts.Clear();
            return true;
        }

        private void EndEncounter(float now)
        {
            if (!encounterActive) return;
            floorStatistics.UpdateClock(Time.time, false);
            encounterActive = false;
            encounterEndedAt = Time.time;
            encounterAreaLocator.InvalidateSelection();
            if (encounterDamage > 0f || defeats.DefeatedCount > 0)
                PublishEncounterReport(CreateEncounterReport(
                    EncounterReportKind.Ordinary), now);
        }

        private EncounterReportSnapshot CreateEncounterReport(
            EncounterReportKind kind)
        {
            var players = new List<CombatStatisticsPlayerSnapshot>(4);
            foreach (PlayerDamageState state in states.Values)
            {
                float damage = kind == EncounterReportKind.Boss
                    ? bossEncounter.GetDamage(state.Key)
                    : state.EncounterDamage;
                if (!state.IsLocal && damage <= 0f) continue;
                players.Add(new CombatStatisticsPlayerSnapshot(state.Key,
                    state.Name, state.IsLocal, damage));
            }
            players.Sort(CompareReportPlayers);
            var damageTypes = new List<CombatStatisticsDamageTypeSnapshot>(
                damageByType.Count);
            foreach (KeyValuePair<EncounterDamageType, float> type in
                damageByType)
            {
                if (type.Value > 0f)
                    damageTypes.Add(new CombatStatisticsDamageTypeSnapshot(
                        type.Key, type.Value));
            }
            damageTypes.Sort((left, right) =>
                right.Damage.CompareTo(left.Damage));

            float duration = kind == EncounterReportKind.Boss
                ? bossEncounter.Elapsed(Time.time)
                : Mathf.Max(0f, encounterEndedAt - encounterStartedAt);
            return new EncounterReportSnapshot(kind, players, duration,
                defeats.NormalDefeated, defeats.MinibossDefeated,
                defeats.BossDefeated, defeats.LocalFinalBlows, damageTypes);
        }

        private void PublishEncounterReport(EncounterReportSnapshot report,
            float now)
        {
            reportWindow.Clear();
            if (report == null ||
                (report.TotalDamage <= 0f && report.DefeatedCount == 0))
                return;
            encounterReport = report;
            if (!StatisticsBrowserOpen &&
                (ModSettings.DisplayPolicy != CombatInsightsDisplayPolicy.BossOnly ||
                    report.Kind == EncounterReportKind.Boss))
                reportWindow.Start(now, EncounterReportPresentationPolicy.DisplaySeconds(report));
        }

        private void ClearEncounterReport()
        {
            reportWindow.Clear();
            encounterReport = null;
        }

        private static int CompareReportPlayers(
            CombatStatisticsPlayerSnapshot left,
            CombatStatisticsPlayerSnapshot right)
        {
            int damage = right.Damage.CompareTo(left.Damage);
            if (damage != 0) return damage;
            if (left.IsLocal != right.IsLocal) return left.IsLocal ? -1 : 1;
            return left.Key.CompareTo(right.Key);
        }

        private PlayerDamageState FindLocal()
        {
            for (int i = 0; i < ordered.Count; i++) if (ordered[i].IsLocal) return ordered[i];
            return null;
        }

        private void TrackLocalIdentity(PlayerDamageState local, float now)
        {
            if (local != null || ordered.Count == 0)
            {
                localIdentityMissingSince = -1f;
                localIdentityWarningLogged = false;
                return;
            }
            if (localIdentityMissingSince < 0f) { localIdentityMissingSince = now; return; }
            if (localIdentityWarningLogged || now - localIdentityMissingSince < 2f) return;
            int owned = 0, localPlayers = 0;
            for (int index = 0; index < ordered.Count; index++)
            {
                PlayerSpawner spawner = ordered[index].Spawner;
                if (spawner == null) continue;
                if (spawner.isOwned) owned++;
                if (spawner.isLocalPlayer) localPlayers++;
            }
            uint observerNetId = GameCamera.Instance?.Observer?.netId ?? 0;
            uint mirrorLocalNetId = NetworkClient.localPlayer?.netId ?? 0;
            SupportLogger.Warning("local_player_identity_pending", "[SephiriaEnhancements] HUD is waiting for an authoritative local player identity (players=" +
                ordered.Count + ", owned=" + owned + ", localPlayers=" + localPlayers +
                ", observerNetId=" + observerNetId + ", mirrorLocalNetId=" + mirrorLocalNetId + ").");
            localIdentityWarningLogged = true;
        }

        private void OnDisable()
        {
            if (DeveloperLogger.IsEnabled)
            {
                DeveloperLogger.RecordCombatInsightsVisibility(
                    CombatInsightsVisibilityReason.ControllerDisabled.ToString(),
                    ModSettings.DisplayPolicy.ToString(), ViewMode.ToString(),
                    encounterActive, bossEncounter.Active,
                    encounterReport?.Kind == EncounterReportKind.Ordinary &&
                        reportWindow.IsOpen(Time.unscaledTime),
                    encounterReport?.Kind == EncounterReportKind.Boss &&
                        reportWindow.IsOpen(Time.unscaledTime),
                    encounterReport?.Kind == EncounterReportKind.Ordinary &&
                        reportWindow.IsPaused,
                    encounterReport?.Kind == EncounterReportKind.Boss &&
                        reportWindow.IsPaused,
                    hudHiddenByUser, hud.IsAttached, hud.IsActiveInHierarchy,
                    0, null, false, false, false, false, false,
                    reportWindow.State(Time.unscaledTime).ToString(),
                    presentationBlock.ToString());
            }
            if (statisticsBrowser != null) statisticsBrowser.Close();
            hud.Hide();
            hitStreakFeedback.Hide();
        }
        private void OnDestroy()
        {
            Initialize(null);
            hud.Dispose();
            hitStreakFeedback.Dispose();
        }
        private void OnApplicationQuit() => DeveloperLogger.Shutdown();

        internal void RecordBossDamage(UnitAvatar target, PlayerAvatar owner, float damage,
            EncounterDamageType damageType)
        {
            PlayerAvatar localAvatar = LocalPlayerResolver.Resolve();
            if (!bossEncounter.Active) encounterAreaLocator.Reset();
            if (retryStatistics.Pending || encounterDefeated || target == null || owner == null || localAvatar == null ||
                localAvatar.loadingScreenType != -1 || !encounterAreaLocator.TryLocate(localAvatar, out EncounterScope scope) ||
                scope.Kind != EncounterScopeKind.Boss) return;
            Vector3 ownerPosition = owner.transform.position;
            Vector3 targetPosition = target.transform.position;
            if ((bossEncounter.Active && scope.SourceInstanceId != bossSourceInstanceId) ||
                !scope.AllowsDamage(owner.NetworkcurrentFloorGuid, ownerPosition.x, ownerPosition.y,
                    targetPosition.x, targetPosition.y)) return;
            if (StatisticsCaptureEnabled && EnsureBossEncounterFromDamage())
            {
                PlayerDamageState state = GetOrCreateDamageState(owner,
                    Time.unscaledTime);
                floorStatistics.ObserveFloor(localAvatar.NetworkcurrentFloorGuid);
                bossEncounter.Record(state.Key, damage);
                floorStatistics.RecordDamage(state.Key, state.Name, state.IsLocal, damage, damageType);
                RecordDamageType(damageType, damage);
            }
        }

        internal void RecordCombatDamage(UnitAvatar target, PlayerAvatar owner,
            float damage, EncounterDamageType damageType)
        {
            if (retryStatistics.Pending || encounterDefeated || !StatisticsCaptureEnabled || target == null || owner == null ||
                damage <= 0f || bossEncounter.Active ||
                !IsHostileEnemy(target)) return;
            PlayerAvatar localAvatar = LocalPlayerResolver.Resolve();
            if (localAvatar == null || localAvatar.loadingScreenType != -1 || !encounterAreaLocator.TryLocate(localAvatar,
                out EncounterScope scope)) return;
            Vector3 ownerPosition = owner.transform.position;
            Vector3 targetPosition = target.transform.position;
            if (!scope.AllowsDamage(owner.NetworkcurrentFloorGuid, ownerPosition.x,
                ownerPosition.y, targetPosition.x, targetPosition.y)) return;

            float now = Time.unscaledTime;
            floorStatistics.ObserveFloor(localAvatar.NetworkcurrentFloorGuid);
            if (!EnsureAreaEncounter(scope, now)) return;
            PlayerDamageState state = GetOrCreateDamageState(owner, now);
            state.Window.Record(now, damage);
            state.RollingDps = state.Window.Dps(now);
            state.EncounterDamage += damage;
            floorStatistics.RecordDamage(state.Key, state.Name, state.IsLocal, damage, damageType);
            encounterDamage += damage;
            RecordDamageType(damageType, damage);
            encounterLastActivity = now;
            if (target.monsterType == EMonsterType.Miniboss ||
                target.monsterType == EMonsterType.Boss)
                majorEncounter = true;
        }

        private void BeginBossEncounter(int sourceInstanceId = 0)
        {
            PlayerAvatar local = LocalPlayerResolver.Resolve();
            if (retryStatistics.Pending || encounterDefeated || local == null || local.loadingScreenType != -1 ||
                !encounterAreaLocator.TryLocate(local, out EncounterScope scope) || scope.Kind != EncounterScopeKind.Boss) return;
            if (sourceInstanceId != 0 && scope.SourceInstanceId != sourceInstanceId) return;
            if (!StatisticsCaptureEnabled)
            {
                ResetCombatState();
                return;
            }
            if (bossEncounter.Active)
            {
                if (scope.SourceInstanceId != bossSourceInstanceId) return;
                encounterScope = scope;
                if (bossEncounter.Resume(Time.time))
                {
                    floorStatistics.UpdateClock(Time.time, true);
                    reportWindow.CloseForEncounter(true, false, false);
                }
                return;
            }
            if (!bossEncounter.Begin(Time.time))
                return;
            BeginFreshEncounter(Time.unscaledTime);
            bossSourceInstanceId = scope.SourceInstanceId;
            encounterScope = scope;
            majorEncounter = true;
        }

        private void PauseBossEncounter()
        {
            if (!StatisticsCaptureEnabled)
                return;
            bossEncounter.Pause(Time.time);
            floorStatistics.UpdateClock(Time.time, false);
        }

        private void OnEncounterLifecycleChanged(
            EncounterLifecycleEvent lifecycleEvent)
        {
            if (lifecycleEvent == null)
            {
                return;
            }

            if (lifecycleEvent.Transition ==
                EncounterTransition.GameplayContextReset)
            {
                ResetGameplayContext();
                return;
            }

            if (lifecycleEvent.Kind == EncounterKind.Ordinary)
            {
                if (lifecycleEvent.Transition == EncounterTransition.Cleared &&
                    encounterActive && encounterScope != null &&
                    encounterScope.SourceInstanceId ==
                        lifecycleEvent.SourceInstanceId)
                {
                    EndEncounter(lifecycleEvent.OccurredAt);
                }
                return;
            }

            if (lifecycleEvent.Kind != EncounterKind.Boss)
            {
                return;
            }

            bool starts = lifecycleEvent.Transition == EncounterTransition.Started ||
                lifecycleEvent.Transition == EncounterTransition.Resumed;
            if (starts) encounterAreaLocator.Reset();
            else if (!bossEncounter.Active || bossSourceInstanceId !=
                (lifecycleEvent.Transition == EncounterTransition.ContinuationPrepared
                    ? lifecycleEvent.PreviousSourceInstanceId : lifecycleEvent.SourceInstanceId)) return;

            switch (lifecycleEvent.Transition)
            {
                case EncounterTransition.Started:
                case EncounterTransition.Resumed:
                    BeginBossEncounter(lifecycleEvent.SourceInstanceId);
                    break;
                case EncounterTransition.Paused:
                case EncounterTransition.CompletionStarted:
                    PauseBossEncounter();
                    break;
                case EncounterTransition.ContinuationPrepared:
                    bossSourceInstanceId = lifecycleEvent.SourceInstanceId;
                    PauseBossEncounter();
                    break;
                case EncounterTransition.Cleared:
                    CompleteBossEncounter();
                    break;
                case EncounterTransition.Defeated:
                    DefeatBossEncounter();
                    break;
                default:
                    return;
            }
        }

        internal bool EnsureBossEncounterFromDamage()
        {
            if (retryStatistics.Pending || encounterDefeated || !StatisticsCaptureEnabled) return false;
            if (bossEncounter.Active) return true;
            BeginBossEncounter();
            if (bossEncounter.Active)
            {
                SupportLogger.Info("boss_report_damage_fallback", "[SephiriaEnhancements] BOSS report started from damage fallback because the native battle-start callback was not observed.");
            }
            return bossEncounter.Active;
        }

        private void CompleteBossEncounter()
        {
            if (!StatisticsCaptureEnabled)
            {
                ResetCombatState();
                return;
            }
            if (!bossEncounter.End(Time.time))
                return;
            float now = Time.unscaledTime;
            floorStatistics.UpdateClock(Time.time, false);
            encounterActive = false;
            encounterEndedAt = Time.time;
            encounterAreaLocator.InvalidateSelection();
            encounterScope = null;
            PublishEncounterReport(CreateEncounterReport(
                EncounterReportKind.Boss), now);
        }

        private void DefeatBossEncounter()
        {
            if (!bossEncounter.Active)
            {
                return;
            }

            FinishDefeatedEncounter();
        }

        internal void RecordEnemyDeath(UnitAvatar target)
        {
            if (retryStatistics.Pending || encounterDefeated || !StatisticsCaptureEnabled || !IsHostileEnemy(target)) return;
            // The published report is immutable. Once the encounter ends,
            // delayed death callbacks must not mutate the frozen result.
            if (!bossEncounter.Active && !encounterActive) return;
            float now = Time.unscaledTime;
            if (!bossEncounter.Active)
            {
                PlayerAvatar localAvatar = LocalPlayerResolver.Resolve();
                if (localAvatar == null || !encounterAreaLocator.TryLocate(localAvatar,
                    out EncounterScope scope)) return;
                Vector3 targetPosition = target.transform.position;
                if (!scope.Contains(targetPosition.x, targetPosition.y)) return;
                if (!EnsureAreaEncounter(scope, now)) return;
            }
            encounterLastActivity = now;
            EncounterEnemyTier tier = target.monsterType == EMonsterType.Boss
                ? EncounterEnemyTier.Boss : target.monsterType == EMonsterType.Miniboss
                    ? EncounterEnemyTier.Miniboss : EncounterEnemyTier.Normal;
            if (tier != EncounterEnemyTier.Normal) majorEncounter = true;
            uint identity = target.netId != 0 ? target.netId : unchecked((uint)target.GetInstanceID());
            defeats.RecordDefeat(identity, tier);
            floorStatistics.RecordDefeat(identity, tier);
        }

        internal void RecordLocalFinalBlow(UnitKillData data)
        {
            if (retryStatistics.Pending || encounterDefeated || !StatisticsCaptureEnabled ||
                (!bossEncounter.Active && !encounterActive) ||
                string.IsNullOrEmpty(data.factionName))
                return;
            PlayerDamageState local = FindLocal();
            if (local?.Avatar == null || RuntimeFactionManager.Instance == null) return;
            try
            {
                long hostileLayers = local.Avatar.GetHostileFactionLayers(data.fromType);
                if (CombatManager.ContainsAttackableFaction(hostileLayers, data.factionName))
                {
                    defeats.RecordLocalFinalBlow();
                    floorStatistics.RecordLocalFinalBlow();
                }
            }
            catch { }
        }

        private bool IsHostileEnemy(UnitAvatar target)
        {
            if (target == null || target is PlayerAvatar || target.monsterType == EMonsterType.Dummy)
                return false;
            if (target.NetworkLeader is PlayerAvatar) return false;
            PlayerAvatar localAvatar = FindLocal()?.Avatar ??
                CombatManager.Instance?.CurrentPlayer ?? GameCamera.Instance?.Observer;
            if (localAvatar == null) return false;
            try
            {
                return CombatManager.ContainsAttackableFaction(
                    target.GetHostileFactionLayers(EDamageFromType.None), localAvatar.faction);
            }
            catch { return false; }
        }

        private void BeginFreshEncounter(float now)
        {
            floorStatistics.ObserveFloor(LocalPlayerResolver.Resolve()?.NetworkcurrentFloorGuid);
            encounterActive = true;
            encounterStartedAt = Time.time;
            floorStatistics.UpdateClock(Time.time, true);
            encounterEndedAt = -1f;
            encounterLastActivity = now;
            reportWindow.CloseForEncounter(true, false, false);
            encounterDamage = 0f;
            majorEncounter = false;
            defeats.Reset();
            foreach (PlayerDamageState state in states.Values)
            {
                state.EncounterDamage = 0f;
                state.Window.Reset();
                state.RollingDps = 0f;
            }
            damageByType.Clear();
            damageTypeContexts.Clear();
        }

        internal void ResetGameplayContext()
        {
            states.Clear();
            ordered.Clear();
            floorGuid = null;
            encounterScope = null;
            encounterAreaLocator.Reset();
            localIdentityMissingSince = -1f;
            localIdentityWarningLogged = false;
            runtimeSuspended = false;
            statisticsWereEnabled = true;
            ResetCombatState();
            hitStreakFeedback.Reset();
            hud.InvalidateLayout();
        }

        internal void RecordDamageDetail(UnitAvatar target, DamageData damage)
        {
            if (StatisticsCaptureEnabled && target != null)
            {
                EncounterDamageType damageType = MapDamageType(
                    damage.elementalType);
                bool indirect = damage.damageType ==
                    EDamageType.ElementalEffectDamage;
                float now = Time.unscaledTime;
                int targetId = target.GetInstanceID();
                RecordDamageTypeContext(now, targetId, damage.damage,
                    damage.position, indirect, damageType, truncate: true);
                RecordDamageTypeContext(now, targetId, damage.shieldDamage,
                    damage.position, indirect, damageType, truncate: false);
                RecordDamageTypeContext(now, targetId, damage.mpShieldDamage,
                    damage.position, indirect, damageType, truncate: false);
            }
            if (EnhancementsSettings.Enabled && ModSettings.HitStreakFeedback)
                hitStreakFeedback.CaptureDamageDetail(target, damage);
        }

        internal EncounterDamageType ResolveDamageType(UnitAvatar target,
            DamageFeedback feedback)
        {
            if (target == null || feedback == null)
                return EncounterDamageType.Unknown;
            return damageTypeContexts.TryMatchDamageType(Time.unscaledTime,
                target.GetInstanceID(), feedback.damageValue,
                feedback.position.x, feedback.position.y,
                out EncounterDamageType damageType)
                    ? damageType : EncounterDamageType.Unknown;
        }

        internal void RecordHitStreakFeedback(DamageFeedback feedback, PlayerAvatar attacker)
        {
            if (!EnhancementsSettings.Enabled || !ModSettings.HitStreakFeedback ||
                feedback == null || attacker == null ||
                feedback.damageValue <= 0 || feedback.self == null || feedback.self is PlayerAvatar ||
                !LocalPlayerResolver.IsLocal(attacker) ||
                UIManager.Instance?.CurrentControlStack != null) return;
            hitStreakFeedback.CaptureFeedback(feedback);
        }

        private void ResetCombatState()
        {
            if (statisticsBrowser != null) statisticsBrowser.Close();
            floorStatistics.UpdateClock(Time.time, false);
            statisticsShortcut.Reset();
            bossEncounter.Reset();
            bossSourceInstanceId = 0;
            encounterActive = false;
            majorEncounter = false;
            encounterStartedAt = encounterEndedAt = -1f;
            encounterLastActivity = -1000f;
            ClearEncounterReport();
            encounterDamage = 0f;
            encounterScope = null;
            defeats.Reset();
            damageByType.Clear();
            damageTypeContexts.Clear();
        }

        private void RecordDamageType(EncounterDamageType type, float damage)
        {
            if (damage <= 0f) return;
            damageByType.TryGetValue(type, out float current);
            damageByType[type] = current + damage;
        }

        private void RecordDamageTypeContext(float now, int targetId,
            float damage, Vector3 position, bool indirect,
            EncounterDamageType damageType, bool truncate)
        {
            if (damage <= 0f) return;
            int visibleDamage = truncate ? (int)damage :
                Mathf.RoundToInt(damage);
            damageTypeContexts.Record(now, targetId, visibleDamage,
                position.x, position.y, indirect, damageType);
        }

        private static EncounterDamageType MapDamageType(
            EDamageElementalType nativeType)
        {
            switch (nativeType)
            {
                case EDamageElementalType.Physical:
                    return EncounterDamageType.Physical;
                case EDamageElementalType.Fire:
                    return EncounterDamageType.Fire;
                case EDamageElementalType.Ice:
                    return EncounterDamageType.Ice;
                case EDamageElementalType.Lightning:
                    return EncounterDamageType.Lightning;
                case EDamageElementalType.Chaos:
                    return EncounterDamageType.Chaos;
                case EDamageElementalType.Normal:
                    return EncounterDamageType.Normal;
                case EDamageElementalType.IceAndLightning:
                case EDamageElementalType.FireAndIce:
                case EDamageElementalType.FireAndLightning:
                    return EncounterDamageType.Mixed;
                default:
                    return EncounterDamageType.Unknown;
            }
        }

        private PlayerDamageState GetOrCreateDamageState(PlayerAvatar avatar, float now)
        {
            long key = GetPlayerKey(avatar);
            if (!states.TryGetValue(key, out PlayerDamageState state))
            {
                state = new PlayerDamageState(key);
                states.Add(key, state);
            }
            state.Avatar = avatar;
            state.Name = string.IsNullOrWhiteSpace(avatar.Name) ? "Player" : avatar.Name;
            state.IsLocal = LocalPlayerResolver.IsLocal(avatar);
            state.IsInBattle = avatar.IsInBattle;
            state.RollingDps = state.Window.Dps(now);
            return state;
        }

        private static long GetPlayerKey(PlayerAvatar avatar) =>
            PlayerIdentityKey.Resolve(avatar.netId, avatar.GetInstanceID());

        private bool ShouldIncludePlayer(PlayerDamageState state)
        {
            if (state.IsLocal) return true;
            if (state.Avatar == null || string.IsNullOrEmpty(floorGuid) ||
                !string.Equals(state.Avatar.NetworkcurrentFloorGuid, floorGuid,
                    StringComparison.Ordinal)) return false;
            if (bossEncounter.Active) return true;
            if (encounterScope == null) return false;
            Vector3 position = state.Avatar.transform.position;
            return state.EncounterDamage > 0f || state.RollingDps > 0f ||
                encounterScope.Contains(position.x, position.y);
        }

        private int CompareBossDamage(PlayerDamageState left, PlayerDamageState right)
        {
            int damage = bossEncounter.GetDamage(right.Key).CompareTo(bossEncounter.GetDamage(left.Key));
            return damage != 0 ? damage : CompareIdentity(left, right);
        }

        private static int CompareRollingDps(PlayerDamageState left, PlayerDamageState right)
        {
            int dps = right.RollingDps.CompareTo(left.RollingDps);
            return dps != 0 ? dps : CompareIdentity(left, right);
        }

        private static int CompareIdentity(PlayerDamageState left, PlayerDamageState right)
        {
            if (left.IsLocal != right.IsLocal) return left.IsLocal ? -1 : 1;
            return left.Key.CompareTo(right.Key);
        }

        internal sealed class PlayerDamageState
        {
            internal PlayerDamageState(long key) { Key = key; Window = new RollingDamageWindow(5f); }
            internal long Key { get; }
            internal string Name { get; set; } = "Player";
            internal bool IsLocal { get; set; }
            internal bool IsInBattle { get; set; }
            internal float RollingDps { get; set; }
            internal float EncounterDamage { get; set; }
            internal RollingDamageWindow Window { get; }
            internal PlayerAvatar Avatar { get; set; }
            internal PlayerSpawner Spawner { get; set; }
        }
    }
}
