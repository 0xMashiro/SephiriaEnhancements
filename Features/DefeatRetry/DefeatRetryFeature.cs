using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
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
                if (kind == RetryCheckpointKind.FloorEntry)
                {
                    checkpoints.RestartFloor();
                    BossRetryWorld.ClearRecipes();
                }
                IsRetrying = true;
                panel.button.interactable = false;
                panel.Close();
                SaveManager.Save(saveCurrent: true, saveCurrentRun: true);
                (NetworkManager.singleton as HorayNetworkManager)?.RestartGame();
                SupportLogger.Info("retry_restarted", "[SephiriaEnhancements] Host restarted from the " +
                    (selected.Kind == RetryCheckpointKind.BossEncounter
                        ? "boss encounter" : "floor-entry") + " checkpoint.");
            }
            catch (Exception ex)
            {
                IsRetrying = false;
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
            if (avatar == null || avatar.netIdentity == null ||
                pendingPlacements == null ||
                !pendingPlacements.TryGetValue(avatar.netIdentity.netId,
                    out RetryPlacement placement))
            {
                return;
            }

            pendingPlacements.Remove(avatar.netIdentity.netId);
            if (DefeatRetryPolicy.ShouldApplyPlacement(true, placement.FloorGuid,
                    requestedFloorGuid))
            {
                if (!string.IsNullOrEmpty(placement.SpawnPoint))
                {
                    spawnPoint = placement.SpawnPoint;
                }
                overridePosition = placement.Position;
                SupportLogger.Info("retry_placement_applied", "[SephiriaEnhancements] Applied retry placement on floor " +
                    placement.FloorGuid + ".");
            }
            else
            {
                SupportLogger.Warning("retry_floor_mismatch", "[SephiriaEnhancements] Retry placement floor " +
                    placement.FloorGuid + " did not match requested floor " +
                    requestedFloorGuid + ".");
            }

            if (pendingPlacements.Count == 0)
            {
                pendingPlacements = null;
            }
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
        private struct LayoutElementState
        {
            internal float MinWidth;
            internal float MinHeight;
            internal float PreferredWidth;
            internal float PreferredHeight;
            internal float FlexibleWidth;
            internal float FlexibleHeight;
            internal int LayoutPriority;
            internal bool IgnoreLayout;
        }

        private UI_GameOverLabel panel;
        private UI_HorayButton originalButton;
        private UI_HorayButton retryButton;
        private UI_HorayButton bossRetryButton;
        private Transform originalParent;
        private int originalSiblingIndex;
        private RectTransform originalRect;
        private Vector2 originalPosition;
        private Vector2 originalSize;
        private Navigation originalNavigation;
        private GameObject actionGroup;
        private LayoutElement originalLayoutElement;
        private LayoutElementState originalLayoutState;
        private bool addedOriginalLayoutElement;
        private bool manuallyPositioned;
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
            originalSiblingIndex = originalButton.transform.GetSiblingIndex();
            originalRect = originalButton.transform as RectTransform;
            originalPosition = originalRect != null
                ? originalRect.anchoredPosition : Vector2.zero;
            originalSize = originalRect != null
                ? originalRect.sizeDelta : Vector2.zero;
            originalNavigation = originalButton.navigation;

            GameObject clone = UnityEngine.Object.Instantiate(
                originalButton.gameObject, originalParent,
                worldPositionStays: false);
            clone.name = "SephiriaEnhancements_RetryCheckpoint";
            retryButton = clone.GetComponent<UI_HorayButton>();
            retryButton.text.enableAutoSizing = true;
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
            if (bossEligible)
            {
                GameObject bossClone = UnityEngine.Object.Instantiate(
                    originalButton.gameObject, originalParent, worldPositionStays: false);
                bossClone.name = "SephiriaEnhancements_RetryBossEncounter";
                bossRetryButton = bossClone.GetComponent<UI_HorayButton>();
                bossRetryButton.text.enableAutoSizing = true;
                bossRetryButton.onClick.RemoveAllListeners();
                bossRetryButton.onClick.AddListener(OnBossRetryClicked);
            }
            SetLocalizedText();

            LayoutGroup parentLayout = originalParent.GetComponent<LayoutGroup>();
            if (parentLayout != null && originalRect != null)
            {
                CreateActionGroup();
            }
            else
            {
                SplitOriginalSlotManually();
            }

            ConfigureNavigation();
            retryButton.gameObject.SetActive(false);
            bossRetryButton?.gameObject.SetActive(false);
        }

        private void CreateActionGroup()
        {
            actionGroup = new GameObject("Sephiria Enhancements — Retry Actions",
                typeof(RectTransform), typeof(HorizontalLayoutGroup),
                typeof(LayoutElement));
            RectTransform groupRect = actionGroup.GetComponent<RectTransform>();
            groupRect.SetParent(originalParent, false);
            groupRect.SetSiblingIndex(originalSiblingIndex);
            groupRect.anchorMin = originalRect.anchorMin;
            groupRect.anchorMax = originalRect.anchorMax;
            groupRect.pivot = originalRect.pivot;
            groupRect.anchoredPosition = originalPosition;
            groupRect.sizeDelta = originalSize;

            LayoutElement sourceLayout = originalButton.GetComponent<LayoutElement>();
            LayoutElement groupElement = actionGroup.GetComponent<LayoutElement>();
            float resolvedWidth = Mathf.Max(0f, originalRect.rect.width);
            float resolvedHeight = Mathf.Max(0f, originalRect.rect.height);
            groupElement.minWidth = sourceLayout != null && sourceLayout.minWidth >= 0f
                ? sourceLayout.minWidth : 0f;
            groupElement.minHeight = sourceLayout != null && sourceLayout.minHeight >= 0f
                ? sourceLayout.minHeight : 0f;
            groupElement.preferredWidth = sourceLayout != null &&
                sourceLayout.preferredWidth >= 0f
                ? sourceLayout.preferredWidth : resolvedWidth;
            groupElement.preferredHeight = sourceLayout != null &&
                sourceLayout.preferredHeight >= 0f
                ? sourceLayout.preferredHeight : resolvedHeight;
            groupElement.flexibleWidth = sourceLayout?.flexibleWidth ?? -1f;
            groupElement.flexibleHeight = sourceLayout?.flexibleHeight ?? -1f;
            groupElement.layoutPriority = sourceLayout?.layoutPriority ?? 1;

            HorizontalLayoutGroup layout =
                actionGroup.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            retryButton.transform.SetParent(actionGroup.transform, false);
            if (bossRetryButton != null)
            {
                bossRetryButton.transform.SetParent(actionGroup.transform, false);
                ConfigureChildLayout(bossRetryButton.gameObject, preserveState: false);
            }
            originalButton.transform.SetParent(actionGroup.transform, false);
            ConfigureChildLayout(retryButton.gameObject, preserveState: false);
            ConfigureChildLayout(originalButton.gameObject, preserveState: true);
            actionGroup.SetActive(false);
        }

        private void ConfigureChildLayout(GameObject child, bool preserveState)
        {
            LayoutElement element = child.GetComponent<LayoutElement>();
            if (preserveState)
            {
                originalLayoutElement = element;
                addedOriginalLayoutElement = element == null;
                if (element != null)
                {
                    originalLayoutState = CaptureLayoutState(element);
                }
            }
            if (element == null)
            {
                element = child.AddComponent<LayoutElement>();
                if (preserveState)
                {
                    originalLayoutElement = element;
                }
            }

            element.minWidth = 0f;
            element.preferredWidth = 0f;
            element.flexibleWidth = 1f;
            element.ignoreLayout = false;
        }

        private static LayoutElementState CaptureLayoutState(LayoutElement element)
        {
            return new LayoutElementState
            {
                MinWidth = element.minWidth,
                MinHeight = element.minHeight,
                PreferredWidth = element.preferredWidth,
                PreferredHeight = element.preferredHeight,
                FlexibleWidth = element.flexibleWidth,
                FlexibleHeight = element.flexibleHeight,
                LayoutPriority = element.layoutPriority,
                IgnoreLayout = element.ignoreLayout
            };
        }

        private void SplitOriginalSlotManually()
        {
            RectTransform retryRect = retryButton.transform as RectTransform;
            if (originalRect == null || retryRect == null)
            {
                return;
            }

            float width = Mathf.Max(originalRect.rect.width, originalSize.x);
            int count = bossRetryButton != null ? 3 : 2;
            float childWidth = Mathf.Max(40f, (width - 12f * (count - 1)) / count);
            float offset = (childWidth + 12f) * (count - 1) * 0.5f;
            originalRect.sizeDelta = new Vector2(childWidth, originalSize.y);
            retryRect.sizeDelta = new Vector2(childWidth, originalSize.y);
            retryRect.anchoredPosition = originalPosition + Vector2.left * offset;
            originalRect.anchoredPosition = originalPosition + Vector2.right * offset;
            if (bossRetryButton != null)
            {
                RectTransform bossRect = bossRetryButton.transform as RectTransform;
                bossRect.sizeDelta = new Vector2(childWidth, originalSize.y);
                bossRect.anchoredPosition = originalPosition;
            }
            retryButton.transform.SetSiblingIndex(originalSiblingIndex);
            manuallyPositioned = true;
        }

        private void ConfigureNavigation()
        {
            Navigation retryNavigation = originalNavigation;
            retryNavigation.mode = Navigation.Mode.Explicit;
            retryNavigation.selectOnRight = bossRetryButton != null && bossReady
                ? bossRetryButton : originalButton;
            retryButton.navigation = retryNavigation;

            Navigation returnNavigation = originalNavigation;
            returnNavigation.mode = Navigation.Mode.Explicit;
            returnNavigation.selectOnLeft = bossRetryButton != null && bossReady
                ? bossRetryButton : retryButton;
            originalButton.navigation = returnNavigation;
            if (bossRetryButton != null)
            {
                Navigation bossNavigation = originalNavigation;
                bossNavigation.mode = Navigation.Mode.Explicit;
                bossNavigation.selectOnLeft = retryButton;
                bossNavigation.selectOnRight = originalButton;
                bossRetryButton.navigation = bossNavigation;
            }
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

            retryButton.text.text = ModLocalization.Get(ModLocalization.RetryFloor);
            if (bossRetryButton?.text != null)
            {
                bossRetryButton.text.text = ModLocalization.Get(bossReady
                    ? ModLocalization.RetryBossEncounter : ModLocalization.RetryBossUnavailable);
            }
        }

        private void RemoveButton()
        {
            if (panel != null &&
                ((retryButton != null && panel.defaultSelectable == retryButton.gameObject) ||
                 (bossRetryButton != null && panel.defaultSelectable == bossRetryButton.gameObject)))
            {
                panel.defaultSelectable = originalButton?.gameObject;
            }

            if (originalButton != null)
            {
                originalButton.navigation = originalNavigation;
            }

            if (actionGroup != null && originalButton != null && originalParent != null)
            {
                originalButton.transform.SetParent(originalParent, false);
                originalButton.transform.SetSiblingIndex(originalSiblingIndex);
                if (originalRect != null)
                {
                    originalRect.anchoredPosition = originalPosition;
                    originalRect.sizeDelta = originalSize;
                }
                RestoreOriginalLayoutElement();
                UnityEngine.Object.Destroy(actionGroup);
                actionGroup = null;
                retryButton = null;
                bossRetryButton = null;
            }
            else
            {
                if (bossRetryButton != null)
                {
                    UnityEngine.Object.Destroy(bossRetryButton.gameObject);
                    bossRetryButton = null;
                }
                if (retryButton != null)
                {
                    UnityEngine.Object.Destroy(retryButton.gameObject);
                    retryButton = null;
                }
                if (manuallyPositioned && originalRect != null)
                {
                    originalRect.anchoredPosition = originalPosition;
                    originalRect.sizeDelta = originalSize;
                }
            }

            manuallyPositioned = false;
            selectedRetry = false;
        }

        private void RestoreOriginalLayoutElement()
        {
            if (originalLayoutElement == null)
            {
                return;
            }
            if (addedOriginalLayoutElement)
            {
                UnityEngine.Object.Destroy(originalLayoutElement);
                originalLayoutElement = null;
                return;
            }

            originalLayoutElement.minWidth = originalLayoutState.MinWidth;
            originalLayoutElement.minHeight = originalLayoutState.MinHeight;
            originalLayoutElement.preferredWidth = originalLayoutState.PreferredWidth;
            originalLayoutElement.preferredHeight = originalLayoutState.PreferredHeight;
            originalLayoutElement.flexibleWidth = originalLayoutState.FlexibleWidth;
            originalLayoutElement.flexibleHeight = originalLayoutState.FlexibleHeight;
            originalLayoutElement.layoutPriority = originalLayoutState.LayoutPriority;
            originalLayoutElement.ignoreLayout = originalLayoutState.IgnoreLayout;
        }

        private void OnDestroy()
        {
            RemoveButton();
        }
    }
}
