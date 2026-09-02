using SephiriaEnhancements.Diagnostics;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SephiriaEnhancements.Integration
{
    internal static class CompatibilityProbe
    {
        internal static void Report()
        {
            List<string> missing = new List<string>();
            RequireProperty(typeof(UnitAvatar), "IsInBattle", missing);
            RequireField(typeof(UnitAvatar), "stencilSolidColor", missing);
            RequireField(typeof(UnitAvatar), "canBeTarget", missing);
            RequireProperty(typeof(CombatManager), "AllCreatures", missing);
            RequireProperty(typeof(CombatManager), "CurrentPlayer", missing);
            RequireProperty(typeof(PlayerAvatar), "NetworkaimObject", missing);
            RequireProperty(typeof(PlayerSpawner), "PlayerAvatar", missing);
            RequireField(typeof(PlayerSpawner), "MultiplayerList", missing);
            RequireNativeCompanionLobbyState(missing);
            RequireField(typeof(UI_PlayerMP), "mpBar", missing);
            RequireDamageFeedbackHandler(missing);
            RequireDamageDetailHandler(missing);
            RequireMethod(typeof(NetworkAreaProp), "HookMapElementUsed", missing);
            RequireMethod(typeof(BossSpawner), "UserCode_RpcStartBattle", missing);
            RequireMethod(typeof(BossSpawner), "UserCode_RpcStopBattle", missing);
            RequireMethod(typeof(BossSpawner),
                "UserCode_RpcByeBegin__Boolean", missing);
            RequireMethod(typeof(BossSpawner), "UserCode_RpcByeEnd", missing);
            RequireMethod(typeof(BossSpawner),
                "UserCode_RpcPhaseChangeBegin", missing);
            RequireMethod(typeof(BossSpawner),
                "UserCode_RpcPhaseChangeEnd", missing);
            RequireProperty(typeof(BossSpawner), "NetworkbossObject", missing);
            RequireProperty(typeof(BossSpawner), "NetworkbossAI", missing);
            RequireMethod(typeof(SeedBossSpawner), "UserCode_RpcStartBattle", missing);
            RequireMethod(typeof(SeedBossSpawner), "UserCode_RpcStopBattle", missing);
            RequireMethod(typeof(SeedBossSpawner), "UserCode_RpcByeBegin", missing);
            RequireMethod(typeof(SeedBossSpawner), "UserCode_RpcByeEnd", missing);
            RequireProperty(typeof(SeedBossSpawner), "NetworkbossObject", missing);
            RequireProperty(typeof(UnitAvatar), "Networkhp", missing);
            RequireProperty(typeof(UnitAvatar), "MaxHp", missing);
            RequireMethod(typeof(UI_BossHPBar), "SetBoss", missing);
            RequireField(typeof(UI_BossHPBar), "barImage", missing);
            RequireProperty(typeof(UnitAI_BossBasic), "Environment", missing);
            RequireField(typeof(BossEnvironment_QQBoss), "qqqBossSpawner", missing);
            RequireProperty(typeof(RandomEnemyPhaseSpawner), "NetworkdetectArea_lb", missing);
            RequireProperty(typeof(RandomEnemyPhaseSpawner), "NetworkdetectArea_rt", missing);
            RequireProperty(typeof(EnemySpawner), "NetworkplayerPreventArea_lb", missing);
            RequireProperty(typeof(EnemySpawner), "NetworkplayerPreventArea_rt", missing);
            RequireProperty(typeof(CommonEnemySpawner), "NetworkplayerPreventArea_lb", missing);
            RequireProperty(typeof(CommonEnemySpawner), "NetworkplayerPreventArea_rt", missing);
            RequireField(typeof(BossSpawner), "playerPreventArea_lb", missing);
            RequireField(typeof(BossSpawner), "playerPreventArea_rt", missing);
            RequireField(typeof(SeedBossSpawner), "playerPreventArea_lb", missing);
            RequireField(typeof(SeedBossSpawner), "playerPreventArea_rt", missing);
            RequireMethod(typeof(UnitAvatar), "DieClientside", missing);
            RequireMethod(typeof(PlayerAvatar), "AttackButtonDown", missing);
            RequireMethod(typeof(PlayerAvatar), "SubAttackButtonDown", missing);
            RequireMethod(typeof(IntegratedActionController), "Cast", missing);
            RequireMethod(typeof(IntegratedActionController), "CastStop", missing);
            RequireMethod(typeof(TargetTracker), "LateUpdate", missing);
            RequireField(typeof(GameCamera), "targetTracker", missing);
            RequireField(typeof(PlayerInputController), "playerInput", missing);
            RequireField(typeof(PlayerInputController), "autoAimedTarget", missing);
            RequireMethod(typeof(ControlsChangeHandler),
                "HandleOnControlsChanged", missing);
            RequirePropertySetter(typeof(ControlsChangeHandler),
                "UseDefaultSelectable", missing);
            RequireNativeActions(OptionsBinding.Instance?.actionAsset,
                NativeUiActions.RequiredByKeyboardNavigation, missing);
            RequireMethod(typeof(UI_ItemIcon), "ClickButton", missing);
            RequireField(typeof(UI_ItemIcon), "OnClick", missing);
            RequireMethod(typeof(UI_ItemBoxPanel), "Update", missing);
            RequireMethod(typeof(UI_TreeShopPanel), "Update", missing);
            RequireField(typeof(UI_TreeShopPanel), "currentSelected", missing);
            RequireMethod(typeof(UI_MapPanel), "Show", missing);
            RequireField(typeof(EnhancedProceduralFloorGenerator),
                "hiddenRoomInstances", missing);
            RequireField(typeof(LibraryFloorGenerator), "hiddenRoomInstances", missing);
            RequireField(typeof(WeaponControllerSimple), "currentWeapon", missing);
            RequireMethod(typeof(UnitAvatar),
                "UserCode_TargetKillUnit__NetworkConnectionToClient__UnitKillData", missing);

            Assembly gameAssembly = typeof(HorayModAPI).Assembly;
            string basis = "game=" + Application.version +
                ", unity=" + Application.unityVersion +
                ", assembly=" + gameAssembly.GetName().Version;
            if (missing.Count == 0)
            {
                SupportLogger.Info("compatibility_passed", "[SephiriaEnhancements] Compatibility probe passed (" + basis + ").");
            }
            else
            {
                SupportLogger.Warning("compatibility_members_missing", "[SephiriaEnhancements] Compatibility probe found " +
                    missing.Count + " missing member(s): " + string.Join(", ", missing) +
                    " (" + basis + "). Features will fail closed where possible.");
            }
        }

        private static void RequireProperty(Type type, string name, List<string> missing)
        {
            if (type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic) == null)
                missing.Add(type.Name + "." + name);
        }

        private static void RequireNativeActions(
            UnityEngine.InputSystem.InputActionAsset asset,
            IReadOnlyList<NativeActionId> actions, List<string> missing)
        {
            if (asset == null)
            {
                return;
            }

            for (int index = 0; index < actions.Count; index++)
            {
                if (NativeInputActions.FindAction(asset, actions[index]) == null)
                {
                    missing.Add("input:" + actions[index].QualifiedName);
                }
            }
        }

        private static void RequirePropertySetter(Type type, string name,
            List<string> missing)
        {
            PropertyInfo property = type.GetProperty(name,
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetSetMethod(true) == null)
                missing.Add(type.Name + "." + name + " setter");
        }

        private static void RequireField(Type type, string name, List<string> missing)
        {
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic) == null)
                missing.Add(type.Name + "." + name);
        }

        private static void RequireMethod(Type type, string name, List<string> missing)
        {
            if (type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic) == null)
                missing.Add(type.Name + "." + name);
        }

        private static void RequireDamageFeedbackHandler(List<string> missing)
        {
            foreach (MethodInfo method in typeof(UnitAvatar).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name.StartsWith("UserCode_RpcShowAllDamageParticles__DamageFeedback",
                    StringComparison.Ordinal) && parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(DamageFeedback[])) return;
            }
            missing.Add("UnitAvatar damage-feedback handler");
        }

        private static void RequireNativeCompanionLobbyState(List<string> missing)
        {
            bool eosAvailable = typeof(EOSLobbyManager).GetProperty("Instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) != null &&
                typeof(EOSLobbyManager).GetProperty("HasLobby",
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic) != null;
            FieldInfo invitationInstance = typeof(SteamInvitation).GetField("instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo lobbyManager = typeof(SteamInvitation).GetField("lobbyManager",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bool legacyAvailable = invitationInstance != null && lobbyManager?.FieldType.GetProperty(
                "HasLobby", BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic) != null;
            if (!eosAvailable && !legacyAvailable)
            {
                missing.Add("multiplayer lobby presence");
            }
        }

        private static void RequireDamageDetailHandler(List<string> missing)
        {
            foreach (MethodInfo method in typeof(UnitAvatar).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name.StartsWith("UserCode_RpcApplyDamage__DamageData",
                    StringComparison.Ordinal) && parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(DamageData)) return;
            }
            missing.Add("UnitAvatar damage-detail handler");
        }
    }
}
