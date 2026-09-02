#nullable disable

namespace SephiriaEnhancements.Runtime.Inventory
{
    internal readonly struct InventoryKnownCellContributions
    {
        internal InventoryKnownCellContributions(int enchantLevel,
            int fixedLevel, int fixedDisableCount,
            int fixedCriteriaBypassCount, int fixedLevelMultiplier,
            int tabletLevel, int tabletDisableCount,
            int tabletCriteriaBypassCount, int tabletLevelMultiplier)
        {
            EnchantLevel = enchantLevel;
            FixedLevel = fixedLevel;
            FixedDisableCount = fixedDisableCount;
            FixedCriteriaBypassCount = fixedCriteriaBypassCount;
            FixedLevelMultiplier = fixedLevelMultiplier;
            TabletLevel = tabletLevel;
            TabletDisableCount = tabletDisableCount;
            TabletCriteriaBypassCount = tabletCriteriaBypassCount;
            TabletLevelMultiplier = tabletLevelMultiplier;
        }

        internal int EnchantLevel { get; }
        internal int FixedLevel { get; }
        internal int FixedDisableCount { get; }
        internal int FixedCriteriaBypassCount { get; }
        internal int FixedLevelMultiplier { get; }
        internal int TabletLevel { get; }
        internal int TabletDisableCount { get; }
        internal int TabletCriteriaBypassCount { get; }
        internal int TabletLevelMultiplier { get; }
    }

    internal static class InventoryBaselineInference
    {
        internal static bool TryInfer(int settledLevel,
            int settledMaximumLevel, int settledTemporaryLevel,
            int settledLevelMultiplier, int settledDisableCount,
            int settledCriteriaBypassCount, bool artifactEffectsEnabled,
            bool artifactOccupiesCell,
            InventoryKnownCellContributions contributions,
            out InventoryCellSettlementSnapshot settlement)
        {
            int additiveLevel;
            if (settledLevelMultiplier == 0)
            {
                additiveLevel = settledLevel;
            }
            else
            {
                if (settledLevel % settledLevelMultiplier != 0)
                {
                    settlement = null;
                    return false;
                }
                additiveLevel = settledLevel / settledLevelMultiplier;
            }

            int baselineLevel = additiveLevel - contributions.EnchantLevel -
                contributions.FixedLevel - contributions.TabletLevel;
            int baselineMultiplier = settledLevelMultiplier -
                contributions.FixedLevelMultiplier -
                contributions.TabletLevelMultiplier;
            int baselineDisable = settledDisableCount -
                contributions.FixedDisableCount -
                contributions.TabletDisableCount;
            int baselineCriteriaBypass = settledCriteriaBypassCount -
                contributions.FixedCriteriaBypassCount -
                contributions.TabletCriteriaBypassCount;
            int baselineMaximumLevel = artifactEffectsEnabled &&
                artifactOccupiesCell
                    ? -1
                    : settledMaximumLevel;

            settlement = new InventoryCellSettlementSnapshot(
                baselineKnown: true, baselineLevel, baselineMaximumLevel,
                settledTemporaryLevel, baselineMultiplier, baselineDisable,
                baselineCriteriaBypass, contributions.EnchantLevel,
                contributions.FixedLevel, contributions.FixedDisableCount,
                contributions.FixedCriteriaBypassCount,
                contributions.FixedLevelMultiplier,
                contributions.TabletLevel, contributions.TabletDisableCount,
                contributions.TabletCriteriaBypassCount,
                contributions.TabletLevelMultiplier);
            return true;
        }
    }
}
