using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;

namespace SephiriaEnhancements.CombatVisuals
{
    [HarmonyPatch(typeof(SaveData), nameof(SaveData.GetInt))]
    internal static class CombatVisualOptionReadPatch
    {
        private static void Postfix(string key, ref int __result)
        {
            if (CombatVisualRuntime.TryGetTransparencyOverride(key,
                    out int value))
            {
                __result = value;
            }
        }
    }

    [HarmonyPatch(typeof(UnitAvatar), nameof(UnitAvatar.SetTransparencyRenderMode))]
    internal static class CompanionBodyTransparencyPatch
    {
        private static void Prefix(UnitAvatar __instance, UnitAvatar newValue,
            out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(__instance,
                CombatVisualSurface.Body, newValue);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Bullet), nameof(Bullet.OnStartClient))]
    internal static class CompanionBulletTransparencyPatch
    {
        private static void Prefix(Bullet __instance, out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(__instance.NetworkOwner,
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(SpecialProjectile_AreaJudgement),
        nameof(SpecialProjectile_AreaJudgement.OnStartClient))]
    internal static class CompanionAreaJudgementTransparencyPatch
    {
        private static void Prefix(SpecialProjectile_AreaJudgement __instance,
            out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(__instance.Networkowner,
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(SpecialProjectile_SpreadAOE),
        nameof(SpecialProjectile_SpreadAOE.OnStartClient))]
    internal static class CompanionSpreadAoeTransparencyPatch
    {
        private static void Prefix(SpecialProjectile_SpreadAOE __instance,
            out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(__instance.Networkowner,
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class CompanionMeleeTransparencyPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(MeleeCollision)).Single(
                method => method.Name.StartsWith(
                    "UserCode_RpcSpawn__UnitAvatar__Boolean",
                    StringComparison.Ordinal));
        }

        private static void Prefix(UnitAvatar owner, out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(owner,
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class CompanionBulletTailTransparencyPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] moveModuleTypes =
            {
                typeof(BulletMoveModule_AccelerationVector3),
                typeof(BulletMoveModule_AskardDarkness),
                typeof(BulletMoveModule_BirdDemonCircleBullet),
                typeof(BulletMoveModule_FireworkHoming),
                typeof(BulletMoveModule_Howitzer),
                typeof(BulletMoveModule_IceScythe),
                typeof(BulletMoveModule_Meteor),
                typeof(BulletMoveModule_OrbitOwner),
                typeof(BulletMoveModule_Parabola),
                typeof(BulletMoveModule_UniformVector2)
            };
            for (int index = 0; index < moveModuleTypes.Length; index++)
            {
                yield return AccessTools.DeclaredMethod(moveModuleTypes[index],
                    nameof(BulletMoveModule.ClientMove));
            }

            yield return AccessTools.DeclaredMethod(
                typeof(BulletMoveModule_UniformVector2),
                nameof(BulletMoveModule.OnCanMoveCountChanged));
        }

        private static void Prefix(BulletMoveModule __instance,
            out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(__instance.Bullet?.NetworkOwner,
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class CompanionBulletHitTransparencyPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(DungeonManager)).Single(
                method => method.Name.StartsWith(
                    "UserCode_RpcAttackBullet__UInt32__Boolean",
                    StringComparison.Ordinal));
        }

        private static void Prefix(uint ownerNetId, out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(
                CombatVisualSourceResolver.FindSource(ownerNetId),
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(BulletDestroyModule),
        nameof(BulletDestroyModule.OnDestroyRequestReceived))]
    internal static class CompanionBulletDestroyTransparencyPatch
    {
        private static void Prefix(BulletDestroyModule __instance,
            out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(
                __instance.Bullet?.NetworkOwner, CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }

    [HarmonyPatch]
    internal static class CompanionChainLightningTransparencyPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(DungeonManager)).Single(
                method => method.Name.StartsWith(
                    "UserCode_RpcCreateChainLightning__UInt32",
                    StringComparison.Ordinal));
        }

        private static void Prefix(uint ownerNetId, out IDisposable __state)
        {
            __state = CombatVisualRuntime.Begin(
                CombatVisualSourceResolver.FindSource(ownerNetId),
                CombatVisualSurface.Effect);
        }

        private static void Postfix(IDisposable __state)
        {
            __state?.Dispose();
        }

        private static Exception Finalizer(IDisposable __state,
            Exception __exception)
        {
            __state?.Dispose();
            return __exception;
        }
    }
}
