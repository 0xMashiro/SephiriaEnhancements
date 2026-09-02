namespace SephiriaEnhancements.Integration
{
    // Stable integration boundary for Sephiria's UI action map. Add every newly
    // reused game action here and to the narrow feature dependency set below.
    // Always resolve
    // these semantic actions from the live InputActionAsset and use their
    // effective bindings. Scene InputActionReference objects identify consumers,
    // but do not contain the authoritative or complete binding table.
    internal static class NativeUiActions
    {
        private const string MapName = "UI";

        internal static readonly NativeActionId Navigate = Action("Navigate");
        internal static readonly NativeActionId Submit = Action("Submit");
        internal static readonly NativeActionId Cancel = Action("Cancel");
        internal static readonly NativeActionId Point = Action("Point");
        internal static readonly NativeActionId Click = Action("Click");
        internal static readonly NativeActionId ScrollWheel = Action("ScrollWheel");
        internal static readonly NativeActionId MiddleClick = Action("MiddleClick");
        internal static readonly NativeActionId RightClick = Action("RightClick");
        internal static readonly NativeActionId TrackedDevicePosition =
            Action("TrackedDevicePosition");
        internal static readonly NativeActionId TrackedDeviceOrientation =
            Action("TrackedDeviceOrientation");
        internal static readonly NativeActionId Skip = Action("Skip");
        internal static readonly NativeActionId CloseControl =
            Action("CloseControl");

        // Contextual UI commands reused by several panels. Their physical
        // bindings are deliberately not modeled here because the game can change
        // them or the player can rebind them.
        // Secondary command: inventory drop, fine adjustment, filter jump, and
        // tree-shop preview depending on the active UI.
        internal static readonly NativeActionId ThrowItem = Action("ThrowItem");

        // Rotation command: inventory/tablet rotation and reward-die rotation;
        // the item box reuses it as its controller favorite command.
        internal static readonly NativeActionId RotateItem = Action("RotateItem");
        internal static readonly NativeActionId PrevTab = Action("PrevTab");
        internal static readonly NativeActionId NextTab = Action("NextTab");
        internal static readonly NativeActionId PrevTab2 = Action("PrevTab2");
        internal static readonly NativeActionId NextTab2 = Action("NextTab2");
        // Held command for inventory tablet engraving.
        internal static readonly NativeActionId EngraveTablet =
            Action("EngraveTablet");
        internal static readonly NativeActionId RightStickScroll =
            Action("RightStickScroll");
        internal static readonly NativeActionId RebindReject =
            Action("RebindReject");
        internal static readonly NativeActionId ShowDetail = Action("ShowDetail");

        internal static readonly NativeActionId[] All =
        {
            Navigate,
            Submit,
            Cancel,
            Point,
            Click,
            ScrollWheel,
            MiddleClick,
            RightClick,
            TrackedDevicePosition,
            TrackedDeviceOrientation,
            Skip,
            CloseControl,
            ThrowItem,
            RotateItem,
            PrevTab,
            NextTab,
            PrevTab2,
            NextTab2,
            EngraveTablet,
            RightStickScroll,
            RebindReject,
            ShowDetail
        };

        internal static readonly NativeActionId[] RequiredByKeyboardNavigation =
        {
            Navigate,
            Submit,
            ThrowItem,
            RotateItem,
            EngraveTablet
        };

        private static NativeActionId Action(string actionName) =>
            new NativeActionId(MapName, actionName);
    }
}
