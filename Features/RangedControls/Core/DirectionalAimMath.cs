using System;

namespace SephiriaEnhancements.RangedControls
{
    internal static class DirectionalAimMath
    {
        private const float DirectionWeight = 0.7f;
        private const float DistanceWeight = 0.3f;
        private const float CurrentTargetBonus = 0.15f;

        internal static (float X, float Y) Normalize(float x, float y)
        {
            float lengthSquared = x * x + y * y;
            if (lengthSquared <= 0.0001f)
            {
                return (0f, 0f);
            }

            float inverseLength = 1f / MathF.Sqrt(lengthSquared);
            return (x * inverseLength, y * inverseLength);
        }

        internal static float DotNormalized(float directionX, float directionY,
            float targetX, float targetY)
        {
            (float X, float Y) direction = Normalize(directionX, directionY);
            (float X, float Y) target = Normalize(targetX, targetY);
            if ((direction.X == 0f && direction.Y == 0f) ||
                (target.X == 0f && target.Y == 0f))
            {
                return -1f;
            }

            return direction.X * target.X + direction.Y * target.Y;
        }

        internal static float AutomaticTargetScore(float directionX, float directionY,
            float targetX, float targetY, float distanceSquared, float maxDistanceSquared,
            bool preferDirection, bool currentTarget)
        {
            if (distanceSquared <= 0f || distanceSquared > maxDistanceSquared ||
                maxDistanceSquared <= 0f)
            {
                return float.NegativeInfinity;
            }

            float proximity = 1f - MathF.Sqrt(distanceSquared / maxDistanceSquared);
            float score = proximity;
            if (preferDirection)
            {
                float dot = DotNormalized(directionX, directionY, targetX, targetY);
                score = dot * DirectionWeight + proximity * DistanceWeight;
            }

            return currentTarget ? score + CurrentTargetBonus : score;
        }
    }
}
