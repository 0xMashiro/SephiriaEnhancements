using System;
using System.Collections;
using System.Collections.Generic;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal sealed class NativeMapTravel
    {
        private readonly NativeMapGeometry geometry;
        private readonly UI_MapPanel panel;
        private readonly List<Vector3> candidates = new();
        private readonly List<Vector3> path = new();
        private MapLocationMarkerView cachedMarker;
        private MapTravelState cachedState;
        private float refreshAt;
        private bool active = true;

        internal NativeMapTravel(NativeMapGeometry geometry, UI_MapPanel panel)
        {
            this.geometry = geometry;
            this.panel = panel;
        }
        internal void Clear() { active = false; cachedMarker = null; }

        private string UnavailableReason(PlayerAvatar player, MapLocationMarkerView marker)
        {
            if (!active) return "map_cleared";
            if (geometry.Floor == null) return "floor_removed";
            if (marker == null || marker.Target == null || !marker.Target.gameObject.activeInHierarchy)
                return "target_removed";
            if (player == null || LocalPlayerResolver.Resolve() != player) return "local_player_changed";
            if (player.currentFloorGuid != geometry.Floor.guid) return "floor_changed";
            if (player.loadingScreenType != -1) return "player_loading";
            if (GameCamera.Instance?.Observer != player || GameCamera.Instance.CurrentSeeingFloor != geometry.Floor)
                return "camera_changed";
            if (player.IsDead) return "player_dead";
            if (player.IsInBattle) return "player_in_battle";
            if (!player.CanMove || player.blockMoveByInput > 0 || player.CanFastMove.IsFalse())
                return "movement_blocked";
            return null;
        }

        internal MapTravelState State(MapLocationMarkerView marker)
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (UnavailableReason(player, marker) != null || ScreenFader.Instance == null || ScreenFader.Instance.IsFading)
                return MapTravelState.Unavailable;
            if (marker.Destination != null)
                return marker.Destination.CanTravel && geometry.Contains(marker.Destination.Position)
                    ? MapTravelState.Ready : MapTravelState.Unavailable;
            if (geometry.Designed == null)
                return geometry.TryGetRoomDestination(marker.WorldPosition, out _)
                    ? MapTravelState.Ready : MapTravelState.Unavailable;
            if (cachedMarker != marker || Time.unscaledTime >= refreshAt)
            {
                cachedMarker = marker;
                cachedState = FindNearbyLanding(player, marker, out _);
                refreshAt = Time.unscaledTime + .2f;
            }
            return cachedState;
        }

        internal void Travel(MapLocationMarkerView marker)
        {
            refreshAt = 0f;
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            string kind = marker?.Destination != null ? marker.Destination.Kind.ToString()
                : marker != null && marker.IsPerson ? "Person" : "Facility";
            MapTravelState state = State(marker);
            if (state != MapTravelState.Ready)
            {
                SupportLogger.Record("map_travel_rejected",
                    "target=" + kind + " reason=" + (UnavailableReason(player, marker) ?? state.ToString()));
                return;
            }
            if (geometry.Designed == null)
            {
                if (geometry.TryGetRoomDestination(marker.WorldPosition, out var room)) room.TeleportToRoom();
                return;
            }
            SupportLogger.Record("map_travel_started", "target=" + kind + " playerNetId=" + player.netId);
            // Revalidate after the native fade: targets, local player and floor can
            // change while it runs. ReqSetPosition retains native transform authority.
            ScreenFader.Instance.FadeOut(ScreenFader.ScreenType.NextFloor, () =>
            {
                string reason = UnavailableReason(player, marker);
                if (reason != null)
                {
                    SupportLogger.Record("map_travel_cancelled", "target=" + kind + " reason=" + reason);
                    return;
                }
                Vector3 position;
                if (marker.Destination != null)
                {
                    // UIManager disables the map's CanvasGroup during the fade.
                    // Recheck the destination itself, not that temporary UI input block.
                    if (!marker.Destination.IsEnabled || !geometry.Contains(marker.Destination.Position))
                    {
                        SupportLogger.Record("map_travel_cancelled", "target=" + kind + " reason=destination_unavailable");
                        return;
                    }
                    position = marker.Destination.Position;
                }
                else
                {
                    MapTravelState landing = FindNearbyLanding(player, marker, out position);
                    if (landing != MapTravelState.Ready)
                    {
                        SupportLogger.Record("map_travel_cancelled", "target=" + kind + " reason=" + landing);
                        return;
                    }
                }
                SupportLogger.Record("map_travel_position_requested", FormattableString.Invariant(
                    $"target={kind} playerNetId={player.netId} server={player.isServer} owned={player.isOwned} from={player.transform.position.ToString("F3")} destination={position.ToString("F3")}"));
                player.ReqSetPosition(position, teleport: true);
                GameCamera.Instance.targetTracker.SetCameraPosition(player.transform.position);
                player.StartCoroutine(ObservePositionNextFrame(player, geometry.Floor, position, kind));
                // Closing clears the navigator and markers; only do it after requesting movement.
                panel.Close();
            }, autoFadeIn: true, .33f, 2f);
        }

        private static IEnumerator ObservePositionNextFrame(PlayerAvatar player, FloorGenerator floor,
            Vector3 destination, string kind)
        {
            yield return null;
            if (player == null || floor == null || LocalPlayerResolver.Resolve() != player ||
                GameCamera.Instance?.CurrentSeeingFloor != floor || player.currentFloorGuid != floor.guid ||
                player.loadingScreenType != -1)
            {
                SupportLogger.Record("map_travel_observation_skipped", "target=" + kind + " reason=context_changed");
                yield break;
            }
            // An observation is not a server acknowledgement, especially for remote requests.
            SupportLogger.Record("map_travel_position_observed", FormattableString.Invariant(
                $"target={kind} playerNetId={player.netId} position={player.transform.position.ToString("F3")} destination={destination.ToString("F3")} distance={(player.transform.position - destination).magnitude:F3}"));
        }

        private MapTravelState FindNearbyLanding(PlayerAvatar player, MapLocationMarkerView marker,
            out Vector3 position)
        {
            position = default;
            if (!geometry.Floor.isSafeFloor) return MapTravelState.Unavailable;
            PathGrid grid = PathGrid.Current;
            Vector2 center = (Vector2)geometry.Floor.transform.position + geometry.Floor.Center;
            if (grid == null || !grid.IsBuilt || (grid.Center - center).sqrMagnitude > .001f ||
                grid.Width != Mathf.FloorToInt(geometry.Floor.Size.x) * 2 ||
                grid.Height != Mathf.FloorToInt(geometry.Floor.Size.y) * 2)
                return MapTravelState.MapNotReady;
            CircleCollider2D collider = player.TopdownRigidbody?.MovementCollider;
            if (collider == null) return MapTravelState.MapNotReady;
            Vector3 target = marker.IsPerson ? marker.Target.position : marker.WorldPosition;
            if (!geometry.Contains(target) || !grid.WorldToCell(target, out int cx, out int cy))
                return MapTravelState.Unavailable;
            Bounds body = collider.bounds;
            Vector3 offset = body.center - player.transform.position;
            CircleCollider2D targetBody = marker.IsPerson
                ? marker.Target.GetComponent<TopdownRigidbody>()?.MovementCollider : null;
            int radius = PathFinder.nearestWalkableRadius;
            float maxDistance = radius * grid.CellSize;
            candidates.Clear();
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (grid.IsBlocked(x, y)) continue;
                    Vector3 candidate = grid.CellToWorld(x, y);
                    if ((candidate - target).sqrMagnitude > maxDistance * maxDistance) continue;
                    Bounds landing = new Bounds(candidate + offset, body.size);
                    if (!geometry.Contains(landing.min) || !geometry.Contains(landing.max) ||
                        (targetBody != null && landing.Intersects(targetBody.bounds)) ||
                        Physics2D.OverlapBox(landing.center, landing.size, 0f,
                            CombatManager.PathfindingObstacleLayerMask) != null) continue;
                    candidates.Add(candidate);
                }
            candidates.Sort((a, b) =>
            {
                int distance = (a - target).sqrMagnitude.CompareTo((b - target).sqrMagnitude);
                return distance != 0 ? distance :
                    (a - player.transform.position).sqrMagnitude.CompareTo((b - player.transform.position).sqrMagnitude);
            });
            foreach (Vector3 candidate in candidates)
            {
                // A nearby clear cell across a closed wall is not an accessible landing.
                if (!PathFinder.Find(grid, player.transform.position, candidate, path)) continue;
                position = candidate;
                return MapTravelState.Ready;
            }
            return MapTravelState.NoNearbyLanding;
        }
    }
}
