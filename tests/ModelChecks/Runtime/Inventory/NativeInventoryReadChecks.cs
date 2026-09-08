using SephiriaEnhancements.Runtime.GameBridge.Inventory;

namespace SephiriaEnhancements.ModelChecks.Runtime.Inventory;

internal static class NativeInventoryReadChecks
{
    internal static void Run()
    {
        Require(NativeInventoryRead.Required("unique_pair_combo", () => 0) == 0,
            "zero is a valid native constant, not a read failure");
        Require(!NativeInventoryRead.Required("arrangement_bonus", () => false),
            "a disabled native mechanic must remain disabled");
        Require(NativeInventoryRead.Required("artifact_neighbors", () => Array.Empty<int>()).Length == 0,
            "a successfully observed empty neighborhood must remain valid");

        foreach (string operation in new[] { "unique_pair_combo", "unlimited_combo", "arrangement_bonus", "artifact_neighbors", "tablet_queries" })
        {
            var cause = new InvalidOperationException("PRIVATE_GAME_DATA\nPRIVATE_PATH");
            int attempts = 0;
            Exception failure = ExpectFailure(() => NativeInventoryRead.Required<int>(operation, () =>
            {
                attempts++;
                throw cause;
            }));
            Require(attempts == 1, "required reads must not retry inside an observation");
            Require(ReferenceEquals(failure.GetBaseException(), cause), "original failure was lost");
            string details = NativeInventoryRead.FailureDetails(failure);
            Require(details == "operation=" + operation + " exception=System.InvalidOperationException",
                "failure details must preserve the operation and exception type without copying messages");
            Require(NativeInventoryRead.Required(operation, () => 7) == 7,
                "a later successful capture must not inherit a previous failure");
        }

        Exception deferred = ExpectFailure(() => NativeInventoryRead.Required("artifact_neighbors", () =>
            new[] { 0, 1 }.Select(value => value == 0 ? value : throw new MissingMethodException("PRIVATE_DETAIL")).ToArray()));
        Require(NativeInventoryRead.FailureDetails(deferred) ==
            "operation=artifact_neighbors exception=System.MissingMethodException",
            "failure during enumeration must not become a partial neighborhood");
        Require(NativeInventoryRead.FailureDetails(new NullReferenceException("PRIVATE_DETAIL")) ==
            "operation=snapshot exception=System.NullReferenceException",
            "unannotated failures must retain their type without inventing a more specific cause");
        Console.WriteLine("NativeInventoryRead: valid zero/false/empty results, required-read failures, deferred enumeration, recovery and safe diagnostics passed");
    }

    private static Exception ExpectFailure(Action read)
    {
        try { read(); }
        catch (Exception exception) { return exception; }
        throw new InvalidOperationException("Required native read silently returned a result after failure");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
