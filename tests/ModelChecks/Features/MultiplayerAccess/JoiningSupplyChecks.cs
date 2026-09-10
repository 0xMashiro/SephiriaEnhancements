using SephiriaEnhancements.MultiplayerAccess;

namespace SephiriaEnhancements.ModelChecks.Features.MultiplayerAccess;

internal static class JoiningSupplyChecks
{
    internal static void Run()
    {
        Require(JoiningSupplyLedger.Mean(new[] { 10, 30 }) == 20, "Mean level must not average cumulative experience.");
        Require(JoiningSupplyLedger.Mean(new[] { int.MaxValue, int.MaxValue }) == int.MaxValue, "Mean must not overflow.");
        Require(JoiningSupplyLedger.Mean(new[] { 1, 2 }) == 1, "Fractional levels round down.");
        var ledger = new JoiningSupplyLedger { RecordedFromExplorationStart = true };
        void Record(string id, string floor, int? slot, JoiningSupplyKind kind = JoiningSupplyKind.ArtifactReward) =>
            ledger.Record(new JoiningSupplyOpportunity { Id = id, Floor = floor, Kind = kind }, slot);
        Record("boss", "floor1", 0);
        Record("boss", "floor1", 1);
        Record("boss", "floor1", 0);
        Record("side-route", "floor2", 1);
        Record("miracle", "floor1", null, JoiningSupplyKind.MiracleChoice);
        Record("native-pending", "floor1", 0);
        Record("native-pending", "floor1", 2);
        Record("still-present", "floor3", null, JoiningSupplyKind.InventorySpace);
        Require(ledger.Opportunities.Count == 5 && ledger.Opportunities[0].Recipients.Count == 2,
            "Per-player spawns must describe one opportunity with distinct recipients.");
        var supplied = ledger.Admit(2, 20, 100, 0, new HashSet<string> { "floor1", "floor3" }, new HashSet<string> { "still-present" });
        Require(supplied.Claims.Select(claim => claim.Opportunity.Id).SequenceEqual(new[] { "boss", "miracle" }),
            "Admission must follow one teammate's route, exclude native pending and present facilities, and preserve order.");
        Require(supplied.Next?.Opportunity.Kind == JoiningSupplyKind.ArtifactReward, "First reward kind is retained.");
        supplied.Next!.Completed = true;
        supplied.Next!.Selection = "saved choice";
        supplied.BasicsComplete = true;
        supplied.MoneyGranted = true;
        for (int reconnect = 0; reconnect < 10; reconnect++)
        {
            var returned = ledger.Admit(2, 30, 9999, 1, new HashSet<string> { "floor2" }, new HashSet<string>());
            Require(ReferenceEquals(returned, supplied) && returned.TargetLevel == 20 && returned.TargetMoney == 100 &&
                returned.BasicsComplete && returned.MoneyGranted && returned.MoneyGrant == 100 && returned.Remaining == 1 && returned.Next?.Selection == "saved choice",
                "Reconnect must preserve the original entitlement and claimed progress, never recompute or append rewards.");
        }
        Record("later", "floor1", 0);
        Require(supplied.Claims.Count == 2, "Rewards earned after admission do not inflate the frozen catch-up allowance.");
        var nextArrival = ledger.Admit(3, 20, 100, 2, new HashSet<string> { "floor3" }, new HashSet<string> { "still-present" });
        Require(nextArrival.Claims.Select(claim => claim.Opportunity.Id).SequenceEqual(new[] { "boss", "miracle", "native-pending" }),
            "A teammate who joined late must carry their historical allowance when original teammates leave.");
        var oldRun = new JoiningSupplyLedger();
        var money = new JoiningSupplyLedger().Admit(0, 2, 100, 1, new HashSet<string>(), new HashSet<string>(), 40);
        Require(money.MoneyGrant == 60, "The initial money difference is frozen, not recalculated after spending during growth.");
        var unavailable = oldRun.Admit(1, 2, 0, 0, new HashSet<string>(), new HashSet<string>());
        Require(!unavailable.RecordedFromExplorationStart && unavailable.Remaining == 0, "Missing history must not invent historical rewards.");
        oldRun.Record(new JoiningSupplyOpportunity { Id = "observed-later" }, 0);
        var laterArrival = oldRun.Admit(2, 2, 0, 0, new HashSet<string>(), new HashSet<string>());
        Require(!laterArrival.RecordedFromExplorationStart && laterArrival.Remaining == 1,
            "An incomplete earlier history must not prevent recording later observed rewards.");
        var nativePending = new JoiningSupplyClaim { Opportunity = ledger.Opportunities[0] };
        Require(nativePending.AcceptNativeDelivery(false) && nativePending.Completed && nativePending.DeliveredByGame,
            "A delayed native reward takes ownership of the pending allowance.");
        Require(nativePending.AcceptNativeDelivery(false), "Native-owned restoration remains under the game's save lifecycle.");
        var modCompleted = new JoiningSupplyClaim { Opportunity = ledger.Opportunities[0], Completed = true };
        Require(!modCompleted.AcceptNativeDelivery(false), "Native delayed delivery cannot duplicate a Mod-completed reward.");
        var modActive = new JoiningSupplyClaim { Opportunity = ledger.Opportunities[0] };
        Require(!modActive.AcceptNativeDelivery(true) && !modActive.Completed,
            "A native duplicate cannot replace a currently selectable supply or complete it prematurely.");
        var jsonOptions = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
        string checkpoint = System.Text.Json.JsonSerializer.Serialize(ledger, jsonOptions);
        var restored = System.Text.Json.JsonSerializer.Deserialize<JoiningSupplyLedger>(checkpoint, jsonOptions)!;
        var restoredPlayer = restored.Admit(2, 30, 9999, 0, new HashSet<string>(), new HashSet<string>());
        Require(restoredPlayer.MoneyGranted && restoredPlayer.MoneyGrant == 100 && restoredPlayer.Remaining == 1 &&
            restoredPlayer.Next!.Selection == "saved choice", "Serialized fields retain allowance and choice progress.");
        restoredPlayer.Next!.Completed = true;
        Require(supplied.Remaining == 1 && restoredPlayer.Remaining == 0,
            "Restoring a checkpoint must not mutate another run's in-memory claims.");
        Console.WriteLine("Joining supply entitlement, delivery ownership, serialization and repeated reconnect checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
