#nullable disable
using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Runtime
{
    internal enum FeatureId
    {
        Gameplay, Inventory, CombatInsights, CombatTargeting, AutoCasting,
        MapEnhancements, KeyboardUiNavigation, CombatVisuals, CombatRelationOutlines,
        NativeCompanion, ViewDistance, ResourceBarValues, DefeatRetry,
        MultiplayerRules, MultiplayerAccess, ModJournal, Settings, ModInformation,
        DeveloperTools, EffectStats, CharacterPanelNavigation, OptionsNavigation,
        RewardNavigation, WorldMapKeyboardScrolling
    }

    // Failure belongs to the loaded Mod instance, not to a floor or exploration.
    internal static class FeatureFailure
    {
        private static readonly HashSet<FeatureId> failed = new();
        private static readonly Queue<FeatureId> pending = new();
        private static readonly HashSet<FeatureId> notices = new();
        internal static Action<FeatureId, Exception> Report;

        internal static FeatureId[] PendingNotices => new List<FeatureId>(notices).ToArray();
        internal static void AcknowledgeNotice(FeatureId feature) => notices.Remove(feature);

        internal static bool IsAvailable(FeatureId feature) => !failed.Contains(feature);

        internal static void Reset()
        {
            failed.Clear();
            pending.Clear();
            notices.Clear();
            Report = null;
        }

        internal static void Disable(FeatureId feature, Exception exception)
        {
            if (!failed.Add(feature)) return;
            pending.Enqueue(feature);
            if (feature != FeatureId.Gameplay && feature != FeatureId.DeveloperTools)
                notices.Add(feature);
            // Reporting must never replace the exception we are containing.
            try { Report?.Invoke(feature, exception); }
            catch (Exception) { }
            if (feature == FeatureId.KeyboardUiNavigation)
            {
                Disable(FeatureId.CharacterPanelNavigation, exception);
                Disable(FeatureId.OptionsNavigation, exception);
                Disable(FeatureId.RewardNavigation, exception);
                Disable(FeatureId.WorldMapKeyboardScrolling, exception);
            }
            if (feature == FeatureId.Gameplay)
            {
                Disable(FeatureId.Inventory, exception);
                Disable(FeatureId.CombatInsights, exception);
            }
            if (feature == FeatureId.CombatInsights)
                Disable(FeatureId.DefeatRetry, exception);
        }

        internal static bool Run(FeatureId feature, Action action)
        {
            if (!IsAvailable(feature)) return false;
            try { action(); return IsAvailable(feature); }
            catch (Exception exception) { Disable(feature, exception); return false; }
        }

        internal static bool TryTakeFailure(out FeatureId feature)
        {
            if (pending.Count == 0) { feature = default; return false; }
            feature = pending.Dequeue();
            return true;
        }
    }
}
