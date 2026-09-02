using SephiriaEnhancements.Runtime.Inventory;
#if SEPHIRIA_ENHANCEMENTS_DEVTOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace SephiriaEnhancements.Diagnostics
{
    [HarmonyPatch]
    internal static class NativeLoadingOperationProfilingPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return Require(typeof(SaveData), nameof(SaveData.Load),
                typeof(string));
            yield return Require(typeof(SaveData), nameof(SaveData.LoadFromString),
                typeof(string));
            yield return Require(typeof(SaveManager), nameof(SaveManager.Load),
                typeof(string));
            yield return Require(typeof(SaveManager), nameof(SaveManager.LoadTMP),
                typeof(string));
            yield return Require(typeof(SaveManager),
                nameof(SaveManager.ApplyPostLoadSaveFixes));
            yield return Require(typeof(DungeonManager),
                nameof(DungeonManager.LoadDungeon));
            yield return Require(typeof(DungeonManager),
                nameof(DungeonManager.FloorAlloc), typeof(string));
            yield return Require(typeof(PlayerSpawner),
                nameof(PlayerSpawner.OnStartAuthority));
            yield return Require(typeof(PlayerSpawner), "Initialize", typeof(int),
                typeof(string), typeof(string), typeof(int));
            yield return Require(typeof(ScreenFader),
                nameof(ScreenFader.SetLoadingScreen), typeof(int));
            yield return Require(typeof(ScreenFader),
                nameof(ScreenFader.ClearLoadingScreen));
            yield return Require(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalizedVeryFirst),
                typeof(string));
            yield return Require(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalized),
                typeof(string));
        }

        private static void Prefix(MethodBase __originalMethod,
            out LoadingOperationState __state)
        {
            string operation = OperationName(__originalMethod);
            __state = new LoadingOperationState(Stopwatch.GetTimestamp(),
                GameLoadProfiler.ObserveNativeOperationStarted(operation));
        }

        private static Exception Finalizer(MethodBase __originalMethod,
            LoadingOperationState __state, Exception __exception)
        {
            DeveloperLogger.RecordGameLoadingOperation(
                __state.LoadAttemptId, OperationName(__originalMethod),
                ElapsedMilliseconds(__state.StartedAt), __exception == null);
            return __exception;
        }

        private static string OperationName(MethodBase method)
        {
            return method.DeclaringType.Name + "." + method.Name;
        }

        private static MethodBase Require(Type type, string name,
            params Type[] parameters)
        {
            MethodInfo method = AccessTools.DeclaredMethod(type, name, parameters);
            if (method == null)
            {
                throw new MissingMethodException(type.FullName, name);
            }

            return method;
        }

        private static float ElapsedMilliseconds(long startedAt)
        {
            return (float)((Stopwatch.GetTimestamp() - startedAt) * 1000d /
                Stopwatch.Frequency);
        }

        private readonly struct LoadingOperationState
        {
            internal LoadingOperationState(long startedAt, int loadAttemptId)
            {
                StartedAt = startedAt;
                LoadAttemptId = loadAttemptId;
            }

            internal long StartedAt { get; }
            internal int LoadAttemptId { get; }
        }
    }

    [HarmonyPatch]
    internal static class NativeLoadingStateProfilingPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(PlayerAvatar),
                "HookLoadingScreenType", new[] { typeof(int), typeof(int) });
        }

        private static void Postfix(PlayerAvatar __instance, int oldValue,
            int newValue)
        {
            GameLoadProfiler.ObserveNativeLoadingTransition(__instance, oldValue,
                newValue);
        }
    }

    [HarmonyPatch]
    internal static class NativeFloorRenderProfilingPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalizedVeryFirst),
                new[] { typeof(string) });
            yield return AccessTools.DeclaredMethod(typeof(PlayerLocalDataStorage),
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalized),
                new[] { typeof(string) });
        }

        private static void Postfix(MethodBase __originalMethod,
            string floorGuid)
        {
            string milestone = __originalMethod.Name ==
                nameof(PlayerLocalDataStorage.OnFloorRenderFinalizedVeryFirst)
                ? "floor_render_first_pass_completed"
                : "floor_render_completed";
            GameLoadProfiler.ObserveFloorRenderFinalized(milestone, floorGuid);
        }
    }
}
#endif
