using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class InventorySettlementValidationChecks
{
    internal static void Run()
    {
        var verifiedSettlement = new InventoryCellSettlementSnapshot(true,
            baselineLevel: 1, baselineMaximumLevel: -1, baselineTemporaryLevel: 0,
            baselineLevelMultiplier: 0, baselineDisableCount: 0,
            baselineCriteriaBypassCount: 0, enchantLevel: 1, fixedLevel: 0,
            fixedDisableCount: 0, fixedCriteriaBypassCount: 0,
            fixedLevelMultiplier: 0, tabletLevel: 0, tabletDisableCount: 0,
            tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
        var verifiedArtifact = new ArtifactSnapshot(2, 3, 1, 2, 2,
            true, false, false, "", true, false, false, "Default",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable), new[] { "STURDY" },
            new[] { "STURDY" }, false, null);
        var verifiedSnapshot = new InventorySnapshot(1, 1,
            new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
                false, verifiedSettlement) },
            new[] { new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
                "Item_Verified", "Charm", "Common", new[] { "STURDY" },
                InventoryItemKind.Artifact, verifiedArtifact, null) },
            comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
                1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
        if (!verifiedSnapshot.SettlementValidation.CurrentLayoutVerified ||
            !verifiedSnapshot.SettlementValidation.LayoutProjectionReady ||
            (verifiedSnapshot.SettlementValidation.Capabilities &
                InventorySettlementCapabilities.SnapshotShapeVerified) == 0 ||
            !verifiedSnapshot.SettlementValidation.LayoutProjectionReady ||
            verifiedSnapshot.SettlementValidation.Issues.Count != 0)
            throw new InvalidOperationException(
                "verified settlement must satisfy every evaluator prerequisite");
        ProjectedInventorySettlement evaluatedCurrent =
            InventorySettlementProjector.Evaluate(verifiedSnapshot,
                InventoryLayoutProjection.Current(verifiedSnapshot));
        if (!evaluatedCurrent.Succeeded || evaluatedCurrent.Cells[0].Level != 2 ||
            evaluatedCurrent.Cells[0].MaximumLevel != 3 ||
            evaluatedCurrent.Cells[0].TemporaryLevel != 0 ||
            !evaluatedCurrent.Artifacts[0].Enabled ||
            evaluatedCurrent.Artifacts[0].CappedEffectiveLevel != 2 ||
            evaluatedCurrent.ComboCounts["STURDY"] != 1)
            throw new InvalidOperationException(
                "candidate evaluator must reproduce the verified current layout");
        InventorySettlementDifferentialReport matchingDifferential =
            InventorySettlementDifferentialVerifier.Compare(verifiedSnapshot,
                InventoryLayoutProjection.Current(verifiedSnapshot), evaluatedCurrent,
                verifiedSnapshot);
        if (!matchingDifferential.Matched ||
            matchingDifferential.Mismatches.Count != 0 ||
            matchingDifferential.Coverage.ArtifactCount != 1 ||
            matchingDifferential.Coverage.EnchantedArtifactCount != 1 ||
            !matchingDifferential.Coverage.NativeItemTypes.SequenceEqual(
                new[] { "Charm" }))
            throw new InvalidOperationException(
                "identical native and predicted settlements must match");

        var arrangementEnabledSnapshot = new InventorySnapshot(1, 1,
            new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
                false, verifiedSettlement) },
            new[] { new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
                "Item_Verified", "Charm", "Common", new[] { "STURDY" },
                InventoryItemKind.Artifact, verifiedArtifact, null) },
            comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
                1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) },
            arrangementBonusesEnabled: true);
        if (arrangementEnabledSnapshot.SettlementValidation.LayoutProjectionReady ||
            !arrangementEnabledSnapshot.SettlementValidation.Issues.Contains(
                "LayoutProjectionArrangementBonusesUnavailable"))
            throw new InvalidOperationException(
                "unmodeled arrangement bonuses must fail candidate readiness");

        var inactiveEffectsSnapshot = new InventorySnapshot(1, 1,
            new[] { new InventoryCellSnapshot(0, 0, 0, 0, -1, 0, 0, 0, 0,
                false, new InventoryCellSettlementSnapshot(true, 0, -1, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)) },
            new[] { new InventoryItemSnapshot(24, 204, 1, 0, 0, 0, "Inactive",
                "Item_Inactive", "Charm", "Common", new[] { "STURDY" },
                InventoryItemKind.Artifact,
                new ArtifactSnapshot(0, 3, 0, 0, 0,
                    false, true, false, "", true, false, false, "Default",
                    new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                        CriteriaEvaluationState.NotApplicable,
                        CriteriaEvaluationState.NotApplicable),
                    new[] { "STURDY" }, new[] { "STURDY" }, false, null),
                null) },
            artifactEffectsEnabled: false,
            comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
                1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
        if (inactiveEffectsSnapshot.SettlementValidation.LayoutProjectionReady ||
            !inactiveEffectsSnapshot.SettlementValidation.Issues.Contains(
                "LayoutProjectionArtifactEffectsInactive"))
            throw new InvalidOperationException(
                "inactive native artifact settlement must fail candidate readiness");

        var mixedSnapshot = new InventorySnapshot(2, 2,
            new[]
            {
                new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
                    false, verifiedSettlement),
                new InventoryCellSnapshot(1, 1, 0, 0, -1, 0, 0, 0, 0,
                    false, new InventoryCellSettlementSnapshot(true, 0, -1, 0,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))
            },
            new[]
            {
                new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
                    "Item_Verified", "Charm", "Common", new[] { "STURDY" },
                    InventoryItemKind.Artifact, verifiedArtifact, null),
                new InventoryItemSnapshot(22, 202, 1, 1, 1, 0, "Ordinary",
                    "Item_Ordinary", "Misc", "Common", Array.Empty<string>(),
                    InventoryItemKind.Other, null, null)
            },
            comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
                1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
        if (!mixedSnapshot.SettlementValidation.CurrentLayoutVerified ||
            !mixedSnapshot.SettlementValidation.LayoutProjectionReady ||
            !mixedSnapshot.SettlementValidation.LayoutProjectionReady)
            throw new InvalidOperationException(
                "ordinary items mixed with artifacts must remain candidate-ready");

        var malformedPayloadSnapshot = new InventorySnapshot(1, 1,
            new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
                false, verifiedSettlement) },
            new[] { new InventoryItemSnapshot(23, 203, 1, 0, 0, 0, "Malformed",
                "Item_Malformed", "Misc", "Common", Array.Empty<string>(),
                InventoryItemKind.Other, verifiedArtifact, null) });
        if (malformedPayloadSnapshot.SettlementValidation.CurrentLayoutVerified ||
            malformedPayloadSnapshot.SettlementValidation.LayoutProjectionReady ||
            !malformedPayloadSnapshot.SettlementValidation.Issues.Contains(
                "SnapshotItemPayloadInvalid:23"))
            throw new InvalidOperationException(
                "inconsistent item kinds must fail at the snapshot shape boundary");

        var mismatchedSnapshot = new InventorySnapshot(1, 1,
            new[] { new InventoryCellSnapshot(0, 0, 0, 3, 3, 0, 0, 0, 0,
                false, verifiedSettlement) }, Array.Empty<InventoryItemSnapshot>());
        if (mismatchedSnapshot.SettlementValidation.CurrentLayoutVerified ||
            !mismatchedSnapshot.SettlementValidation.Issues.Contains(
                "CellSettlementMismatch:0"))
            throw new InvalidOperationException(
                "settlement mismatch must block candidate evaluation");
        InventorySettlementDifferentialReport detectedDifferential =
            InventorySettlementDifferentialVerifier.Compare(verifiedSnapshot,
                InventoryLayoutProjection.Current(verifiedSnapshot), evaluatedCurrent,
                mismatchedSnapshot);
        if (detectedDifferential.Matched ||
            !detectedDifferential.Mismatches.Contains("CellLevel:0") ||
            !detectedDifferential.Mismatches.Contains("ItemMissing:21"))
            throw new InvalidOperationException(
                "native differential must report field and identity mismatches");
        Console.WriteLine("InventorySettlementValidator: positive and mismatch gates passed");
        Console.WriteLine("InventorySettlementDifferentialVerifier: native parity gates passed");
    }
}
