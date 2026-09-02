using SephiriaEnhancements.RangedControls;

namespace SephiriaEnhancements.ModelChecks.Features.RangedControls;

internal static class DirectionalAimMathChecks
{
    internal static void Run()
    {
        float forward = DirectionalAimMath.AutomaticTargetScore(1f, 0f, 8f, 0f,
            64f, 100f, preferDirection: true, currentTarget: false);
        float closeBehind = DirectionalAimMath.AutomaticTargetScore(1f, 0f, -2f, 0f,
            4f, 100f, preferDirection: true, currentTarget: false);
        float closeIdle = DirectionalAimMath.AutomaticTargetScore(1f, 0f, -2f, 0f,
            4f, 100f, preferDirection: false, currentTarget: false);
        float farIdle = DirectionalAimMath.AutomaticTargetScore(1f, 0f, 8f, 0f,
            64f, 100f, preferDirection: false, currentTarget: false);
        if (forward <= closeBehind || closeIdle <= farIdle ||
            DirectionalAimMath.AutomaticTargetScore(1f, 0f, 8f, 0f, 64f, 100f,
                preferDirection: true, currentTarget: true) <= forward)
            throw new InvalidOperationException("automatic aim direction, distance or target hold failed");
        Console.WriteLine("DirectionalAimMath: automatic target scoring checks passed");
    }
}
