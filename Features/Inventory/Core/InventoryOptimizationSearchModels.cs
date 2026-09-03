#nullable disable
using SephiriaEnhancements.Runtime.Inventory;

using System;
using System.Collections.Generic;

namespace SephiriaEnhancements.Inventory
{
    internal enum InventorySearchTerminationReason
    {
        NeighborhoodLocalOptimum,
        SearchSpaceExhausted,
        ScoreUpperBoundReached,
        ImprovementRoundLimit,
        CandidateEvaluationLimit,
        ElapsedTimeLimit,
        InputRejected
    }

    internal enum InventoryOptimizationSearchMethod
    {
        Neighborhood,
        Exhaustive,
        MultiStart
    }

    internal enum InventorySearchStage
    {
        Simple,
        SwapAndRotation,
        TwoSwaps,
        TwoItemRelocationAndRotation,
        ThreeItemRelocation
    }

    internal sealed class InventorySearchStageStatistics
    {
        internal InventorySearchStageStatistics(InventorySearchStage stage, int round)
        {
            Stage = stage;
            Round = round;
        }
        internal InventorySearchStage Stage { get; }
        internal int Round { get; }
        internal int CandidateEvaluations { get; set; }
        internal int DuplicateLayoutsSkipped { get; set; }
        internal long ElapsedMilliseconds { get; set; }
        internal int Improvements { get; set; }
        internal int LastImprovementCandidate { get; set; }
    }

    internal sealed class InventorySearchBudget
    {
        internal InventorySearchBudget(int maximumImprovementRounds = 8,
            int maximumCandidateEvaluations = 5000,
            int maximumElapsedMilliseconds = 200, bool useElapsedTimeLimit = true)
        {
            MaximumImprovementRounds = Math.Max(1,
                maximumImprovementRounds);
            MaximumCandidateEvaluations = Math.Max(1,
                maximumCandidateEvaluations);
            MaximumElapsedMilliseconds = Math.Max(0,
                maximumElapsedMilliseconds);
            UseElapsedTimeLimit = useElapsedTimeLimit;
        }

        internal int MaximumImprovementRounds { get; }
        internal int MaximumCandidateEvaluations { get; }
        internal int MaximumElapsedMilliseconds { get; }
        internal bool UseElapsedTimeLimit { get; }

        internal static InventorySearchBudget ForEffort(
            InventorySearchEffort effort)
        {
            return effort switch
            {
                InventorySearchEffort.Fast =>
                    new InventorySearchBudget(4, 1500, 50),
                InventorySearchEffort.Thorough =>
                    new InventorySearchBudget(16, 15000, 1500),
                _ => new InventorySearchBudget()
            };
        }
    }

