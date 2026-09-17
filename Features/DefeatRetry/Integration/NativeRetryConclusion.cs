using HarmonyLib;
using System.Collections.Generic;
using Mirror;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryConclusion
    {
        // This scope exists only while the native death handler can dispatch its
        // party-wipe RPC. A death on its own must never authorize a later ending.
        internal static RetryConclusionKind DeathDispatch;
        private static RetryConclusionKind pending, current;
        private static NetworkConnectionToServer connection;
        private static UI_GameOverLabel panel;
        private static readonly Dictionary<PlayerAvatar, RetryConclusionKind> deaths =
            new Dictionary<PlayerAvatar, RetryConclusionKind>();

        internal static RetryConclusionKind ReadDeath(DamageInstance damage)
        {
            if (damage == null) return RetryConclusionKind.ForcedDefeat;
            // The Q story wipe can also kill through damage before its direct Die call.
            if (damage.origin is Unit_QBoss boss && boss.activeWipeAttack && !boss.heroActive)
                return RetryConclusionKind.ScriptedDefeat;
            return RetryConclusionKind.CombatDefeat;
        }

        internal static void CaptureServer()
        {
            if (!NetworkServer.active) return;
            if (UIManager.Instance?.GetElement<UI_GameOverLabel>()?.IsOpened == true) return;
            var dungeon = DungeonManager.Instance;
            if (dungeon == null) return;
            var kind = dungeon.isGiveUpRun ? RetryConclusionKind.Abandoned : DeathDispatch;
            if (kind == RetryConclusionKind.CombatDefeat) kind = ReadPartyDeaths(null);
            DefeatRetryBridge.PublishConclusion(kind);
            SupportLogger.Record("retry_conclusion_dispatched", "kind=" + kind +
                " presentationType=" + dungeon.victoryType);
        }

        internal static void Receive(RetryConclusionKind kind)
        {
            if (UIManager.Instance?.GetElement<UI_GameOverLabel>()?.IsOpened == true) return;
            connection = NetworkClient.connection;
            pending = kind;
        }

        internal static void Open(UI_GameOverLabel owner)
        {
            panel = owner;
            current = connection == NetworkClient.connection ? pending : RetryConclusionKind.Unknown;
            pending = RetryConclusionKind.Unknown;
            if (DungeonManager.Instance?.isGiveUpRun == true)
                current = RetryConclusionKind.Abandoned;
            // Native victory presentation is useful only without death evidence.
            // In particular, type 2 does not establish the cause of this ending.
            if (current == RetryConclusionKind.Unknown &&
                (owner.openType == 1 || (owner.openType >= 3 && owner.openType <= 6)))
                current = RetryConclusionKind.Victory;
            connection = NetworkClient.connection;
            SupportLogger.Record("retry_conclusion_opened", "kind=" + current +
                " presentationType=" + owner.openType + " authoritative=" + NetworkServer.active);
            if (current == RetryConclusionKind.CombatDefeat)
                DefeatRetryBridge.ObserveTeamDefeat();
        }

        internal static RetryConclusionKind Get(UI_GameOverLabel owner) =>
            owner != null && owner == panel && connection == NetworkClient.connection
                ? current : RetryConclusionKind.Unknown;

        internal static void Reset()
        {
            deaths.Clear();
            DeathDispatch = pending = current = RetryConclusionKind.Unknown;
            connection = null;
            panel = null;
        }

        internal static void RecordDeath(PlayerSpawner player, DamageInstance damage)
        {
            DeathDispatch = ReadDeath(damage);
            if (player.PlayerAvatar != null) deaths[player.PlayerAvatar] = DeathDispatch;
        }

        internal static RetryConclusionKind ReadPartyDeaths(NetworkConnectionToClient departing)
        {
            int remaining = 0;
            foreach (var peer in NetworkServer.connections.Values)
            {
                if (peer == null || peer == departing) continue;
                var avatar = peer.identity?.GetComponent<PlayerAvatar>();
                if (avatar == null) continue;
                if (!avatar.IsDead || !deaths.TryGetValue(avatar, out var kind)) return RetryConclusionKind.Unknown;
                if (kind != RetryConclusionKind.CombatDefeat) return kind;
                remaining++;
            }
            return remaining > 0 ? RetryConclusionKind.CombatDefeat : RetryConclusionKind.Unknown;
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner), "HandleDieServerside")]
    internal static class RetryDeathCausePatch
    {
        private static void Prefix(PlayerSpawner __instance, DamageInstance damage, out RetryConclusionKind __state)
        {
            __state = NativeRetryConclusion.DeathDispatch;
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry)) return;
            try { PrefixCore(__instance, damage); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(PlayerSpawner player, DamageInstance damage)
        {
            NativeRetryConclusion.RecordDeath(player, damage);
        }

        private static void Finalizer(RetryConclusionKind __state)
        {
            try { FinalizerCore(__state); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FinalizerCore(RetryConclusionKind state)
        {
            NativeRetryConclusion.DeathDispatch = state;
        }
    }

    [HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.OnServerDisconnect))]
    internal static class RetryDisconnectCausePatch
    {
        private static void Prefix(NetworkConnectionToClient conn, out RetryConclusionKind __state)
        {
            __state = NativeRetryConclusion.DeathDispatch;
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry)) return;
            try { PrefixCore(conn); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore(NetworkConnectionToClient departing)
        {
            NativeRetryConclusion.DeathDispatch = NativeRetryConclusion.ReadPartyDeaths(departing);
        }

        private static void Finalizer(RetryConclusionKind __state)
        {
            try { FinalizerCore(__state); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void FinalizerCore(RetryConclusionKind state)
        {
            NativeRetryConclusion.DeathDispatch = state;
        }
    }

    [HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.RpcGameOver))]
    internal static class RetryConclusionDispatchPatch
    {
        private static void Prefix()
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry)) return;
            try { PrefixCore(); }
            catch (System.Exception exception) { FeatureFailure.Disable(FeatureId.DefeatRetry, exception); }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PrefixCore()
        {
            NativeRetryConclusion.CaptureServer();
        }
    }
    [HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnOpened))]
    internal static class GameOverDefeatRetryButtonPatch
    {
        private static void Postfix(UI_GameOverLabel __instance)
        {
            if (!FeatureFailure.IsAvailable(FeatureId.DefeatRetry))
            {
                return;
            }

            try
            {
                PostfixCore(__instance);
            }
            catch (System.Exception exception)
            {
                FeatureFailure.Disable(FeatureId.DefeatRetry, exception);
                return;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void PostfixCore(UI_GameOverLabel __instance)
        {
            NativeRetryConclusion.Open(__instance);
            DefeatRetryFeature.AddButton(__instance);
        }
    }

}
