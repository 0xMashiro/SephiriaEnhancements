#nullable enable

namespace SephiriaEnhancements.AutoCasting.Integration
{
    internal static class NativeAutoCastingBindings
    {
        internal static int SlotFor(string? map, string? action)
        {
            // The native gamepad magic actions have a suffix absent from keyboard actions.
            if (map == "Magic_Keyboard" || map == "Magic_Joystick")
            {
                string suffix = map == "Magic_Joystick" ? "_J" : string.Empty;
                for (int number = 1; number <= 8; number++)
                    if (action == "QuickCast" + number + suffix) return number - 1;
                return -1;
            }
            if (map != "Player") return -1;
            return action == "Fire" ? 8 : action == "SubFire" ? 9 : action == "Reload" ? 10 : -1;
        }
    }
}
