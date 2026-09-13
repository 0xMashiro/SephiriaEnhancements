using SephiriaEnhancements.CostumeAppearance;

namespace SephiriaEnhancements.ModelChecks.Features.CostumeAppearance;

internal static class AppearancePickerChecks
{
    internal static void Run()
    {
        foreach (int count in new[] { 0, 1, 2, 5, 6, 7, 8, 10, 24, 25, 79, 120 })
            foreach (AppearanceViewMode mode in Enum.GetValues<AppearanceViewMode>())
            {
                var layout = new AppearancePickerLayout(mode, count);
                Require(layout.Height >= AppearancePickerLayout.ViewportHeight, "Short content fills viewport");
                if (count == 0) Require(layout.Reveal(0, 100) == 0, "Empty selection resets scroll");
                for (int i = 0; i < count; i++)
                {
                    Require(layout.X(i) >= 0 && layout.X(i) + layout.CellWidth <= AppearancePickerLayout.ContentWidth,
                        "Every appearance fits the content width");
                    Require(layout.Y(i) >= 0 && layout.Y(i) + layout.CellHeight <= layout.Height,
                        "All appearances, including the last row, fit the content height");
                    foreach (int target in new[] { layout.Up(i), layout.Down(i), layout.Left(i), layout.Right(i) })
                        Require(target >= -1 && target < count, "Navigation never targets an absent appearance");
                    Require(layout.Left(i) / layout.Columns == i / layout.Columns, "Left does not wrap into another row");
                    if (layout.Right(i) >= 0)
                        Require(layout.Right(i) / layout.Columns == i / layout.Columns, "Right does not wrap into another row");
                    if (layout.Down(i) >= 0) Require(layout.Down(i) / layout.Columns == i / layout.Columns + 1,
                        "Down reaches the next row even when its final column is missing");
                    else Require(i / layout.Columns == layout.Rows - 1, "Only the final row exits downward");

                    // Switching modes preserves the selected index and reveals it from either saved scroll position.
                    foreach (float offset in new[] { -20f, 0f, 64f, 6000f })
                    {
                        Visible(layout, i, layout.Reveal(i, offset));
                        var other = new AppearancePickerLayout(mode == AppearanceViewMode.Thumbnails ? AppearanceViewMode.List : AppearanceViewMode.Thumbnails, count);
                        Visible(other, i, other.Reveal(i, offset));
                    }
                    float shown = layout.Reveal(i, 0);
                    Require(layout.Reveal(i, shown) == shown, "Selecting an already visible appearance does not jump");
                }
            }
        var grid = new AppearancePickerLayout(AppearanceViewMode.Thumbnails, 79);
        var list = new AppearancePickerLayout(AppearanceViewMode.List, 79);
        Require(grid.Y(23) + grid.CellHeight == AppearancePickerLayout.ViewportHeight, "24 thumbnails fit without clipping");
        Require(list.Y(9) + list.CellHeight <= AppearancePickerLayout.ViewportHeight, "10 named entries fit without clipping");
        Require(grid.Down(76) == 78 && grid.Down(78) == -1, "Partial last thumbnail row is reachable and escapable");
        Console.WriteLine("Costume appearance: dual-view layout, all-item access, partial rows, navigation and selection reveal passed.");
    }

    private static void Visible(AppearancePickerLayout layout, int index, float offset)
    {
        Require(offset >= 0 && offset <= layout.Height - AppearancePickerLayout.ViewportHeight, "Scroll stays inside bounds");
        Require(layout.Y(index) >= offset && layout.Y(index) + layout.CellHeight <= offset + AppearancePickerLayout.ViewportHeight,
            "Selected appearance remains fully visible after mode changes and keyboard movement");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
