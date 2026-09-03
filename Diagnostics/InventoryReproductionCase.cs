#nullable disable
using System;
using System.Diagnostics;
using System.Linq;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Diagnostics
{
    [Flags]
    internal enum InventoryReproductionReason
    {
        None = 0,
        InputRejected = 1,
        SolverException = 2,
        GoalsUnmet = 4,
        BudgetExhausted = 8,
        ScoreRegression = 16,
        ApplicationPlanRejected = 32,
        ProjectionRejected = 64,
        ApplicationTimedOut = 128,
        PositionEffectsChanged = 256,
        SettlementMismatch = 512,
        LayoutMismatch = 1024,
        ApplicationException = 2048
    }

    internal sealed class InventoryReproductionCase
    {
        internal InventoryReproductionCase(InventorySnapshot snapshot,
            InventoryOptimizationPreferences preferences, ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget)
        {
            Id = Guid.NewGuid().ToString("N");
            Snapshot = snapshot;
            Preferences = preferences;
            Policy = policy;
            Budget = budget;
        }

        internal string Id { get; }
        internal InventorySnapshot Snapshot { get; }
        internal InventoryOptimizationPreferences Preferences { get; }
        internal ResolvedInventoryOptimizationPolicy Policy { get; }
        internal InventorySearchBudget Budget { get; }

        internal static InventoryReproductionReason Classify(InventoryOptimizationProposal proposal)
        {
            if (!proposal.Succeeded) return InventoryReproductionReason.InputRejected;
            InventoryReproductionReason reason = InventoryReproductionReason.None;
            if (proposal.TargetEvaluations.Any(target => !target.AfterConditionReached))
                reason |= InventoryReproductionReason.GoalsUnmet;
            if (proposal.TerminationReason is InventorySearchTerminationReason.ElapsedTimeLimit or
                InventorySearchTerminationReason.CandidateEvaluationLimit or InventorySearchTerminationReason.ImprovementRoundLimit)
                reason |= InventoryReproductionReason.BudgetExhausted;
            if (proposal.BestScore.CompareTo(proposal.CurrentScore) < 0)
                reason |= InventoryReproductionReason.ScoreRegression;
            return reason;
        }

        internal object Record(InventoryReproductionReason reason, InventoryOptimizationProposal proposal = null,
            InventorySnapshot actual = null, InventorySettlementDifferentialReport differential = null,
            Exception exception = null, int swapsCompleted = 0, int rotationsCompleted = 0) => new
            {
                Event = "inventory_reproduction",
                Utc = DateTime.UtcNow.ToString("O"),
                Reason = reason,
                Case = new { Id, Snapshot, Preferences, Budget },
                Evidence = new
                {
                    Policy = InventoryReproductionEvidence.Policy(Policy),
                    SourceValidation = Snapshot?.SettlementValidation,
                    Proposal = InventoryReproductionEvidence.Proposal(proposal),
                    ActualSnapshot = actual,
                    ActualValidation = actual?.SettlementValidation,
                    Differential = InventoryReproductionEvidence.Differential(differential)
                },
                SwapsCompleted = swapsCompleted,
                RotationsCompleted = rotationsCompleted,
                // Preserve method frames, without exception messages or source file paths.
                Exception = exception == null ? null : new
                {
                    Type = exception.GetType().FullName,
                    Frames = (new StackTrace(exception, false).GetFrames() ?? Array.Empty<StackFrame>())
                        .Select(frame => frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name).ToArray()
                }
            };
    }
}
