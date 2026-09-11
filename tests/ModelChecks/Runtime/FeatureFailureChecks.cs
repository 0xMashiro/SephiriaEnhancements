using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SephiriaEnhancements.Runtime;

namespace SephiriaEnhancements.ModelChecks.Runtime;

internal static class FeatureFailureChecks
{
    internal static void Run()
    {
        FeatureFailure.Reset();
        int reports = 0, calls = 0;
        FeatureFailure.Report = (_, _) => reports++;
        Require(!FeatureFailure.Run(FeatureId.Inventory, () => throw new MissingMethodException()), "failure must stop the feature");
        Require(!FeatureFailure.Run(FeatureId.Inventory, () => calls++), "failure must persist until reload");
        Require(FeatureFailure.Run(FeatureId.CombatInsights, () => calls++), "inventory failure must not stop combat statistics");
        FeatureFailure.Disable(FeatureId.Inventory, new Exception());
        Require(reports == 1 && calls == 1, "failed feature must not run or report repeatedly");
        Require(FeatureFailure.TryTakeFailure(out var failed) && failed == FeatureId.Inventory &&
            !FeatureFailure.TryTakeFailure(out _), "cleanup must run once");
        Require(FeatureFailure.PendingNotices.SequenceEqual(new[] { FeatureId.Inventory }), "a feature failure must name its owner");
        FeatureFailure.Disable(FeatureId.MapEnhancements, new Exception());
        FeatureFailure.AcknowledgeNotice(FeatureId.Inventory);
        Require(FeatureFailure.PendingNotices.SequenceEqual(new[] { FeatureId.MapEnhancements }), "specific operation feedback must not swallow another feature's failure");
        FeatureFailure.AcknowledgeNotice(FeatureId.MapEnhancements);
        FeatureFailure.Disable(FeatureId.AutoCasting, new Exception());
        Require(FeatureFailure.PendingNotices.SequenceEqual(new[] { FeatureId.AutoCasting }), "later independent faults remain visible");
        FeatureFailure.AcknowledgeNotice(FeatureId.AutoCasting);
        FeatureFailure.Disable(FeatureId.AutoCasting, new Exception());
        Require(FeatureFailure.PendingNotices.Length == 0, "the same disabled feature must not repeat its notice");

        FeatureFailure.Reset();
        FeatureFailure.Report = (_, _) => throw new InvalidOperationException();
        FeatureFailure.Disable(FeatureId.Gameplay, new MissingFieldException());
        Require(!FeatureFailure.IsAvailable(FeatureId.Inventory) && !FeatureFailure.IsAvailable(FeatureId.CombatInsights) &&
            !FeatureFailure.IsAvailable(FeatureId.DefeatRetry), "shared state failure must disable its consumers");
        Require(FeatureFailure.IsAvailable(FeatureId.MapEnhancements), "independent features must survive shared state failure");
        Require(FeatureFailure.PendingNotices.Length == 3, "dependent failures must remain available for a combined notice");
        FeatureFailure.Reset();
        FeatureFailure.Disable(FeatureId.DeveloperTools, new Exception());
        Require(FeatureFailure.PendingNotices.Length == 0, "diagnostic failures must not notify the player");
        FeatureFailure.Reset();
        Require(Enum.GetValues<FeatureId>().All(FeatureFailure.IsAvailable), "reload must clear failure state");
        CheckPatchRollback();
        Console.WriteLine("Feature isolation: independent execution, lifetime, dependencies, reporting, transactional patch rollback and foreign ownership passed");
    }

    private static void CheckPatchRollback()
    {
        var patches = new FeaturePatchSet("SephiriaEnhancements.Tests.FeatureFailure");
        var foreign = new Harmony("SephiriaEnhancements.Tests.ForeignOwner");
        try
        {
            foreign.CreateClassProcessor(typeof(ForeignPatch)).Patch();
            Require(patches.Install(FeatureId.Inventory, typeof(FirstPatch)), "first patch must install");
            Require(patches.Install(FeatureId.MapEnhancements, typeof(IndependentPatch)), "independent patch must install");
            Require(Target() == 111, "test patches must actually execute");
            bool threw = false;
            try { patches.Install(FeatureId.Inventory, typeof(InvalidPatch)); }
            catch (Exception) { threw = true; }
            Require(threw && !FeatureFailure.IsAvailable(FeatureId.Inventory), "invalid target must fail installation");
            Require(Target() == 101, "installation failure must remove earlier feature patches and retain other owners");
            Require(!patches.Install(FeatureId.Inventory, typeof(FirstPatch)), "failed feature cannot reinstall another class");
            patches.Remove(FeatureId.MapEnhancements);
            Require(Target() == 1, "feature cleanup must not remove another Mod's patch");
            patches.Remove(FeatureId.MapEnhancements);
            Require(Target() == 1, "cleanup must be repeatable");
            patches.Install(FeatureId.AutoCasting, typeof(FirstPatch));
            threw = false;
            try { patches.Install(FeatureId.AutoCasting, typeof(InitializerFailurePatch)); }
            catch (TypeInitializationException) { threw = true; }
            Require(threw && Target() == 1 && !FeatureFailure.IsAvailable(FeatureId.AutoCasting),
                "type initializer failure must roll back earlier patches before any callback can run");
        }
        finally
        {
            foreach (FeatureId feature in Enum.GetValues<FeatureId>()) patches.Remove(feature);
            foreign.UnpatchAll(foreign.Id);
            FeatureFailure.Reset();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Target() => 0;

    [HarmonyPatch(typeof(FeatureFailureChecks), nameof(Target))]
    private static class FirstPatch
    {
        private static void Postfix(ref int __result) => __result += 10;
    }

    [HarmonyPatch(typeof(FeatureFailureChecks), nameof(Target))]
    private static class IndependentPatch
    {
        private static void Postfix(ref int __result) => __result += 100;
    }

    [HarmonyPatch(typeof(FeatureFailureChecks), nameof(Target))]
    private static class ForeignPatch
    {
        private static void Postfix(ref int __result) => __result++;
    }

    [HarmonyPatch]
    private static class InvalidPatch
    {
        private static MethodBase TargetMethod() => throw new MissingMethodException("Required target is unavailable");
        private static void Postfix() { }
    }

    [HarmonyPatch(typeof(FeatureFailureChecks), nameof(Target))]
    private static class InitializerFailurePatch
    {
        static InitializerFailurePatch() => throw new MissingFieldException("Required static dependency is unavailable");
        private static void Postfix() { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
