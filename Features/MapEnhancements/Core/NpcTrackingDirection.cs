using System;

namespace SephiriaEnhancements.MapEnhancements.Core
{
    internal static class NpcTrackingDirection
    {
        internal static bool TryPlace(float targetX, float targetY, float cameraX, float cameraY,
            float halfWidth, float halfHeight, out FixedFloorMapPoint point)
        {
            const float inset = .3125f;
            float x = Math.Max(cameraX - halfWidth + inset, Math.Min(targetX, cameraX + halfWidth - inset));
            float y = Math.Max(cameraY - halfHeight + inset, Math.Min(targetY, cameraY + halfHeight - inset));
            point = new FixedFloorMapPoint(x, y);
            return x != targetX || y != targetY;
        }
    }
}
