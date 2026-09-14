using SephiriaEnhancements.DefeatRetry;

namespace SephiriaEnhancements.ModelChecks.Features.DefeatRetry;

internal static class DefeatRetryPolicyChecks
{
    internal static void Run()
    {
        if (!DefeatRetryPolicy.ShouldPresent(true, true, true, true, 0, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, false, true, 0, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, 1, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, 2, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, 0, true) ||
            DefeatRetryPolicy.ShouldPresent(true, false, true, true, 0, false))
            throw new InvalidOperationException("ordinary host defeat must show retry availability even without a checkpoint");

        if (!DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, false, true, 0, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 1, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 2, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, true,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, false, true, true, 0, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, false,
                saveIdle: false, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, false, true, true, true, 0, false,
                saveIdle: true, nativeRestarting: false))
            throw new InvalidOperationException(
                "floor retry must be enabled, host-only, defeat-only and floor-start-snapshot-gated");
        Console.WriteLine("DefeatRetryPolicy: setting, host and checkpoint gates passed");

        if (DefeatRetryPolicy.ClassifyConclusion(0) !=
                RetryConclusionKind.CombatDefeat ||
            DefeatRetryPolicy.ClassifyConclusion(2) !=
                RetryConclusionKind.ScriptedDefeat ||
            DefeatRetryPolicy.ClassifyConclusion(1) != RetryConclusionKind.Victory ||
            DefeatRetryPolicy.ClassifyConclusion(6) != RetryConclusionKind.Victory ||
            DefeatRetryPolicy.ClassifyConclusion(99) != RetryConclusionKind.Unknown)
        {
            throw new InvalidOperationException(
                "native game-over types must preserve combat, scripted and victory semantics");
        }
        Console.WriteLine("DefeatRetryPolicy: game-over semantics passed");

        if (!DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
                true, true, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(false, true, false,
                true, true, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, false, false,
                true, true, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, true,
                true, true, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
                false, true, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
                true, false, true, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
                true, true, false, true) ||
            DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
                true, true, true, false))
        {
            throw new InvalidOperationException(
                "floor-entry checkpoints must be captured only for an enabled active host run");
        }
        Console.WriteLine("DefeatRetryPolicy: floor-entry capture gates passed");

        if (!DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
                true, true, true, true, hasFloor: true, hasBoss: true) ||
            DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
                true, true, true, true, hasFloor: false, hasBoss: true) ||
            DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
                true, true, true, true, hasFloor: true, hasBoss: false) ||
            !DefeatRetryPolicy.ShouldApplyPlacement(restorePending: true,
                "boss-floor", "boss-floor") ||
            DefeatRetryPolicy.ShouldApplyPlacement(restorePending: true,
                "boss-floor", "other-floor") ||
            DefeatRetryPolicy.ShouldApplyPlacement(restorePending: false,
                "boss-floor", "boss-floor"))
        {
            throw new InvalidOperationException(
                "boss checkpoints require a live encounter and placements must stay floor-bound");
        }
        Console.WriteLine("DefeatRetryPolicy: boss checkpoint and placement gates passed");
    }
}
