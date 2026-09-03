namespace SephiriaEnhancements.Integration
{
    internal static class ModShortcuts
    {
        internal const string MapName = "SephiriaEnhancements";
        internal const string SwitchLockedTarget = "SwitchLockedTarget";
        internal const string ToggleCurrentFloorMapOverlay =
            "ToggleCurrentFloorMapOverlay";
        internal const string ToggleDamageStatistics =
            "ToggleDamageStatistics";
        // This action starts scoring-based optimization, not generic item arranging.
        internal const string OptimizeInventory = "OptimizeInventory";
        internal const string KeyboardScheme = "Keyboard&Mouse";
        internal const string GamepadScheme = "Gamepad";

        internal static readonly string[] ActionNames =
        {
            SwitchLockedTarget,
            ToggleCurrentFloorMapOverlay,
            ToggleDamageStatistics,
            OptimizeInventory
        };

        internal const string ActionMapJson =
            "{\"maps\":[{\"name\":\"SephiriaEnhancements\",\"id\":\"b8c4ee8a-56d6-4acd-84f0-1a46396f45f7\",\"actions\":[" +
            "{\"name\":\"SwitchLockedTarget\",\"type\":\"Button\",\"id\":\"2f42ed30-0d71-43a1-b530-c4cf3ab2fc70\",\"expectedControlType\":\"Button\",\"processors\":\"\",\"interactions\":\"\"}," +
            "{\"name\":\"ToggleCurrentFloorMapOverlay\",\"type\":\"Button\",\"id\":\"e3c17d5b-7329-48c9-bd82-7679810fba5e\",\"expectedControlType\":\"Button\",\"processors\":\"\",\"interactions\":\"\"}," +
            "{\"name\":\"ToggleDamageStatistics\",\"type\":\"Button\",\"id\":\"4463886d-62fd-434b-b63e-51a9ddd09b59\",\"expectedControlType\":\"Button\",\"processors\":\"\",\"interactions\":\"\"}," +
            "{\"name\":\"OptimizeInventory\",\"type\":\"Button\",\"id\":\"bc18d113-80d2-4bd4-bdee-ddf6b50d3fa7\",\"expectedControlType\":\"Button\",\"processors\":\"\",\"interactions\":\"\"}],\"bindings\":[" +
            "{\"name\":\"\",\"id\":\"48ae6552-5007-43f3-955f-7393f9701dc4\",\"path\":\"<Mouse>/middleButton\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"SwitchLockedTarget\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"9ee0066c-129a-48fd-8c03-c8604cf487cf\",\"path\":\"<Keyboard>/l\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"SwitchLockedTarget\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"44f640e8-3ed3-4994-bb53-69c64b772533\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Gamepad\",\"action\":\"SwitchLockedTarget\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"3f7b4309-38ce-4ed3-83ea-589a1bf1d496\",\"path\":\"<Keyboard>/m\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"ToggleCurrentFloorMapOverlay\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"f9e20248-e2ac-4a65-95a0-ad6a56e56cd4\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"ToggleCurrentFloorMapOverlay\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"04133f05-7aa8-40a7-bb65-37621bdd00e6\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Gamepad\",\"action\":\"ToggleCurrentFloorMapOverlay\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"bc1de948-ef6d-420f-bc51-72494d767771\",\"path\":\"<Keyboard>/f7\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"ToggleDamageStatistics\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"bf1e0bf9-1adc-4722-b7b1-714a842b8ca4\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"ToggleDamageStatistics\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"735b4a89-af78-43b1-a5df-9ed1698a87b6\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Gamepad\",\"action\":\"ToggleDamageStatistics\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"508c62f2-2b21-4699-8713-7690e1261aa5\",\"path\":\"<Keyboard>/f8\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"OptimizeInventory\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"7ed721d2-bdf0-467a-9922-7d37926eed5a\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Keyboard&Mouse\",\"action\":\"OptimizeInventory\",\"isComposite\":false,\"isPartOfComposite\":false}," +
            "{\"name\":\"\",\"id\":\"2f3c14d6-52aa-4f30-aa54-57d0ebf85c36\",\"path\":\"\",\"interactions\":\"\",\"processors\":\"\",\"groups\":\"Gamepad\",\"action\":\"OptimizeInventory\",\"isComposite\":false,\"isPartOfComposite\":false}]}]}";
    }
}
