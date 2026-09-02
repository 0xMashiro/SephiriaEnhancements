using System;

namespace SephiriaEnhancements.Inventory
{
    internal static class InventoryOptimizationHudLayout
    {
        internal const int IntentSlotsPerPage = 6;
        internal const int TargetRowsPerPage = 4;
        // Layout uses two design units per native inventory UI unit.
        internal const float NativeUnitScale = 2f;
        internal const float Width = 360f;
        internal const float Height = 496f;
        internal const float PrioritySlotsTop = 128f;
        internal const float AvoidSlotsTop = 216f;
        internal const float SlotSize = 48f;
        internal const float TargetRowsTop = 142f;
        internal const float TargetRowStride = 56f;
        internal const float TargetRowHeight = 52f;
        internal const float BoardPagingTop = 278f;
        internal const float TargetPagingTop = 366f;
        internal const float PagingHeight = 26f;
        internal const float HintTop = 316f;
        internal const float HintHeight = 74f;
        internal const float DetailsTop = 402f;
        internal const float DetailsHeight = 32f;
        internal const float ActionsTop = 444f;
        internal const float ActionsHeight = 36f;

        // Include an insertion slot so a full page can still append a rule.
        internal static int IntentPageCount(int priorityCount, int avoidCount) =>
            Math.Max(priorityCount, avoidCount) / IntentSlotsPerPage + 1;
    }
}
