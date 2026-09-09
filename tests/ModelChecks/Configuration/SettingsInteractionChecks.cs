using SephiriaEnhancements.Configuration;

namespace SephiriaEnhancements.ModelChecks.Configuration;

internal static class SettingsInteractionChecks
{
    internal static void Run()
    {
        Assert(SettingInteractionKind.Master, false, true, true, false, false, SettingLockReason.None);
        Assert(SettingInteractionKind.Master, true, true, true, true, false, SettingLockReason.Exploration);
        Assert(SettingInteractionKind.Master, true, false, true, false, false, SettingLockReason.Connected);
        Assert(SettingInteractionKind.Master, false, false, false, false, true, SettingLockReason.ReconnectPending);
        Assert(SettingInteractionKind.Master, true, false, false, false, false, SettingLockReason.Connecting);
        Assert(SettingInteractionKind.HostRule, true, false, true, false, false, SettingLockReason.HostOnly);
        Assert(SettingInteractionKind.Admission, true, true, true, false, false, SettingLockReason.None);
        Assert(SettingInteractionKind.Reconnect, true, false, true, false, false, SettingLockReason.Connected);
        Assert(SettingInteractionKind.Companion, true, false, true, true, false, SettingLockReason.HostOnly);
        Assert(SettingInteractionKind.Companion, true, true, true, true, false, SettingLockReason.None);
        foreach (var kind in new[] { SettingInteractionKind.Admission, SettingInteractionKind.Reconnect })
        {
            if (SettingsInteractionPolicy.Resolve(kind, false, false, false, false, false, true, true, false) != SettingLockReason.Extension ||
                SettingsInteractionPolicy.Resolve(kind, false, false, false, false, false, true, false, false) != SettingLockReason.Unavailable)
                throw new InvalidOperationException("Extension ownership and unavailable integration must have distinct reasons.");
        }
        if (SettingsInteractionPolicy.Resolve(SettingInteractionKind.HostRule, true, true, true, false, false, false, false, true) != SettingLockReason.ModDisabled ||
            SettingsInteractionPolicy.Resolve(SettingInteractionKind.Master, false, false, false, false, false, false, false, true) != SettingLockReason.None)
            throw new InvalidOperationException("Disabled Mod must block host rules but permit enabling in the menu.");
        Console.WriteLine("Settings interaction: town, exploration, host/client, reconnect, unavailable and extension ownership passed");
    }

    private static void Assert(SettingInteractionKind kind, bool client, bool server, bool ready,
        bool exploring, bool pending, SettingLockReason expected)
    {
        var actual = SettingsInteractionPolicy.Resolve(kind, client, server, ready, exploring, pending, true, false, true);
        if (actual != expected) throw new InvalidOperationException($"{kind}: expected {expected}, got {actual}");
    }
}
