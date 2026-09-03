using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using SephiriaEnhancements.Diagnostics;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;

internal static class InventoryReproductionReplay
{
    // Only the recording contract's model types and fields are accepted.
    internal static T Read<T>(JsonElement json) => (T)Read(typeof(T), json)!;

    private static object? Read(Type type, JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Null) return null;
        Type? nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null) return Read(nullable, json);
        if (type.IsEnum) return Enum.Parse(type, json.GetString()!);
        if (type == typeof(double) && json.ValueKind == JsonValueKind.String)
            return double.Parse(json.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (type == typeof(float) && json.ValueKind == JsonValueKind.String)
            return float.Parse(json.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return JsonSerializer.Deserialize(json.GetRawText(), type);
        if (type.IsArray)
        {
            Type element = type.GetElementType()!;
            Array values = Array.CreateInstance(element, json.GetArrayLength());
            int index = 0;
            foreach (JsonElement value in json.EnumerateArray()) values.SetValue(Read(element, value), index++);
            return values;
        }
        ConstructorInfo constructor = InventoryReproductionJson.InputConstructor(type);
        string[] names = type == typeof(InventoryLayoutProjection)
            ? new[] { "CellsByItem", "RotationsByItem" }
            : InventoryReproductionJson.InputProperties(type).Select(property => property.Name).ToArray();
        if (json.EnumerateObject().Count() != names.Length)
            throw new InvalidDataException("Unexpected inventory input fields: " + type.Name);
        return constructor.Invoke(constructor.GetParameters().Select((parameter, index) =>
            Read(parameter.ParameterType, json.GetProperty(names[index]))).ToArray());
    }

    internal static bool PolicyMatchesRecorded(ResolvedInventoryOptimizationPolicy policy, JsonElement recorded) =>
        JsonNode.DeepEquals(JsonNode.Parse(InventoryReproductionJson.Serialize(InventoryReproductionEvidence.Policy(policy))),
            JsonNode.Parse(recorded.GetRawText()));

    internal static void Run(string[] args)
    {
        if (args.Length < 2) throw new ArgumentException("Usage: --inventory-replay <jsonl> [case-id] [--no-time-limit]");
        string? caseId = args.Skip(2).FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));
        JsonElement? selected = null;
        JsonElement? header = null;
        foreach (string line in File.ReadLines(args[1]))
        {
            using JsonDocument document = JsonDocument.Parse(line);
            JsonElement row = document.RootElement;
            if (!row.TryGetProperty("Event", out JsonElement kind)) continue;
            if (kind.GetString() == "inventory_reproduction_start") header = row.Clone();
            if (kind.GetString() == "inventory_reproduction" &&
                (caseId == null || row.GetProperty("Case").GetProperty("Id").GetString() == caseId)) selected = row.Clone();
        }
        if (header?.GetProperty("SchemaVersion").GetInt32() != InventoryReproductionJson.SchemaVersion)
            throw new InvalidDataException("Unsupported reproduction schema.");
        JsonElement record = selected ?? throw new InvalidDataException("No matching inventory reproduction case.");
        JsonElement input = record.GetProperty("Case");
        JsonElement evidence = record.GetProperty("Evidence");
        var snapshot = Read<InventorySnapshot>(input.GetProperty("Snapshot"));
        var preferences = Read<InventoryOptimizationPreferences>(input.GetProperty("Preferences"));
        var budget = Read<InventorySearchBudget>(input.GetProperty("Budget"));
        if (args.Contains("--no-time-limit")) budget = new InventorySearchBudget(
            budget.MaximumImprovementRounds, budget.MaximumCandidateEvaluations, int.MaxValue);
        var policy = InventoryOptimizationPolicyResolver.Resolve(snapshot, preferences);
        Console.WriteLine("Recorded build: " + header.Value.GetRawText());
        Console.WriteLine("Replay uses the current model sources; wall-clock cutoffs and native application timing are not deterministic.");
        if (!snapshot.SettlementValidation.LayoutProjectionReady)
        {
            Console.WriteLine(InventoryReproductionJson.Serialize(new { Validation = snapshot.SettlementValidation }));
            return;
        }
        try
        {
            InventoryOptimizationProposal result = InventoryOptimizerSelector.Solve(snapshot, policy, budget);
            InventoryLayoutProjection layout = result.Layout;
            ProjectedInventorySettlement projected = InventorySettlementProjector.Evaluate(snapshot, layout);
            JsonElement recordedProposal = evidence.GetProperty("Proposal");
            InventorySettlementDifferentialReport? differential = null;
            // Application diagnosis must use the recorded layout, not a new search result.
            if (evidence.GetProperty("ActualSnapshot").ValueKind != JsonValueKind.Null && recordedProposal.ValueKind != JsonValueKind.Null)
            {
                var actual = Read<InventorySnapshot>(evidence.GetProperty("ActualSnapshot"));
                var recordedLayout = Read<InventoryLayoutProjection>(recordedProposal.GetProperty("Layout"));
                differential = InventorySettlementDifferentialVerifier.Compare(snapshot, recordedLayout,
                    InventorySettlementProjector.Evaluate(snapshot, recordedLayout), actual);
            }
            Console.WriteLine(InventoryReproductionJson.Serialize(new
            {
                CaseId = input.GetProperty("Id").GetString(),
                RecordedReason = record.GetProperty("Reason").GetString(),
                PolicyMatchesRecorded = evidence.GetProperty("Policy").ValueKind == JsonValueKind.Null ? (bool?)null :
                    PolicyMatchesRecorded(policy, evidence.GetProperty("Policy")),
                Result = InventoryReproductionEvidence.Proposal(result),
                RecordedBestScore = recordedProposal.ValueKind == JsonValueKind.Null ? null : recordedProposal.GetProperty("BestScore").GetRawText(),
                RecordedLayout = recordedProposal.ValueKind == JsonValueKind.Null ? null : recordedProposal.GetProperty("Layout").GetRawText(),
                ReplayedComboCounts = projected.ComboCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new { CategoryId = pair.Key, Count = pair.Value }).ToArray(),
                RecordedApplicationDifferential = InventoryReproductionEvidence.Differential(differential)
            }));
        }
        catch (Exception exception)
        {
            Console.WriteLine(InventoryReproductionJson.Serialize(new { ReplayException = exception.GetType().FullName }));
            Environment.ExitCode = 2;
        }
    }
}
