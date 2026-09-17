using System;
using System.Linq;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryArrangementBlockers
    {
        // These failures identify a movable item. Cell-wide and global failures do not.
        private static readonly string[] ItemReasons =
        {
            "ArtifactActivationMismatch", "ArtifactDisplayedLevelMismatch", "ArtifactCriteriaUnknown",
            "ArtifactLayoutCriteriaUnavailable", "ArtifactCategoryRuleUnavailable",
            "TabletProjectionUnavailable", "TabletLayoutProjectionIncomplete", "TabletApplicationMismatch",
            "PositionEffectRowCategoryMismatch", "PositionEffectCaptureUnavailable"
        };

        internal static InventoryItemSnapshot[] Find(InventorySnapshot snapshot) =>
            snapshot == null || snapshot.SettlementValidation.HasItemIdentityConflict
                ? Array.Empty<InventoryItemSnapshot>()
                : snapshot.Items.Where(item => ItemReasons.Any(reason => HasIssue(snapshot, item, reason)))
                    .ToArray();

        private static bool HasIssue(InventorySnapshot snapshot, InventoryItemSnapshot item, string reason) =>
            snapshot.SettlementValidation.Issues.Any(issue => issue == reason + ":" + item.ItemKey ||
                reason == "PositionEffectCaptureUnavailable" && issue.StartsWith(reason + ":" + item.ItemKey + ":", StringComparison.Ordinal));

        internal static string Details(InventorySnapshot snapshot, InventoryItemSnapshot item)
        {
            string details = "item=" + item.ItemKey + " nameKey=" + item.NameKey + " cell=" + item.CellIndex + " issues=" +
                string.Join(",", ItemReasons.Where(reason => HasIssue(snapshot, item, reason)));
            var artifact = item.Artifact;
            if (artifact == null) return details;
            var cell = snapshot.Cells[item.CellIndex];
            bool expected = InventorySettlementValidator.ExpectedArtifactEnabled(snapshot, item);
            return details + " enabled=" + artifact.EffectEnabled + " expectedEnabled=" + expected +
                " limitedLevel=" + artifact.LimitedEffectEnabledLevel + " expectedLimitedLevel=" +
                (expected ? Math.Min(artifact.MaxLevel, cell.Level) : 0) +
                " cellLevel=" + cell.Level + " displayedLevel=" + artifact.DisplayedLevel +
                " globalActive=" + snapshot.GlobalActiveValue + " disabled=" + cell.Disabled +
                " bypass=" + cell.IgnoresCriteria + " criteria=" + artifact.Criteria?.RuntimeState +
                " weaponCompatible=" + artifact.WeaponCompatible +
                " uniqueRegistered=" + artifact.UniqueEffectRegistered;
        }
    }
}
