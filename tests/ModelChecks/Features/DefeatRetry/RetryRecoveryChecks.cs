using SephiriaEnhancements.DefeatRetry;

namespace SephiriaEnhancements.ModelChecks.Features.DefeatRetry;

internal static class RetryRecoveryChecks
{
    internal static void Run()
    {
        var host = new object();
        var guest = new object();
        var state = new RetryRecovery<object>();
        void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        foreach (object failureOwner in new[] { host, guest })
        {
            state.Begin(1, new[] { host, guest }, 90);
            Require(!state.Report(0, host, true) && !state.Report(1, new object(), true), "stale identity/id ignored");
            state.Report(1, failureOwner == host ? guest : host, true);
            state.Report(1, failureOwner, false);
            Require(state.Status == RetryRecoveryStatus.Failed && state.BlocksBattle, "failure must not open boss gate");
            Require(!state.Report(1, failureOwner, true) && state.BlocksBattle, "late success cannot undo failure");
        }
        state.Begin(2, new[] { host, guest }, 90);
        state.Report(2, host, true);
        Require(state.BlocksBattle && !state.Report(2, host, true), "duplicate host receipt cannot complete team");
        state.Report(2, guest, true);
        Require(state.Status == RetryRecoveryStatus.Completed && !state.BlocksBattle, "all participants required");
        state.Begin(3, new[] { host, guest }, 90);
        state.CheckDeadline(89);
        Require(state.Status == RetryRecoveryStatus.Waiting, "deadline not early");
        state.CheckDeadline(90);
        Require(state.Failure == RetryRecoveryFailure.TimedOut && state.BlocksBattle, "timeout retains failure gate");
        state.Begin(4, new[] { host, guest }, 180);
        state.Fail(RetryRecoveryFailure.Disconnected);
        Require(state.Failure == RetryRecoveryFailure.Disconnected && state.BlocksBattle, "disconnect is a failure");
        state.Cancel();
        Require(state.Status == RetryRecoveryStatus.Cancelled && !state.BlocksBattle && !state.Report(4, guest, true), "explicit lifecycle cancellation clears ownership");
        state.Begin(5, Array.Empty<object>(), 200);
        Require(state.Status == RetryRecoveryStatus.Failed && state.BlocksBattle, "empty participant set is not success");
        Console.WriteLine("RetryRecovery: host/client failure, all-player completion, stale/duplicate receipts, timeout, disconnect and cancellation passed");
    }
}
