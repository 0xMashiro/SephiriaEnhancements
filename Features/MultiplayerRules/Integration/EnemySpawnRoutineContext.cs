#nullable enable
using System;
using System.Collections;
using SephiriaEnhancements.Runtime.Execution;

namespace SephiriaEnhancements.MultiplayerRules.Integration
{
    internal static class EnemySpawnRoutineContext
    {
        private static Func<EnemySpawnRoutineFrame, IDisposable>? ruleScopeFactory;

        internal static EnemySpawnRoutineFrame? CurrentFrame =>
            AmbientExecutionContext<EnemySpawnRoutineFrame>.Current;

        internal static void SetRuleScopeFactory(
            Func<EnemySpawnRoutineFrame, IDisposable>? factory)
        {
            ruleScopeFactory = factory;
        }

        internal static IEnumerator Wrap(IEnumerator routine, EnemySpawnOrigin origin,
            object source)
        {
            return AmbientExecutionContext<EnemySpawnRoutineFrame>.WrapCoroutine(
                routine, new EnemySpawnRoutineFrame(origin, source),
                ruleScopeFactory);
        }

        internal static IDisposable Enter(EnemySpawnOrigin origin, object source)
        {
            return AmbientExecutionContext<EnemySpawnRoutineFrame>.Enter(
                new EnemySpawnRoutineFrame(origin, source));
        }
    }

    internal sealed class EnemySpawnRoutineFrame
    {
        internal EnemySpawnRoutineFrame(EnemySpawnOrigin origin, object source)
        {
            Origin = origin;
            Source = source;
        }

        internal EnemySpawnOrigin Origin { get; }
        internal object Source { get; }
    }
}
