using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    // Personal saves stay on their owner's machine, bound to the same player,
    // connection and profile. Only accepted floor/boss checkpoints are retained.
    internal static class NativeRetryAccount
    {
        private static readonly FieldInfo Current = AccessTools.Field(typeof(SaveManager), "current");
        private static readonly string[] RejoinKeys =
            { "LastJoinedLobbyId", "LastJoinedLobbyPassword", "LastJoinedLobbyProfile", "LastRejoinGuid" };

        private sealed class Checkpoint
        {
            internal long Id;
            internal SaveData Save;
            internal PlayerAvatar Player;
            internal NetworkConnectionToServer Connection;
            internal string BossName;
            internal readonly Dictionary<string, string> Rejoin = new Dictionary<string, string>();
        }

        private static Checkpoint pending, floor, boss;

        internal static void Capture(long id)
        {
            pending = new Checkpoint { Id = id, Save = SaveManager.Current.Copy(),
                Player = LocalPlayerResolver.Resolve(), Connection = NetworkClient.connection };
            foreach (string key in RejoinKeys)
                if (PlayerPrefs.HasKey(key)) pending.Rejoin.Add(key, PlayerPrefs.GetString(key));
        }

        internal static void Commit(long id, RetryCheckpointKind kind, string bossName)
        {
            if (pending == null || pending.Id != id) throw new InvalidOperationException("Owner checkpoint is missing.");
            pending.BossName = bossName;
            if (kind == RetryCheckpointKind.FloorEntry) { floor = pending; boss = null; }
            else if (kind == RetryCheckpointKind.BossEncounter) boss = pending;
            else throw new InvalidOperationException("Invalid owner checkpoint kind.");
            pending = null;
        }

        internal static void Release(long id)
        {
            if (pending?.Id == id) pending = null;
        }

        internal static void Restore(long id)
        {
            Checkpoint selected = floor?.Id == id ? floor : boss?.Id == id ? boss : null;
            if (selected == null || selected.Connection != NetworkClient.connection ||
                selected.Player != LocalPlayerResolver.Resolve() ||
                selected.Save.BindedFileName != SaveManager.Current?.BindedFileName)
                throw new InvalidOperationException("Owner checkpoint no longer belongs to this player.");

            SaveData restored = selected.Save.Copy();
            if (!string.IsNullOrEmpty(selected.BossName))
                foreach (string suffix in new[] { "", "_T1", "_T2" })
                {
                    string key = "BossMet_" + selected.BossName + suffix;
                    if (SaveManager.Current.GetBool(key, false)) restored.SetBool(key, true);
                }
            restored.enableSave = true;
            Current.SetValue(null, restored);
            // Game-over presentation clears these before retry is selected.
            // Restore only for this checkpoint's still-connected owner; normal
            // return and disconnect continue to use the native cleanup.
            foreach (string key in RejoinKeys)
                if (selected.Rejoin.TryGetValue(key, out string value)) PlayerPrefs.SetString(key, value);
                else PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        internal static void Clear() { pending = floor = boss = null; }
    }
}
