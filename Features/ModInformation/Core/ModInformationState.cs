using System;

namespace SephiriaEnhancements.ModInformation
{
    // Owned by this game process, independent of saves, worlds, floors and network identities.
    internal sealed class ModInformationState
    {
        private bool enteredGameplay;
        private bool automaticCheckStarted;
        private string notifiedVersion = "";
        internal ModUpdateResult Result { get; set; } = new ModUpdateResult(ModUpdateStatus.NotChecked);

        internal DateTime? LastCheckedUtc { get; set; }

        internal bool EnterGameplay(bool showWelcome)
        {
            if (enteredGameplay) return false;
            enteredGameplay = true;
            return showWelcome;
        }

        internal bool BeginAutomaticCheck(bool enabled)
        {
            if (!enabled || !enteredGameplay || automaticCheckStarted) return false;
            automaticCheckStarted = true;
            return Result.Status == ModUpdateStatus.NotChecked;
        }

        internal bool TakeUpdateNotice(bool enabled)
        {
            if (!enabled || Result.Status != ModUpdateStatus.UpdateAvailable ||
                notifiedVersion == Result.Version) return false;
            notifiedVersion = Result.Version;
            return true;
        }
    }
}
