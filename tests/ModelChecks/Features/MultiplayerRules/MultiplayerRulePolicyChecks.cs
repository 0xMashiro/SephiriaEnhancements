using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using System.Collections;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;

internal static class MultiplayerRulePolicyChecks
{
    internal static void Run()
    {
        if (Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
                EnemyHealthModifierCombination.ParticipantRuleOnly) - 1.3f) > 0.001f ||
            Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
                EnemyHealthModifierCombination.Additive) - 1.5f) > 0.001f ||
            Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
                EnemyHealthModifierCombination.Multiplicative) - 1.56f) > 0.001f)
        {
            throw new InvalidOperationException(
                "enemy health modifier combination semantics failed");
        }

        var multiplayerRulesLifecycleCases = new[]
        {
            (isHost: true, explorationStarted: false, canEdit: true),
            (isHost: false, explorationStarted: false, canEdit: false),
            (isHost: true, explorationStarted: true, canEdit: false),
            (isHost: false, explorationStarted: true, canEdit: false)
        };
        if (MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
                MultiplayerRulesPreset.Original) ||
            !MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
                MultiplayerRulesPreset.Optimized) ||
            !MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
                MultiplayerRulesPreset.Custom))
        {
            throw new InvalidOperationException(
                "only non-original multiplayer rules require native behavior hooks");
        }
        foreach (var lifecycleCase in multiplayerRulesLifecycleCases)
        {
            if (MultiplayerRulesLifecyclePolicy.CanEditHostPreferences(
                    lifecycleCase.isHost,
                    lifecycleCase.explorationStarted) != lifecycleCase.canEdit)
            {
                throw new InvalidOperationException(
                    "multiplayer-rule edit lifecycle matrix failed");
            }
        }
        if (!MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, true, true, 4, false, false) ||
            !MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, true, true, 4, true, true) ||
            MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, true, true, 4, true, false) ||
            MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, true, true, 5, true, true) ||
            MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                false, true, true, 4, false, false) ||
            MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, false, true, 4, false, false) ||
            MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
                true, true, false, 4, false, false))
        {
            throw new InvalidOperationException(
                "authoritative multiplayer rules must fail open for external providers " +
                "and unsupported participant counts");
        }

        EnemySpawnOrigin? observedOrigin = null;
        bool innerDisposed = false;
        IEnumerator InnerRoutine()
        {
            try
            {
                observedOrigin = EnemySpawnRoutineContext.CurrentFrame?.Origin;
                yield return "spawn";
                observedOrigin = EnemySpawnRoutineContext.CurrentFrame?.Origin;
            }
            finally
            {
                innerDisposed = true;
            }
        }

        IEnumerator wrappedRoutine = EnemySpawnRoutineContext.Wrap(InnerRoutine(),
            EnemySpawnOrigin.RandomEncounter, new object());
        if (EnemySpawnRoutineContext.CurrentFrame != null || !wrappedRoutine.MoveNext() ||
            observedOrigin != EnemySpawnOrigin.RandomEncounter ||
            EnemySpawnRoutineContext.CurrentFrame != null ||
            !Equals(wrappedRoutine.Current, "spawn"))
        {
            throw new InvalidOperationException(
                "enemy spawn origin must exist only while the native routine advances");
        }
        (wrappedRoutine as IDisposable)?.Dispose();
        if (!innerDisposed || EnemySpawnRoutineContext.CurrentFrame != null)
        {
            throw new InvalidOperationException(
                "enemy spawn routine wrapper must forward disposal and restore context");
        }
        Console.WriteLine("MultiplayerRules: original pass-through, optimized fixes, " +
            "lifecycle and spawn context checks passed");
    }
}
