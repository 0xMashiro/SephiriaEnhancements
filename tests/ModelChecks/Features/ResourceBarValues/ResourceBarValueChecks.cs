using SephiriaEnhancements.ResourceBarValues;

namespace SephiriaEnhancements.ModelChecks.Features.ResourceBarValues;

internal static class ResourceBarValueChecks
{
    internal static void Run()
    {
        Equal("1 / 200", ResourceBarValueFormatter.Ratio(0.2f, 200f), "Living fractional HP stays visible");
        Equal("0 / 200", ResourceBarValueFormatter.Ratio(-3f, 200f), "Lethal damage cannot display negative HP");
        Equal("240 / 200", ResourceBarValueFormatter.Ratio(240f, 200f), "Temporary HP must not be discarded");
        Equal("", ResourceBarValueFormatter.Ratio(10f, 0f), "Uninitialized maximum hides HP");
        Equal("120 / 200 (+50)", ResourceBarValueFormatter.HealthWithShield(120f, 200f, 50f),
            "Shield is separate from HP and its maximum, matching the native player HUD");
        Equal("120 / 200", ResourceBarValueFormatter.HealthWithShield(120f, 200f, 0f), "Expired shields disappear");
        Equal("30 / 60 (Reserve 20)", ResourceBarValueFormatter.Mana(30, 80, 20, "Reserve"),
            "Reserved mana reduces available capacity without being deducted from current MP twice");
        Equal("30 / 60 (占用 20)", ResourceBarValueFormatter.Mana(30, 80, 20, "占用"), "Native reserve term is used");
        Equal("30 / 80", ResourceBarValueFormatter.Mana(30, 80, 0, "Reserve"), "Reservation removal restores capacity");
        Equal("0 / 0 (Reserve 80)", ResourceBarValueFormatter.Mana(0, 80, 80, "Reserve"), "Fully reserved MP");
        Equal("0 / 0", ResourceBarValueFormatter.Mana(0, 0, 0, "Reserve"), "Zero MP capacity is a number, not a blank label");
        Equal("405 / 405", ResourceBarValueFormatter.RemainingLivesHealth(135, 135, 2, 2), "Library initial life segments");
        Equal("270 / 405", ResourceBarValueFormatter.RemainingLivesHealth(0, 135, 2, 2), "Library depleted current segment");
        Equal("270 / 405", ResourceBarValueFormatter.RemainingLivesHealth(135, 135, 1, 2), "Life transition preserves aggregate HP");
        Equal("0 / 405", ResourceBarValueFormatter.RemainingLivesHealth(0, 135, 0, 2), "Final life has no hidden remaining HP");
        Equal("1,000,001 / 2,000,000", ResourceBarValueFormatter.Ratio(1000000.5f, 2000000f), "Large values stay exact");
        Console.WriteLine("ResourceBarValues: health, shields, reservations and native life-segment semantics passed");
    }

    private static void Equal(string expected, string actual, string message)
    {
        if (actual != expected) throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
    }
}
