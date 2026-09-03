using System.Text.Json;
using System.Text.Json.Nodes;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.ModelChecks.Features.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;

internal static class InventoryReproductionChecks
{
    internal static void Run()
    {
        foreach (InventorySnapshot snapshot in new[]
        {
            InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 5, 1, 0, 0, 0 }, new[] { 0 }),
            InventoryNeighborhoodFixture.BothSidesArtifacts(),
            InventoryNeighborhoodFixture.StoneTabletMoveAndRotation()
        }) VerifyRoundTrip(snapshot);
        VerifySpecialEffects();
        VerifyPolicyComparison();
        VerifyClassification();
        VerifyCaptureModes();
        VerifyWriter();
        Console.WriteLine("Inventory reproduction: input round-trip, replay, classification, separate writer and I/O failure passed");
    }

    private static void VerifyRoundTrip(InventorySnapshot snapshot)
    {
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            new[] { new ArtifactOptimizationPreference(snapshot.Items[0].InstanceId, snapshot.Items[0].EntityId,
                InventoryPreferenceLevel.Priority, 5, 0) },
            new[] { new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 10),
                new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Avoid, 10) });
        var budget = new InventorySearchBudget(4, 250, int.MaxValue);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var result = InventoryOptimizerSelector.Solve(snapshot, policy, budget);
        var input = new InventoryReproductionCase(snapshot, preferences, policy, budget);
        using JsonDocument document = JsonDocument.Parse(InventoryReproductionJson.Serialize(input.Record(
            InventoryReproductionCase.Classify(result), result)));
        JsonElement saved = document.RootElement.GetProperty("Case");
        var restored = InventoryReproductionReplay.Read<InventorySnapshot>(saved.GetProperty("Snapshot"));
        var restoredPreferences = InventoryReproductionReplay.Read<InventoryOptimizationPreferences>(saved.GetProperty("Preferences"));
        var restoredBudget = InventoryReproductionReplay.Read<InventorySearchBudget>(saved.GetProperty("Budget"));
        Require(InventoryReproductionJson.Serialize(snapshot) == InventoryReproductionJson.Serialize(restored), "snapshot round-trip");
        Require(InventoryReproductionJson.Serialize(preferences) == InventoryReproductionJson.Serialize(restoredPreferences), "preference order and thresholds");
        var replayed = InventoryOptimizerSelector.Solve(restored,
            InventoryOptimizationPolicyResolver.Resolve(restored, restoredPreferences), restoredBudget);
        Require(result.BestScore.CompareTo(replayed.BestScore) == 0 &&
            result.CandidateEvaluations == replayed.CandidateEvaluations &&
            result.Layout.CopyCells().SequenceEqual(replayed.Layout.CopyCells()) &&
            result.Layout.CopyRotations().SequenceEqual(replayed.Layout.CopyRotations()), "deterministic candidate-budget replay");
        JsonElement evidence = document.RootElement.GetProperty("Evidence");
        Require(!saved.GetProperty("Snapshot").TryGetProperty("SettlementValidation", out _) &&
            !saved.GetProperty("Snapshot").TryGetProperty("Height", out _) &&
            !evidence.GetProperty("Proposal").TryGetProperty("Policy", out _) &&
            evidence.GetProperty("SourceValidation").GetProperty("Capabilities").ValueKind == JsonValueKind.String,
            "constructor inputs are separate from explicit evidence and policy is recorded once");
        var savedLayout = InventoryReproductionReplay.Read<InventoryLayoutProjection>(evidence.GetProperty("Proposal").GetProperty("Layout"));
        Require(savedLayout.CopyCells().SequenceEqual(result.Layout.CopyCells()), "recorded application layout");
        JsonObject unexpected = JsonNode.Parse(saved.GetProperty("Snapshot").GetRawText())!.AsObject();
        unexpected["UnregisteredInput"] = 1;
        using JsonDocument invalid = JsonDocument.Parse(unexpected.ToJsonString());
        try
        {
            InventoryReproductionReplay.Read<InventorySnapshot>(invalid.RootElement);
            throw new InvalidOperationException("Extra input field accepted");
        }
        catch (InvalidDataException) { }
    }

    private static void VerifySpecialEffects()
    {
        InventorySnapshot basis = InventoryNeighborhoodFixture.StoneTabletMoveAndRotation();
        var key = basis.Items[0].ItemKey;
        var effects = new InventoryPositionEffectsSnapshot(Enum.GetValues<InventoryPositionEffectKind>()
            .Select(kind => new InventoryPositionEffectRule(key, kind, new[] { 0d, 2.5d }, new[] { 1d },
                new[] { new InventoryOffsetSnapshot(1, 0) }, 3, new[] { "FIRE" }, "ICE", true, 2)).ToArray(),
            new[] { new InventoryPositionTargetTraits(key, true, true, true, 1) },
            Array.Empty<InventoryPositionEffectValue>(), new[] { "synthetic\n\"\\\u0001😀" });
        var snapshot = new InventorySnapshot(basis.Width, basis.Storage, basis.Cells.ToArray(), basis.Items.ToArray(),
            nativePreset: new NativePresetSnapshot(0, true, "private preset label", 1, "costume", new[] { 1000 }, new[] { "FIRE" }),
            comboCategories: new[] { new ComboCategorySnapshot("FIRE", 10, 10, 9, 1, 0, new[] { 5 }, new[] { 10 }, true) },
            evaluationOrder: new InventoryEvaluationOrderSnapshot(7, new[] { key }, new[] { key },
                new[] { new UniqueEffectRegistrationSnapshot(key.NativeInstanceId, key.EntityId, true) }), positionEffects: effects);
        string json = InventoryReproductionJson.Serialize(snapshot);
        using JsonDocument document = JsonDocument.Parse(json);
        var restored = InventoryReproductionReplay.Read<InventorySnapshot>(document.RootElement);
        Require(json == InventoryReproductionJson.Serialize(restored), "special effects, native observations, evaluation order and combo inputs");
        Require(!json.Contains("private preset label"), "preset label omission");
    }

    private static void VerifyPolicyComparison()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 5, 5, 0 }, new[] { 0, 1 });
        var preferences = new InventoryOptimizationPreferences(InventorySearchEffort.Balanced, true,
            snapshot.Items.Select((item, index) => new ArtifactOptimizationPreference(item.InstanceId, item.EntityId,
                InventoryPreferenceLevel.Priority, 3 + index, index)).ToArray(),
            new[] { new ComboOptimizationPreference("FIRE", InventoryPreferenceLevel.Priority, 10),
                new ComboOptimizationPreference("ICE", InventoryPreferenceLevel.Avoid, 10) });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        JsonNode original = JsonNode.Parse(InventoryReproductionJson.Serialize(InventoryReproductionEvidence.Policy(policy)))!;
        var reordered = new ResolvedInventoryOptimizationPolicy(policy.SearchEffort, policy.AllowStoneTabletRotation,
            policy.ArtifactInstanceRules.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value),
            policy.ArtifactEntityRules.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value),
            policy.ComboRules.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value));
        using JsonDocument formatted = JsonDocument.Parse(ReverseObjectProperties(original).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Require(InventoryReproductionReplay.PolicyMatchesRecorded(reordered, formatted.RootElement),
            "whitespace, object property order and dictionary insertion order do not change policy");
        foreach (Action<JsonNode> change in new Action<JsonNode>[]
        {
            node => node["ArtifactInstanceRules"]![0]!["MinimumEffectiveLevel"] = 4,
            node => node["ArtifactInstanceRules"]![0]!["PriorityOrder"] = 1,
            node => node["ComboRules"]![0]!["TargetCount"] = 9,
            node => node["ComboRules"]![1]!["TargetCount"] = 9,
            node => node["AllowStoneTabletRotation"] = false
        })
        {
            JsonNode changed = original.DeepClone();
            change(changed);
            using JsonDocument document = JsonDocument.Parse(changed.ToJsonString());
            Require(!InventoryReproductionReplay.PolicyMatchesRecorded(policy, document.RootElement),
                "minimums, maximums, priority order and rotation rules remain significant");
        }
    }

    private static JsonNode ReverseObjectProperties(JsonNode node) => node switch
    {
        JsonObject value => new JsonObject(value.Reverse().Select(pair =>
            new KeyValuePair<string, JsonNode?>(pair.Key, pair.Value == null ? null : ReverseObjectProperties(pair.Value)))),
        JsonArray value => new JsonArray(value.Select(item => item == null ? null : ReverseObjectProperties(item)).ToArray()),
        _ => node.DeepClone()
    };

    private static void VerifyClassification()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 5, 1, 0, 0, 0 }, new[] { 0 });
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, InventoryOptimizationPreferences.Default);
        var bounded = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(1, 2, int.MaxValue));
        Require(InventoryReproductionCase.Classify(bounded) == InventoryReproductionReason.BudgetExhausted,
            "normal budget cutoff is distinct from an exception");
        var exact = InventoryOptimizerSelector.Solve(snapshot, policy, new InventorySearchBudget(4, 100, int.MaxValue));
        Require(InventoryReproductionCase.Classify(exact) == InventoryReproductionReason.None, "successful solved case is not an anomaly");
        var input = new InventoryReproductionCase(snapshot, InventoryOptimizationPreferences.Default, policy,
            new InventorySearchBudget());
        try { throw new InvalidOperationException("private exception detail"); }
        catch (InvalidOperationException exception)
        {
            string json = InventoryReproductionJson.Serialize(input.Record(InventoryReproductionReason.SolverException, exception: exception));
            using JsonDocument record = JsonDocument.Parse(json);
            Require(record.RootElement.GetProperty("Reason").GetString() == "SolverException" &&
                record.RootElement.GetProperty("Exception").GetProperty("Frames").GetArrayLength() > 0 &&
                !json.Contains("private exception detail"), "exception type and frames without private message");
        }
    }

    private static void VerifyCaptureModes()
    {
        var snapshot = InventorySnapshotFixture.ArtifactsAtLevels(new[] { 0, 5, 1, 0, 0, 0 }, new[] { 0 });
        var preferences = InventoryOptimizationPreferences.Default;
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        var budget = new InventorySearchBudget(4, 100, int.MaxValue);
        var exact = InventoryOptimizerSelector.Solve(snapshot, policy, budget);
        foreach (bool recordAll in new[] { false, true })
        {
            var input = new InventoryReproductionCase(snapshot, preferences, policy, budget, recordAll);
            Require(input.SearchReason(exact) == (recordAll ? InventoryReproductionReason.SearchCompleted : InventoryReproductionReason.None),
                "successful searches are sampled only when enabled");
            Require(input.ApplicationReason(true, true) == (recordAll ? InventoryReproductionReason.ApplicationCompleted : InventoryReproductionReason.None),
                "successful application checks follow the captured search setting");
            Require(input.ApplicationReason(false, false).HasFlag(InventoryReproductionReason.LayoutMismatch) &&
                input.ApplicationReason(false, false).HasFlag(InventoryReproductionReason.SettlementMismatch),
                "application failures are retained in both modes");
            var bounded = InventoryOptimizer.Solve(snapshot, policy, new InventorySearchBudget(1, 2, int.MaxValue));
            Require(input.SearchReason(bounded).HasFlag(InventoryReproductionReason.BudgetExhausted),
                "sampling does not hide the budget cutoff reason");
        }
        var manual = new InventoryReproductionCase(snapshot, preferences, policy, budget);
        using var document = JsonDocument.Parse(InventoryReproductionJson.Serialize(manual.Record(InventoryReproductionReason.ManualCapture)));
        Require(document.RootElement.GetProperty("Evidence").GetProperty("Proposal").ValueKind == JsonValueKind.Null,
            "manual capture has no invented search result");
        var saved = document.RootElement.GetProperty("Case");
        var restored = InventoryReproductionReplay.Read<InventorySnapshot>(saved.GetProperty("Snapshot"));
        var replayed = InventoryOptimizerSelector.Solve(restored, policy, budget);
        Require(replayed.BestScore.CompareTo(exact.BestScore) == 0, "manual capture can be solved offline");
        string path = Path.GetTempFileName();
        TextWriter output = Console.Out;
        using var replayOutput = new StringWriter();
        try
        {
            File.WriteAllLines(path, new[] {
                InventoryReproductionJson.Serialize(new { Event = "inventory_reproduction_start", SchemaVersion = InventoryReproductionJson.SchemaVersion }),
                document.RootElement.GetRawText() });
            Console.SetOut(replayOutput);
            InventoryReproductionReplay.Run(new[] { "--inventory-replay", path, manual.Id, "--no-time-limit" });
            Require(replayOutput.ToString().Contains("ManualCapture") && replayOutput.ToString().Contains("\"RecordedBestScore\":null"),
                "replay command accepts manual records without a prior solver result");
        }
        finally
        {
            Console.SetOut(output);
            File.Delete(path);
        }
        var texts = new Dictionary<(string Key, string Language), string>();
        InventoryReproductionLocalization.Register((language, key, text) => texts.Add((key, language), text));
        foreach (var entry in texts.Where(entry => entry.Key.Language == "en-US"))
        {
            Require(texts[(entry.Key.Key, "zh-CN")] != entry.Value, "capture group has complete Chinese localization");
            foreach (string language in SephiriaEnhancements.Configuration.LocalizationLanguages.All.Where(language => language != "en-US"))
                Require(texts[(entry.Key.Key, language)] != entry.Value, "capture group is translated for every game language");
        }
    }

    private static void VerifyWriter()
    {
        string directory = Path.Combine(Path.GetTempPath(), "inventory-reproduction-check-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "inventory-reproductions.jsonl");
            string header = InventoryReproductionJson.Serialize(new { Event = "inventory_reproduction_start", SchemaVersion = InventoryReproductionJson.SchemaVersion });
            using (var log = new InventoryReproductionLog(path, header))
            {
                Require(log.Record(new { Event = "test", Value = "after exception" }), "writer accepts a manual capture");
            }
            using (var restarted = new InventoryReproductionLog(path, header))
                restarted.Record(new { Event = "restart" });
            Require(File.ReadAllLines(path).Length == 2 && File.ReadAllText(Path.Combine(directory, "inventory-reproductions.1.jsonl")).Contains("after exception"), "flush and restart retention");
            string blocked = Path.Combine(directory, "blocked");
            File.WriteAllText(blocked, "file, not directory");
            using var failed = new InventoryReproductionLog(Path.Combine(blocked, "log.jsonl"), header);
            failed.Record(new { Event = "failed write" });
            failed.Dispose();
            Require(failed.TakeError() != null, "I/O error visible outside writer thread");
            Require(!failed.Record(new { Event = "after failed writer" }), "closed writer rejects capture instead of reporting success");
            Require(failed.TakeDroppedCount() > 0, "failed writer never blocks gameplay");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Inventory reproduction: " + message);
    }
}
