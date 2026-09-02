using System;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class EnemySpawnOriginCapture
    {
        private static Action<UnitAvatar, EnemySpawnRoutineFrame> observer;

        internal static bool IsObserved => observer != null;

        internal static void SetObserver(Action<UnitAvatar, EnemySpawnRoutineFrame> value)
        {
            observer = value;
        }

        internal static void Publish(UnitAvatar unit, EnemySpawnRoutineFrame frame)
        {
            observer?.Invoke(unit, frame);
        }
    }
}