    internal sealed class InventoryOptimizationScore :
        IComparable<InventoryOptimizationScore>
    {
        // Identifies the preference comparator, independently of game mechanisms.
        internal const string ObjectiveId = "hard-feasible-intent-first-v2";
        internal InventoryOptimizationScore(int priorityTargetsSatisfied,
            int priorityTargetCompletionPoints, int avoidedTargetsActive,
            int presetTargetsSatisfied,
            int presetTargetCompletionPoints,
            int sourceEnabledArtifactsDeactivated, int enabledArtifactCount,
            int comboBreakpointValue,
            int cappedEffectiveArtifactLevelTotal,
            int excessArtifactLevelTotal, int movedItemCount,
            int rotatedTabletCount,
            int[] orderedPriorityCompletionPoints = null,
            int positionEffectRegressions = 0, int automaticLevelRegressions = 0,
            int hardConstraintViolations = 0, int hardConstraintCompletionPoints = 0)
        {
            HardConstraintViolations = hardConstraintViolations;
            HardConstraintCompletionPoints = hardConstraintCompletionPoints;
            PriorityTargetsSatisfied = priorityTargetsSatisfied;
            PriorityTargetCompletionPoints = priorityTargetCompletionPoints;
            AvoidedTargetsActive = avoidedTargetsActive;
            PresetTargetsSatisfied = presetTargetsSatisfied;
            PresetTargetCompletionPoints =
                presetTargetCompletionPoints;
            SourceEnabledArtifactsDeactivated =
                sourceEnabledArtifactsDeactivated;
            EnabledArtifactCount = enabledArtifactCount;
            ComboBreakpointValue = comboBreakpointValue;
            CappedEffectiveArtifactLevelTotal =
                cappedEffectiveArtifactLevelTotal;
            ExcessArtifactLevelTotal = excessArtifactLevelTotal;
            MovedItemCount = movedItemCount;
            RotatedTabletCount = rotatedTabletCount;
            PositionEffectRegressions = positionEffectRegressions;
            AutomaticLevelRegressions = automaticLevelRegressions;
            OrderedPriorityCompletionPoints = Array.AsReadOnly(
                orderedPriorityCompletionPoints == null
                    ? Array.Empty<int>()
                    : (int[])orderedPriorityCompletionPoints.Clone());
        }

        internal int PriorityTargetsSatisfied { get; }
        internal int HardConstraintViolations { get; }
        internal int HardConstraintCompletionPoints { get; }
        internal bool HardConstraintsSatisfied => HardConstraintViolations == 0;
        internal int PriorityTargetCompletionPoints { get; }
        internal int AvoidedTargetsActive { get; }
        internal int PresetTargetsSatisfied { get; }
        internal int PresetTargetCompletionPoints { get; }
        internal int SourceEnabledArtifactsDeactivated { get; }
        internal int EnabledArtifactCount { get; }
        internal int ComboBreakpointValue { get; }
        internal int CappedEffectiveArtifactLevelTotal { get; }
        internal int ExcessArtifactLevelTotal { get; }
        internal int MovedItemCount { get; }
        internal int RotatedTabletCount { get; }
        internal int PositionEffectRegressions { get; }
        internal int AutomaticLevelRegressions { get; }
        internal IReadOnlyList<int> OrderedPriorityCompletionPoints { get; }
        internal bool HasDefaultProtectionTradeoff => PositionEffectRegressions > 0 || AutomaticLevelRegressions > 0;

        public int CompareTo(InventoryOptimizationScore other)
        {
            if (other == null)
            {
                return 1;
            }

            int comparison = CompareUserRequirementsTo(other);
            if (comparison != 0) return comparison;
            // Defaults break ties in user requirements; they are not feasibility constraints.
            comparison = other.HasDefaultProtectionTradeoff.CompareTo(HasDefaultProtectionTradeoff);
            if (comparison != 0) return comparison;
            if (HasDefaultProtectionTradeoff)
            {
                // Neither loss counts nor unrelated aggregate gains price an attribute exchange.
                // Use a deterministic minimal-change fallback, not pairwise Pareto ties: the
                // comparator must remain transitive for exact and bounded search alike.
                return CompareChangesTo(other);
            }
            comparison = PresetTargetsSatisfied.CompareTo(
                other.PresetTargetsSatisfied);
            if (comparison != 0) return comparison;
            comparison = PresetTargetCompletionPoints.CompareTo(
                other.PresetTargetCompletionPoints);
            if (comparison != 0) return comparison;
            comparison = other.SourceEnabledArtifactsDeactivated.CompareTo(
                SourceEnabledArtifactsDeactivated);
            if (comparison != 0) return comparison;
            comparison = EnabledArtifactCount.CompareTo(
                other.EnabledArtifactCount);
            if (comparison != 0) return comparison;
            comparison = ComboBreakpointValue.CompareTo(
                other.ComboBreakpointValue);
            if (comparison != 0) return comparison;
            comparison = CappedEffectiveArtifactLevelTotal.CompareTo(
                other.CappedEffectiveArtifactLevelTotal);
            if (comparison != 0) return comparison;
            comparison = other.ExcessArtifactLevelTotal.CompareTo(
                ExcessArtifactLevelTotal);
            return comparison != 0 ? comparison : CompareChangesTo(other);
        }

        internal int CompareUserRequirementsTo(InventoryOptimizationScore other)
        {
            // Infeasible candidates are useful only as search intermediates. No
            // amount of soft benefit may outrank a feasible candidate.
            int comparison = other.HardConstraintViolations.CompareTo(HardConstraintViolations);
            if (comparison != 0) return comparison;
            comparison = HardConstraintCompletionPoints.CompareTo(other.HardConstraintCompletionPoints);
            if (comparison != 0) return comparison;
            comparison = other.AvoidedTargetsActive.CompareTo(AvoidedTargetsActive);
            if (comparison != 0) return comparison;
            int orderedCount = Math.Max(OrderedPriorityCompletionPoints.Count,
                other.OrderedPriorityCompletionPoints.Count);
            for (int index = 0; index < orderedCount; index++)
            {
                int current = index < OrderedPriorityCompletionPoints.Count
                    ? OrderedPriorityCompletionPoints[index]
                    : 0;
                int candidate = index <
                        other.OrderedPriorityCompletionPoints.Count
                    ? other.OrderedPriorityCompletionPoints[index]
                    : 0;
                comparison = current.CompareTo(candidate);
                if (comparison != 0) return comparison;
            }
            comparison = PriorityTargetsSatisfied.CompareTo(
                other.PriorityTargetsSatisfied);
            if (comparison != 0) return comparison;
            comparison = PriorityTargetCompletionPoints.CompareTo(
                other.PriorityTargetCompletionPoints);
            return comparison;
        }

        private int CompareChangesTo(InventoryOptimizationScore other)
        {
            int comparison = other.MovedItemCount.CompareTo(MovedItemCount);
            return comparison != 0
                ? comparison
                : other.RotatedTabletCount.CompareTo(RotatedTabletCount);
        }
    }

