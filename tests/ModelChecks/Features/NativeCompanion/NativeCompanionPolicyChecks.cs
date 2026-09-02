using SephiriaEnhancements.NativeCompanion;

namespace SephiriaEnhancements.ModelChecks.Features.NativeCompanion;

internal static class NativeCompanionPolicyChecks
{
    internal static void Run()
    {
        NativeCompanionPresence Companion(NativeCompanionMode mode,
            NativeCompanionSessionKind sessionKind,
            int humans, bool present = false, bool inBattle = false, bool server = true,
            bool alive = true)
        {
            return NativeCompanionPolicy.Evaluate(true, mode, server, true, true, server, alive,
                sessionKind, humans, present, inBattle);
        }

        if (NativeCompanionPolicy.ClassifySession(true, false, true, 1) !=
                NativeCompanionSessionKind.OfflineSolo ||
            NativeCompanionPolicy.ClassifySession(true, true, true, 1) !=
                NativeCompanionSessionKind.OnlineHost ||
            NativeCompanionPolicy.ClassifySession(true, true, false, 2) !=
                NativeCompanionSessionKind.OnlineClient ||
            NativeCompanionPolicy.ClassifySession(false, false, true, 2) !=
                NativeCompanionSessionKind.OnlineHost ||
            NativeCompanionPolicy.ClassifySession(false, false, true, 1) !=
                NativeCompanionSessionKind.Unknown ||
            NativeCompanionPolicy.ClassifySession(true, false, true, 0) !=
                NativeCompanionSessionKind.Unknown)
            throw new InvalidOperationException("native companion session classification failed");

        if (Companion(NativeCompanionMode.SoloOnly,
                NativeCompanionSessionKind.OfflineSolo, 1) != NativeCompanionPresence.Present ||
            Companion(NativeCompanionMode.SoloOnly,
                NativeCompanionSessionKind.OnlineHost, 1) != NativeCompanionPresence.Absent ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.OnlineHost, 1) != NativeCompanionPresence.Present ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.OnlineHost, 2) != NativeCompanionPresence.Absent ||
            Companion(NativeCompanionMode.AlwaysHost,
                NativeCompanionSessionKind.OnlineHost, 4) != NativeCompanionPresence.Present ||
            Companion(NativeCompanionMode.AlwaysHost,
                NativeCompanionSessionKind.OnlineClient, 2) != NativeCompanionPresence.Absent ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.Unknown, 0) != NativeCompanionPresence.Hold ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.OnlineHost, 2,
                present: true, inBattle: true) != NativeCompanionPresence.Hold ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.OfflineSolo, 1,
                server: false) != NativeCompanionPresence.Absent ||
            Companion(NativeCompanionMode.SmartFill,
                NativeCompanionSessionKind.OfflineSolo, 1,
                alive: false) != NativeCompanionPresence.Absent)
            throw new InvalidOperationException("native companion solo/online presence policy failed");
        Console.WriteLine("NativeCompanionPolicy: solo, smart-fill, mid-run human handoff, " +
            "host and retirement checks passed");
    }
}
