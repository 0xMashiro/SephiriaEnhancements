using System;
using Mirror;
using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.CombatVisuals
{
    internal static class CombatVisualSourceResolver
    {
        internal static CombatVisualSourceRelation Resolve(UnitAvatar source,
            UnitAvatar leaderOverride = null)
        {
            if (source == null)
            {
                return CombatVisualSourceRelation.Unknown;
            }

            if (source is PlayerAvatar player)
            {
                return IsLocal(player) ? CombatVisualSourceRelation.LocalPlayer
                    : CombatVisualSourceRelation.RemotePlayer;
            }

            UnitAvatar leader = leaderOverride ?? source.NetworkLeader;
            for (int depth = 0; depth < 16 && leader != null; depth++)
            {
                if (leader is PlayerAvatar playerLeader)
                {
                    return IsLocal(playerLeader)
                        ? CombatVisualSourceRelation.LocalCompanion
                        : CombatVisualSourceRelation.RemoteCompanion;
                }

                leader = leader.NetworkLeader;
            }

            return leader == null ? CombatVisualSourceRelation.Other
                : CombatVisualSourceRelation.Unknown;
        }

        private static bool IsLocal(PlayerAvatar player)
        {
            return player.isLocalPlayer || player.isOwned;
        }

        internal static UnitAvatar FindSource(uint netId)
        {
            if (netId != 0 && NetworkClient.spawned.TryGetValue(netId,
                    out NetworkIdentity identity))
            {
                return identity.GetComponent<UnitAvatar>();
            }

            return null;
        }
    }

    internal static class CombatVisualRuntime
    {
        [ThreadStatic]
        private static int? transparencyOverride;

        internal static bool TryGetTransparencyOverride(string key,
            out int value)
        {
            value = 0;
            if (!transparencyOverride.HasValue ||
                key != "MyFX" && key != "PartyMemberFX")
            {
                return false;
            }

            value = transparencyOverride.Value;
            return true;
        }

        internal static IDisposable Begin(UnitAvatar source,
            CombatVisualSurface surface, UnitAvatar leaderOverride = null)
        {
            CombatVisualSourceRelation relation =
                CombatVisualSourceResolver.Resolve(source, leaderOverride);
            if (relation != CombatVisualSourceRelation.LocalCompanion ||
                !EnhancementsSettings.Enabled)
            {
                return null;
            }

            CombatVisualPreset preset = CombatVisualSettings.Preset;
            if (preset == CombatVisualPreset.FollowGame)
            {
                return null;
            }

            if (!CombatVisualPolicy.TryGetTransparencyLevel(
                    preset, relation, surface,
                    CombatVisualSettings.CompanionBody,
                    CombatVisualSettings.CompanionEffects,
                    out EffectTransparencyLevel level))
            {
                return null;
            }

            int? previous = transparencyOverride;
            transparencyOverride = (int)level;
            return new OverrideScope(previous);
        }

        internal static void RefreshCompanionBodies()
        {
            PlayerAvatar player = CombatManager.Instance?.CurrentPlayer;
            if (player?.followers == null)
            {
                return;
            }

            for (int index = 0; index < player.followers.Count; index++)
            {
                UnitAvatar follower = player.followers[index];
                if (follower != null)
                {
                    follower.SetTransparencyRenderMode(follower.NetworkLeader);
                }
            }
        }

        private sealed class OverrideScope : IDisposable
        {
            private readonly int? previous;
            private bool disposed;

            internal OverrideScope(int? previousValue)
            {
                previous = previousValue;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                transparencyOverride = previous;
                disposed = true;
            }
        }
    }
}
