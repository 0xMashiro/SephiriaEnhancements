using System.Runtime.InteropServices;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Inventory.Integration.Gpu;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Features.Inventory;

internal static class InventoryGpuChecks
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    internal static void Run()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !Environment.Is64BitProcess)
            throw new PlatformNotSupportedException("GPU checks require 64-bit Windows and a DirectCompute device.");
        foreach (var unsupported in new[] { InventoryKnownSolutionChecks.CreateCases().First().Snapshot,
            InventorySnapshotFixture.FullWithArtifactAndBlockers(6, 65, 0, 64) })
        {
            try
            {
                _ = new GpuInventorySnapshot(unsupported);
                throw new InvalidOperationException("unsupported GPU mechanics must remain on CPU");
            }
            catch (NotSupportedException) { }
        }
        using var kernel = new DirectComputeKernel(DirectComputeKernel.Compile(InventorySettlementShader.Source));
        var snapshots = new[] {
            InventorySnapshotFixture.RowDependentArtifact(),
            InventorySnapshotFixture.ArtifactsAtLevels(new[] { -1, 0, 2, 9, 4, 1 }, new[] { 0, 1, 2 }),
            InventorySnapshotFixture.DuplicateArtifactsAtLevels(new[] { 0, 1, -1, 5, 2, 0 }, new[] { 0, 1, 2 }),
            InventorySnapshotFixture.FullWithArtifactAndBlockers(6, 32, 0, 31),
            InventorySnapshotFixture.FullWithArtifactAndBlockers(6, 64, 0, 63),
            InventoryNeighborhoodFixture.BothSidesArtifacts(),
            InventoryNeighborhoodFixture.StoneTabletMoveAndRotation()
        };
        int checkedCandidates = 0, searches = 0;
        foreach (var snapshot in snapshots)
            foreach (var preference in Preferences(snapshot))
            {
                var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preference);
                var backend = new GpuInventoryBatchEvaluator(snapshot, policy, kernel);
                var scorer = new InventoryOptimizationScorer(snapshot, policy);
                var workspace = new InventorySettlementProjectionWorkspace(snapshot);
                var layouts = new List<InventoryLayoutProjection> { InventoryLayoutProjection.Current(snapshot) };
                layouts.AddRange(InventoryCandidateNeighborhoods.Simple(snapshot, layouts[0], true).Take(127));
                var random = new Random(104729);
                for (int c = 0; c < 128; c++)
                {
                    int[] cells = Enumerable.Range(0, snapshot.Storage).ToArray(); random.Shuffle(cells);
                    layouts.Add(new InventoryLayoutProjection(cells.Take(snapshot.Items.Count).ToArray(),
                        snapshot.Items.Select(item => item.StoneTablet == null ? 0 : random.Next(4)).ToArray()));
                }
                var expectedEvidence = new Dictionary<string, InventoryTargetSearchEvidence>();
                var actualEvidence = new Dictionary<string, InventoryTargetSearchEvidence>();
                var scores = new InventoryOptimizationScore[layouts.Count];
                backend.Evaluate(layouts, scores, actualEvidence, default);
                for (int c = 0; c < layouts.Count; c++)
                {
                    var settlement = InventorySettlementProjector.EvaluateForScoring(snapshot, layouts[c], workspace);
                    var expected = settlement.Succeeded ? scorer.Score(layouts[c], settlement) : null;
                    scorer.ObserveTargets(settlement, expectedEvidence);
                    if (Encode(expected) != Encode(scores[c])) throw new InvalidOperationException("GPU candidate score differs from CPU");
                }
                if (Encode(expectedEvidence.OrderBy(p => p.Key)) != Encode(actualEvidence.OrderBy(p => p.Key)))
                    throw new InvalidOperationException("GPU target evidence differs from CPU");
                checkedCandidates += layouts.Count;
                foreach (int limit in new[] { 1, 2, 255, 256, 257, 5000 })
                {
                    int before = backend.GpuCandidates;
                    var budget = new InventorySearchBudget(8, limit, int.MaxValue);
                    var expected = InventoryOptimizer.Solve(snapshot, policy, budget);
                    var actual = InventoryOptimizer.Solve(snapshot, policy, budget, batchEvaluator: backend);
                    if (Fingerprint(expected) != Fingerprint(actual) || actual.CandidateEvaluations != 1 + backend.GpuCandidates - before)
                        throw new InvalidOperationException("GPU full search or physical candidate budget differs from CPU");
                    searches++;
                }
            }
        Console.WriteLine($"InventoryGpu: {checkedCandidates} candidate scores/evidence and {searches} full searches matched on {kernel.AdapterName}");
    }

    private static IEnumerable<InventoryOptimizationPreferences> Preferences(InventorySnapshot snapshot)
    {
        yield return InventoryOptimizationPreferences.Default;
        foreach (var level in new[] { InventoryPreferenceLevel.Priority, InventoryPreferenceLevel.Avoid })
            foreach (bool instance in new[] { true, false })
                foreach (var strength in new[] { InventoryConstraintStrength.Soft, InventoryConstraintStrength.Hard })
                    yield return new InventoryOptimizationPreferences(InventorySearchEffort.Thorough, true,
                        snapshot.Items.Where(i => i.Artifact != null).GroupBy(i => instance ? i.InstanceId : i.EntityId)
                            .Select((g, index) => new ArtifactOptimizationPreference(instance ? g.First().InstanceId : -1,
                                g.First().EntityId, level, index == 0 ? int.MaxValue : 2, intentSlotIndex: index, strength: strength)).ToArray(),
                        snapshot.ComboCategories.Select(c => new ComboOptimizationPreference(c.CategoryId, level, 2, strength: strength)).ToArray());
    }

    private static string Encode(object? value) => JsonSerializer.Serialize(value, JsonOptions);
    private static JsonSerializerOptions CreateJsonOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Kind != JsonTypeInfoKind.Object || info.Type.Namespace?.StartsWith("SephiriaEnhancements") != true) return;
            foreach (var property in info.Type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (property.GetIndexParameters().Length != 0 || info.Properties.Any(p => p.Name == property.Name)) continue;
                var entry = info.CreateJsonPropertyInfo(property.PropertyType, property.Name);
                entry.Get = property.GetValue;
                info.Properties.Add(entry);
            }
        });
        return new JsonSerializerOptions { TypeInfoResolver = resolver };
    }
    private static string Fingerprint(InventoryOptimizationProposal p) => Encode(new
    {
        p.Succeeded,
        Cells = p.Layout?.CopyCells(),
        Rotations = p.Layout?.CopyRotations(),
        p.CurrentScore,
        p.BestScore,
        p.CandidateEvaluations,
        p.DuplicateLayoutsSkipped,
        p.TargetEvaluations,
        p.TerminationReason,
        p.Outcome
    });
}
