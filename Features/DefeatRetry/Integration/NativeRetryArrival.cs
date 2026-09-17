using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    internal sealed class NativeRetryArrival
    {
        internal PlayerAvatar Player { get; }
        internal string FloorGuid { get; }
        internal Vector3 Destination { get; }
        private FloorGenerator arrivedFloor;

        internal NativeRetryArrival(PlayerAvatar player, string floor, Vector3 destination)
        {
            Player = player;
            FloorGuid = floor;
            Destination = destination;
        }

        internal void Reset() => arrivedFloor = null;

        internal bool IsCurrent => arrivedFloor != null && arrivedFloor == ReadyFloor();

        private FloorGenerator ReadyFloor()
        {
            if (Player == null || Player.IsDead || Player.loadingScreenType != -1 ||
                Player.currentFloorGuid != FloorGuid) return null;
            FloorGenerator floor = FloorGenerator.FindByGuid(FloorGuid);
            return floor != null && floor.GenerateSuccess ? floor : null;
        }

        // Arrival and the owner's save/receipt can finish on different frames.
        // Walking within the restored floor must not erase an observed arrival.
        internal bool Observe()
        {
            FloorGenerator floor = ReadyFloor();
            if (floor == null)
            {
                Reset();
                return false;
            }
            if (arrivedFloor != floor)
            {
                Reset();
                if (IsAtDestination(Player, FloorGuid, Destination)) arrivedFloor = floor;
            }
            return arrivedFloor != null;
        }

        internal static string Describe(PlayerAvatar player, string floor, Vector3 destination) =>
            "player=" + (player != null ? player.netId : 0) + " expectedFloor=" + floor +
            " actualFloor=" + player?.currentFloorGuid + " destination=" + destination +
            " position=" + player?.transform.position + " loading=" + player?.loadingScreenType;

        // The saved position is supplied as an explicit native travel override.
        // Allow two world units for movement/network interpolation after arrival.
        internal static bool IsAtDestination(PlayerAvatar player, string floor, Vector3 destination)
        {
            if (player == null || player.loadingScreenType != -1 || player.currentFloorGuid != floor) return false;
            FloorGenerator generator = FloorGenerator.FindByGuid(floor);
            if (generator == null || !generator.GenerateSuccess) return false;
            float distance = ((Vector2)(player.transform.position - destination)).sqrMagnitude;
            return !float.IsNaN(distance) && !float.IsInfinity(distance) && distance <= 4f;
        }
    }
}
