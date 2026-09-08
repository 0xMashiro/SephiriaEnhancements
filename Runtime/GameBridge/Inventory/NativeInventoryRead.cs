using System;

namespace SephiriaEnhancements.Runtime.GameBridge.Inventory
{
    internal static class NativeInventoryRead
    {
        // A failed required read must reach the capture boundary, never become a valid default.
        internal static T Required<T>(string operation, Func<T> read)
        {
            try { return read(); }
            catch (Exception exception) { throw new ReadFailure(operation, exception); }
        }

        internal static string FailureDetails(Exception exception)
        {
            string operation = exception is ReadFailure failure ? failure.Operation : "snapshot";
            // Operation names are fixed by this adapter. Exception messages can contain game/user data.
            return "operation=" + operation + " exception=" + exception.GetBaseException().GetType().FullName;
        }

        private sealed class ReadFailure : Exception
        {
            internal string Operation { get; }

            internal ReadFailure(string operation, Exception cause)
                : base("Required inventory read failed: " + operation, cause)
            {
                Operation = operation;
            }
        }
    }
}
