using System;
using System.Collections.Generic;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.CombatVisuals;
using UnityEngine;

namespace SephiriaEnhancements.CombatRelationOutlines
{
    internal sealed class CombatRelationOutlinesController : MonoBehaviour
    {
        private const float RefreshInterval = 0.2f;

        private readonly Dictionary<int, RendererState> states =
            new Dictionary<int, RendererState>();
        private readonly HashSet<int> activeKeys = new HashSet<int>();
        private readonly List<int> staleKeys = new List<int>();
        private float nextRefresh;
        private bool runtimeCompatible = true;

        private void Update()
        {
            if (!runtimeCompatible || Time.unscaledTime < nextRefresh)
            {
                return;
            }

            nextRefresh = Time.unscaledTime + RefreshInterval;
            try
            {
                Refresh();
            }
            catch (Exception ex)
            {
                RestoreAll();
                runtimeCompatible = false;
                Debug.LogWarning("[SephiriaEnhancements] Combat-relation outlines disabled " +
                    "for the current gameplay context: " + ex);
            }
        }

        internal void ResetGameplayContext()
        {
            RestoreAll();
            nextRefresh = 0f;
            runtimeCompatible = true;
        }

        private void OnDisable() => RestoreAll();

        private void OnDestroy() => RestoreAll();

        private void Refresh()
        {
            CombatManager manager = CombatManager.Instance;
            PlayerAvatar localPlayer = manager?.CurrentPlayer ?? GameCamera.Instance?.Observer;
            IReadOnlyList<PlayerSpawner> players = PlayerSpawner.MultiplayerList;
            int multiplayerCount = players?.Count ?? 0;
            bool featureActive = EnhancementsSettings.Enabled &&
                CombatRelationOutlinesSettings.Enabled && localPlayer != null;

            if (!featureActive || manager?.AllCreatures == null)
            {
                RestoreAll();
                return;
            }

            activeKeys.Clear();
            List<UnitAvatar> creatures = manager.AllCreatures;
            for (int index = 0; index < creatures.Count; index++)
            {
                UnitAvatar avatar = creatures[index];
                SpriteRenderer renderer = avatar?.stencilSolidColor;
                if (avatar == null || renderer == null)
                {
                    continue;
                }

                int key = avatar.GetInstanceID();
                activeKeys.Add(key);
                if (!states.TryGetValue(key, out RendererState state) ||
                    state.Renderer != renderer)
                {
                    state?.Restore();
                    state = new RendererState(renderer);
                    states[key] = state;
                }

                // Faction is unit membership; ERelationBehaviour is the current
                // relationship between factions. Keep those concepts separate.
                ERelationBehaviour relation = RuntimeFactionManager.Instance == null
                    ? ERelationBehaviour.Neutral
                    : RuntimeFactionManager.Instance.GetRelationBehaviour(
                        avatar.faction, localPlayer.faction, avatar.attackableTargetSelector);
                bool isFriendly = relation == ERelationBehaviour.Friendly;
                bool isHostile = relation == ERelationBehaviour.Hostile;
                bool hasCombatRelation = isFriendly || isHostile;
                bool relationAllowed = CombatVisualPolicy.AllowsOutline(
                    CombatVisualSettings.Preset, CombatVisualSettings.OutlineScope,
                    multiplayerCount, isFriendly, isHostile);
                bool visible = CombatRelationOutlinePolicy.ShouldShow(
                    suiteEnabled: true,
                    featureEnabled: true,
                    hasLocalPlayer: true,
                    isLocalPlayer: avatar == localPlayer,
                    relationAllowed,
                    isAlive: !avatar.IsDead,
                    isTargetable: avatar.canBeTarget > 0,
                    isActive: avatar.gameObject.activeInHierarchy);
                Color relationColor = relation == ERelationBehaviour.Friendly
                    ? avatar.stencilAllyColor : avatar.stencilEnemyColor;
                state.Apply(visible, hasCombatRelation, relationColor);
            }

            staleKeys.Clear();
            foreach (int key in states.Keys)
            {
                if (!activeKeys.Contains(key))
                {
                    staleKeys.Add(key);
                }
            }

            for (int index = 0; index < staleKeys.Count; index++)
            {
                int key = staleKeys[index];
                states[key].Restore();
                states.Remove(key);
            }
        }

        private void RestoreAll()
        {
            foreach (RendererState state in states.Values)
            {
                state.Restore();
            }

            states.Clear();
            activeKeys.Clear();
            staleKeys.Clear();
        }

        private sealed class RendererState
        {
            private readonly bool originalEnabled;
            private readonly Color originalColor;

            internal RendererState(SpriteRenderer renderer)
            {
                Renderer = renderer;
                originalEnabled = renderer.enabled;
                originalColor = renderer.color;
            }

            internal SpriteRenderer Renderer { get; }

            internal void Apply(bool visible, bool hasCombatRelation,
                Color relationColor)
            {
                if (Renderer != null)
                {
                    Renderer.enabled = visible || originalEnabled;
                    // Relation can change dynamically without the game's faction
                    // color hook running. Refresh RGB here, but preserve the
                    // game's current alpha-based follower transparency.
                    if (hasCombatRelation && Renderer.enabled)
                    {
                        relationColor.a = Renderer.color.a;
                        Renderer.color = relationColor;
                    }
                    else
                    {
                        Renderer.color = originalColor;
                    }
                }
            }

            internal void Restore()
            {
                if (Renderer != null)
                {
                    Renderer.enabled = originalEnabled;
                    Renderer.color = originalColor;
                }
            }
        }
    }
}
