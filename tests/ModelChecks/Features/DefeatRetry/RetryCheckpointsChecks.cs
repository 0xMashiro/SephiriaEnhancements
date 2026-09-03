using SephiriaEnhancements.DefeatRetry;

namespace SephiriaEnhancements.ModelChecks.Features.DefeatRetry;

internal static class RetryCheckpointsChecks
{
    internal static void Run()
    {
        var checkpoints = new RetryCheckpoints<object>();
        var entry = new object();
        var bossStart = new object();
        var secondPhase = new object();
        if (!checkpoints.EnterFloor("floor-a", entry) ||
            !checkpoints.BeginBoss("floor-a", bossStart) ||
            checkpoints.BeginBoss("floor-a", secondPhase) ||
            checkpoints.EnterFloor("floor-a", secondPhase) ||
            checkpoints.Get(RetryCheckpointKind.FloorEntry) != entry ||
            checkpoints.Get(RetryCheckpointKind.BossEncounter) != bossStart)
        {
            throw new InvalidOperationException("later phases and same-floor saves must not overwrite either checkpoint");
        }
        // Retrying the boss repeatedly preserves the exact first-battle snapshot.
        for (int i = 0; i < 3; i++)
        {
            if (checkpoints.BeginBoss("floor-a", new object()) ||
                checkpoints.BossEncounter != bossStart)
            {
                throw new InvalidOperationException("boss retry must retain the original encounter snapshot");
            }
        }
        checkpoints.RestartFloor();
        if (checkpoints.FloorEntry != entry || checkpoints.BossEncounter != null ||
            !checkpoints.BeginBoss("floor-a", secondPhase) ||
            checkpoints.BeginBoss("floor-b", new object()))
        {
            throw new InvalidOperationException("floor retry must discard only its later boss snapshot");
        }
        var nextFloor = new object();
        if (!checkpoints.EnterFloor("floor-b", nextFloor) ||
            checkpoints.BossEncounter != null || checkpoints.FloorEntry != nextFloor)
        {
            throw new InvalidOperationException("entering a new floor must invalidate the old boss snapshot");
        }
        checkpoints.Clear();
        if (checkpoints.FloorGuid != null || checkpoints.FloorEntry != null ||
            checkpoints.BossEncounter != null || checkpoints.BeginBoss("floor-b", bossStart))
        {
            throw new InvalidOperationException("world reset must clear both checkpoints");
        }
        checkpoints.EnterFloor("floor-c", new object());
        if (!checkpoints.BeginBoss("floor-c", null) ||
            checkpoints.BeginBoss("floor-c", secondPhase) ||
            checkpoints.BossEncounter != null)
        {
            throw new InvalidOperationException("failed first-phase capture must not fall back to a later phase");
        }
        checkpoints.Clear();
        checkpoints.BeginBoss("early-floor", null);
        if (checkpoints.EnterFloor("early-floor", entry) || checkpoints.FloorEntry != null ||
            checkpoints.BeginBoss("early-floor", secondPhase) || checkpoints.BossEncounter != null)
        {
            throw new InvalidOperationException("a snapshot arriving after battle starts is neither floor entry nor pre-battle state");
        }
        Console.WriteLine("RetryCheckpoints: independent floor/boss snapshots, phase transitions, repeated retry and resets passed");
    }
}
