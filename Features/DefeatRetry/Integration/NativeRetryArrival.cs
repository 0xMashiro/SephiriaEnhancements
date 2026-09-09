using UnityEngine;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryArrival
    {
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
