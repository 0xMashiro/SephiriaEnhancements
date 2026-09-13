using System;

namespace SephiriaEnhancements.CostumeAppearance
{
    internal enum AppearanceViewMode { Thumbnails, List }

    // Content coordinates use a top-left origin, in the native UI canvas units.
    internal readonly struct AppearancePickerLayout
    {
        internal const int ContentWidth = 302, ViewportHeight = 188, Gap = 4;
        internal readonly int Count;
        internal readonly AppearanceViewMode Mode;
        internal AppearancePickerLayout(AppearanceViewMode mode, int count) { Mode = mode; Count = count; }
        internal int Columns => Mode == AppearanceViewMode.Thumbnails ? 6 : 2;
        internal int CellWidth => Mode == AppearanceViewMode.Thumbnails ? 47 : 149;
        internal int CellHeight => Mode == AppearanceViewMode.Thumbnails ? 44 : 34;
        internal int Rows => (Count + Columns - 1) / Columns;
        internal int Height => Math.Max(ViewportHeight, Rows * (CellHeight + Gap) - Gap);
        internal int X(int index) => index % Columns * (CellWidth + Gap);
        internal int Y(int index) => index / Columns * (CellHeight + Gap);
        internal int Up(int index) => index < Columns ? -1 : index - Columns;
        internal int Down(int index) => index / Columns == Rows - 1 ? -1 : Math.Min(index + Columns, Count - 1);
        internal int Left(int index) => index % Columns == 0 ? index : index - 1;
        internal int Right(int index) => index % Columns == Columns - 1 || index == Count - 1 ? -1 : index + 1;

        internal float Reveal(int index, float offset)
        {
            if (Count == 0) return 0;
            int top = Y(index), bottom = top + CellHeight;
            if (top < offset) offset = top;
            else if (bottom > offset + ViewportHeight) offset = bottom - ViewportHeight;
            return Math.Max(0, Math.Min(offset, Height - ViewportHeight));
        }
    }
}
