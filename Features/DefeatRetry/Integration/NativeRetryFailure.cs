using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryFailure
    {
        private static RetryRecoveryFailure pending;
        private static bool shown;
        internal static bool IsPending => pending != RetryRecoveryFailure.None;

        internal static void Show(RetryRecoveryFailure reason) => pending = reason;
        internal static void Clear() { pending = RetryRecoveryFailure.None; shown = false; }

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
            if (pending == RetryRecoveryFailure.None || shown || UIManager.Instance == null) return;
            UI_MessageBoxHolder holder = UIManager.Instance.GetElement<UI_MessageBoxHolder>();
            UI_PausePanel pause = UIManager.Instance.GetElement<UI_PausePanel>();
            if (holder == null || pause == null) return;
            string key = pending == RetryRecoveryFailure.TimedOut ? RetryRecoveryLocalization.TimedOut :
                pending == RetryRecoveryFailure.Disconnected ? RetryRecoveryLocalization.Disconnected : RetryRecoveryLocalization.Failed;
            // A failed restore may still have the native loading overlay active.
            ScreenFader.Instance?.ClearLoadingScreen();
            shown = true;
            Runtime.FeatureFailure.ConsumeNotice();
            holder.OpenYes(ModLocalization.Get(key), () => pause.Open());
        }
    }
}
