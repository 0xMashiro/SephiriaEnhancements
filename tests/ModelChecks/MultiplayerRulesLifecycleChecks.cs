using SephiriaEnhancements.MultiplayerRules;

internal static class MultiplayerRulesLifecycleChecks
{
    internal static string Run()
    {
        foreach (var entry in new[]
        {
            (server: true, started: false, begin: true),
            (server: true, started: true, begin: false),
            (server: false, started: false, begin: false),
            (server: false, started: true, begin: false)
        })
        {
            if (MultiplayerRulesLifecyclePolicy.ShouldBeginNewExploration(
                    entry.server, entry.started) != entry.begin)
                throw new InvalidOperationException(
                    "only the server's first stage entry may freeze new rules");
        }

        var session = new MultiplayerRulesSession();
        AssertEditing(session, expectedHostEditing: true);
        var preferred = new PreferredMultiplayerRules(
            MultiplayerRulesPreset.Optimized, MultiplayerRuleSnapshot.Original(),
            EnemyHealthModifierCombination.ParticipantRuleOnly);
        ActiveExplorationMultiplayerRules saved =
            session.BeginNewExploration(preferred);
        AssertEditing(session, expectedHostEditing: false);

        // Opening a room or moving between floors must retain the frozen rules.
        if (MultiplayerRulesLifecyclePolicy.ShouldBeginNewExploration(
                serverActive: true, explorationStarted: true) ||
            !session.TryGetActive(out var unchanged) ||
            !ReferenceEquals(saved, unchanged))
            throw new InvalidOperationException(
                "an active exploration must retain its rules across stage entries");

        // Returning to town clears the exploration even if the server stays up.
        session.EndExploration();
        AssertEditing(session, expectedHostEditing: true);
        session.EndExploration();
        AssertEditing(session, expectedHostEditing: true);

        var changedPreference = new PreferredMultiplayerRules(
            MultiplayerRulesPreset.Original, MultiplayerRuleSnapshot.Original(),
            EnemyHealthModifierCombination.ParticipantRuleOnly);
        session.ResumeExploration(saved);
        AssertEditing(session, expectedHostEditing: false);
        if (!session.TryGetActive(out var restored) ||
            restored.Preset != MultiplayerRulesPreset.Optimized)
            throw new InvalidOperationException(
                "resuming an exploration must preserve its saved rules");

        session.EndExploration();
        AssertEditing(session, expectedHostEditing: true);
        if (session.BeginNewExploration(changedPreference).Preset !=
            MultiplayerRulesPreset.Original)
            throw new InvalidOperationException(
                "the next departure must use preferences edited in town");
        AssertEditing(session, expectedHostEditing: false);
        return "town editing, host departure, stage continuity, saved exploration and return to town passed";
    }

    private static void AssertEditing(MultiplayerRulesSession session,
        bool expectedHostEditing)
    {
        bool explorationActive = session.TryGetActive(out _);
        if (MultiplayerRulesLifecyclePolicy.CanEditHostPreferences(
                localPeerIsHost: true, explorationActive) != expectedHostEditing ||
            MultiplayerRulesLifecyclePolicy.CanEditHostPreferences(
                localPeerIsHost: false, explorationActive))
            throw new InvalidOperationException(
                "host settings must be editable in town and read-only for guests or active explorations");
    }
}
