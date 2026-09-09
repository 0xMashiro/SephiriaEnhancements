using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Integration;
using UnityEngine;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class DefeatRetryFeature
    {
        private sealed class RetryPlacement
        {
            internal RetryPlacement(string floorGuid, string spawnPoint,
                Vector3 position)
            {
                FloorGuid = floorGuid;
                SpawnPoint = spawnPoint;
                Position = position;
            }

            internal string FloorGuid { get; }
            internal string SpawnPoint { get; }
            internal Vector3 Position { get; }
        }

        private sealed class RetryCheckpoint
        {
            internal RetryCheckpoint(RetryCheckpointKind kind,
                SaveData current, SaveData currentRun, string bossName,
                string floorGuid, Dictionary<uint, RetryPlacement> placements,
                BossRetryWorld world)
            {
                Kind = kind;
                Current = current;
                CurrentRun = currentRun;
                BossName = bossName;
                FloorGuid = floorGuid;
                Placements = placements;
                World = world;
            }

            internal RetryCheckpointKind Kind { get; }
            internal SaveData Current { get; }
            internal SaveData CurrentRun { get; }
            internal string BossName { get; }
            internal string FloorGuid { get; }
            internal Dictionary<uint, RetryPlacement> Placements { get; }
            internal BossRetryWorld World { get; }
            internal long StatisticsCheckpointId { get; set; }
            internal bool RebuildBossFloor { get; set; }
        }

        private static readonly FieldInfo CurrentField =
            AccessTools.Field(typeof(SaveManager), "current");
        private static readonly FieldInfo CurrentRunField =
            AccessTools.Field(typeof(SaveManager), "currentRun");
        private static readonly FieldInfo NativeRestartingField =
            AccessTools.Field(typeof(HorayNetworkManager), "restarting");
        private static readonly FieldInfo BossBattlePhaseField =
            AccessTools.Field(typeof(BossSpawner), "battlePhase");
        private static readonly FieldInfo SeedBossSpawnStateField =
            AccessTools.Field(typeof(SeedBossSpawner), "spawnState");
        private static readonly MethodInfo SaveDungeonSessionMethod =
            AccessTools.Method(typeof(DungeonManager), "SaveCurrentSessionData",
                new[] { typeof(string) });

        private static readonly RetryCheckpoints<RetryCheckpoint> checkpoints =
            new RetryCheckpoints<RetryCheckpoint>();
        private static BossRetryWorld pendingWorldRestore;
        private static Dictionary<uint, RetryPlacement> pendingPlacements;
        private static string runFileName = string.Empty;

        internal static bool IsRetrying { get; private set; }

        internal static void CaptureFloorEntryCheckpoint()
        {
            SaveData current = SaveManager.Current;
            SaveData currentRun = SaveManager.CurrentRun;
            if (!DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(
                    EnhancementsSettings.Enabled, DefeatRetrySettings.Enabled,
                    IsRetrying, NetworkServer.active, current != null,
                    currentRun != null,
                    currentRun?.GetBool("RunStarted", false) == true))
            {
                return;
            }

            string floorGuid = currentRun.GetString("LastFloorGuid", string.Empty);
            if ((checkpoints.FloorGuid == floorGuid && checkpoints.FloorEntry != null) ||
                !AllPlayersOnFloor(floorGuid))
            {
                return;
            }
            CaptureCheckpoint(RetryCheckpointKind.FloorEntry, current, currentRun,
                string.Empty, floorGuid, CaptureCurrentPlacements(),
                "native_run_save");
        }

        internal static void CaptureRenderedCombatFloorFallback(string floorGuid)
        {
            SaveData current = SaveManager.Current;
            SaveData currentRun = SaveManager.CurrentRun;
            DungeonManager dungeon = DungeonManager.Instance;
            FloorData floor = null;
            if (dungeon != null && !string.IsNullOrEmpty(floorGuid))
            {
                dungeon.generatedFloors.TryGetValue(floorGuid, out floor);
            }

            FloorGenerator generator = FindFloorGenerator(floorGuid);
            bool explorationActivated = generator != null &&
                generator.ExplorationActivated;
            bool combatThreat = floor != null && IsCombatThreat(floor.threatType);
            bool checkpointMatchesFloor = checkpoints.FloorEntry != null &&
                string.Equals(checkpoints.FloorGuid, floorGuid,
                    StringComparison.Ordinal);
            bool capture = DefeatRetryPolicy.ShouldCaptureRenderedCombatFloorFallback(
                EnhancementsSettings.Enabled, DefeatRetrySettings.Enabled,
                IsRetrying, NetworkServer.active, current != null,
                currentRun != null,
                currentRun?.GetBool("RunStarted", false) == true,
                explorationActivated, combatThreat, checkpointMatchesFloor);

            DeveloperLogger.RecordRetryFloorEvaluation(floorGuid, floor?.name,
                floor?.stageName, floor?.threatType.ToString(),
                generator?.GetType().Name, explorationActivated,
                checkpoints.FloorEntry?.Kind.ToString() ?? RetryCheckpointKind.None.ToString(),
                checkpointMatchesFloor, capture);
            if (!capture || !AllPlayersOnFloor(floorGuid))
            {
                return;
            }

            try
            {
                SerializeCurrentSession(floorGuid);
                CaptureCheckpoint(RetryCheckpointKind.FloorEntry, current,
                    currentRun, string.Empty, floorGuid,
                    CaptureCurrentPlacements(), "rendered_combat_floor_fallback");
            }
            catch (Exception ex)
            {
                SupportLogger.Error("retry_floor_checkpoint_failed", "[SephiriaEnhancements] Rendered combat floor " +
                    "checkpoint capture failed; keeping the previous checkpoint: " + ex);
            }
        }

        internal static void CaptureBossEncounterSnapshot(BossSpawner boss,
            PlayerAvatar challenger, Vector3 encounterPosition, string bossName)
        {
            CaptureBossEncounterSnapshot(challenger, encounterPosition, bossName,
                IsBossEncounterNotStarted(boss), boss);
        }

        internal static void CaptureSeedBossEncounterSnapshot(SeedBossSpawner boss,
            PlayerAvatar challenger)
        {
            bool notStarted = boss != null && SeedBossSpawnStateField != null &&
                Convert.ToInt32(SeedBossSpawnStateField.GetValue(boss)) == 0;
            CaptureBossEncounterSnapshot(challenger,
                challenger != null ? challenger.transform.position : Vector3.zero,
                boss?.bossSocialID?.name, notStarted, null);
        }

        private static void CaptureBossEncounterSnapshot(PlayerAvatar challenger,
            Vector3 encounterPosition, string bossName, bool encounterNotStarted,
            BossSpawner boss)
        {
            SaveData current = SaveManager.Current;
            SaveData currentRun = SaveManager.CurrentRun;
            string floorGuid = challenger?.currentFloorGuid ?? string.Empty;
            // The first encounter snapshot owns every phase until the floor changes
            // or the player explicitly retries the entire floor.
            if (checkpoints.BossEncounterStarted && checkpoints.FloorGuid == floorGuid)
            {
                return;
            }
            if (!DefeatRetryPolicy.ShouldCaptureBossEncounter(
                    EnhancementsSettings.Enabled, DefeatRetrySettings.Enabled,
                    IsRetrying, NetworkServer.active, current != null,
                    currentRun != null,
                    currentRun?.GetBool("RunStarted", false) == true,
                    !string.IsNullOrEmpty(floorGuid),
                    encounterNotStarted &&
                    !string.IsNullOrEmpty(bossName)))
            {
                return;
            }

            // Reserve the encounter even if capture fails: a later phase cannot
            // become a substitute "before battle" checkpoint.
            checkpoints.BeginBoss(floorGuid, null);
            if (checkpoints.FloorEntry == null || !AllPlayersOnFloor(floorGuid))
            {
                return;
            }
            try
            {
                SerializeCurrentSession(floorGuid);

                CaptureCheckpoint(RetryCheckpointKind.BossEncounter, current,
                    currentRun, bossName, floorGuid,
                    CaptureCurrentPlacements(floorGuid, encounterPosition),
                    "boss_spawner", BossRetryWorld.Capture(boss));
                // The library encounter creates sibling golem/hand objects. Restore
                // its serialized floor instead of retaining the later-phase world.
                checkpoints.BossEncounter.RebuildBossFloor = NativeRetryBoss.RequiresFloorRebuild(boss);
            }
            catch (Exception ex)
            {
                SupportLogger.Error("retry_boss_checkpoint_failed", "[SephiriaEnhancements] Boss encounter checkpoint " +
                    "capture failed; keeping the previous floor checkpoint: " + ex);
            }
        }

        private static bool IsBossEncounterNotStarted(BossSpawner boss)
        {
            return boss != null && BossBattlePhaseField != null &&
                Convert.ToInt32(BossBattlePhaseField.GetValue(boss)) == 0;
        }

        private static void SerializeCurrentSession(string floorGuid)
        {
            if (SaveDungeonSessionMethod == null || DungeonManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Native session serializer is unavailable.");
            }

            SaveDungeonSessionMethod.Invoke(DungeonManager.Instance,
                new object[] { floorGuid });
            if (PlayerSpawner.MultiplayerList == null)
            {
                return;
            }
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                player?.SaveCurrentSessionData();
            }
        }

        private static FloorGenerator FindFloorGenerator(string floorGuid)
        {
            if (string.IsNullOrEmpty(floorGuid) ||
                FloorGenerator.FloorGenerators == null)
            {
                return null;
            }

            foreach (FloorGenerator generator in FloorGenerator.FloorGenerators)
            {
                if (generator != null && string.Equals(generator.guid, floorGuid,
                        StringComparison.Ordinal))
                {
                    return generator;
                }
            }
            return null;
        }

        private static bool IsCombatThreat(EFloorThreatType threatType)
        {
            switch (threatType)
            {
                case EFloorThreatType.UnknownBattle:
                case EFloorThreatType.Battle:
                case EFloorThreatType.HardBattle:
                case EFloorThreatType.MiniBoss:
                case EFloorThreatType.Boss:
                case EFloorThreatType.QliphothScenario:
                case EFloorThreatType.BattleFloor:
                    return true;
                default:
                    return false;
            }
        }

        private static void CaptureCheckpoint(RetryCheckpointKind kind,
            SaveData current, SaveData currentRun, string bossName,
            string floorGuid, Dictionary<uint, RetryPlacement> placements,
            string source, BossRetryWorld world = null)
        {
            if (placements.Count == 0)
            {
                SupportLogger.Warning("retry_placement_missing", "[SephiriaEnhancements] Retry checkpoint has no " +
                    "player placement and was ignored.");
                return;
            }

            long started = Stopwatch.GetTimestamp();
            var captured = new RetryCheckpoint(kind, current.Copy(),
                currentRun.Copy(), bossName, floorGuid, placements, world);
            float elapsedMilliseconds = (float)((Stopwatch.GetTimestamp() - started) *
                1000d / Stopwatch.Frequency);

            bool accepted = kind == RetryCheckpointKind.FloorEntry
                ? checkpoints.EnterFloor(floorGuid, captured)
                : checkpoints.BossEncounterStarted;
            if (!accepted)
            {
                return;
            }
            if (kind == RetryCheckpointKind.BossEncounter)
            {
                checkpoints.CompleteBossCapture(captured);
                captured.StatisticsCheckpointId = DefeatRetryBridge.CaptureBoss(floorGuid);
            }
            runFileName = captured.CurrentRun.BindedFileName ?? string.Empty;
            FloorData floor = null;
            DungeonManager.Instance?.generatedFloors.TryGetValue(floorGuid, out floor);
            FloorGenerator generator = FindFloorGenerator(floorGuid);
            DeveloperLogger.RecordRetryCheckpointCapture(elapsedMilliseconds,
                kind.ToString(), source, floorGuid, floor?.name, floor?.stageName,
                floor?.threatType.ToString(), generator?.GetType().Name,
                bossName, placements.Count);
            SupportLogger.Info("retry_checkpoint_captured", "[SephiriaEnhancements] Captured " +
                (kind == RetryCheckpointKind.BossEncounter
                    ? "boss encounter" : "floor-entry") +
                " retry checkpoint: " +
                captured.CurrentRun.GetString("LastFloorGuid", string.Empty));
        }

        private static Dictionary<uint, RetryPlacement> CaptureCurrentPlacements(
            string sharedFloorGuid = null, Vector3? sharedPosition = null)
        {
            var placements = new Dictionary<uint, RetryPlacement>();
            if (PlayerSpawner.MultiplayerList == null)
            {
                return placements;
            }

            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                PlayerAvatar avatar = player?.PlayerAvatar;
                if (avatar == null || avatar.netIdentity == null)
                {
                    continue;
                }

                string floorGuid = string.IsNullOrEmpty(sharedFloorGuid)
                    ? avatar.currentFloorGuid : sharedFloorGuid;
                if (string.IsNullOrEmpty(floorGuid))
                {
                    continue;
                }

                placements[avatar.netIdentity.netId] = new RetryPlacement(
                    floorGuid, avatar.currentSpawnPoint ?? string.Empty,
                    sharedPosition ?? avatar.transform.position);
            }
            return placements;
        }

        internal static void Reset()
        {
            if (IsRetrying)
            {
                return;
            }

            checkpoints.Clear();
            DefeatRetryBridge.ClearArrivals();
            pendingWorldRestore = null;
            BossRetryWorld.ClearRecipes();
            pendingPlacements = null;
            runFileName = string.Empty;
        }

        private static bool AllPlayersOnFloor(string floorGuid)
        {
            if (string.IsNullOrEmpty(floorGuid) || PlayerSpawner.MultiplayerList == null ||
                PlayerSpawner.MultiplayerList.Count == 0)
            {
                return false;
            }
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                if (player?.PlayerAvatar == null || player.PlayerAvatar.currentFloorGuid != floorGuid)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool MatchesPlayers(RetryCheckpoint checkpoint)
        {
            if (checkpoint == null || !AllPlayersOnFloor(checkpoint.FloorGuid) ||
                checkpoint.Placements.Count != PlayerSpawner.MultiplayerList.Count)
            {
                return false;
            }
            foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
            {
                if (!checkpoint.Placements.ContainsKey(player.PlayerAvatar.netId))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool CanRetry(UI_GameOverLabel panel, RetryCheckpointKind kind)
        {
            DungeonManager dungeon = DungeonManager.Instance;
            HorayNetworkManager manager = NetworkManager.singleton as HorayNetworkManager;
            bool nativeRestarting = manager != null && NativeRestartingField != null &&
                (bool)NativeRestartingField.GetValue(manager);

            return panel != null && dungeon != null && manager != null &&
                DefeatRetryBridge.AllPlayersReady() &&
                DefeatRetryPolicy.ShouldOffer(EnhancementsSettings.Enabled,
                    DefeatRetrySettings.Enabled, MatchesPlayers(checkpoints.Get(kind)),
                    NetworkServer.active, dungeon.isRunStarted, panel.openType,
                    dungeon.isGiveUpRun, SaveManager.IsSaving == SaveManager.ESaveState.None,
                    nativeRestarting);
        }

        private static bool CanPresent(UI_GameOverLabel panel, RetryCheckpointKind kind)
        {
            DungeonManager dungeon = DungeonManager.Instance;
            return panel != null && dungeon != null &&
                DefeatRetryPolicy.ShouldOffer(EnhancementsSettings.Enabled,
                    DefeatRetrySettings.Enabled, MatchesPlayers(checkpoints.Get(kind)),
                    NetworkServer.active, dungeon.isRunStarted, panel.openType,
                    dungeon.isGiveUpRun, saveIdle: true, nativeRestarting: false);
        }

        internal static void AddButton(UI_GameOverLabel panel)
        {
            DefeatRetryButton view = panel?.GetComponent<DefeatRetryButton>();
            if (view == null && panel != null)
            {
                view = panel.gameObject.AddComponent<DefeatRetryButton>();
            }

            bool floor = CanPresent(panel, RetryCheckpointKind.FloorEntry);
            bool boss = CanPresent(panel, RetryCheckpointKind.BossEncounter);
            bool bossReady = boss && CanRestoreBossWorld();
            view?.Configure(panel, floor, boss, bossReady);
        }

        private static bool CanRestoreBossWorld()
        {
            try
            {
                if (checkpoints.BossEncounter?.RebuildBossFloor == true)
                    return NativeRetryBoss.CanRebuildLibraryFloor(checkpoints.BossEncounter.FloorGuid);
                return checkpoints.BossEncounter?.World?.CanRestore() == true;
            }
            catch (Exception ex)
            {
                SupportLogger.Error("retry_boss_validation_failed",
                    "[SephiriaEnhancements] Boss retry validation failed: " + ex);
                return false;
            }
        }

        internal static void TryRetry(UI_GameOverLabel panel, RetryCheckpointKind kind)
        {
            RetryCheckpoint selected = checkpoints.Get(kind);
            if (!CanRetry(panel, kind) || selected == null || CurrentField == null ||
                CurrentRunField == null)
            {
                return;
            }
            if (kind == RetryCheckpointKind.BossEncounter && !CanRestoreBossWorld())
            {
                AddButton(panel);
                return;
            }

            try
            {
                SaveData restoredCurrent = selected.Current.Copy();
                SaveData restoredRun = selected.CurrentRun.Copy();
                PreserveSeenBossState(restoredCurrent, selected.BossName);
                restoredCurrent.enableSave = true;
                restoredRun.enableSave = true;
                CurrentField.SetValue(null, restoredCurrent);
                CurrentRunField.SetValue(null, restoredRun);

                pendingPlacements = new Dictionary<uint, RetryPlacement>(
                    selected.Placements);
                pendingWorldRestore = selected.World;
                if (selected.RebuildBossFloor) BossRetryWorld.ClearRecipes();
                if (kind == RetryCheckpointKind.FloorEntry)
                {
                    checkpoints.RestartFloor();
                    BossRetryWorld.ClearRecipes();
                }
                IsRetrying = true;
                NativeRetryTravel.CancelDefeatedWorldTravel(DungeonManager.Instance);
                panel.button.interactable = false;
                panel.Close();
                SaveManager.Save(saveCurrent: true, saveCurrentRun: true);
                DefeatRetryBridge.Publish(kind == RetryCheckpointKind.BossEncounter
                    ? RetryTransition.RetryBoss : RetryTransition.RetryFloor,
                    selected.StatisticsCheckpointId, selected.FloorGuid);
                (NetworkManager.singleton as HorayNetworkManager)?.RestartGame();
                SupportLogger.Info("retry_restart_requested", "[SephiriaEnhancements] Host requested restart from the " +
                    (selected.Kind == RetryCheckpointKind.BossEncounter
                        ? "boss encounter" : "floor-entry") + " checkpoint.");
            }
            catch (Exception ex)
            {
                IsRetrying = false;
                DefeatRetryBridge.FailRecovery();
                pendingPlacements = null;
                pendingWorldRestore = null;
                SupportLogger.Error("retry_failed", "[SephiriaEnhancements] Checkpoint retry failed: " + ex);
            }
        }

        private static void PreserveSeenBossState(SaveData restoredCurrent,
            string bossName)
        {
            SaveData liveCurrent = SaveManager.Current;
            if (liveCurrent == null || restoredCurrent == null ||
                string.IsNullOrEmpty(bossName))
            {
                return;
            }

            string[] keys =
            {
                "BossMet_" + bossName,
                "BossMet_" + bossName + "_T1",
                "BossMet_" + bossName + "_T2"
            };
            foreach (string key in keys)
            {
                if (liveCurrent.GetBool(key, false))
                {
                    restoredCurrent.SetBool(key, true);
                }
            }
        }

        internal static bool HasPendingPlacement(PlayerAvatar avatar)
        {
            return avatar != null && avatar.netIdentity != null &&
                pendingPlacements != null &&
                pendingPlacements.ContainsKey(avatar.netIdentity.netId);
        }

        internal static void ApplyPendingPlacement(PlayerAvatar avatar,
            string requestedFloorGuid, ref string spawnPoint,
            ref Vector3? overridePosition)
        {
            if (avatar == null || avatar != DefeatRetryPlayerRestorePatch.RestoringPlayer || avatar.netIdentity == null ||
                pendingPlacements == null ||
                !pendingPlacements.TryGetValue(avatar.netIdentity.netId,
                    out RetryPlacement placement))
            {
                return;
            }

            if (DefeatRetryPolicy.ShouldApplyPlacement(true, placement.FloorGuid,
                    requestedFloorGuid))
            {
                if (!string.IsNullOrEmpty(placement.SpawnPoint))
                {
                    spawnPoint = placement.SpawnPoint;
                }
                overridePosition = placement.Position;
                SupportLogger.Record("retry_placement_requested", "player=" + avatar.netId);
            }
            else
            {
                throw new InvalidOperationException("Retry destination does not match the checkpoint.");
            }
        }

        internal static bool TryGetPendingDestination(PlayerAvatar avatar, out Vector3 position)
        {
            position = default;
            if (avatar == null || pendingPlacements == null || !pendingPlacements.TryGetValue(avatar.netId, out RetryPlacement placement)) return false;
            position = placement.Position;
            return true;
        }

        internal static void AbortRecovery()
        {
            IsRetrying = false;
            pendingPlacements = null;
            pendingWorldRestore = null;
        }

        internal static void FinishPlayerRestore(PlayerAvatar avatar)
        {
            pendingPlacements?.Remove(avatar.netId);
            if (pendingPlacements?.Count == 0) pendingPlacements = null;
        }

        internal static bool PreserveRunFile(string fileName)
        {
            return IsRetrying && !string.IsNullOrEmpty(runFileName) &&
                string.Equals(fileName, runFileName, StringComparison.Ordinal);
        }

        internal static bool PreserveRunCreation(string fileName)
        {
            return IsRetrying &&
                (string.Equals(fileName, runFileName, StringComparison.Ordinal) ||
                 string.Equals(fileName + "TMP", runFileName, StringComparison.Ordinal));
        }

        internal static void CompleteRestart()
        {
            pendingWorldRestore?.RecreateBoss();
            pendingWorldRestore = null;
            IsRetrying = false;
        }

        internal static bool PreserveBossRetryWorld()
        {
            if (!IsRetrying || pendingWorldRestore == null)
            {
                return false;
            }
            pendingWorldRestore.RemoveEncounterObjects();
            return true;
        }
    }

    internal sealed class DefeatRetryButton : MonoBehaviour
    {
        private UI_GameOverLabel panel;
        private UI_HorayButton originalButton;
        private UI_HorayButton retryButton;
        private UI_HorayButton bossRetryButton;
        private Transform originalParent;
        private RectTransform originalRect;
        private Navigation originalNavigation;
        private GameObject actionGroup;
        private bool stacked;
        private readonly List<RectTransform> resultRoots = new List<RectTransform>();
        private readonly Dictionary<RectTransform, Vector2> resultPositions = new Dictionary<RectTransform, Vector2>();
        private readonly Vector3[] corners = new Vector3[4];
        private bool selectedRetry;
        private bool eligible;
        private bool bossEligible;
        private bool bossReady;

        internal void Configure(UI_GameOverLabel owner, bool canRetry,
            bool canRetryBoss, bool canRestoreBoss)
        {
            RemoveButton();
            panel = owner;
            eligible = canRetry;
            bossEligible = canRetryBoss;
            bossReady = canRestoreBoss;
            if (!eligible)
            {
                RemoveButton();
                return;
            }

            if (retryButton != null || panel?.button == null)
            {
                return;
            }

            originalButton = panel.button;
            originalParent = originalButton.transform.parent;
            originalRect = originalButton.transform as RectTransform;
            originalNavigation = originalButton.navigation;

            GameObject clone = UnityEngine.Object.Instantiate(
                originalButton.gameObject, originalParent,
                worldPositionStays: false);
            clone.name = "SephiriaEnhancements_RetryCheckpoint";
            retryButton = clone.GetComponent<UI_HorayButton>();
            NativeRetryBoss.RestoreButtonColor(retryButton, originalButton);
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
            if (bossEligible)
            {
                GameObject bossClone = UnityEngine.Object.Instantiate(
                    originalButton.gameObject, originalParent, worldPositionStays: false);
                bossClone.name = "SephiriaEnhancements_RetryBossEncounter";
                bossRetryButton = bossClone.GetComponent<UI_HorayButton>();
                NativeRetryBoss.RestoreButtonColor(bossRetryButton, originalButton);
                bossRetryButton.onClick.RemoveAllListeners();
                bossRetryButton.onClick.AddListener(OnBossRetryClicked);
            }
            SetLocalizedText();

            CreateActionGroup();

            ConfigureNavigation();
            retryButton.gameObject.SetActive(false);
            bossRetryButton?.gameObject.SetActive(false);
        }

        private void CreateActionGroup()
        {
            actionGroup = new GameObject("SephiriaEnhancements_RetryActions",
                typeof(RectTransform), typeof(LayoutElement));
            actionGroup.transform.SetParent(panel.transform, false);
            actionGroup.GetComponent<LayoutElement>().ignoreLayout = true;
            retryButton.transform.SetParent(actionGroup.transform, false);
            bossRetryButton?.transform.SetParent(actionGroup.transform, false);
            AddResultRoot(panel.timeText?.rectTransform);
            AddResultRoot(panel.placeNameText?.rectTransform);
            AddResultRoot(panel.levelText?.rectTransform);
            AddResultRoot(panel.hardModeInfo?.transform as RectTransform);
            actionGroup.SetActive(false);
        }

        private void AddResultRoot(RectTransform item)
        {
            if (item == null) return;
            // Move a complete native result row, including its label, while keeping
            // the title and original action row in their native positions.
            while (item.parent is RectTransform parent && parent != panel.transform &&
                !originalParent.IsChildOf(parent) &&
                !(panel.titleLabel_Defeat != null && panel.titleLabel_Defeat.IsChildOf(parent)))
                item = parent;
            foreach (RectTransform existing in resultRoots)
                if (item == existing || item.IsChildOf(existing)) return;
            resultRoots.RemoveAll(existing => existing.IsChildOf(item));
            resultRoots.Add(item);
        }

        private Rect BoundsInPanel(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = panel.transform.InverseTransformPoint(corners[0]);
            Vector2 max = min;
            for (int i = 1; i < 4; i++)
            {
                Vector2 point = panel.transform.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void LateUpdate()
        {
            if (actionGroup == null || !actionGroup.activeSelf || originalRect == null) return;
            RectTransform panelRect = (RectTransform)panel.transform;
            // The native button is a direct child of the full-screen panel.
            // Anchor the retry row to the button, not the panel top edge.
            Rect source = BoundsInPanel(originalRect);
            float height = source.height;
            float gap = height * 0.2f;
            float available = Mathf.Max(height, panelRect.rect.width - gap * 2f);
            float padding = height * 0.6f;
            float width = Mathf.Max(height * 2.5f, PreferredWidth(retryButton) + padding);
            if (bossRetryButton != null) width = Mathf.Max(width, PreferredWidth(bossRetryButton) + padding);
            stacked = bossRetryButton != null && width * 2f + gap > available;
            width = Mathf.Min(width, available);
            height = Mathf.Max(height, PreferredHeight(retryButton, width - padding) / 0.8f);
            if (bossRetryButton != null) height = Mathf.Max(height, PreferredHeight(bossRetryButton, width - padding) / 0.8f);
            float groupWidth = bossRetryButton != null && !stacked ? width * 2f + gap : width;
            float groupHeight = bossRetryButton != null && stacked ? height * 2f + gap : height;
            RectTransform group = (RectTransform)actionGroup.transform;
            group.anchorMin = group.anchorMax = new Vector2(0.5f, 0.5f);
            group.pivot = new Vector2(0.5f, 0f);
            group.sizeDelta = new Vector2(groupWidth, groupHeight);
            group.anchoredPosition = new Vector2(0f, source.yMax + gap - panelRect.rect.center.y);
            Place(retryButton, width, height, padding, bossRetryButton == null ? Vector2.zero :
                stacked ? new Vector2(0f, (height + gap) * 0.5f) : new Vector2(-(width + gap) * 0.5f, 0f));
            if (bossRetryButton != null) Place(bossRetryButton, width, height, padding,
                stacked ? new Vector2(0f, -(height + gap) * 0.5f) : new Vector2((width + gap) * 0.5f, 0f));

            float bottom = float.PositiveInfinity;
            foreach (RectTransform item in resultRoots)
            {
                if (item == null) continue;
                if (!resultPositions.ContainsKey(item)) resultPositions.Add(item, item.anchoredPosition);
                Vector3 shift = panel.transform.InverseTransformVector(item.parent.TransformVector(
                    (Vector3)(item.anchoredPosition - resultPositions[item])));
                bottom = Mathf.Min(bottom, BoundsInPanel(item).yMin - shift.y);
            }
            MoveResults(Mathf.Max(0f, source.yMax + gap * 2f + groupHeight - bottom));
            ConfigureNavigation();
        }

        private float PreferredWidth(UI_HorayButton button)
        {
            // Measure at the native design size, never at last frame's shrunken size.
            button.text.fontSize = originalButton.text.fontSize;
            return button.text.GetPreferredValues(button.text.text, Mathf.Infinity, Mathf.Infinity).x *
                button.text.transform.lossyScale.x / panel.transform.lossyScale.x;
        }

        private float PreferredHeight(UI_HorayButton button, float width) =>
            button.text.GetPreferredValues(button.text.text,
                width * panel.transform.lossyScale.x / button.text.transform.lossyScale.x, Mathf.Infinity).y *
                button.text.transform.lossyScale.y / panel.transform.lossyScale.y;

        private static void Place(UI_HorayButton button, float width, float height, float padding, Vector2 position)
        {
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = position;
            RectTransform text = button.text.rectTransform;
            text.anchorMin = Vector2.zero;
            text.anchorMax = Vector2.one;
            text.offsetMin = new Vector2(padding * 0.5f, height * 0.1f);
            text.offsetMax = -text.offsetMin;
            button.text.margin = Vector4.zero;
        }

        private void MoveResults(float offset)
        {
            if (resultPositions.Count == 0) return;
            Vector3 delta = panel.transform.TransformVector(Vector3.up * offset);
            foreach (KeyValuePair<RectTransform, Vector2> item in resultPositions)
                if (item.Key != null) item.Key.anchoredPosition = item.Value +
                    (Vector2)item.Key.parent.InverseTransformVector(delta);
        }

        private void ConfigureNavigation()
        {
            // Let Unity resolve visible, interactable neighbors after layout.
            // Native buttons are still hidden when this row is created.
            Navigation navigation = originalNavigation;
            navigation.mode = Navigation.Mode.Automatic;
            originalButton.navigation = navigation;
            retryButton.navigation = navigation;
            if (bossRetryButton != null) bossRetryButton.navigation = navigation;
        }

        private void Update()
        {
            if (retryButton == null)
            {
                return;
            }

            bool visible = eligible && panel != null && panel.IsOpened &&
                originalButton != null && originalButton.gameObject.activeSelf;
            if (actionGroup != null)
            {
                actionGroup.SetActive(visible);
            }
            retryButton.gameObject.SetActive(visible);
            bossRetryButton?.gameObject.SetActive(visible && bossEligible);
            if (!visible)
            {
                MoveResults(0f);
                selectedRetry = false;
                return;
            }

            retryButton.interactable = DefeatRetryFeature.CanRetry(panel, RetryCheckpointKind.FloorEntry);
            if (bossRetryButton != null)
            {
                bossRetryButton.interactable = bossReady &&
                    DefeatRetryFeature.CanRetry(panel, RetryCheckpointKind.BossEncounter);
            }
            ConfigureNavigation();
            SetLocalizedText();
            if (!selectedRetry && retryButton.interactable)
            {
                panel.defaultSelectable = retryButton.gameObject;
                panel.DoControlSelection(retryButton.gameObject);
                KeyboardUiNavigation.KeyboardUiNavigationController.RequestSelection(
                    panel, retryButton.gameObject);
                selectedRetry = true;
            }
        }

        private void OnRetryClicked()
        {
            if (retryButton != null)
            {
                retryButton.interactable = false;
            }
            DefeatRetryFeature.TryRetry(panel, RetryCheckpointKind.FloorEntry);
        }

        private void OnBossRetryClicked()
        {
            DefeatRetryFeature.TryRetry(panel, RetryCheckpointKind.BossEncounter);
        }

        private void SetLocalizedText()
        {
            if (retryButton?.text == null)
            {
                return;
            }

            SetRetryButtonText(retryButton, ModLocalization.Get(DefeatRetryBridge.AllPlayersReady()
                ? ModLocalization.RetryFloor : DefeatRetryAvailabilityLocalization.PlayersNotReady));
            if (bossRetryButton?.text != null)
            {
                SetRetryButtonText(bossRetryButton, ModLocalization.Get(bossReady
                    ? ModLocalization.RetryBossEncounter : ModLocalization.RetryBossUnavailable));
            }
        }

        private void SetRetryButtonText(UI_HorayButton button, string text)
        {
            NativeLocalizedText.MatchFontSize(button.text, originalButton.text);
            button.text.text = text;
        }

        private void RemoveButton()
        {
            if (panel != null &&
                ((retryButton != null && panel.defaultSelectable == retryButton.gameObject) ||
                 (bossRetryButton != null && panel.defaultSelectable == bossRetryButton.gameObject)))
                panel.defaultSelectable = originalButton?.gameObject;
            if (originalButton != null) originalButton.navigation = originalNavigation;
            MoveResults(0f);
            resultRoots.Clear();
            resultPositions.Clear();
            if (actionGroup != null)
            {
                actionGroup.SetActive(false);
                UnityEngine.Object.Destroy(actionGroup);
            }
            actionGroup = null;
            retryButton = null;
            bossRetryButton = null;
            selectedRetry = false;
        }

        private void OnDestroy()
        {
            RemoveButton();
        }
    }
}
