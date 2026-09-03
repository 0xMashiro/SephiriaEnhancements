#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal static partial class InventoryOptimizationLocalization
    {
        internal const string Analyzing =
            "SephiriaEnhancements.Inventory.Analyzing";
        internal const string Applying =
            "SephiriaEnhancements.Inventory.Applying";
        internal const string Completed =
            "SephiriaEnhancements.Inventory.Completed";
        internal const string NoImprovementFound =
            "SephiriaEnhancements.Inventory.NoImprovementFound";
        internal const string Unavailable =
            "SephiriaEnhancements.Inventory.Unavailable";
        internal const string RuntimeNotReady =
            "SephiriaEnhancements.Inventory.RuntimeNotReady";
        internal const string EmptyInventory =
            "SephiriaEnhancements.Inventory.EmptyInventory";
        internal const string ItemIdentityConflict =
            "SephiriaEnhancements.Inventory.ItemIdentityConflict";
        internal const string PositionEffectsUnavailable =
            "SephiriaEnhancements.Inventory.PositionEffectsUnavailable";
        internal const string PositionEffectObservationUnavailableOnClient =
            "SephiriaEnhancements.Inventory.PositionEffectObservationUnavailableOnClient";
        internal const string Unsupported =
            "SephiriaEnhancements.Inventory.Unsupported";
        internal const string Changed =
            "SephiriaEnhancements.Inventory.Changed";
        internal const string InventoryClosed =
            "SephiriaEnhancements.Inventory.InventoryClosed";
        internal const string GameplayContextChanged =
            "SephiriaEnhancements.Inventory.GameplayContextChanged";
        internal const string ApplyTimedOut =
            "SephiriaEnhancements.Inventory.ApplyTimedOut";
        internal const string Failed =
            "SephiriaEnhancements.Inventory.Failed";
        internal const string VerificationFailed =
            "SephiriaEnhancements.Inventory.VerificationFailed";
        internal const string Busy =
            "SephiriaEnhancements.Inventory.Busy";
        internal const string FinishMovingItem =
            "SephiriaEnhancements.Inventory.FinishMovingItem";
        internal const string MovingItemInterrupted =
            "SephiriaEnhancements.Inventory.MovingItemInterrupted";
        internal const string DisabledForGameplayContext =
            "SephiriaEnhancements.Inventory.DisabledForGameplayContext";
        internal const string SettingOptimizationTendency =
            "SephiriaEnhancements.Setting.InventoryOptimizationTendency";
        internal const string HelpOptimizationTendency =
            "SephiriaEnhancements.Help.InventoryOptimizationTendency";
        internal static readonly string[] OptimizationTendencyKeys =
        {
            "SephiriaEnhancements.InventoryOptimizationTendency.Automatic",
            "SephiriaEnhancements.InventoryOptimizationTendency.Stable",
            "SephiriaEnhancements.InventoryOptimizationTendency.Aggressive"
        };
        internal const string HudTitle =
            "SephiriaEnhancements.InventoryHud.Title";
        internal const string HudComboTargets =
            "SephiriaEnhancements.InventoryHud.ComboTargets";
        internal const string HudOptimize =
            "SephiriaEnhancements.InventoryHud.Optimize";
        internal const string HudMarkArtifacts =
            "SephiriaEnhancements.InventoryHud.MarkArtifacts";
        internal const string HudFinishMarking =
            "SephiriaEnhancements.InventoryHud.FinishMarking";
        internal const string HudMarkingHint =
            "SephiriaEnhancements.InventoryHud.MarkingHint";
        internal const string HudMarkedCount =
            "SephiriaEnhancements.InventoryHud.MarkedCount";
        internal const string HudMarkedAndAdjustmentCount =
            "SephiriaEnhancements.InventoryHud.MarkedAndAdjustmentCount";
        internal const string HudPriorityQueue =
            "SephiriaEnhancements.InventoryHud.PriorityQueue";
        internal const string HudAvoidZone =
            "SephiriaEnhancements.InventoryHud.AvoidZone";
        internal const string HudIntentBoardHint =
            "SephiriaEnhancements.InventoryHud.IntentBoardHint";
        internal const string HudLevelEditUnbound =
            "SephiriaEnhancements.InventoryHud.LevelEditUnbound";
        internal const string HudChooseIntentSlot =
            "SephiriaEnhancements.InventoryHud.ChooseIntentSlot";
        internal const string HudOpen =
            "SephiriaEnhancements.InventoryHud.Open";
        internal const string HudAdjustTargets =
            "SephiriaEnhancements.InventoryHud.AdjustTargets";
        internal const string HudHideTargets =
            "SephiriaEnhancements.InventoryHud.HideTargets";
        internal const string HudAutomaticPreset =
            "SephiriaEnhancements.InventoryHud.AutomaticPreset";
        internal const string HudAutomaticInventory =
            "SephiriaEnhancements.InventoryHud.AutomaticInventory";
        internal const string HudAdjustmentCount =
            "SephiriaEnhancements.InventoryHud.AdjustmentCount";
        internal const string HudEnabled =
            "SephiriaEnhancements.InventoryHud.Enabled";
        internal const string HudAutomaticTarget =
            "SephiriaEnhancements.InventoryHud.AutomaticTarget";
        internal const string HudMinimumLevel =
            "SephiriaEnhancements.InventoryHud.MinimumLevel";
        internal const string HudMinimumCount =
            "SephiriaEnhancements.InventoryHud.MinimumCount";
        internal const string HudMaximumCount =
            "SephiriaEnhancements.InventoryHud.MaximumCount";
        internal const string HudNoMinimumCount =
            "SephiriaEnhancements.InventoryHud.NoMinimumCount";
        internal const string HudNoTargets =
            "SephiriaEnhancements.InventoryHud.NoTargets";
        internal const string HudPage =
            "SephiriaEnhancements.InventoryHud.Page";
        internal const string HudSearching =
            "SephiriaEnhancements.InventoryHud.Searching";
        internal const string HudApplying =
            "SephiriaEnhancements.InventoryHud.Applying";
        internal static readonly string[] PreferenceChoiceKeys =
        {
            "SephiriaEnhancements.InventoryPreference.Automatic",
            "SephiriaEnhancements.InventoryPreference.Priority",
            "SephiriaEnhancements.InventoryPreference.Avoid"
        };

        internal static string FormatTargetCondition(
            InventoryComboTarget target, Func<string, string> localize)
        {
            if (target.Choice == InventoryPreferenceChoice.Automatic)
            {
                return localize(HudAutomaticTarget);
            }
            return target.Choice == InventoryPreferenceChoice.Priority && target.RequiredValue == 0
                ? localize(HudNoMinimumCount)
                : string.Format(localize(target.Choice == InventoryPreferenceChoice.Avoid
                    ? HudMaximumCount : HudMinimumCount), target.RequiredValue);
        }

        internal static string FormatArtifactMinimumLevel(int level, Func<string, string> localize) =>
            level == 0 ? localize(HudEnabled) : string.Format(localize(HudMinimumLevel), level);

        internal static string PositionEffectFailureMessage(InventorySettlementValidationSnapshot validation) =>
            validation.PositionEffectObservationUnavailableOnClient
                ? PositionEffectObservationUnavailableOnClient : PositionEffectsUnavailable;

        private static readonly string[] Keys =
        {
            Analyzing,
            Applying,
            Completed,
            NoImprovementFound,
            Unavailable,
            RuntimeNotReady,
            EmptyInventory,
            ItemIdentityConflict,
            Unsupported,
            PositionEffectsUnavailable,
            PositionEffectObservationUnavailableOnClient,
            Changed,
            InventoryClosed,
            GameplayContextChanged,
            ApplyTimedOut,
            Failed,
            VerificationFailed,
            Busy,
            FinishMovingItem,
            MovingItemInterrupted,
            DisabledForGameplayContext,
            SettingOptimizationTendency,
            HelpOptimizationTendency,
            OptimizationTendencyKeys[0],
            OptimizationTendencyKeys[1],
            OptimizationTendencyKeys[2],
            HudTitle,
            HudComboTargets,
            HudOptimize,
            HudMarkArtifacts,
            HudFinishMarking,
            HudMarkingHint,
            HudMarkedCount,
            HudMarkedAndAdjustmentCount,
            HudPriorityQueue,
            HudAvoidZone,
            HudIntentBoardHint,
            HudLevelEditUnbound,
            HudChooseIntentSlot,
            HudOpen,
            HudAdjustTargets,
            HudHideTargets,
            HudAutomaticPreset,
            HudAutomaticInventory,
            HudAdjustmentCount,
            HudEnabled,
            HudAutomaticTarget,
            HudMinimumLevel,
            HudMinimumCount,
            HudMaximumCount,
            HudNoMinimumCount,
            HudNoTargets,
            HudPage,
            HudSearching,
            HudApplying,
            PreferenceChoiceKeys[0],
            PreferenceChoiceKeys[1],
            PreferenceChoiceKeys[2],
        };

        internal static void Register(Action<string, string, string> addText)
        {
            foreach (string language in Configuration.LocalizationLanguages.All)
            {
                string[] values = Texts.TryGetValue(language, out string[] translated)
                    ? translated : Texts["en-US"];
                for (int index = 0; index < Keys.Length; index++)
                    addText(language, Keys[index], values[index]);
            }
        }
    }
}