    internal enum InventoryOptimizationTargetKind
    {
        Artifact,
        ComboCategory
    }

    internal enum InventoryTargetReachability
    {
        SelectedLayoutReachesCondition,
        ObservedReachable,
        ProvenUnreachable,
        Unresolved
    }

    internal sealed class InventoryTargetSearchEvidence
    {
        internal InventoryTargetSearchEvidence(int maximumObservedValue,
            int maximumObservedCompletionPoints, bool conditionObserved)
        {
            Observe(maximumObservedValue, maximumObservedCompletionPoints,
                conditionObserved);
        }

        internal int MaximumObservedValue { get; private set; }
        internal int MaximumObservedCompletionPoints { get; private set; }
        internal bool ConditionObserved { get; private set; }

        internal void Observe(int value, int completionPoints,
            bool conditionReached)
        {
            MaximumObservedValue = Math.Max(MaximumObservedValue,
                Math.Max(0, value));
            MaximumObservedCompletionPoints = Math.Max(
                MaximumObservedCompletionPoints,
                Math.Max(0, completionPoints));
            ConditionObserved |= conditionReached;
        }
    }

    internal sealed class InventoryOptimizationTargetEvaluation
    {
        internal InventoryOptimizationTargetEvaluation(string target,
            InventoryOptimizationTargetKind kind,
            InventoryPreferenceLevel level, InventoryPreferenceSource source,
            int requiredValue, int beforeValue, int afterValue,
            bool beforeConditionReached, bool afterConditionReached,
            int beforeCompletionPoints, int afterCompletionPoints,
            int maximumObservedValue,
            int maximumObservedCompletionPoints,
            InventoryTargetReachability reachability)
        {
            Target = target ?? string.Empty;
            Kind = kind;
            Level = level;
            Source = source;
            RequiredValue = requiredValue;
            BeforeValue = beforeValue;
            AfterValue = afterValue;
            BeforeConditionReached = beforeConditionReached;
            AfterConditionReached = afterConditionReached;
            BeforeCompletionPoints = beforeCompletionPoints;
            AfterCompletionPoints = afterCompletionPoints;
            MaximumObservedValue = maximumObservedValue;
            MaximumObservedCompletionPoints =
                maximumObservedCompletionPoints;
            Reachability = reachability;
        }

