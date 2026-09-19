namespace SephiriaEnhancements.AutoCasting
{
    internal sealed class AutoCastingPreferences
    {
        internal AutoCastingSelection Selection { get; } = new AutoCastingSelection();
        internal bool IsPaused { get; set; }
    }
}
