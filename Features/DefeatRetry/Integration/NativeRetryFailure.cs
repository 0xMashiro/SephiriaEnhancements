using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.DefeatRetry
{
    internal static class NativeRetryFailure
    {
        private static RetryRecoveryFailure pending;
        private static bool shown;

        internal static void Show(RetryRecoveryFailure reason) => pending = reason;
        internal static void Clear() { pending = RetryRecoveryFailure.None; shown = false; }

        internal static void Tick()
        {
            if (pending == RetryRecoveryFailure.None || shown || UIManager.Instance == null) return;
            UI_MessageBoxHolder holder = UIManager.Instance.GetElement<UI_MessageBoxHolder>();
            UI_PausePanel pause = UIManager.Instance.GetElement<UI_PausePanel>();
            if (holder == null || pause == null) return;
            string key = pending == RetryRecoveryFailure.TimedOut ? RetryRecoveryLocalization.TimedOut :
                pending == RetryRecoveryFailure.Disconnected ? RetryRecoveryLocalization.Disconnected : RetryRecoveryLocalization.Failed;
            // A failed restore may still have the native loading overlay active.
            ScreenFader.Instance?.ClearLoadingScreen();
            holder.OpenYes(ModLocalization.Get(key), () => pause.Open());
            shown = true;
        }
    }
}
