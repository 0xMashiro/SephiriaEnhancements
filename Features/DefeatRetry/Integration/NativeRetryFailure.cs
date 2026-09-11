using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryFailure
    {
        private static RetryRecoveryFailure pending;
        private static bool shown;
        private static Mirror.NetworkConnectionToServer connection;
        internal static bool IsPending => pending != RetryRecoveryFailure.None;

        internal static void Show(RetryRecoveryFailure reason)
        {
            pending = reason;
            connection = Mirror.NetworkClient.connection;
        }
        internal static void Clear() { pending = RetryRecoveryFailure.None; shown = false; connection = null; }

        internal static void Tick()
        {
            try { ShowPending(); }
            catch (System.Exception exception)
            {
                Clear();
                Diagnostics.SupportLogger.Failure("retry_failure_notice_unavailable", exception);
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ShowPending()
        {
            if (!ReferenceEquals(connection, Mirror.NetworkClient.connection)) { Clear(); return; }
            if (pending == RetryRecoveryFailure.None || shown || UIManager.Instance == null) return;
            UI_PausePanel pause = UIManager.Instance.GetElement<UI_PausePanel>();
            if (pause == null) return;
            string key = pending == RetryRecoveryFailure.TimedOut ? RetryRecoveryLocalization.TimedOut :
                pending == RetryRecoveryFailure.Disconnected ? RetryRecoveryLocalization.Disconnected : RetryRecoveryLocalization.Failed;
            // A failed restore may still have the native loading overlay active.
            ScreenFader.Instance?.ClearLoadingScreen();
            if (!NativeModNotifications.Confirm(() => ModLocalization.Get(key), () => pause.Open())) return;
            shown = true;
            NativeModNotifications.Important("retry/" + DefeatRetryBridge.LocalRecoveryId,
                () => ModLocalization.Get(key), showShort: false,
                onShown: () => Runtime.FeatureFailure.AcknowledgeNotice(Runtime.FeatureId.DefeatRetry));
        }
    }
}
