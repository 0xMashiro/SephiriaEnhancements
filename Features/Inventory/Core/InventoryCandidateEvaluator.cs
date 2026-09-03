#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.Inventory
{
    internal sealed class InventoryCandidateEvaluator
    {
        private readonly int batchSize;
        private readonly IInventoryCandidateBatchEvaluator batchEvaluator;
        private readonly InventorySnapshot snapshot;
        private readonly InventorySearchBudget budget;
        private readonly Stopwatch elapsed;
        private readonly CancellationToken cancellationToken;
        private readonly Worker[] workers;
        private readonly HashSet<InventoryLayoutProjection> visited = new(LayoutComparer.Instance);
        private readonly Dictionary<InventoryLayoutProjection, int> indexes = new(LayoutComparer.Instance);
        private readonly List<(InventoryLayoutProjection Layout, int Index)> pending;
        private readonly List<InventoryLayoutProjection> unique;
        private readonly InventoryOptimizationScore[] scores;

        internal InventoryCandidateEvaluator(InventorySnapshot snapshot, ResolvedInventoryOptimizationPolicy policy,
            InventorySearchBudget budget, Stopwatch elapsed, CancellationToken cancellationToken,
            InventoryLayoutProjection current, IInventoryCandidateBatchEvaluator batchEvaluator = null)
        {
            this.snapshot = snapshot;
            this.budget = budget;
            this.elapsed = elapsed;
            this.cancellationToken = cancellationToken;
            this.batchEvaluator = batchEvaluator;
            batchSize = batchEvaluator?.BatchSize ?? 256;
            pending = new List<(InventoryLayoutProjection Layout, int Index)>(batchSize);
            unique = new List<InventoryLayoutProjection>(batchSize);
            scores = new InventoryOptimizationScore[batchSize];
            int count = batchEvaluator != null ? 1 : Math.Min(4, Math.Max(1, Environment.ProcessorCount - 1));
            workers = Enumerable.Range(0, count).Select(_ => new Worker(snapshot, policy)).ToArray();
            visited.Add(current);
        }

        internal int CandidateEvaluations { get; private set; } = 1;
        internal int DuplicateLayoutsSkipped { get; private set; }
        internal List<InventorySearchStageStatistics> SearchStages { get; } = new();
        internal Dictionary<string, InventoryTargetSearchEvidence> TargetEvidence { get; } = new(StringComparer.Ordinal);
        internal InventorySettlementProjectionWorkspace EvaluationWorkspace => workers[0].Workspace;

        internal bool Search(InventorySearchStage stage, int round,
            IEnumerable<InventoryLayoutProjection> candidates, bool firstImprovement,
            ref InventoryLayoutProjection bestLayout, ref InventoryOptimizationScore bestScore,
            out InventorySearchTerminationReason reason)
        {
            var statistics = new InventorySearchStageStatistics(stage, round);
            SearchStages.Add(statistics);
            int evaluationsBefore = CandidateEvaluations, duplicatesBefore = DuplicateLayoutsSkipped;
            long startedAt = elapsed.ElapsedMilliseconds;
            try
            {
                reason = InventorySearchTerminationReason.ImprovementRoundLimit;
                InventoryOptimizationScore startingScore = bestScore;
                using var iterator = candidates.GetEnumerator();
                while (true)
                {
                    pending.Clear();
                    unique.Clear();
                    indexes.Clear();
                    // A submitted batch is indivisible: every evaluated candidate counts and
                    // participates in selection before a first-improvement branch resumes.
                    int capacity = Math.Min(batchSize,
                        Math.Max(1, budget.MaximumCandidateEvaluations - CandidateEvaluations));
                    while (pending.Count < capacity)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!iterator.MoveNext()) break;
                        if (CandidateEvaluations >= budget.MaximumCandidateEvaluations)
                        {
                            reason = InventorySearchTerminationReason.CandidateEvaluationLimit;
                            return false;
                        }
                        if (elapsed.ElapsedMilliseconds >= budget.MaximumElapsedMilliseconds)
                        {
                            reason = InventorySearchTerminationReason.ElapsedTimeLimit;
                            return false;
                        }
                        InventoryLayoutProjection layout = iterator.Current;
                        int index = -1;
                        if (!visited.Contains(layout) && !indexes.TryGetValue(layout, out index))
                        {
                            index = unique.Count;
                            indexes.Add(layout, index);
                            unique.Add(layout);
                        }
                        pending.Add((layout, index));
                    }
                    if (pending.Count == 0) return true;
                    if (elapsed.ElapsedMilliseconds >= budget.MaximumElapsedMilliseconds)
                    {
                        reason = InventorySearchTerminationReason.ElapsedTimeLimit;
                        return false;
                    }

                    EvaluateBatch();
                    // Complete an admitted batch, then check the soft deadline before admitting another.
                    foreach (var (layout, index) in pending)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!visited.Add(layout))
                        {
                            DuplicateLayoutsSkipped++;
                            continue;
                        }
                        InventoryOptimizationScore score = scores[index];
                        if (score == null) continue;
                        int comparison = score.CompareTo(bestScore);
                        if (comparison > 0 || comparison == 0 && layout.CompareStableTo(bestLayout) < 0)
                        {
                            if (comparison > 0)
                            {
                                statistics.Improvements++;
                                statistics.LastImprovementCandidate = CandidateEvaluations - unique.Count + index + 1;
                            }
                            bestLayout = layout;
                            bestScore = score;
                        }
                    }
                    if (firstImprovement && bestScore.CompareTo(startingScore) > 0) return true;
                }
            }
            finally
            {
                statistics.CandidateEvaluations = CandidateEvaluations - evaluationsBefore;
                statistics.DuplicateLayoutsSkipped = DuplicateLayoutsSkipped - duplicatesBefore;
                statistics.ElapsedMilliseconds = elapsed.ElapsedMilliseconds - startedAt;
            }
        }

        private void EvaluateBatch()
        {
            if (unique.Count == 0) return;
            if (batchEvaluator != null)
            {
                batchEvaluator.Evaluate(unique, scores, TargetEvidence, cancellationToken);
                CandidateEvaluations += unique.Count;
                return;
            }
            int count = Math.Min(workers.Length, unique.Count);
            void EvaluateRange(int workerIndex)
            {
                Worker worker = workers[workerIndex];
                for (int index = unique.Count * workerIndex / count;
                    index < unique.Count * (workerIndex + 1) / count; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ProjectedInventorySettlement settlement = InventorySettlementProjector.EvaluateForScoring(
                        snapshot, unique[index], worker.Workspace);
                    scores[index] = settlement.Succeeded ? worker.Scorer.Score(unique[index], settlement) : null;
                    worker.Scorer.ObserveTargets(settlement, worker.Evidence);
                }
            }
            if (count == 1) EvaluateRange(0);
            else Parallel.For(0, count, new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = count
            }, EvaluateRange);
            CandidateEvaluations += unique.Count;
        }

        internal void CompleteEvidence()
        {
            // Maxima and observed reachability combine independently of worker execution order.
            foreach (Worker worker in workers)
                foreach (var entry in worker.Evidence)
                {
                    if (TargetEvidence.TryGetValue(entry.Key, out InventoryTargetSearchEvidence previous))
                        previous.Observe(entry.Value.MaximumObservedValue,
                            entry.Value.MaximumObservedCompletionPoints, entry.Value.ConditionObserved);
                    else TargetEvidence.Add(entry.Key, entry.Value);
                }
        }

        private sealed class Worker
        {
            internal readonly InventoryOptimizationScorer Scorer;
            internal readonly InventorySettlementProjectionWorkspace Workspace;
            internal readonly Dictionary<string, InventoryTargetSearchEvidence> Evidence = new(StringComparer.Ordinal);
            internal Worker(InventorySnapshot snapshot, ResolvedInventoryOptimizationPolicy policy)
            {
                Scorer = new InventoryOptimizationScorer(snapshot, policy);
                Workspace = new InventorySettlementProjectionWorkspace(snapshot);
            }
        }

        private sealed class LayoutComparer : IEqualityComparer<InventoryLayoutProjection>
        {
            internal static readonly LayoutComparer Instance = new();
            public bool Equals(InventoryLayoutProjection first, InventoryLayoutProjection second) =>
                ReferenceEquals(first, second) || first != null && first.ContentEquals(second);
            public int GetHashCode(InventoryLayoutProjection layout)
            {
                unchecked
                {
                    int hash = 17 * 31 + layout.ItemCount;
                    for (int index = 0; index < layout.ItemCount; index++)
                    {
                        hash = hash * 31 + layout.GetCell(index);
                        hash = hash * 31 + layout.GetRotation(index);
                    }
                    return hash;
                }
            }
        }
    }
}
