using SephiriaEnhancements.DefeatRetry;

namespace SephiriaEnhancements.ModelChecks.Features.DefeatRetry;

internal static class DefeatRetryPolicyChecks
{
    internal static void Run()
    {
        if (!DefeatRetryPolicy.ShouldPresent(true, true, true, true, RetryConclusionKind.CombatDefeat, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, false, true, RetryConclusionKind.CombatDefeat, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, RetryConclusionKind.Victory, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, RetryConclusionKind.ScriptedDefeat, false) ||
            DefeatRetryPolicy.ShouldPresent(true, true, true, true, RetryConclusionKind.CombatDefeat, true) ||
            DefeatRetryPolicy.ShouldPresent(true, false, true, true, RetryConclusionKind.CombatDefeat, false))
            throw new InvalidOperationException("ordinary host defeat must show retry availability even without a checkpoint");

        if (!DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, RetryConclusionKind.CombatDefeat, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, false, true, RetryConclusionKind.CombatDefeat, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, RetryConclusionKind.Victory, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, RetryConclusionKind.ScriptedDefeat, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, RetryConclusionKind.CombatDefeat, true,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, false, true, true, RetryConclusionKind.CombatDefeat, false,
                saveIdle: true, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, RetryConclusionKind.CombatDefeat, false,
                saveIdle: false, nativeRestarting: false) ||
            DefeatRetryPolicy.ShouldOffer(true, false, true, true, true, RetryConclusionKind.CombatDefeat, false,
                saveIdle: true, nativeRestarting: false))
            throw new InvalidOperationException(
                "floor retry must be enabled, host-only, defeat-only and floor-start-snapshot-gated");
        Console.WriteLine("DefeatRetryPolicy: setting, host and checkpoint gates passed");

        foreach (RetryConclusionKind kind in Enum.GetValues<RetryConclusionKind>())
        {
            bool expected = kind == RetryConclusionKind.CombatDefeat;
            if (DefeatRetryPolicy.ShouldPresent(true, true, true, true, kind, false) != expected ||
                DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, kind, false, true, false) != expected)
                throw new InvalidOperationException("only a confirmed combat defeat may present or execute retry");
        }
        if (DefeatRetryPolicy.ShouldOffer(true, true, true, true, true,
                RetryConclusionKind.CombatDefeat, false, true, true))
            throw new InvalidOperationException("native restart must block another retry");
        Console.WriteLine("DefeatRetryPolicy: conclusion and availability matrix passed");

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
