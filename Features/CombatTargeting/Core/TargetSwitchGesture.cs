namespace SephiriaEnhancements.CombatTargeting
{
    internal enum TargetSwitchCommand { None, Switch, Unlock }

    internal sealed class TargetSwitchGesture
    {
        internal const float HoldSeconds = 0.45f;
        private bool pending;
        private float pressedAt;

        internal bool IsPending => pending;

        internal TargetSwitchCommand Update(bool pressed, bool held, bool released, float now)
        {
            if (pressed)
            {
                pending = true;
                pressedAt = now;
            }
            if (!pending) return TargetSwitchCommand.None;
            if ((held || released) && now - pressedAt >= HoldSeconds)
            {
                pending = false;
                return TargetSwitchCommand.Unlock;
            }
            if (released)
            {
                pending = false;
                return TargetSwitchCommand.Switch;
            }
            if (!held) pending = false;
            return TargetSwitchCommand.None;
        }

        internal void Clear() => pending = false;
    }
}
