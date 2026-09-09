using System.Collections.Generic;

namespace SephiriaEnhancements.DefeatRetry
{
    internal enum RetryRecoveryStatus { Idle, Waiting, Completed, Failed, Cancelled }
    internal enum RetryRecoveryFailure : byte { None, RestoreFailed, TimedOut, Disconnected }
    internal enum RetryTransition : byte { CaptureBoss, RetryBoss, RetryFloor, Cancel, RecoveryFailed, RecoveryCompleted }

    // One recovery owns its participants until all succeed or the recovery fails.
    internal sealed class RetryRecovery<T> where T : class
    {
        private readonly HashSet<T> waiting = new HashSet<T>();
        internal RetryRecoveryStatus Status { get; private set; }
        internal RetryRecoveryFailure Failure { get; private set; }
        internal long Id { get; private set; }
        internal double Deadline { get; private set; }
        internal bool BlocksBattle => Status == RetryRecoveryStatus.Waiting || Status == RetryRecoveryStatus.Failed;

        internal void Begin(long id, IEnumerable<T> participants, double deadline)
        {
            waiting.Clear();
            foreach (T participant in participants) waiting.Add(participant);
            Id = id;
            Deadline = deadline;
            Failure = RetryRecoveryFailure.None;
            Status = waiting.Count > 0 ? RetryRecoveryStatus.Waiting : RetryRecoveryStatus.Failed;
            if (waiting.Count == 0) Failure = RetryRecoveryFailure.RestoreFailed;
        }

        internal bool IsWaiting(T participant) => Status == RetryRecoveryStatus.Waiting && waiting.Contains(participant);

        internal bool Report(long id, T participant, bool success)
        {
            if (id != Id || !IsWaiting(participant)) return false;
            if (!success) Fail(RetryRecoveryFailure.RestoreFailed);
            else
            {
                waiting.Remove(participant);
                if (waiting.Count == 0) Status = RetryRecoveryStatus.Completed;
            }
            return true;
        }

        internal void CheckDeadline(double now)
        {
            if (Status == RetryRecoveryStatus.Waiting && now >= Deadline) Fail(RetryRecoveryFailure.TimedOut);
        }

        internal void Fail(RetryRecoveryFailure reason)
        {
            if (Status != RetryRecoveryStatus.Waiting) return;
            Failure = reason;
            Status = RetryRecoveryStatus.Failed;
        }

        internal void Cancel()
        {
            waiting.Clear();
            Status = RetryRecoveryStatus.Cancelled;
            Failure = RetryRecoveryFailure.None;
        }
    }
}
