namespace SephiriaEnhancements.Combat
{
    internal enum CombatInsightsShortcutAction { None, ToggleStatistics, ToggleDisplay }

    internal sealed class CombatInsightsShortcut
    {
        private const float HoldSeconds = 0.5f;
        private float pressedAt = -1f;
        private bool holdHandled;

        internal CombatInsightsShortcutAction Update(bool allowed, bool pressed,
            bool held, bool released, float now)
        {
            if (!allowed)
            {
                Reset();
                return CombatInsightsShortcutAction.None;
            }
            if (pressed)
            {
                pressedAt = now;
                holdHandled = false;
            }
            if (released)
            {
                CombatInsightsShortcutAction action = pressedAt < 0f || holdHandled
                    ? CombatInsightsShortcutAction.None
                    : now - pressedAt >= HoldSeconds
                        ? CombatInsightsShortcutAction.ToggleDisplay
                        : CombatInsightsShortcutAction.ToggleStatistics;
                Reset();
                return action;
            }
            if (!held)
            {
                Reset();
                return CombatInsightsShortcutAction.None;
            }
            if (!holdHandled && pressedAt >= 0f && now - pressedAt >= HoldSeconds)
            {
                holdHandled = true;
                return CombatInsightsShortcutAction.ToggleDisplay;
            }
            return CombatInsightsShortcutAction.None;
        }

        internal void Reset()
        {
            pressedAt = -1f;
            holdHandled = false;
        }
    }
}
