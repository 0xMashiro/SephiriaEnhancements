using System.Collections.Generic;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.MapEnhancements.Core;
using UnityEngine;

namespace SephiriaEnhancements.MapEnhancements.Integration
{
    internal sealed class NativeMapTravel
    {
        private readonly NativeMapGeometry geometry;
        private readonly List<Vector3> candidates = new();
        private readonly List<Vector3> path = new();
        private MapLocationMarkerView cachedMarker;
        private MapTravelState cachedState;
        private float refreshAt;
        private bool active = true;

        internal NativeMapTravel(NativeMapGeometry geometry) { this.geometry = geometry; }
        internal void Clear() { active = false; cachedMarker = null; }

        private bool CanMove(PlayerAvatar player, MapLocationMarkerView marker) =>
            active && geometry.Floor != null && marker != null && marker.Target != null &&
            marker.Target.gameObject.activeInHierarchy && player != null &&
            LocalPlayerResolver.Resolve() == player && player.currentFloorGuid == geometry.Floor.guid &&
            player.loadingScreenType == -1 && GameCamera.Instance?.Observer == player &&
            GameCamera.Instance.CurrentSeeingFloor == geometry.Floor &&
            !player.IsDead && !player.IsInBattle && player.CanMove &&
            player.blockMoveByInput <= 0 && !player.CanFastMove.IsFalse();

        internal MapTravelState State(MapLocationMarkerView marker)
        {
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            if (!CanMove(player, marker) || ScreenFader.Instance == null || ScreenFader.Instance.IsFading)
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
            if (State(marker) != MapTravelState.Ready) return;
            if (geometry.Designed == null)
            {
                if (geometry.TryGetRoomDestination(marker.WorldPosition, out var room)) room.TeleportToRoom();
                return;
            }
            PlayerAvatar player = LocalPlayerResolver.Resolve();
            // Revalidate after the native fade: targets, local player and floor can
            // change while it runs. ReqSetPosition retains native transform authority.
            ScreenFader.Instance.FadeOut(ScreenFader.ScreenType.NextFloor, () =>
            {
                if (!CanMove(player, marker)) return;
                Vector3 position;
                if (marker.Destination != null)
                {
                    if (!marker.Destination.CanTravel || !geometry.Contains(marker.Destination.Position)) return;
                    position = marker.Destination.Position;
                }
                else if (FindNearbyLanding(player, marker, out position) != MapTravelState.Ready) return;
                player.ReqSetPosition(position, teleport: true);
                GameCamera.Instance.targetTracker.SetCameraPosition(player.transform.position);
            }, autoFadeIn: true, .33f, 2f);
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