        internal string Target { get; }
        internal InventoryOptimizationTargetKind Kind { get; }
        internal InventoryPreferenceLevel Level { get; }
        internal InventoryPreferenceSource Source { get; }
        internal int RequiredValue { get; }
        internal int BeforeValue { get; }
        internal int AfterValue { get; }
        internal bool BeforeConditionReached { get; }
        internal bool AfterConditionReached { get; }
        internal int BeforeCompletionPoints { get; }
        internal int AfterCompletionPoints { get; }
        internal int MaximumObservedValue { get; }
        internal int MaximumObservedCompletionPoints { get; }
        internal InventoryTargetReachability Reachability { get; }
    }

    internal enum InventoryHardConstraintStatus
    {
        NotEvaluated,
        Feasible,
        ProvenInfeasible,
        NotFound
    }

    internal sealed class InventoryOptimizationProposal
    {
        internal InventoryOptimizationProposal(bool succeeded,
            InventoryLayoutProjection layout, InventoryOptimizationScore current,
            InventoryOptimizationScore best, int candidateEvaluations,
            string[] issues,
            ResolvedInventoryOptimizationPolicy policy = null,
            InventoryOptimizationTargetEvaluation[] targetEvaluations = null,
            InventorySearchTerminationReason terminationReason =
                InventorySearchTerminationReason.InputRejected,
            long elapsedMilliseconds = 0,
            InventoryOptimizationSearchMethod searchMethod =
                InventoryOptimizationSearchMethod.Neighborhood,
            bool optimalityProven = false,
            int duplicateLayoutsSkipped = 0,
            InventoryOptimizationOutcome outcome = null,
            InventorySearchStageStatistics[] searchStages = null)
        {
            bool rejectedByHard = succeeded && best != null && !best.HardConstraintsSatisfied;
            HardConstraintStatus = rejectedByHard
                ? optimalityProven ? InventoryHardConstraintStatus.ProvenInfeasible : InventoryHardConstraintStatus.NotFound
                : succeeded && best != null ? InventoryHardConstraintStatus.Feasible : InventoryHardConstraintStatus.NotEvaluated;
            if (rejectedByHard)
            {
                succeeded = false;
                layout = null;
                issues = new[] { HardConstraintStatus.ToString() };
                optimalityProven = false;
                outcome = null;
            }
            Succeeded = succeeded;
            Layout = layout;
            CurrentScore = current;
            BestScore = best;
            CandidateEvaluations = candidateEvaluations;
            Issues = Array.AsReadOnly(issues ?? Array.Empty<string>());
            Policy = policy;
            TargetEvaluations = Array.AsReadOnly(targetEvaluations ??
                Array.Empty<InventoryOptimizationTargetEvaluation>());
            TerminationReason = terminationReason;
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds);
            SearchMethod = searchMethod;
            OptimalityProven = optimalityProven;
            DuplicateLayoutsSkipped = Math.Max(0, duplicateLayoutsSkipped);
            Outcome = outcome;
            SearchStages = Array.AsReadOnly(searchStages ?? Array.Empty<InventorySearchStageStatistics>());
        }

        internal bool Succeeded { get; }
        internal InventoryHardConstraintStatus HardConstraintStatus { get; }
        internal InventoryLayoutProjection Layout { get; }
        internal InventoryOptimizationScore CurrentScore { get; }
        internal InventoryOptimizationScore BestScore { get; }
        internal int CandidateEvaluations { get; }
        internal bool Improved => Succeeded && BestScore != null &&
            CurrentScore != null && BestScore.CompareTo(CurrentScore) > 0;
        internal IReadOnlyList<string> Issues { get; }
        internal ResolvedInventoryOptimizationPolicy Policy { get; }
        internal IReadOnlyList<InventoryOptimizationTargetEvaluation>
            TargetEvaluations
        { get; }
        internal InventorySearchTerminationReason TerminationReason { get; }
        internal long ElapsedMilliseconds { get; }
        internal InventoryOptimizationSearchMethod SearchMethod { get; }
        internal bool OptimalityProven { get; }
        internal int DuplicateLayoutsSkipped { get; }
        internal InventoryOptimizationOutcome Outcome { get; }
        internal IReadOnlyList<InventorySearchStageStatistics> SearchStages { get; }
    }
}
