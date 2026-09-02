using SephiriaEnhancements.Core;

namespace SephiriaEnhancements.ModelChecks.Features.CombatInsights;

internal static class EncounterReportSnapshotChecks
{
    internal static void Run()
    {
        var reportPlayers = new[]
        {
            new EncounterReportPlayerSnapshot(1, "Local\n<size=99>Hero", true, 600f),
            new EncounterReportPlayerSnapshot(2, "Teammate", false, 400f)
        };
        var ordinaryReport = new EncounterReportSnapshot(
            EncounterReportKind.Ordinary, reportPlayers, 10f,
            normalDefeated: 4, minibossDefeated: 1, bossDefeated: 0,
            localFinalBlows: 2,
            new[]
            {
                new EncounterReportDamageTypeSnapshot(
                    EncounterDamageType.Fire, 700f),
                new EncounterReportDamageTypeSnapshot(
                    EncounterDamageType.Ice, 300f)
            });
        reportPlayers[0] = new EncounterReportPlayerSnapshot(9, "Changed", false, 1f);
        if (ordinaryReport.Players.Count != 2 ||
            ordinaryReport.Players[0].Key != 1 ||
            ordinaryReport.TotalDamage != 1000f ||
            ordinaryReport.DamageTypes.Count != 2 ||
            ordinaryReport.DamageTypes[0].Type != EncounterDamageType.Fire ||
            ordinaryReport.DefeatedCount != 5 ||
            EncounterReportPresentationPolicy.DisplaySeconds(ordinaryReport) != 6f ||
            CombatInsightsText.SingleLinePlayerName("  A\r\n\tB  ") != "A B" ||
            CombatInsightsText.SingleLinePlayerName("\0") != "Player")
            throw new InvalidOperationException(
                "encounter report snapshot, duration or player-name policy failed");
        var bossReport = new EncounterReportSnapshot(EncounterReportKind.Boss,
            reportPlayers, 2f, 0, 0, 1, 0,
            Array.Empty<EncounterReportDamageTypeSnapshot>());
        if (EncounterReportPresentationPolicy.DisplaySeconds(bossReport) != 8f)
            throw new InvalidOperationException(
                "boss encounter report must retain the detailed display duration");
        Console.WriteLine("EncounterReportSnapshot: frozen rows, duration and safe names passed");
    }
}
