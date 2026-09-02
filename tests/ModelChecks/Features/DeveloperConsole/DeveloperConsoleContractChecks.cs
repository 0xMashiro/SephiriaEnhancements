using SephiriaEnhancements.DeveloperConsole;

namespace SephiriaEnhancements.ModelChecks.Features.DeveloperConsole;

internal static class DeveloperConsoleContractChecks
{
    internal static void Run()
    {
        if (DeveloperConsoleContract.DefaultEnabled ||
            DeveloperConsoleContract.ActionMapName != "Player" ||
            DeveloperConsoleContract.ActionName != "OpenDevCommandPanel")
            throw new InvalidOperationException(
                "developer console must remain opt-in and reuse the native action");
        Console.WriteLine("DeveloperConsole: opt-in default and native action contract passed");
    }
}
