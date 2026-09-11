using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Diagnostics;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    // Display is local. Feature bridges own network delivery and operation identities.
    internal static class NativeModNotifications
    {
        internal const string Prefix = "[Sephiria Enhancements] ";
        private static readonly Dictionary<string, (Func<string> Text, Action Shown)> pending = new();
        private static readonly HashSet<string> delivered = new();
        private static PlayerAvatar contextPlayer;
        private static NetworkConnectionToServer contextConnection;
        private static bool failed;
        private static string lastToast;

        internal static void Reset()
        {
            ClearContext();
            failed = false;
        }

        internal static void ClearContext()
        {
            pending.Clear();
            delivered.Clear();
            contextPlayer = null;
            contextConnection = null;
            lastToast = null;
        }

        private static PlayerAvatar ObservePlayer()
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (!ReferenceEquals(player, contextPlayer) ||
                !ReferenceEquals(NetworkClient.connection, contextConnection))
            {
                ClearContext();
                contextPlayer = player;
                contextConnection = NetworkClient.connection;
            }
            return player;
        }

        // An unavailable or broken UI must never turn a notification into a feature failure.
        private static bool Attempt(Func<bool> display)
        {
            if (failed) return false;
            try { return display(); }
            catch (Exception exception)
            {
                failed = true;
                pending.Clear();
                SupportLogger.Failure("notification_display_failed", exception);
                return false;
            }
        }

        private static bool Visible(PlayerAvatar player) => player != null &&
            player.loadingScreenType == -1 && UIManager.Instance != null && !UIManager.Instance.IsHidden &&
            (ScreenFader.Instance == null || !ScreenFader.Instance.IsFading);

        internal static bool Chat(Func<string> text) => Attempt(() =>
        {
            PlayerAvatar player = ObservePlayer();
            if (!Visible(player)) return false;
            var viewer = UIManager.Instance.GetElement<UI_HUDLogViewer>();
            if (viewer == null || !viewer.isActiveAndEnabled || viewer.logPool.Count == 0 ||
                !ReferenceEquals(AccessTools.Field(typeof(UI_HUDLogViewer), "unitAvatar").GetValue(viewer), player)) return false;
            // Use the same local event as native player logs, never DungeonManager.Chat.
            foreach (string line in text().Split('\n'))
                if (!string.IsNullOrWhiteSpace(line)) player.WriteLog(Prefix + line, Color.cyan);
            return true;
        });

        internal static bool Short(string key, float seconds = 2f) => ShortText(() => ModLocalization.Get(key), seconds);

        internal static bool ShortText(Func<string> text, float seconds = 2f) => Attempt(() =>
        {
            PlayerAvatar player = ObservePlayer();
            if (!Visible(player)) return false;
            var message = UIManager.Instance.GetElement<UI_SystemMessage>();
            if (message == null || message.hasControl ||
                !ReferenceEquals(AccessTools.Field(typeof(UI_SystemMessage), "unitAvatar").GetValue(message), player)) return false;
            // Do not overwrite an official message (or another Mod's message).
            if (message.IsOpened && message.messageText.text != lastToast) return false;
            message.Open(Prefix + text(), seconds);
            lastToast = message.messageText.text;
            return true;
        });

        internal static bool Important(string operation, Func<string> text, float seconds = 4f, bool showShort = true, Action onShown = null)
        {
            return Attempt(() =>
            {
                if (ObservePlayer() == null || delivered.Contains(operation)) return false;
                delivered.Add(operation);
                pending[operation] = (text, onShown);
                if (showShort) ShortText(text, seconds);
                return true;
            });
        }

        internal static void Tick()
        {
            Attempt(() =>
            {
                ObservePlayer();
                foreach (string key in new List<string>(pending.Keys))
                {
                    var notice = pending[key];
                    if (!Chat(notice.Text)) break;
                    pending.Remove(key);
                    notice.Shown?.Invoke();
                }
                return true;
            });
        }

        internal static bool Confirm(Func<string> text, Action confirmed) => Attempt(() =>
        {
            if (UIManager.Instance == null) return false;
            var holder = UIManager.Instance.GetElement<UI_MessageBoxHolder>();
            if (holder == null || holder.HasOpenedBox) return false;
            holder.OpenYes(Prefix + text(), confirmed);
            return true;
        });
    }
}
