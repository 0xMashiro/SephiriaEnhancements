using SephiriaEnhancements.Runtime.Inventory;
using SephiriaEnhancements.Core;
using SephiriaEnhancements.Configuration;
using SephiriaEnhancements.Combat;
using SephiriaEnhancements.CombatRelationOutlines;
using SephiriaEnhancements.CombatVisuals;
using SephiriaEnhancements.RangedControls;
using SephiriaEnhancements.NativeCompanion;
using SephiriaEnhancements.Integration;
using SephiriaEnhancements.DeveloperConsole;
using SephiriaEnhancements.DeveloperTools.Core;
using SephiriaEnhancements.DefeatRetry;
using SephiriaEnhancements.Inventory;
using SephiriaEnhancements.Runtime;
using SephiriaEnhancements.Runtime.GameBridge;
using SephiriaEnhancements.Runtime.Execution;
using SephiriaEnhancements.MultiplayerRules;
using SephiriaEnhancements.MultiplayerRules.Integration;
using SephiriaEnhancements.MultiplayerRules.Presentation;
using SephiriaEnhancements.MultiplayerAccess;
using SephiriaEnhancements.MultiplayerAccess.Presentation;
using SephiriaEnhancements.MapEnhancements.Core;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;

Console.WriteLine("InventoryOptimizationPreferencesCodec: " +
    InventoryOptimizationPreferencesCodecChecks.Run());
Console.WriteLine("InventoryOptimizationArchitecture: " +
    InventoryOptimizationArchitectureChecks.Run());
Console.WriteLine("InventoryArtifactIntent: " +
    InventoryArtifactIntentEditorChecks.Run());
Console.WriteLine("KeyboardSelectionRecovery: " +
    KeyboardSelectionRecoveryPolicyChecks.Run());
Console.WriteLine("InventoryLocalRuntime: " +
    InventoryLocalRuntimeChecks.Run());
Console.WriteLine("InventoryEntityTargets: " +
    InventoryEntityTargetChecks.Run());
Console.WriteLine("NativeUiActionCatalog: " +
    NativeUiActionCatalogChecks.Run());

TownMapPoint townMapPoint = TownMapProjection.Project(
    worldX: 42f, worldY: 18f,
    floorOriginX: 10f, floorOriginY: -2f,
    mapScale: 3f, mapOffsetX: -8f, mapOffsetY: 5f);
if (Math.Abs(townMapPoint.X - 88f) > 0.001f ||
    Math.Abs(townMapPoint.Y - 65f) > 0.001f)
{
    throw new InvalidOperationException(
        "town NPC map projection must preserve native floor origin, scale and offset");
}
Console.WriteLine("TownMapProjection: native town-map coordinate mapping passed");

if (DeveloperPlayerDamagePolicy.MultiplierCount != 5 ||
    DeveloperPlayerDamagePolicy.NormalizeIndex(-1) != 0 ||
    DeveloperPlayerDamagePolicy.NormalizeIndex(5) != 4 ||
    Math.Abs(DeveloperPlayerDamagePolicy.GetMultiplier(0) - 1f) > 0.001f ||
    Math.Abs(DeveloperPlayerDamagePolicy.GetMultiplier(4) - 100f) > 0.001f ||
    Math.Abs(DeveloperPlayerDamagePolicy.Apply(12.5f, 2) - 62.5f) > 0.001f)
{
    throw new InvalidOperationException(
        "developer player damage multiplier mapping failed");
}

object outerExecutionContext = new();
object innerExecutionContext = new();
using (AmbientExecutionContext<object>.Enter(outerExecutionContext))
{
    if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
            outerExecutionContext))
        throw new InvalidOperationException(
            "ambient execution context must expose the current scope");
    using (AmbientExecutionContext<object>.Enter(innerExecutionContext))
    {
        if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
                innerExecutionContext))
            throw new InvalidOperationException(
                "nested ambient execution context must expose the inner scope");
    }
    if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
            outerExecutionContext))
        throw new InvalidOperationException(
            "ambient execution context must restore the outer scope");
}
if (AmbientExecutionContext<object>.Current != null)
    throw new InvalidOperationException(
        "ambient execution context must clear after the outer scope ends");

object coroutineExecutionContext = new();
bool coroutineStepScopeDisposed = false;
int coroutineCompletionCount = 0;
IEnumerator ContextAwareRoutine()
{
    if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
            coroutineExecutionContext))
        throw new InvalidOperationException(
            "coroutine execution context must be active while advancing");
    yield return "context-step";
}
IEnumerator contextualCoroutine =
    AmbientExecutionContext<object>.WrapCoroutine(
        ContextAwareRoutine(), coroutineExecutionContext,
        _ => new CallbackScope(() => coroutineStepScopeDisposed = true),
        _ => coroutineCompletionCount++);
if (!contextualCoroutine.MoveNext() ||
    !Equals(contextualCoroutine.Current, "context-step") ||
    !coroutineStepScopeDisposed ||
    AmbientExecutionContext<object>.Current != null ||
    contextualCoroutine.MoveNext() ||
    coroutineCompletionCount != 1 ||
    contextualCoroutine.MoveNext() ||
    coroutineCompletionCount != 1)
    throw new InvalidOperationException(
        "coroutine execution context must restore each step and complete once");
bool failedStepScopeDisposed = false;
IEnumerator FailingContextAwareRoutine()
{
    yield return null;
    throw new InvalidOperationException("expected contextual coroutine failure");
}
IEnumerator failingContextualCoroutine =
    AmbientExecutionContext<object>.WrapCoroutine(
        FailingContextAwareRoutine(), coroutineExecutionContext,
        _ => new CallbackScope(() => failedStepScopeDisposed = true));
failingContextualCoroutine.MoveNext();
failedStepScopeDisposed = false;
try
{
    failingContextualCoroutine.MoveNext();
    throw new InvalidOperationException(
        "failing contextual coroutine must propagate its exception");
}
catch (InvalidOperationException exception) when (
    exception.Message == "expected contextual coroutine failure")
{
}
if (!failedStepScopeDisposed ||
    AmbientExecutionContext<object>.Current != null)
    throw new InvalidOperationException(
        "failing coroutine steps must dispose and restore their context");
IEnumerator disposalFailureCoroutine =
    AmbientExecutionContext<object>.WrapCoroutine(
        ContextAwareRoutine(), coroutineExecutionContext,
        _ => new CallbackScope(() => throw new InvalidOperationException(
            "expected step-scope disposal failure")));
try
{
    disposalFailureCoroutine.MoveNext();
    throw new InvalidOperationException(
        "step-scope disposal failure must propagate");
}
catch (InvalidOperationException exception) when (
    exception.Message == "expected step-scope disposal failure")
{
}
if (AmbientExecutionContext<object>.Current != null)
    throw new InvalidOperationException(
        "ambient context must restore when step-scope disposal fails");
Console.WriteLine("AmbientExecutionContext: nesting, coroutine completion and failure cleanup passed");

if (Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
        EnemyHealthModifierCombination.ParticipantRuleOnly) - 1.3f) > 0.001f ||
    Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
        EnemyHealthModifierCombination.Additive) - 1.5f) > 0.001f ||
    Math.Abs(EnemyHealthRuleCalculator.Combine(1.3f, 20f,
        EnemyHealthModifierCombination.Multiplicative) - 1.56f) > 0.001f)
{
    throw new InvalidOperationException(
        "enemy health modifier combination semantics failed");
}

var multiplayerRulesLifecycleCases = new[]
{
    (isHost: true, explorationStarted: false, canEdit: true),
    (isHost: false, explorationStarted: false, canEdit: false),
    (isHost: true, explorationStarted: true, canEdit: false),
    (isHost: false, explorationStarted: true, canEdit: false)
};
if (MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
        MultiplayerRulesPreset.Original) ||
    !MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
        MultiplayerRulesPreset.Optimized) ||
    !MultiplayerRulesLifecyclePolicy.RequiresNativeBehaviorHooks(
        MultiplayerRulesPreset.Custom))
{
    throw new InvalidOperationException(
        "only non-original multiplayer rules require native behavior hooks");
}
foreach (var lifecycleCase in multiplayerRulesLifecycleCases)
{
    if (MultiplayerRulesLifecyclePolicy.CanEditHostPreferences(
            lifecycleCase.isHost,
            lifecycleCase.explorationStarted) != lifecycleCase.canEdit)
    {
        throw new InvalidOperationException(
            "multiplayer-rule edit lifecycle matrix failed");
    }
}
if (!MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, true, true, 4, false, false) ||
    !MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, true, true, 4, true, true) ||
    MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, true, true, 4, true, false) ||
    MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, true, true, 5, true, true) ||
    MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        false, true, true, 4, false, false) ||
    MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, false, true, 4, false, false) ||
    MultiplayerRulesLifecyclePolicy.CanApplyAuthoritativeRules(
        true, true, false, 4, false, false))
{
    throw new InvalidOperationException(
        "authoritative multiplayer rules must fail open for external providers " +
        "and unsupported participant counts");
}

var vanillaMultiplayerSession = new MultiplayerSessionSnapshot(4,
    MultiplayerExtensionProvider.None);
var extendedMultiplayerSession = new MultiplayerSessionSnapshot(7,
    MultiplayerExtensionProvider.SephiriaTogether);
if (vanillaMultiplayerSession.ConnectedHumanParticipantCount != 4 ||
    vanillaMultiplayerSession.HasMultiplayerExtension ||
    extendedMultiplayerSession.ConnectedHumanParticipantCount != 7 ||
    !extendedMultiplayerSession.HasMultiplayerExtension)
    throw new InvalidOperationException(
        "multiplayer runtime snapshot must preserve connected humans and provider");

if (MultiplayerExtensionDiscovery.HasDetectedExtension)
    throw new InvalidOperationException(
        "multiplayer extension discovery must start empty in model checks");
AssemblyBuilder.DefineDynamicAssembly(
    new AssemblyName("SephiriaTogether"), AssemblyBuilderAccess.Run)
    .DefineDynamicModule("Main");
if (!MultiplayerExtensionDiscovery.HasDetectedExtension ||
    MultiplayerExtensionDiscovery.DetectedProvider !=
        MultiplayerExtensionProvider.SephiriaTogether)
{
    throw new InvalidOperationException(
        "multiplayer extension discovery must observe assemblies loaded later");
}
Console.WriteLine("MultiplayerExtensionDiscovery: initial and late-load detection passed");

if (!MidRunAdmissionPolicy.DefaultEnabled ||
    !MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
        true, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(false, true, true, true,
        true, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, false, true, true,
        true, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, true, false, true,
        true, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, false,
        true, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
        false, true, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
        true, false, false) ||
    MidRunAdmissionPolicy.CanOwnAdmission(true, true, true, true,
        true, true, true) ||
    !MidRunAdmissionPolicy.CanEnableNativeReconnect(true, true, true, false) ||
    MidRunAdmissionPolicy.CanEnableNativeReconnect(true, true, true, true))
{
    throw new InvalidOperationException(
        "mid-run admission must default on, remain host-owned and " +
        "per-player-save only, and stay passive beside an extension");
}

var multiplayerAccessTexts =
    new Dictionary<(string Language, string Key), string>();
MultiplayerAccessLocalization.Register(
    (language, key, value) =>
        multiplayerAccessTexts[(language, key)] = value,
    new[] { "en-US", "zh-CN", "fr-FR" });
if (multiplayerAccessTexts[("zh-CN",
        MultiplayerAccessLocalization.AllowJoinAndReconnectSetting)] !=
        "中途加入与重连" ||
    !multiplayerAccessTexts.ContainsKey(("fr-FR",
        MultiplayerAccessLocalization.AllowJoinAndReconnectHelp)))
{
    throw new InvalidOperationException(
        "multiplayer-access localization must be complete with en-US fallback");
}

EnemySpawnOrigin? observedOrigin = null;
bool innerDisposed = false;
IEnumerator InnerRoutine()
{
    try
    {
        observedOrigin = EnemySpawnRoutineContext.CurrentFrame?.Origin;
        yield return "spawn";
        observedOrigin = EnemySpawnRoutineContext.CurrentFrame?.Origin;
    }
    finally
    {
        innerDisposed = true;
    }
}

IEnumerator wrappedRoutine = EnemySpawnRoutineContext.Wrap(InnerRoutine(),
    EnemySpawnOrigin.RandomEncounter, new object());
if (EnemySpawnRoutineContext.CurrentFrame != null || !wrappedRoutine.MoveNext() ||
    observedOrigin != EnemySpawnOrigin.RandomEncounter ||
    EnemySpawnRoutineContext.CurrentFrame != null ||
    !Equals(wrappedRoutine.Current, "spawn"))
{
    throw new InvalidOperationException(
        "enemy spawn origin must exist only while the native routine advances");
}
(wrappedRoutine as IDisposable)?.Dispose();
if (!innerDisposed || EnemySpawnRoutineContext.CurrentFrame != null)
{
    throw new InvalidOperationException(
        "enemy spawn routine wrapper must forward disposal and restore context");
}
Console.WriteLine("MultiplayerRules: original pass-through, optimized fixes, " +
    "lifecycle and spawn context checks passed");

var multiplayerRulesSession = new MultiplayerRulesSession();
if (multiplayerRulesSession.TryGetActive(out _))
    throw new InvalidOperationException(
        "multiplayer rules must be inactive before exploration starts");
ActiveExplorationMultiplayerRules frozenRules =
    multiplayerRulesSession.BeginNewExploration(new PreferredMultiplayerRules(
        MultiplayerRulesPreset.Optimized, MultiplayerRuleSnapshot.Original(),
        EnemyHealthModifierCombination.ParticipantRuleOnly));
if (!multiplayerRulesSession.TryGetActive(out var activeRules) ||
    !ReferenceEquals(frozenRules, activeRules) ||
    activeRules.Preset != MultiplayerRulesPreset.Optimized)
{
    throw new InvalidOperationException(
        "preferred multiplayer rules must freeze when exploration starts");
}
if (!EnemyHealthRuleResolver.TryResolveMultiplier(activeRules,
        EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
        participantCount: 4, otherModifierPercent: 20f,
        out float optimizedRandomHealthMultiplier) ||
    Math.Abs(optimizedRandomHealthMultiplier - 1.5f) > 0.001f ||
    EnemyHealthRuleResolver.TryResolveMultiplier(activeRules,
        EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
        participantCount: 3, otherModifierPercent: 20f, out _) ||
    EnemyHealthRuleResolver.TryResolveMultiplier(
        ActiveExplorationMultiplayerRules.FromPreset(MultiplayerRulesPreset.Original),
        EnemySpawnOrigin.RandomEncounter, EnemyHealthCategory.Regular,
        participantCount: 4, otherModifierPercent: 20f, out _))
{
    throw new InvalidOperationException(
        "enemy health rules must resolve sparse overrides without replacing game behavior");
}
multiplayerRulesSession.EndExploration();
if (multiplayerRulesSession.TryGetActive(out _))
    throw new InvalidOperationException(
        "multiplayer rules must release exploration-owned state when exploration ends");
Console.WriteLine("MultiplayerRulesSession: freeze, sparse resolution and release passed");
Console.WriteLine("MultiplayerRulesLifecycle: " + MultiplayerRulesLifecycleChecks.Run());
Console.WriteLine("LocalGameplayContext: " + LocalGameplayContextChecks.Run());

var multiplayerRulesTexts = new Dictionary<(string Language, string Key), string>();
int multiplayerLocalizationRegistrations = 0;
MultiplayerRulesLocalization.Register(
    (language, key, value) =>
    {
        multiplayerLocalizationRegistrations++;
        multiplayerRulesTexts[(language, key)] = value;
    },
    new[] { "en-US", "zh-CN", "fr-FR" });
if (multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.Section)] !=
        "多人游戏" ||
    multiplayerRulesTexts[("zh-CN", MultiplayerRulesLocalization.PresetSetting)] !=
        "规则预设" ||
    multiplayerRulesTexts[("fr-FR", MultiplayerRulesLocalization.Section)] !=
        "Multiplayer" ||
    multiplayerRulesTexts[("fr-FR", MultiplayerRulesLocalization.OptimizedPreset)] !=
        "Optimized" ||
    multiplayerRulesTexts[("zh-CN",
        MultiplayerRulesLocalization.CopyParticipantValuesSetting)] !=
        "复制当前人数参数" ||
    multiplayerRulesTexts[("zh-CN",
        MultiplayerRulesLocalization.ExternalRuleStackingSetting)] !=
        "与联机扩展叠加规则" ||
    multiplayerRulesTexts[("fr-FR",
        MultiplayerRulesLocalization.GroupEncountersAndBosses)] !=
        "Encounters and Bosses")
{
    throw new InvalidOperationException(
        "multiplayer-rule localization must use one complete language group");
}
MultiplayerRuleDefinition regularHealthDefinition = MultiplayerRuleCatalog.Get(
    MultiplayerRuleId.RegularEnemyHealthMultiplier);
MultiplayerRuleDefinition eliteHealthDefinition = MultiplayerRuleCatalog.Get(
    MultiplayerRuleId.EliteEnemyHealthMultiplier);
MultiplayerRuleDefinition regularDamageDefinition = MultiplayerRuleCatalog.Get(
    MultiplayerRuleId.RegularEnemyDamageBonus);
if (MultiplayerRulesLocalization.NumericValueKey(regularHealthDefinition, 15) !=
        MultiplayerRulesLocalization.NumericValueKey(eliteHealthDefinition, 15) ||
    MultiplayerRulesLocalization.NumericValueKey(regularHealthDefinition, 15) ==
        MultiplayerRulesLocalization.NumericValueKey(regularDamageDefinition, 0) ||
    multiplayerLocalizationRegistrations >= 3000)
{
    throw new InvalidOperationException(
        "multiplayer-rule localization must share identical unit/value keys");
}
foreach (MultiplayerRuleDefinition definition in MultiplayerRuleCatalog.All)
{
    int valueCount = MultiplayerRulesLocalization.NumericValueCount(definition);
    for (int stepIndex = 0; stepIndex < valueCount; stepIndex++)
    {
        string key = MultiplayerRulesLocalization.NumericValueKey(
            definition, stepIndex);
        foreach (string language in new[] { "en-US", "zh-CN", "fr-FR" })
        {
            if (!multiplayerRulesTexts.TryGetValue((language, key),
                    out string? value) || string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    $"missing multiplayer-rule value text: {language}/{key}");
            }
        }
    }
}
Console.WriteLine("MultiplayerRulesLocalization: native terms, group fallback and " +
    $"{multiplayerLocalizationRegistrations} deduplicated registrations passed");

var optionsCategoryTexts =
    new Dictionary<(string Language, string Key), string>();
OptionsCategoryLocalization.Register(
    (language, key, value) => optionsCategoryTexts[(language, key)] = value,
    new[] { "en-US", "zh-CN", "fr-FR" });
if (Enum.GetValues<OptionsCategory>().Length !=
        OptionsCategoryLocalization.CategoryKeys.Length ||
    optionsCategoryTexts[("zh-CN", OptionsCategoryLocalization.Setting)] !=
        "设置分类" ||
    optionsCategoryTexts[("zh-CN",
        OptionsCategoryLocalization.CategoryKeys[
            (int)OptionsCategory.InventoryArrangement])] != "背包整理" ||
    optionsCategoryTexts[("fr-FR",
        OptionsCategoryLocalization.CategoryKeys[
            (int)OptionsCategory.Multiplayer])] != "Multiplayer")
{
    throw new InvalidOperationException(
        "options categories must preserve enum/key alignment and complete fallback");
}
Console.WriteLine("OptionsCategoryLocalization: category alignment and fallback passed");
if (!OptionsCategoryVisibility.IsVisible(OptionsCategory.General,
        OptionsCategory.General, requiresCustomPreset: false,
        customPresetVisible: false, memberMultiplayerRuleGroup: -1,
        selectedMultiplayerRuleGroup: 0) ||
    OptionsCategoryVisibility.IsVisible(OptionsCategory.CombatAndDisplay,
        OptionsCategory.General, requiresCustomPreset: false,
        customPresetVisible: true, memberMultiplayerRuleGroup: -1,
        selectedMultiplayerRuleGroup: 0) ||
    OptionsCategoryVisibility.IsVisible(OptionsCategory.Multiplayer,
        OptionsCategory.Multiplayer, requiresCustomPreset: true,
        customPresetVisible: false, memberMultiplayerRuleGroup: -1,
        selectedMultiplayerRuleGroup: 0) ||
    !OptionsCategoryVisibility.IsVisible(OptionsCategory.Multiplayer,
        OptionsCategory.Multiplayer, requiresCustomPreset: true,
        customPresetVisible: true, memberMultiplayerRuleGroup: 2,
        selectedMultiplayerRuleGroup: 2) ||
    OptionsCategoryVisibility.IsVisible(OptionsCategory.Multiplayer,
        OptionsCategory.Multiplayer, requiresCustomPreset: true,
        customPresetVisible: true, memberMultiplayerRuleGroup: 3,
        selectedMultiplayerRuleGroup: 2))
{
    throw new InvalidOperationException(
        "options-category visibility must gate category, custom preset and rule group");
}
Console.WriteLine("OptionsCategoryVisibility: category and custom-rule matrix passed");

var groupedMultiplayerRuleIds = new HashSet<MultiplayerRuleId>();
foreach (MultiplayerRulePresentationGroup group in
    MultiplayerRulePresentationGroups.All)
{
    if (string.IsNullOrEmpty(group.LocalizationKey) || group.RuleIds.Count == 0)
        throw new InvalidOperationException(
            "multiplayer-rule presentation groups must be named and non-empty");
    foreach (MultiplayerRuleId ruleId in group.RuleIds)
    {
        if (!groupedMultiplayerRuleIds.Add(ruleId))
            throw new InvalidOperationException(
                "multiplayer-rule presentation groups must not duplicate " + ruleId);
    }
}
if (groupedMultiplayerRuleIds.Count !=
        Enum.GetValues<MultiplayerRuleId>().Length)
    throw new InvalidOperationException(
        "multiplayer-rule presentation groups must cover the full catalog");
Console.WriteLine("MultiplayerRulePresentationGroups: exact catalog coverage passed");

if (MultiplayerRuleCatalog.All.Count !=
        Enum.GetValues<MultiplayerRuleId>().Length ||
    !MultiplayerRuleCatalog.Get(MultiplayerRuleId.MonsterSpawnEntryMultiplier)
        .IsValidOverride(1.45f) ||
    MultiplayerRuleCatalog.Get(MultiplayerRuleId.EnemyGroupDifficultyOffset)
        .IsValidOverride(1.5f) ||
    !MultiplayerRuleCatalog.Get(
        MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant)
        .IsValidOverride(1f) ||
    MultiplayerRuleCatalog.Get(
        MultiplayerRuleId.LifeSupplyOnPositiveProgressFloor)
        .IsValidOverride(0.5f) ||
    MultiplayerRuleCatalog.Get(MultiplayerRuleId.TargetedExperienceOrbDivisor)
        .IsValidOverride(0f))
{
    throw new InvalidOperationException(
        "multiplayer-rule catalog coverage or value constraints failed");
}
MultiplayerRuleSnapshot originalRuleSnapshot = MultiplayerRuleSnapshot.Original();
foreach (MultiplayerRuleId ruleId in Enum.GetValues<MultiplayerRuleId>())
{
    for (int participantCount = 1; participantCount <= 4;
        participantCount++)
    {
        if (originalRuleSnapshot.Get(ruleId, participantCount).Source !=
            MultiplayerRuleValueSource.UseGameBehavior)
        {
            throw new InvalidOperationException(
                "original rule snapshot must not contain copied game values");
        }
    }
}
MultiplayerRuleSnapshot optimizedRuleSnapshot = MultiplayerRuleSnapshot.Optimized();
if (!optimizedRuleSnapshot.Get(
        MultiplayerRuleId.RandomEncounterHealthMultiplier, 4)
        .TryGetOverride(out float optimizedRandomSnapshotValue) ||
    Math.Abs(optimizedRandomSnapshotValue - 1.3f) > 0.001f ||
    optimizedRuleSnapshot.Get(
        MultiplayerRuleId.TargetedExperienceOrbDivisor, 4).Source !=
        MultiplayerRuleValueSource.UseGameBehavior ||
    !optimizedRuleSnapshot.HasAnyOverride(
        MultiplayerRuleId.RandomEncounterHealthMultiplier,
        MultiplayerRuleId.SeedEncounterBossHealthMultiplier,
        MultiplayerRuleId.MindEaterRootSummonHealthMultiplier) ||
    optimizedRuleSnapshot.HasAnyOverride(
        MultiplayerRuleId.TargetedExperienceOrbDivisor,
        MultiplayerRuleId.SharedMoneyAwardFactorPerParticipant))
{
    throw new InvalidOperationException(
        "optimized rule snapshot must remain a sparse confirmed-fix set");
}
Console.WriteLine("MultiplayerRuleCatalog: complete catalog, constraints and sparse presets passed");

MultiplayerRuleSnapshot customPayloadSnapshot = MultiplayerRuleSnapshot.Create(
    (ruleId, participantCount) =>
        ruleId == MultiplayerRuleId.RestorativePotionQuantity &&
            participantCount == 4
            ? MultiplayerRuleValue<float>.Override(7f)
            : ruleId ==
                MultiplayerRuleId.QliphothFinalBattleEntryAttackTracksParticipant &&
                participantCount == 2
                ? MultiplayerRuleValue<float>.Override(1f)
                : MultiplayerRuleValue<float>.UseGameBehavior());
var customPayloadRules = ActiveExplorationMultiplayerRules.Custom(
    customPayloadSnapshot, EnemyHealthModifierCombination.Multiplicative);
string customPayload = ActiveExplorationRulesPayloadCodec.Encode(customPayloadRules);
string[] mismatchedPresetPayloadCells = customPayload.Split('|');
mismatchedPresetPayloadCells[1] =
    ((int)MultiplayerRulesPreset.Original).ToString();
string mismatchedPresetPayload = string.Join('|', mismatchedPresetPayloadCells);
foreach (MultiplayerRulesPreset fixedPreset in new[]
    { MultiplayerRulesPreset.Original, MultiplayerRulesPreset.Optimized })
{
    ActiveExplorationMultiplayerRules fixedPresetRules =
        ActiveExplorationMultiplayerRules.FromPreset(fixedPreset);
    if (!ActiveExplorationRulesPayloadCodec.TryDecode(
            ActiveExplorationRulesPayloadCodec.Encode(fixedPresetRules),
            out ActiveExplorationMultiplayerRules decodedFixedPresetRules) ||
        decodedFixedPresetRules.Preset != fixedPreset ||
        decodedFixedPresetRules.HealthModifierCombination !=
            fixedPresetRules.HealthModifierCombination ||
        !decodedFixedPresetRules.Rules.IsEquivalentTo(fixedPresetRules.Rules))
    {
        throw new InvalidOperationException(
            "fixed multiplayer-rule presets must round-trip exactly");
    }
}
if (!ActiveExplorationRulesPayloadCodec.TryDecode(customPayload,
        out ActiveExplorationMultiplayerRules decodedPayloadRules) ||
    decodedPayloadRules.Preset != MultiplayerRulesPreset.Custom ||
    decodedPayloadRules.HealthModifierCombination !=
        EnemyHealthModifierCombination.Multiplicative ||
    !decodedPayloadRules.Rules.Get(
        MultiplayerRuleId.RestorativePotionQuantity, 4)
        .TryGetOverride(out float decodedPotionQuantity) ||
    Math.Abs(decodedPotionQuantity - 7f) > 0.001f ||
    decodedPayloadRules.Rules.Get(
        MultiplayerRuleId.RestorativePotionQuantity, 3).Source !=
        MultiplayerRuleValueSource.UseGameBehavior ||
    ActiveExplorationRulesPayloadCodec.TryDecode(
        customPayload.Replace("|7", "|999"), out _) ||
    ActiveExplorationRulesPayloadCodec.TryDecode(
        mismatchedPresetPayload, out _))
{
    throw new InvalidOperationException(
        "active multiplayer-rule payload must round-trip sparse values and reject invalid overrides");
}
Console.WriteLine("MultiplayerRulesPayload: sparse host snapshot round trip passed");
if (args.Contains("--multiplayer-rules-only"))
    return;

if (!DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, false,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, true, false, true, 0, false,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 1, false,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 2, false,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, true,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, false, true, true, 0, false,
        saveIdle: true, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, true, true, true, true, 0, false,
        saveIdle: false, nativeRestarting: false) ||
    DefeatRetryPolicy.ShouldOffer(true, false, true, true, true, 0, false,
        saveIdle: true, nativeRestarting: false))
    throw new InvalidOperationException(
        "floor retry must be enabled, host-only, defeat-only and floor-start-snapshot-gated");
Console.WriteLine("DefeatRetryPolicy: setting, host and checkpoint gates passed");

if (DefeatRetryPolicy.ClassifyConclusion(0) !=
        RetryConclusionKind.CombatDefeat ||
    DefeatRetryPolicy.ClassifyConclusion(2) !=
        RetryConclusionKind.ScriptedDefeat ||
    DefeatRetryPolicy.ClassifyConclusion(1) != RetryConclusionKind.Victory ||
    DefeatRetryPolicy.ClassifyConclusion(6) != RetryConclusionKind.Victory ||
    DefeatRetryPolicy.ClassifyConclusion(99) != RetryConclusionKind.Unknown)
{
    throw new InvalidOperationException(
        "native game-over types must preserve combat, scripted and victory semantics");
}
Console.WriteLine("DefeatRetryPolicy: game-over semantics passed");

if (!DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
        true, true, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(false, true, false,
        true, true, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, false, false,
        true, true, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, true,
        true, true, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
        false, true, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
        true, false, true, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
        true, true, false, true) ||
    DefeatRetryPolicy.ShouldCaptureFloorEntryCheckpoint(true, true, false,
        true, true, true, false))
{
    throw new InvalidOperationException(
        "floor-entry checkpoints must be captured only for an enabled active host run");
}
Console.WriteLine("DefeatRetryPolicy: floor-entry capture gates passed");

if (!DefeatRetryPolicy.ShouldCaptureRenderedCombatFloorFallback(true, true,
        false, true, true, true, true, explorationActivated: false,
        combatThreat: true, checkpointMatchesFloor: false) ||
    DefeatRetryPolicy.ShouldCaptureRenderedCombatFloorFallback(true, true,
        false, true, true, true, true, explorationActivated: true,
        combatThreat: true, checkpointMatchesFloor: false) ||
    DefeatRetryPolicy.ShouldCaptureRenderedCombatFloorFallback(true, true,
        false, true, true, true, true, explorationActivated: false,
        combatThreat: false, checkpointMatchesFloor: false) ||
    DefeatRetryPolicy.ShouldCaptureRenderedCombatFloorFallback(true, true,
        false, true, true, true, true, explorationActivated: false,
        combatThreat: true, checkpointMatchesFloor: true))
{
    throw new InvalidOperationException(
        "rendered-floor fallback must target only uncaptured scripted combat floors");
}
Console.WriteLine("DefeatRetryPolicy: scripted combat floor fallback passed");

if (!DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
        true, true, true, true, hasFloor: true, hasBoss: true) ||
    DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
        true, true, true, true, hasFloor: false, hasBoss: true) ||
    DefeatRetryPolicy.ShouldCaptureBossEncounter(true, true, false,
        true, true, true, true, hasFloor: true, hasBoss: false) ||
    !DefeatRetryPolicy.ShouldApplyPlacement(restorePending: true,
        "boss-floor", "boss-floor") ||
    DefeatRetryPolicy.ShouldApplyPlacement(restorePending: true,
        "boss-floor", "other-floor") ||
    DefeatRetryPolicy.ShouldApplyPlacement(restorePending: false,
        "boss-floor", "boss-floor"))
{
    throw new InvalidOperationException(
        "boss checkpoints require a live encounter and placements must stay floor-bound");
}
Console.WriteLine("DefeatRetryPolicy: boss checkpoint and placement gates passed");

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

if (!CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, true,
        true, true, true) ||
    CombatRelationOutlinePolicy.ShouldShow(true, true, true, true, true,
        true, true, true) ||
    CombatRelationOutlinePolicy.ShouldShow(true, false, true, false, true,
        true, true, true) ||
    CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, false,
        true, true, true) ||
    CombatRelationOutlinePolicy.ShouldShow(true, true, true, false, true,
        false, true, true))
    throw new InvalidOperationException("combat-relation outline visibility policy failed");
Console.WriteLine("CombatRelationOutlinePolicy: relation and lifecycle checks passed");

if (CombatVisualPolicy.DefaultPreset != CombatVisualPreset.Balanced ||
    CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.FollowGame,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Body,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out _) ||
    !CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Balanced,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Body,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out EffectTransparencyLevel balancedBody) ||
    balancedBody != EffectTransparencyLevel.SlightlyTransparent ||
    !CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Balanced,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Effect,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out EffectTransparencyLevel balancedEffect) ||
    balancedEffect != EffectTransparencyLevel.VeryTransparent ||
    !CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Minimal,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Body,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out EffectTransparencyLevel minimalBody) ||
    minimalBody != EffectTransparencyLevel.VeryTransparent ||
    !CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Minimal,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Effect,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out EffectTransparencyLevel minimalEffect) ||
    minimalEffect != EffectTransparencyLevel.CompletelyTransparent ||
    CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Minimal,
        CombatVisualSourceRelation.RemoteCompanion,
        CombatVisualSurface.Effect,
        EffectTransparencyLevel.Normal,
        EffectTransparencyLevel.Normal, out _))
    throw new InvalidOperationException(
        "combat visual presets must only override local companion surfaces");

if (!CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Custom,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Body,
        EffectTransparencyLevel.CompletelyTransparent,
        EffectTransparencyLevel.SlightlyTransparent,
        out EffectTransparencyLevel customBody) ||
    customBody != EffectTransparencyLevel.CompletelyTransparent ||
    !CombatVisualPolicy.TryGetTransparencyLevel(
        CombatVisualPreset.Custom,
        CombatVisualSourceRelation.LocalCompanion,
        CombatVisualSurface.Effect,
        EffectTransparencyLevel.CompletelyTransparent,
        EffectTransparencyLevel.SlightlyTransparent,
        out EffectTransparencyLevel customEffect) ||
    customEffect != EffectTransparencyLevel.SlightlyTransparent ||
    CombatVisualPolicy.AllowsOutline(CombatVisualPreset.FollowGame,
        CombatOutlineScope.HostileAndFriendly, 1, isFriendly: false,
        isHostile: true) ||
    !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.FollowGame,
        CombatOutlineScope.Off, 2, isFriendly: true, isHostile: false) ||
    !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Balanced,
        CombatOutlineScope.Off, 1, isFriendly: false, isHostile: true) ||
    CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Custom,
        CombatOutlineScope.HostileOnly, 1, isFriendly: true, isHostile: false) ||
    !CombatVisualPolicy.AllowsOutline(CombatVisualPreset.Custom,
        CombatOutlineScope.HostileOnly, 1, isFriendly: false, isHostile: true))
    throw new InvalidOperationException(
        "combat visual custom values or outline scope failed");
Console.WriteLine("CombatVisualPolicy: preset, surface and outline matrix passed");

var combatVisualTexts = new Dictionary<(string Language, string Key), string>();
CombatVisualLocalization.Register(
    (language, key, value) => combatVisualTexts[(language, key)] = value,
    new[] { "en-US", "zh-CN", "fr-FR" });
if (combatVisualTexts[("zh-CN", CombatVisualLocalization.SettingPreset)] !=
        "战斗视觉预设" ||
    combatVisualTexts[("zh-CN", CombatVisualLocalization.PresetKeys[
        (int)CombatVisualPreset.Balanced])] != "均衡清晰（推荐）" ||
    combatVisualTexts[("fr-FR", CombatVisualLocalization.SettingPreset)] !=
        "Combat visual preset" ||
    combatVisualTexts.Count != 3 * (8 +
        CombatVisualLocalization.PresetKeys.Length +
        CombatVisualLocalization.TransparencyKeys.Length +
        CombatVisualLocalization.OutlineScopeKeys.Length))
    throw new InvalidOperationException(
        "combat visual localization must use complete feature-group fallback");
Console.WriteLine("CombatVisualLocalization: localized group fallback passed");

if (DpsFormatter.Compact(0f) != "0" ||
    DpsFormatter.Compact(999f) != "999" ||
    DpsFormatter.Compact(999.6f) != "1K" ||
    DpsFormatter.Compact(1200f) != "1.2K" ||
    DpsFormatter.Compact(12800f) != "13K" ||
    DpsFormatter.Compact(999900f) != "1M" ||
    DpsFormatter.Compact(1400000f) != "1.4M" ||
    DpsFormatter.Rate(52000f, 42.8f) != "1.2K" ||
    DpsFormatter.Percent(25f, 100f) != "25%" ||
    DpsFormatter.Percent(1f, 0f) != "0%" ||
    DpsFormatter.Seconds(42.8f) != "42.8s")
    throw new InvalidOperationException("compact DPS formatting failed");
Console.WriteLine("DpsFormatter: compact width checks passed");

static void Near(float actual, float expected, string name)
{
    if (Math.Abs(actual - expected) > 0.001f)
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}

var encounter = new BossEncounterTracker();
if (!encounter.Begin(10f) || encounter.Begin(11f))
    throw new InvalidOperationException("boss encounter must start exactly once");
encounter.Record(1, 120f);
encounter.Record(2, 80f);
encounter.Record(1, 30f);
encounter.Record(1, -5f);
Near(encounter.Total, 230f, "boss total accumulates positive damage");
Near(encounter.GetDamage(1), 150f, "player damage accumulates");
Near(encounter.AverageDps(1, 20f), 15f, "live average uses shared encounter time");
if (!encounter.Pause(20f) || encounter.Pause(21f))
    throw new InvalidOperationException("boss phase transition must pause timing exactly once");
Near(encounter.Elapsed(25f), 10f, "phase transition time is excluded");
if (!encounter.Resume(25f) || encounter.Resume(26f))
    throw new InvalidOperationException("next boss phase must resume timing exactly once");
if (!encounter.End(35f) || encounter.End(36f))
    throw new InvalidOperationException("boss encounter must end exactly once");
Near(encounter.Elapsed(99f), 20f, "completed duration is frozen");
Near(encounter.AverageDps(2, 99f), 4f, "post-fight average uses frozen duration");
encounter.Record(1, 500f);
Near(encounter.Total, 230f, "post-fight damage is ignored");
encounter.Reset();
Near(encounter.Total, 0f, "reset clears the encounter");
Console.WriteLine("BossEncounterTracker: lifecycle, totals and shared-time DPS checks passed");

var hitStreak = new HitStreakTracker();
HitStreakUpdate first = hitStreak.Register(0f, 10, HitStreakImpact.Normal,
    indirectDamage: false);
if (first.Count != 1 || first.ShouldRender) throw new InvalidOperationException("first hit must arm without rendering");
HitStreakUpdate second = hitStreak.Register(0.1f, 20, HitStreakImpact.Normal,
    indirectDamage: false);
if (second.Count != 2 || !second.ShouldRender) throw new InvalidOperationException("second hit must begin visible hit streak");
HitStreakUpdate dot = hitStreak.Register(0.2f, 3, HitStreakImpact.Normal,
    indirectDamage: true);
if (dot.Count != 0 || dot.ShouldRender || hitStreak.Count != 2) throw new InvalidOperationException("indirect tick must not extend hit streak");
for (int count = 3; count <= 10; count++) hitStreak.Register(0.2f + count * 0.1f,
    1, HitStreakImpact.Normal, indirectDamage: false);
var milestoneTracker = new HitStreakTracker();
HitStreakUpdate ten = default;
for (int count = 1; count <= 10; count++)
    ten = milestoneTracker.Register(count * 0.1f, 1, HitStreakImpact.Normal,
        indirectDamage: false);
if (!ten.IsMilestone || !ten.ShouldRender || ten.Count != 10 || ten.Tier != 1)
    throw new InvalidOperationException("ten-hit milestone must render and enter tier one");
HitStreakUpdate milestone = hitStreak.Register(1.21f, 1, HitStreakImpact.Critical,
    indirectDamage: false);
if (milestone.Count != 11 || !milestone.ShouldRender || milestone.Tier != 1)
    throw new InvalidOperationException("critical hit must render in the ten-hit tier");
HitStreakUpdate reset = hitStreak.Register(3f, 5, HitStreakImpact.Normal,
    indirectDamage: false);
if (reset.Count != 1 || reset.ShouldRender) throw new InvalidOperationException("hit-streak timeout must restart at one");
Console.WriteLine("HitStreakTracker: timeout, cadence, critical, tier and DOT checks passed");

var contexts = new DamageContextBuffer();
contexts.Record(1f, 7, 42, 3f, 4f, indirectDamage: true);
if (!contexts.TryMatch(1.2f, 7, 42, 3.1f, 4.1f, out bool indirect) || !indirect)
    throw new InvalidOperationException("nearby damage context must correlate");
if (contexts.TryMatch(1.21f, 7, 42, 3.1f, 4.1f, out _))
    throw new InvalidOperationException("damage context must be consumed once");
contexts.Record(2f, 8, 50, 0f, 0f, indirectDamage: false);
if (contexts.TryMatch(2.7f, 8, 50, 0f, 0f, out _))
    throw new InvalidOperationException("expired damage context must not correlate");
contexts.Record(3f, 9, 60, 1f, 2f, indirectDamage: false,
    EncounterDamageType.Lightning);
if (!contexts.TryMatchDamageType(3.1f, 9, 60, 1f, 2f,
        out EncounterDamageType damageType) ||
    damageType != EncounterDamageType.Lightning)
    throw new InvalidOperationException(
        "damage context must preserve the native damage type mapping");
Console.WriteLine("DamageContextBuffer: proximity, type, consumption and expiry checks passed");

var window = new RollingDamageWindow(5f, 8);
window.Record(0.2f, 20f);
Near(window.Dps(0.2f), 20f, "rolling window uses one-second warmup floor");
window.Record(1.2f, 30f);
Near(window.Dps(1.2f), 50f / 1f, "rolling window aggregates recent damage");
window.Reset();
Near(window.Dps(2f), 0f, "source reset clears rolling damage");
for (int hit = 0; hit < 100; hit++) window.Record(3f, 1f);
Near(window.Damage, 100f, "high-rate hits coalesce without overflowing the ring");
Console.WriteLine("RollingDamageWindow: delta, warmup, expiry and source-reset checks passed");

var ordinaryScope = EncounterScope.Create("floor", 10, EncounterScopeKind.Ordinary,
    0f, 0f, 10f, 10f);
var bossScope = EncounterScope.Create("floor", 20, EncounterScopeKind.Boss,
    -5f, -5f, 15f, 15f);
if (ordinaryScope == null || !ordinaryScope.AllowsDamage("floor", 1f, 1f, 9f, 9f) ||
    ordinaryScope.AllowsDamage("other", 1f, 1f, 9f, 9f) ||
    ordinaryScope.AllowsDamage("floor", 1f, 1f, 11f, 9f) ||
    EncounterScope.SelectContaining(ordinaryScope, bossScope, 5f, 5f) != bossScope)
    throw new InvalidOperationException(
        "encounter-area isolation or boss priority failed");
if (PlayerIdentityKey.Resolve(42, 7) != 42 ||
    PlayerIdentityKey.Resolve(0, 7) >= 0 ||
    PlayerIdentityKey.Resolve(0, 7) == PlayerIdentityKey.Resolve(0, 8))
    throw new InvalidOperationException("stable player identity key failed");
Console.WriteLine("EncounterScope: encounter-area isolation, boss priority and " +
    "source identity checks passed");

var defeats = new EncounterDefeatTracker();
if (!defeats.RecordDefeat(1, EncounterEnemyTier.Normal) ||
    defeats.RecordDefeat(1, EncounterEnemyTier.Normal) ||
    !defeats.RecordDefeat(2, EncounterEnemyTier.Miniboss) ||
    !defeats.RecordDefeat(3, EncounterEnemyTier.Boss))
    throw new InvalidOperationException("defeat tracker deduplication failed");
defeats.RecordLocalFinalBlow();
if (defeats.DefeatedCount != 3 || defeats.LocalFinalBlows != 1 ||
    defeats.NormalDefeated != 1 || defeats.MinibossDefeated != 1 ||
    defeats.BossDefeated != 1 || defeats.DefeatedCount !=
        defeats.NormalDefeated + defeats.MinibossDefeated + defeats.BossDefeated)
    throw new InvalidOperationException("defeat tracker totals or tiers failed");
defeats.Reset();
if (defeats.DefeatedCount != 0 || defeats.LocalFinalBlows != 0 ||
    defeats.NormalDefeated != 0 || defeats.MinibossDefeated != 0 ||
    defeats.BossDefeated != 0)
    throw new InvalidOperationException("defeat tracker reset failed");
Console.WriteLine("EncounterDefeatTracker: dedupe, tiers, local final blows and reset checks passed");

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

var reportWindow = new ReportDisplayWindow();
CombatInsightsInteractionChecks.Run();
reportWindow.Start(10f, 6f);
reportWindow.SetPresentationAvailable(available: false, 12f);
if (!reportWindow.IsOpen(30f) || reportWindow.IsVisible(30f))
    throw new InvalidOperationException("unavailable UI must pause a pending report");
reportWindow.SetPresentationAvailable(available: true, 30f);
if (!reportWindow.IsOpen(34f) || !reportWindow.IsVisible(34f) ||
    reportWindow.IsOpen(34.01f))
    throw new InvalidOperationException(
        "report must retain its unshown display duration");
reportWindow.Start(40f, 6f);
reportWindow.SetPresentationAvailable(available: false, 41f);
reportWindow.Clear();
reportWindow.SetPresentationAvailable(available: true, 50f);
if (reportWindow.IsOpen(50f))
    throw new InvalidOperationException(
        "clearing a paused report must not revive it");
Console.WriteLine(
    "ReportDisplayWindow: interaction pause, resume and reset checks passed");

using (JsonDocument shortcutDocument = JsonDocument.Parse(ModShortcuts.ActionMapJson))
{
    JsonElement map = shortcutDocument.RootElement.GetProperty("maps")[0];
    JsonElement actions = map.GetProperty("actions");
    JsonElement bindings = map.GetProperty("bindings");
    if (map.GetProperty("name").GetString() != ModShortcuts.MapName ||
        actions.GetArrayLength() != ModShortcuts.ActionNames.Length ||
        bindings.GetArrayLength() != 12)
        throw new InvalidOperationException("shortcut action map shape failed");

    var actionNames = actions.EnumerateArray()
        .Select(action => action.GetProperty("name").GetString())
        .ToHashSet(StringComparer.Ordinal);
    if (ModShortcuts.ActionNames.Any(action => !actionNames.Contains(action)))
        throw new InvalidOperationException("shortcut action catalog mismatch");

    var bindingIds = bindings.EnumerateArray()
        .Select(binding => binding.GetProperty("id").GetString())
        .ToArray();
    if (bindingIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != bindingIds.Length)
        throw new InvalidOperationException("shortcut binding IDs must be unique");

    JsonElement mapOverlayBinding = bindings.EnumerateArray().Single(binding =>
        binding.GetProperty("action").GetString() ==
            ModShortcuts.ToggleCurrentFloorMapOverlay &&
        binding.GetProperty("groups").GetString() ==
            ModShortcuts.KeyboardScheme &&
        binding.GetProperty("path").GetString() != string.Empty);
    if (mapOverlayBinding.GetProperty("path").GetString() != "<Keyboard>/m")
        throw new InvalidOperationException(
            "current-floor map overlay default binding failed");

    JsonElement optimizeBinding = bindings.EnumerateArray().Single(binding =>
        binding.GetProperty("action").GetString() ==
            ModShortcuts.OptimizeInventory &&
        binding.GetProperty("groups").GetString() ==
            ModShortcuts.KeyboardScheme &&
        binding.GetProperty("path").GetString() != string.Empty);
    if (optimizeBinding.GetProperty("path").GetString() != "<Keyboard>/f8")
        throw new InvalidOperationException("inventory shortcut default binding failed");
}
Console.WriteLine("ModShortcuts: action catalog, binding shape and stable IDs passed");

if (DeveloperConsoleContract.DefaultEnabled ||
    DeveloperConsoleContract.ActionMapName != "Player" ||
    DeveloperConsoleContract.ActionName != "OpenDevCommandPanel")
    throw new InvalidOperationException(
        "developer console must remain opt-in and reuse the native action");
Console.WriteLine("DeveloperConsole: opt-in default and native action contract passed");

var inventoryCells = new[]
{
    new InventoryCellSnapshot(0, 0, 0, 2, 5, 0, 0, 0, 0, false),
    new InventoryCellSnapshot(1, 1, 0, -1, 5, 0, 2, 2, 1, true),
    new InventoryCellSnapshot(2, 0, 1, 1, 5, 0, 0, 0, 0, false)
};
var restrictedCriteria = new CriteriaSnapshot(
    ArtifactActivationConditionKind.Unknown,
    CriteriaEvaluationState.Unsatisfied, CriteriaEvaluationState.Satisfied);
var restrictedArtifact = new ArtifactSnapshot(-1, 5, 2, -1, 0,
    false, true, false, "", true, false, false, "Default",
    restrictedCriteria, new[] { "EMBER" }, new[] { "EMBER", "GLACIER" },
    false, null);
var inventoryItems = new[]
{
    new InventoryItemSnapshot(10, 100, 1, 0, 0, 0, "Ordinary", "Item_Ordinary_Name",
        "Charm", "Rare", new[] { "STURDY" }, InventoryItemKind.Artifact,
        new ArtifactSnapshot(2, 5, 0, 2, 2, true, false, false, "",
            true, false, false, "Default",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable),
            new[] { "STURDY" }, new[] { "STURDY" }, false, null), null),
    new InventoryItemSnapshot(11, 101, 1, 1, 1, 0, "Restricted",
        "Item_Restricted_Name", "Charm", "Legend", new[] { "EMBER" },
        InventoryItemKind.RestrictedArtifact, restrictedArtifact, null)
};
var presetSnapshot = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
    new[] { 101 }, new[] { "EMBER" });
var comboSnapshots = new[]
{
    new ComboCategorySnapshot("EMBER", 3, 3, 1, 1, 1,
        new[] { 2, 4, 6 }, new[] { 2, 4 }, true, 4, 2, 0)
};
var inventorySnapshot = new InventorySnapshot(2, 3, inventoryCells, inventoryItems,
    true, 1, presetSnapshot, comboSnapshots, true, 2,
    unlimitedComboStatValue: 1);
if (inventorySnapshot.Width != 2 || inventorySnapshot.Height != 2 ||
    inventorySnapshot.Storage != 3 || inventorySnapshot.Items.Count != 2 ||
    !inventorySnapshot.TryGetCell(1, 0, out InventoryCellSnapshot ignoredCell) ||
    !ignoredCell.Disabled || !ignoredCell.IgnoresCriteria || !ignoredCell.Mystic ||
    ignoredCell.DisableCount != 2 || ignoredCell.IgnoreCriteriaCount != 1 ||
    inventorySnapshot.TryGetCell(1, 1, out _) ||
    inventorySnapshot.Items[0].Kind != InventoryItemKind.Artifact ||
    inventorySnapshot.Items[0].NativeItemTypeName != "Charm" ||
    inventorySnapshot.Items[0].NativeType != NativeInventoryItemType.Charm ||
    inventorySnapshot.Items[1].Kind != InventoryItemKind.RestrictedArtifact ||
    inventorySnapshot.Items[1].Artifact.Criteria.Kind !=
        ArtifactActivationConditionKind.Unknown ||
    inventorySnapshot.Items[1].Artifact.Criteria.RuntimeState !=
        CriteriaEvaluationState.Unsatisfied ||
    inventorySnapshot.Items[1].Artifact.Criteria.PositionProjectionState !=
        CriteriaEvaluationState.Satisfied ||
    inventorySnapshot.NativePreset.SelectedSlot != 2 ||
    inventorySnapshot.NativePreset.HasExplicitComboTargets ||
    inventorySnapshot.BuildIntent.NativePresetSlot != 2 ||
    inventorySnapshot.BuildIntent.PreferredArtifactEntityIds[0] != 101 ||
    inventorySnapshot.ComboCategories.Count != 1 ||
    inventorySnapshot.ComboCategories[0].CurrentCount != 3 ||
    inventorySnapshot.ComboCategories[0].ArtifactCategoryCount != 1 ||
    inventorySnapshot.ComboCategories[0].InferredUniquePairCount != 1 ||
    inventorySnapshot.ComboCategories[0].HighestComboCount != 4 ||
    inventorySnapshot.ComboCategories[0].HighestReachedThreshold != 2 ||
    inventorySnapshot.ComboCategories[0].UnlimitedComboExtraCount != 0 ||
    !inventorySnapshot.ComboCategories[0].NativePresetFavorite ||
    !inventorySnapshot.SuppressDuplicateComboEntities ||
    inventorySnapshot.UniquePairComboMode != 2 ||
    inventorySnapshot.UnlimitedComboStatValue != 1 ||
    inventorySnapshot.SettlementValidation.LayoutProjectionReady ||
    inventorySnapshot.SettlementValidation.CurrentLayoutVerified ||
    !inventorySnapshot.SettlementValidation.Issues.Contains(
        "BaselineStateUnavailable") ||
    !inventorySnapshot.SettlementValidation.Issues.Contains(
        "LayoutProjectionArtifactCriteriaUnavailable"))
    throw new InvalidOperationException("inventory snapshot dimensions, lookup or semantics failed");
inventoryCells[0] = inventoryCells[1];
inventoryItems[0] = inventoryItems[1];
if (inventorySnapshot.Cells[0].Index != 0 || inventorySnapshot.Items[0].InstanceId != 10)
    throw new InvalidOperationException("inventory snapshot must isolate caller arrays");
Console.WriteLine("InventorySnapshot: dimensions, classification and immutability checks passed");

string[] nativeInventoryTypes = Enum.GetNames<NativeInventoryItemType>();
string[] expectedNativeInventoryTypes =
{
    "Unknown", "Misc", "ThrowingWeapon", "Potion", "Food", "Scroll",
    "Charm", "StoneTablet", "Identifiable"
};
if (!nativeInventoryTypes.SequenceEqual(expectedNativeInventoryTypes))
    throw new InvalidOperationException(
        "native inventory item type contract drifted from Sephiria EItemType");
Console.WriteLine("InventorySnapshot: native EItemType contract passed");

string[] activationConditionKinds =
    Enum.GetNames<ArtifactActivationConditionKind>();
string[] expectedActivationConditionKinds =
{
    "None", "TopRow", "BottomRow", "SideEdge", "Interior", "Border",
    "BothSidesEmpty", "BothSidesArtifacts", "AllNeighborsOccupied",
    "AdjacentMagicArtifact", "FullHealth", "Unknown"
};
if (!activationConditionKinds.SequenceEqual(expectedActivationConditionKinds) ||
    !new InventoryMechanicCoverageSnapshot(inventorySnapshot).
        ActivationConditions.SequenceEqual(new[] { "Unknown" }))
    throw new InvalidOperationException(
        "artifact activation conditions must remain domain concepts");
Console.WriteLine("InventorySnapshot: artifact activation condition contract passed");

var verifiedSettlement = new InventoryCellSettlementSnapshot(true,
    baselineLevel: 1, baselineMaximumLevel: -1, baselineTemporaryLevel: 0,
    baselineLevelMultiplier: 0, baselineDisableCount: 0,
    baselineCriteriaBypassCount: 0, enchantLevel: 1, fixedLevel: 0,
    fixedDisableCount: 0, fixedCriteriaBypassCount: 0,
    fixedLevelMultiplier: 0, tabletLevel: 0, tabletDisableCount: 0,
    tabletCriteriaBypassCount: 0, tabletLevelMultiplier: 0);
var verifiedArtifact = new ArtifactSnapshot(2, 3, 1, 2, 2,
    true, false, false, "", true, false, false, "Default",
    new CriteriaSnapshot(ArtifactActivationConditionKind.None,
        CriteriaEvaluationState.NotApplicable,
        CriteriaEvaluationState.NotApplicable), new[] { "STURDY" },
    new[] { "STURDY" }, false, null);
var verifiedSnapshot = new InventorySnapshot(1, 1,
    new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
        false, verifiedSettlement) },
    new[] { new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
        "Item_Verified", "Charm", "Common", new[] { "STURDY" },
        InventoryItemKind.Artifact, verifiedArtifact, null) },
    comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
        1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
if (!verifiedSnapshot.SettlementValidation.CurrentLayoutVerified ||
    !verifiedSnapshot.SettlementValidation.LayoutProjectionReady ||
    (verifiedSnapshot.SettlementValidation.Capabilities &
        InventorySettlementCapabilities.SnapshotShapeVerified) == 0 ||
    !verifiedSnapshot.SettlementValidation.LayoutProjectionReady ||
    verifiedSnapshot.SettlementValidation.Issues.Count != 0)
    throw new InvalidOperationException(
        "verified settlement must satisfy every evaluator prerequisite");
ProjectedInventorySettlement evaluatedCurrent =
    InventorySettlementProjector.Evaluate(verifiedSnapshot,
        InventoryLayoutProjection.Current(verifiedSnapshot));
if (!evaluatedCurrent.Succeeded || evaluatedCurrent.Cells[0].Level != 2 ||
    evaluatedCurrent.Cells[0].MaximumLevel != 3 ||
    evaluatedCurrent.Cells[0].TemporaryLevel != 0 ||
    !evaluatedCurrent.Artifacts[0].Enabled ||
    evaluatedCurrent.Artifacts[0].CappedEffectiveLevel != 2 ||
    evaluatedCurrent.ComboCounts["STURDY"] != 1)
    throw new InvalidOperationException(
        "candidate evaluator must reproduce the verified current layout");
InventorySettlementDifferentialReport matchingDifferential =
    InventorySettlementDifferentialVerifier.Compare(verifiedSnapshot,
        InventoryLayoutProjection.Current(verifiedSnapshot), evaluatedCurrent,
        verifiedSnapshot);
if (!matchingDifferential.Matched ||
    matchingDifferential.Mismatches.Count != 0 ||
    matchingDifferential.Coverage.ArtifactCount != 1 ||
    matchingDifferential.Coverage.EnchantedArtifactCount != 1 ||
    !matchingDifferential.Coverage.NativeItemTypes.SequenceEqual(
        new[] { "Charm" }))
    throw new InvalidOperationException(
        "identical native and predicted settlements must match");

var arrangementEnabledSnapshot = new InventorySnapshot(1, 1,
    new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
        false, verifiedSettlement) },
    new[] { new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
        "Item_Verified", "Charm", "Common", new[] { "STURDY" },
        InventoryItemKind.Artifact, verifiedArtifact, null) },
    comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
        1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) },
    arrangementBonusesEnabled: true);
if (arrangementEnabledSnapshot.SettlementValidation.LayoutProjectionReady ||
    !arrangementEnabledSnapshot.SettlementValidation.Issues.Contains(
        "LayoutProjectionArrangementBonusesUnavailable"))
    throw new InvalidOperationException(
        "unmodeled arrangement bonuses must fail candidate readiness");

var inactiveEffectsSnapshot = new InventorySnapshot(1, 1,
    new[] { new InventoryCellSnapshot(0, 0, 0, 0, -1, 0, 0, 0, 0,
        false, new InventoryCellSettlementSnapshot(true, 0, -1, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)) },
    new[] { new InventoryItemSnapshot(24, 204, 1, 0, 0, 0, "Inactive",
        "Item_Inactive", "Charm", "Common", new[] { "STURDY" },
        InventoryItemKind.Artifact,
        new ArtifactSnapshot(0, 3, 0, 0, 0,
            false, true, false, "", true, false, false, "Default",
            new CriteriaSnapshot(ArtifactActivationConditionKind.None,
                CriteriaEvaluationState.NotApplicable,
                CriteriaEvaluationState.NotApplicable),
            new[] { "STURDY" }, new[] { "STURDY" }, false, null),
        null) },
    artifactEffectsEnabled: false,
    comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
        1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
if (inactiveEffectsSnapshot.SettlementValidation.LayoutProjectionReady ||
    !inactiveEffectsSnapshot.SettlementValidation.Issues.Contains(
        "LayoutProjectionArtifactEffectsInactive"))
    throw new InvalidOperationException(
        "inactive native artifact settlement must fail candidate readiness");

var mixedSnapshot = new InventorySnapshot(2, 2,
    new[]
    {
        new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
            false, verifiedSettlement),
        new InventoryCellSnapshot(1, 1, 0, 0, -1, 0, 0, 0, 0,
            false, new InventoryCellSettlementSnapshot(true, 0, -1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0))
    },
    new[]
    {
        new InventoryItemSnapshot(21, 201, 1, 0, 0, 0, "Verified",
            "Item_Verified", "Charm", "Common", new[] { "STURDY" },
            InventoryItemKind.Artifact, verifiedArtifact, null),
        new InventoryItemSnapshot(22, 202, 1, 1, 1, 0, "Ordinary",
            "Item_Ordinary", "Misc", "Common", Array.Empty<string>(),
            InventoryItemKind.Other, null, null)
    },
    comboCategories: new[] { new ComboCategorySnapshot("STURDY", 1, 1,
        1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), false) });
if (!mixedSnapshot.SettlementValidation.CurrentLayoutVerified ||
    !mixedSnapshot.SettlementValidation.LayoutProjectionReady ||
    !mixedSnapshot.SettlementValidation.LayoutProjectionReady)
    throw new InvalidOperationException(
        "ordinary items mixed with artifacts must remain candidate-ready");

var malformedPayloadSnapshot = new InventorySnapshot(1, 1,
    new[] { new InventoryCellSnapshot(0, 0, 0, 2, 3, 0, 0, 0, 0,
        false, verifiedSettlement) },
    new[] { new InventoryItemSnapshot(23, 203, 1, 0, 0, 0, "Malformed",
        "Item_Malformed", "Misc", "Common", Array.Empty<string>(),
        InventoryItemKind.Other, verifiedArtifact, null) });
if (malformedPayloadSnapshot.SettlementValidation.CurrentLayoutVerified ||
    malformedPayloadSnapshot.SettlementValidation.LayoutProjectionReady ||
    !malformedPayloadSnapshot.SettlementValidation.Issues.Contains(
        "SnapshotItemPayloadInvalid:23"))
    throw new InvalidOperationException(
        "inconsistent item kinds must fail at the snapshot shape boundary");

var mismatchedSnapshot = new InventorySnapshot(1, 1,
    new[] { new InventoryCellSnapshot(0, 0, 0, 3, 3, 0, 0, 0, 0,
        false, verifiedSettlement) }, Array.Empty<InventoryItemSnapshot>());
if (mismatchedSnapshot.SettlementValidation.CurrentLayoutVerified ||
    !mismatchedSnapshot.SettlementValidation.Issues.Contains(
        "CellSettlementMismatch:0"))
    throw new InvalidOperationException(
        "settlement mismatch must block candidate evaluation");
InventorySettlementDifferentialReport detectedDifferential =
    InventorySettlementDifferentialVerifier.Compare(verifiedSnapshot,
        InventoryLayoutProjection.Current(verifiedSnapshot), evaluatedCurrent,
        mismatchedSnapshot);
if (detectedDifferential.Matched ||
    !detectedDifferential.Mismatches.Contains("CellLevel:0") ||
    !detectedDifferential.Mismatches.Contains("ItemMissing:21"))
    throw new InvalidOperationException(
        "native differential must report field and identity mismatches");
Console.WriteLine("InventorySettlementValidator: positive and mismatch gates passed");
Console.WriteLine("InventorySettlementDifferentialVerifier: native parity gates passed");

var zeroSettlement = new InventoryCellSettlementSnapshot(true, 0, -1, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
var levelTwoSettlement = new InventoryCellSettlementSnapshot(true, 2, 2, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
var rowArtifact = new ArtifactSnapshot(0, 2, 0,
    0, 0, true, false, false, "", true, false, false, "Pre",
    new CriteriaSnapshot(ArtifactActivationConditionKind.None,
        CriteriaEvaluationState.NotApplicable,
        CriteriaEvaluationState.NotApplicable), new[] { "FIRE" },
    new[] { "FIRE", "ICE" }, true, null,
    new ArtifactCategoryRuleSnapshot(ArtifactCategoryRuleKind.RowModulo,
        new[] { "FIRE", "ICE" }));
var rowSnapshot = new InventorySnapshot(2, 4,
    new[]
    {
        new InventoryCellSnapshot(0, 0, 0, 0, 2, 0, 0, 0, 0, false,
            zeroSettlement),
        new InventoryCellSnapshot(1, 1, 0, 0, -1, 0, 0, 0, 0, false,
            zeroSettlement),
        new InventoryCellSnapshot(2, 0, 1, 2, 2, 0, 0, 0, 0, false,
            levelTwoSettlement),
        new InventoryCellSnapshot(3, 1, 1, 0, -1, 0, 0, 0, 0, false,
            zeroSettlement)
    },
    new[] { new InventoryItemSnapshot(31, 301, 1, 0, 0, 0, "Row",
        "Item_Row", "Charm", "Rare", Array.Empty<string>(),
        InventoryItemKind.Artifact, rowArtifact, null) },
    comboCategories: new[]
    {
        new ComboCategorySnapshot("FIRE", 1, 1, 1, 0, 0,
            Array.Empty<int>(), Array.Empty<int>(), false),
        new ComboCategorySnapshot("ICE", 0, 0, 0, 0, 0,
            new[] { 1 }, Array.Empty<int>(), false)
    });
ProjectedInventorySettlement movedRow =
    InventorySettlementProjector.Evaluate(rowSnapshot,
        new InventoryLayoutProjection(new[] { 2 }, new[] { 0 }));
if (!rowSnapshot.SettlementValidation.LayoutProjectionReady || !movedRow.Succeeded ||
    movedRow.ComboCounts["FIRE"] != 0 || movedRow.ComboCounts["ICE"] != 1)
    throw new InvalidOperationException(
        "row-dependent categories must follow the candidate row");
Console.WriteLine("InventorySettlementProjector: dynamic row categories passed");

ResolvedInventoryOptimizationPolicy defaultPolicy =
    InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
        InventoryOptimizationPreferences.Default);
Console.WriteLine("InventoryDefaultObjective: " +
    InventoryDefaultObjectiveChecks.Run(rowSnapshot));
Console.WriteLine("InventoryArtifactLevelBoundary: " +
    InventoryArtifactLevelBoundaryChecks.Run());
Console.WriteLine("InventoryTargetReachability: " +
    InventoryTargetReachabilityChecks.Run());
Console.WriteLine("InventoryPreferenceEditor: " +
    InventoryPreferenceEditorChecks.Run());
InventoryOptimizationProposal optimizedRow = InventoryOptimizer.Solve(rowSnapshot,
    defaultPolicy, new InventorySearchBudget(maximumImprovementRounds: 4,
        maximumCandidateEvaluations: 100,
        maximumElapsedMilliseconds: 1000));
if (!optimizedRow.Succeeded || !optimizedRow.Improved ||
    optimizedRow.Layout.GetCell(0) < 2 ||
    optimizedRow.BestScore.ComboBreakpointValue <=
        optimizedRow.CurrentScore.ComboBreakpointValue ||
    optimizedRow.CandidateEvaluations < 2 ||
    optimizedRow.ElapsedMilliseconds < 0)
    throw new InvalidOperationException(
        "optimizer must move the row-dependent artifact across a real breakpoint");
if (!InventoryLayoutPlanner.TryCreate(rowSnapshot, optimizedRow.Layout,
        out InventoryApplicationPlan rowPlan, out string rowPlanIssue) ||
    rowPlan.Swaps.Count != 1 || rowPlan.Rotations.Count != 0 ||
    rowPlan.Swaps[0].ExpectedFirstInstanceId != -1 ||
    rowPlan.Swaps[0].ExpectedSecondInstanceId != 31 ||
    rowPlanIssue != string.Empty)
    throw new InvalidOperationException(
        "layout planner must produce one identity-checked move into an empty cell");
InventoryOptimizationOutcome? rowOutcome = optimizedRow.Outcome;
if (rowOutcome == null)
{
    throw new InvalidOperationException(
        "inventory outcome must be available for a successful proposal");
}
InventoryCategoryOutcome fireOutcome = rowOutcome.CategoryChanges.Single(
    change => change.CategoryId == "FIRE");
InventoryCategoryOutcome iceOutcome = rowOutcome.CategoryChanges.Single(
    change => change.CategoryId == "ICE");
InventoryArtifactOutcome artifactOutcome = rowOutcome.ArtifactChanges.Single(
    change => change.InstanceId == 31);
if (artifactOutcome.EntityId != 301 ||
    artifactOutcome.NameKey != "Item_Row" ||
    !artifactOutcome.BeforeEnabled || !artifactOutcome.AfterEnabled ||
    artifactOutcome.BeforeEffectiveLevel != 0 ||
    artifactOutcome.AfterEffectiveLevel != 2 ||
    rowOutcome.MovedItems != 1 ||
    rowOutcome.RotatedTablets != 0 ||
    rowOutcome.BeforeArtifactsEnabled != 1 ||
    rowOutcome.AfterArtifactsEnabled != 1 ||
    rowOutcome.BeforeEffectiveLevels != 0 ||
    rowOutcome.AfterEffectiveLevels != 2 ||
    rowOutcome.BeforeBreakpointValue != 0 ||
    rowOutcome.AfterBreakpointValue != 1 ||
    rowOutcome.ArtifactChanges.Count != 1 ||
    rowOutcome.CategoryChanges.Count != 2 ||
    fireOutcome.BeforeCount != 1 || fireOutcome.AfterCount != 0 ||
    fireOutcome.BeforeBreakpointValue != 0 ||
    fireOutcome.AfterBreakpointValue != 0 ||
    iceOutcome.BeforeCount != 0 || iceOutcome.AfterCount != 1 ||
    iceOutcome.BeforeBreakpointValue != 0 ||
    iceOutcome.AfterBreakpointValue != 1)
{
    throw new InvalidOperationException(
        "inventory outcome must explain artifact, category and operation changes");
}
Console.WriteLine("InventoryOptimizer: breakpoint search and native operation planning passed");
Console.WriteLine("InventoryOptimizationOutcome: HUD-ready change summary passed");

InventoryLayoutProjection currentRowLayout = InventoryLayoutProjection.Current(
    rowSnapshot);
if (!currentRowLayout.ContentEquals(new InventoryLayoutProjection(
        currentRowLayout.CopyCells(), currentRowLayout.CopyRotations())) ||
    currentRowLayout.CompareStableTo(optimizedRow.Layout) >= 0)
    throw new InvalidOperationException(
        "candidate layout ordering must be deterministic without string keys");

long rowCandidateLayouts =
    InventoryExhaustiveSearchOracle.EstimateCandidateLayouts(rowSnapshot);
InventoryExhaustiveSearchResult exactRow =
    InventoryExhaustiveSearchOracle.Solve(rowSnapshot, defaultPolicy,
        new InventoryExhaustiveSearchLimits(
            maximumCandidateLayouts: 10,
            maximumElapsedMilliseconds: 1000));
if (rowCandidateLayouts != 4 || !exactRow.SearchStarted ||
    !exactRow.ProvenOptimal || exactRow.EstimatedCandidateLayouts != 4 ||
    exactRow.CandidateLayoutsEvaluated != 4 ||
    exactRow.BestScore.CompareTo(optimizedRow.BestScore) != 0 ||
    !exactRow.BestLayout.ContentEquals(optimizedRow.Layout))
    throw new InvalidOperationException(
        "exhaustive oracle must prove and reproduce the small-layout optimum");

InventoryExhaustiveSearchResult rejectedExactRow =
    InventoryExhaustiveSearchOracle.Solve(rowSnapshot, defaultPolicy,
        new InventoryExhaustiveSearchLimits(
            maximumCandidateLayouts: 3,
            maximumElapsedMilliseconds: 1000));
if (rejectedExactRow.SearchStarted || rejectedExactRow.ProvenOptimal ||
    rejectedExactRow.TerminationReason !=
        InventoryExhaustiveSearchTerminationReason.CandidateLayoutLimit)
    throw new InvalidOperationException(
        "exhaustive oracle must reject search spaces above its exact limit");
Console.WriteLine("InventoryExhaustiveSearchOracle: exact optimum and search-space gate passed");

InventoryOptimizationProposal exactHybrid = InventoryOptimizerSelector.Solve(
    rowSnapshot, defaultPolicy,
    new InventorySearchBudget(maximumImprovementRounds: 4,
        maximumCandidateEvaluations: 10,
        maximumElapsedMilliseconds: 1000));
InventoryOptimizationProposal neighborhoodHybrid =
    InventoryOptimizerSelector.Solve(rowSnapshot, defaultPolicy,
        new InventorySearchBudget(maximumImprovementRounds: 4,
            maximumCandidateEvaluations: 3,
            maximumElapsedMilliseconds: 1000));
if (!exactHybrid.Succeeded || !exactHybrid.OptimalityProven ||
    exactHybrid.SearchMethod != InventoryOptimizationSearchMethod.Exhaustive ||
    exactHybrid.TerminationReason !=
        InventorySearchTerminationReason.SearchSpaceExhausted ||
    exactHybrid.CandidateEvaluations != 4 ||
    exactHybrid.Outcome == null ||
    !exactHybrid.Layout.ContentEquals(exactRow.BestLayout) ||
    neighborhoodHybrid.SearchMethod !=
        InventoryOptimizationSearchMethod.Neighborhood ||
    neighborhoodHybrid.OptimalityProven)
{
    throw new InvalidOperationException(
        "hybrid solver must prove small spaces and budget larger spaces with neighborhood search");
}
Console.WriteLine("InventoryOptimizerSelector: exact-small and bounded-neighborhood selection passed");

Console.WriteLine("InventoryTwoSwapNeighborhood: " +
    InventoryTwoSwapNeighborhoodChecks.Run());
Console.WriteLine("InventorySwapAndStoneTabletRotationNeighborhood: " +
    InventorySwapRotationNeighborhoodChecks.Run());
Console.WriteLine("InventorySolverConformance: " +
    InventorySolverConformanceChecks.Run());
Console.WriteLine("InventoryCapacitySemantics: " +
    InventoryCapacitySemanticsChecks.Run());
Console.WriteLine("InventorySearchPerformance: " +
    InventorySearchPerformanceChecks.Run());

var explicitPreferences = new InventoryOptimizationPreferences(
    InventorySearchEffort.Fast, allowStoneTabletRotation: false,
    new[]
    {
        new ArtifactOptimizationPreference(-1, 301,
            InventoryPreferenceLevel.Prefer),
        new ArtifactOptimizationPreference(31, 301,
            InventoryPreferenceLevel.Priority)
    },
    new[]
    {
        new ComboOptimizationPreference("ICE",
            InventoryPreferenceLevel.Priority, minimumCount: 1)
    });
ResolvedInventoryOptimizationPolicy explicitPolicy =
    InventoryOptimizationPolicyResolver.Resolve(rowSnapshot,
        explicitPreferences);
if (explicitPolicy.SearchEffort != InventorySearchEffort.Fast ||
    explicitPolicy.AllowStoneTabletRotation ||
    explicitPolicy.ArtifactInstanceRules[31].Source !=
        InventoryPreferenceSource.ManualInstance ||
    explicitPolicy.ArtifactInstanceRules[31].Level !=
        InventoryPreferenceLevel.Priority ||
    explicitPolicy.ComboRules["ICE"].Source !=
        InventoryPreferenceSource.UserCategoryRule)
    throw new InvalidOperationException(
        "explicit inventory preferences must override broader rules");
InventoryOptimizationPreferences thoroughPreferences =
    explicitPreferences.WithExecutionSettings(
        InventoryOptimizationTendencyPolicy.GetSearchEffort(
            InventoryOptimizationTendency.Aggressive),
        allowStoneTabletRotation: true);
if (InventoryOptimizationTendencyPolicy.GetSearchEffort(
        InventoryOptimizationTendency.Stable) != InventorySearchEffort.Fast ||
    InventoryOptimizationTendencyPolicy.GetSearchEffort(
        InventoryOptimizationTendency.Automatic) !=
            InventorySearchEffort.Balanced ||
    InventoryOptimizationTendencyPolicy.GetSearchEffort(
        InventoryOptimizationTendency.Aggressive) !=
            InventorySearchEffort.Thorough ||
    thoroughPreferences.SearchEffort != InventorySearchEffort.Thorough ||
    !thoroughPreferences.AllowStoneTabletRotation ||
    thoroughPreferences.ArtifactPreferences.Count != 2 ||
    thoroughPreferences.ComboPreferences.Count != 1)
    throw new InvalidOperationException(
        "optimization tendencies must tune automatic search without losing player intent");
InventoryOptimizationProposal explicitProposal = InventoryOptimizer.Solve(
    rowSnapshot, explicitPolicy,
    new InventorySearchBudget(maximumImprovementRounds: 4,
        maximumCandidateEvaluations: 100,
        maximumElapsedMilliseconds: 1000));
InventoryOptimizationTargetEvaluation iceEvaluation =
    explicitProposal.TargetEvaluations.Single(
        evaluation => evaluation.Target == "Combo:ICE");
if (!explicitProposal.Improved ||
    explicitProposal.BestScore.PriorityTargetsSatisfied <=
        explicitProposal.CurrentScore.PriorityTargetsSatisfied ||
    iceEvaluation.Kind != InventoryOptimizationTargetKind.ComboCategory ||
    iceEvaluation.RequiredValue != 1 ||
    iceEvaluation.BeforeValue != 0 || iceEvaluation.AfterValue != 1 ||
    iceEvaluation.BeforeConditionReached ||
    !iceEvaluation.AfterConditionReached ||
    iceEvaluation.BeforeCompletionPoints != 0 ||
    iceEvaluation.AfterCompletionPoints !=
        InventoryOptimizationScorer.TargetCompletionScale)
    throw new InvalidOperationException(
        "explicit required combo must drive and evaluate the proposal target");
Console.WriteLine("InventoryOptimizationPolicy: precedence, capture and target evaluation passed");
var inventoryTexts = new Dictionary<string,
    Dictionary<string, string>>(StringComparer.Ordinal);
InventoryOptimizationLocalization.Register((language, key, value) =>
{
    if (!inventoryTexts.TryGetValue(language,
            out Dictionary<string, string>? texts))
    {
        texts = new Dictionary<string, string>(StringComparer.Ordinal);
        inventoryTexts.Add(language, texts);
    }
    texts.Add(key, value);
});
if (inventoryTexts.Count != 15 ||
    inventoryTexts.Values.Any(texts =>
        !texts.ContainsKey(InventoryOptimizationLocalization.
            SettingOptimizationTendency) ||
        !InventoryOptimizationLocalization.OptimizationTendencyKeys.All(
            texts.ContainsKey) ||
        !InventoryOptimizationLocalization.PreferenceChoiceKeys.All(
            texts.ContainsKey) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudArtifactsTab) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudCombosTab) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudOptimize) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudMarkArtifacts) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudFinishMarking) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudMarkingHint) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudMarkedCount) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudMarkedAndAdjustmentCount) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudPriorityQueue) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudAvoidZone) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudIntentBoardHint) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudChooseIntentSlot) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudOpen) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudAdjustTargets) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudHideTargets) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudAutomaticPreset) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudAutomaticInventory) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.
            HudAdjustmentCount) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudEnabled) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudNoTargets) ||
        !texts.ContainsKey(InventoryOptimizationLocalization.HudPage)) ||
    inventoryTexts["en-US"][InventoryOptimizationLocalization.
        OptimizationTendencyKeys[0]] != "Automatic")
    throw new InvalidOperationException(
        "inventory target editor must localize as one complete feature group");
Console.WriteLine("InventoryOptimizationTendency: intent-level settings and target-editor localization passed");

InventoryOptimizationProposal evaluationLimited = InventoryOptimizer.Solve(
    rowSnapshot, defaultPolicy,
    new InventorySearchBudget(maximumImprovementRounds: 4,
        maximumCandidateEvaluations: 1,
        maximumElapsedMilliseconds: 1000));
if (!evaluationLimited.Succeeded ||
    evaluationLimited.CandidateEvaluations != 1 ||
    evaluationLimited.TerminationReason !=
        InventorySearchTerminationReason.CandidateEvaluationLimit)
    throw new InvalidOperationException(
        "candidate evaluation budget must stop search after the initial layout");

InventoryOptimizationProposal timeLimited = InventoryOptimizer.Solve(
    rowSnapshot, defaultPolicy,
    new InventorySearchBudget(maximumImprovementRounds: 4,
        maximumCandidateEvaluations: 100,
        maximumElapsedMilliseconds: 0));
if (!timeLimited.Succeeded || timeLimited.CandidateEvaluations != 1 ||
    timeLimited.TerminationReason !=
        InventorySearchTerminationReason.ElapsedTimeLimit)
    throw new InvalidOperationException(
        "elapsed time budget must stop search after the initial layout");
Console.WriteLine("InventorySearchBudget: evaluation and elapsed-time limits passed");

var lifecycleCases = new[]
{
    (InventoryArrangementOperationPhase.Idle, false, false, false, false,
        false, InventoryArrangementInvalidationReason.None),
    (InventoryArrangementOperationPhase.Searching, false, true, true, true,
        true, InventoryArrangementInvalidationReason.FeatureDisabled),
    (InventoryArrangementOperationPhase.Searching, true, false, true, true,
        true, InventoryArrangementInvalidationReason.StandardInventoryClosed),
    (InventoryArrangementOperationPhase.Searching, true, true, false, true,
        true, InventoryArrangementInvalidationReason.GameplayContextChanged),
    (InventoryArrangementOperationPhase.Searching, true, true, true, false,
        true, InventoryArrangementInvalidationReason.InventoryStateChanged),
    (InventoryArrangementOperationPhase.Searching, true, true, true, true,
        false, InventoryArrangementInvalidationReason.InventoryLayoutChanged),
    (InventoryArrangementOperationPhase.Searching, true, true, true, true,
        true, InventoryArrangementInvalidationReason.None)
};
foreach (var lifecycleCase in lifecycleCases)
{
    InventoryArrangementInvalidationReason actual =
        InventoryArrangementLifecyclePolicy.Evaluate(lifecycleCase.Item1,
            lifecycleCase.Item2, lifecycleCase.Item3, lifecycleCase.Item4,
            lifecycleCase.Item5, lifecycleCase.Item6);
    if (actual != lifecycleCase.Item7)
        throw new InvalidOperationException(
            "inventory arrangement lifecycle matrix mismatch: " + actual);
}

using (var cancellation = new CancellationTokenSource())
{
    cancellation.Cancel();
    bool cancellationObserved = false;
    try
    {
        InventoryOptimizer.Solve(rowSnapshot, defaultPolicy,
            new InventorySearchBudget(maximumElapsedMilliseconds: 1000),
            cancellation.Token);
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }
    if (!cancellationObserved)
        throw new InvalidOperationException(
            "inventory search must observe cancellation before exploring candidates");
}
Console.WriteLine("InventoryArrangementLifecyclePolicy: invalidation matrix and cancellation passed");

if (!InventoryArrangementLifecyclePolicy.HasSameCapacity(
        sourceWidth: 6, sourceStorage: 30,
        currentWidth: 6, currentStorage: 30) ||
    InventoryArrangementLifecyclePolicy.HasSameCapacity(
        sourceWidth: 6, sourceStorage: 30,
        currentWidth: 6, currentStorage: 32) ||
    InventoryArrangementLifecyclePolicy.HasSameCapacity(
        sourceWidth: 6, sourceStorage: 32,
        currentWidth: 6, currentStorage: 30) ||
    InventoryArrangementLifecyclePolicy.HasSameCapacity(
        sourceWidth: 6, sourceStorage: 30,
        currentWidth: 5, currentStorage: 30))
{
    throw new InvalidOperationException(
        "inventory application must reject stale capacity snapshots");
}
Console.WriteLine("InventoryArrangementLifecyclePolicy: growth, shrink and " +
    "width mismatch application gates passed");

var tabletAdditions = new[]
{
    new TabletAdditionSnapshot(1, 0, "CHARM", true, false, false,
        true, false, false, true, TabletCriteriaKind.Artifact),
    new TabletAdditionSnapshot(2, 0, "MUL/2", false, true, false,
        false, true, false, false, effectKind: TabletEffectKind.MultiplyLevel,
        levelParameter: 2)
};
var tabletProjection = new TabletRotationProjectionSnapshot(0,
    new[] { tabletAdditions[0] }, new[] { tabletAdditions[1] }, true);
var stoneTabletSnapshot = new StoneTabletSnapshot(0, true, false, true, true,
    "R:CHARM", "R:MUL/2", new[] { tabletProjection });
tabletAdditions[0] = tabletAdditions[1];
if (stoneTabletSnapshot.RotationProjections.Count != 1 ||
    stoneTabletSnapshot.RotationProjections[0].Criteria[0].CriteriaKind !=
        TabletCriteriaKind.Artifact ||
    stoneTabletSnapshot.RotationProjections[0].Effects[0].EffectKind !=
        TabletEffectKind.MultiplyLevel ||
    stoneTabletSnapshot.RotationProjections[0].Effects[0].LevelParameter != 2 ||
    stoneTabletSnapshot.RotationProjections[0].Effects[0].ValidCell)
    throw new InvalidOperationException("tablet projection semantics or immutability failed");
var matchingPreset = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
    new[] { 101 }, new[] { "EMBER" });
var changedPreset = new NativePresetSnapshot(2, true, "Fire", 7, "Scholar",
    new[] { 102 }, new[] { "EMBER" });
if (!presetSnapshot.ContentEquals(matchingPreset) ||
    presetSnapshot.ContentEquals(changedPreset) || presetSnapshot.ContentEquals(null))
    throw new InvalidOperationException("native preset semantic equality failed");
Console.WriteLine("StoneTabletSnapshot: native semantics, validity and immutability checks passed");
Console.WriteLine("BuildIntentSnapshot: native preset remains a soft preference projection");

var catalogItems = new[]
{
    new InventoryCatalogItemSnapshot(100, "Charm", new[] { "EMBER" })
};
var catalogCategories = new[]
{
    new InventoryCategoryCatalogSnapshot("EMBER", new[] { 2, 4, 6 },
        new[] { 2, 4 }, 4)
};
var inventoryCatalog = new InventoryCatalogSnapshot(catalogItems,
    catalogCategories);
catalogItems[0] = new InventoryCatalogItemSnapshot(999, "Other",
    Array.Empty<string>());
catalogCategories[0] = new InventoryCategoryCatalogSnapshot("OTHER",
    Array.Empty<int>(), Array.Empty<int>());
if (!inventoryCatalog.TryGetItem(100, out InventoryCatalogItemSnapshot catalogItem) ||
    catalogItem.NativeItemTypeName != "Charm" ||
    catalogItem.PossibleCategories.Count != 1 ||
    !inventoryCatalog.TryGetCategory("EMBER",
        out InventoryCategoryCatalogSnapshot catalogCategory) ||
    catalogCategory.SetThresholds.Count != 3 ||
    catalogCategory.ComboThresholds[1] != 4 ||
    catalogCategory.HighestComboCount != 4 ||
    inventoryCatalog.TryGetCategory("UNKNOWN", out _))
    throw new InvalidOperationException("inventory catalog lookup or immutability failed");
Console.WriteLine("InventoryCatalogSnapshot: lookup and immutability checks passed");

var runtimeHub = new RuntimeStateHub("game=test");
RuntimeStateSnapshot initialRuntime = runtimeHub.Current;
if (initialRuntime.Consistency != RuntimeConsistencyState.Unavailable ||
    initialRuntime.ContractVersion != RuntimeStateSnapshot.CurrentContractVersion)
    throw new InvalidOperationException("runtime state must begin unavailable");
RuntimeStateSnapshot catalogRuntime = runtimeHub.PublishInventoryCatalog(0.5f);
RuntimeStateSnapshot gameplayContextRuntime =
    runtimeHub.BeginGameplayContext(1f);
RuntimeStateSnapshot attachedRuntime = runtimeHub.AttachPlayer(42,
    RuntimeCapabilities.LocalPlayer | RuntimeCapabilities.GridInventory |
    RuntimeCapabilities.GridInventoryEvents |
    RuntimeCapabilities.InventoryCatalog, 2f);
RuntimeStateSnapshot provisionalRuntime = runtimeHub.PublishInventory(
    settledObservation: false, 2.5f);
var inventoryStore = new InventoryStateStore();
inventoryStore.Publish(inventorySnapshot,
    provisionalRuntime.GameplayContextEpoch,
    provisionalRuntime.InventoryRevision);
if (inventoryStore.TryGetProjectable(provisionalRuntime, out _) ||
    inventoryStore.TryGetSettled(provisionalRuntime, out _))
    throw new InvalidOperationException("provisional inventory must not be consumable");
if (!inventoryStore.TryGetLatest(provisionalRuntime,
        out InventorySnapshot diagnosticInventory) ||
    diagnosticInventory != inventorySnapshot)
    throw new InvalidOperationException(
        "same-revision diagnostic inventory must remain observable");
RuntimeStateSnapshot publishedRuntime = runtimeHub.PublishInventory(
    settledObservation: true, 3f);
inventoryStore.Publish(inventorySnapshot,
    publishedRuntime.GameplayContextEpoch,
    publishedRuntime.InventoryRevision);
if (catalogRuntime.CatalogRevision != 1 ||
    gameplayContextRuntime.GameplayContextEpoch != 1 ||
    gameplayContextRuntime.CatalogRevision != 1 ||
    attachedRuntime.PlayerNetId != 42 ||
    attachedRuntime.Consistency != RuntimeConsistencyState.PendingSettlement ||
    provisionalRuntime.Consistency != RuntimeConsistencyState.PendingSettlement ||
    provisionalRuntime.CanProjectInventoryLayouts ||
    publishedRuntime.Consistency != RuntimeConsistencyState.Consistent ||
    !publishedRuntime.CanProjectInventoryLayouts ||
    publishedRuntime.InventoryRevision != 2 ||
    (publishedRuntime.Capabilities &
        RuntimeCapabilities.SettledInventoryObservation) == 0 ||
    !inventoryStore.TryGetProjectable(publishedRuntime,
        out InventorySnapshot storedInventory) ||
    !inventoryStore.TryGetSettled(publishedRuntime, out _) ||
    storedInventory != inventorySnapshot)
    throw new InvalidOperationException("runtime state attach or publication failed");
RuntimeStateSnapshot invalidInputRuntime = runtimeHub.PublishInventory(
    settledObservation: true, 3.5f, layoutProjectionReady: false);
inventoryStore.Publish(inventorySnapshot,
    invalidInputRuntime.GameplayContextEpoch,
    invalidInputRuntime.InventoryRevision);
if (invalidInputRuntime.CanProjectInventoryLayouts ||
    (invalidInputRuntime.Capabilities &
        RuntimeCapabilities.InventoryLayoutProjection) != 0 ||
    !inventoryStore.TryGetSettled(invalidInputRuntime, out _) ||
    inventoryStore.TryGetProjectable(invalidInputRuntime, out _))
    throw new InvalidOperationException(
        "unprojectable inventory must block layout projection");
long pendingRevision = runtimeHub.MarkInventoryPending(4f).RuntimeRevision;
inventoryStore.Clear();
if (runtimeHub.MarkInventoryPending(5f).RuntimeRevision != pendingRevision ||
    runtimeHub.Current.CanProjectInventoryLayouts ||
    inventoryStore.TryGetProjectable(publishedRuntime, out _))
    throw new InvalidOperationException("runtime dirty events must coalesce");
RuntimeStateSnapshot nextFloorRuntime =
    runtimeHub.BeginGameplayContext(6f);
if (nextFloorRuntime.GameplayContextEpoch != 2 ||
    runtimeHub.Current.InventoryRevision != 0 ||
    runtimeHub.Current.CatalogRevision != 1 ||
    runtimeHub.Current.PlayerNetId != 0 ||
    inventoryStore.TryGetLatest(nextFloorRuntime, out _))
    throw new InvalidOperationException(
        "new gameplay context must invalidate floor-bound runtime state");
Console.WriteLine("RuntimeStateHub: gameplay-context epoch, revision, settlement " +
    "and coalescing checks passed");

var encounterLifecycleHub = new EncounterLifecycleHub();
var encounterEvents = new List<EncounterLifecycleEvent>();
encounterLifecycleHub.Changed += encounterEvents.Add;
EncounterLifecycleEvent contextReset =
    encounterLifecycleHub.BeginGameplayContext(7, 1f);
EncounterLifecycleEvent ordinaryCleared = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Ordinary,
        EncounterTransition.Cleared, 11, 0, 2f));
if (!encounterLifecycleHub.IsOrdinaryEncounterCleared(11) ||
    encounterLifecycleHub.IsOrdinaryEncounterCleared(12) ||
    encounterLifecycleHub.IsOrdinaryEncounterCleared(0))
    throw new InvalidOperationException(
        "ordinary encounter clear state was not retained by source");
EncounterLifecycleEvent bossStarted = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Started, 21, 0, 3f));
EncounterLifecycleEvent bossCompletionStarted = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.CompletionStarted, 21, 0, 4f));
EncounterLifecycleEvent continuationPrepared = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.ContinuationPrepared, 21, 22, 5f));
EncounterLifecycleEvent continuationStarted = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Started, 22, 0, 6f));
EncounterLifecycleEvent bossCleared = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Cleared, 22, 0, 7f));
EncounterLifecycleEvent bossStartedForDefeat = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Started, 31, 0, 7.1f));
EncounterLifecycleEvent bossDefeated = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Defeated, 31, 0, 7.2f));
int publishedEncounterEventCount = encounterEvents.Count;
EncounterLifecycleEvent invalidEncounterEvent = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.None,
        EncounterTransition.Cleared, 0, 0, 8f));
if (contextReset.GameplayContextEpoch != 7 ||
    contextReset.Transition != EncounterTransition.GameplayContextReset ||
    ordinaryCleared.Kind != EncounterKind.Ordinary ||
    ordinaryCleared.Transition != EncounterTransition.Cleared ||
    bossStarted.Transition != EncounterTransition.Started ||
    bossCompletionStarted.Transition !=
        EncounterTransition.CompletionStarted ||
    continuationPrepared.Transition !=
        EncounterTransition.ContinuationPrepared ||
    continuationPrepared.SourceInstanceId != 22 ||
    continuationPrepared.PreviousSourceInstanceId != 21 ||
    continuationStarted.Transition != EncounterTransition.Resumed ||
    bossCleared.Transition != EncounterTransition.Cleared ||
    bossStartedForDefeat.Transition != EncounterTransition.Started ||
    bossDefeated.Transition != EncounterTransition.Defeated ||
    bossCleared.LifecycleRevision <= bossStarted.LifecycleRevision ||
    invalidEncounterEvent != bossDefeated ||
    encounterEvents.Count != publishedEncounterEventCount)
    throw new InvalidOperationException(
        "encounter lifecycle publication or continuation semantics failed");
encounterLifecycleHub.Observe(new EncounterLifecycleObservation(
    EncounterKind.Boss, EncounterTransition.ContinuationPrepared,
    41, 42, 9f));
EncounterLifecycleEvent nextContextReset =
    encounterLifecycleHub.BeginGameplayContext(8, 10f);
EncounterLifecycleEvent startAfterContextReset = encounterLifecycleHub.Observe(
    new EncounterLifecycleObservation(EncounterKind.Boss,
        EncounterTransition.Started, 42, 0, 11f));
if (nextContextReset.GameplayContextEpoch != 8 ||
    startAfterContextReset.GameplayContextEpoch != 8 ||
    startAfterContextReset.Transition != EncounterTransition.Started ||
    encounterLifecycleHub.IsOrdinaryEncounterCleared(11))
    throw new InvalidOperationException(
        "gameplay context reset must discard floor-bound encounter state");
Console.WriteLine("EncounterLifecycleHub: context, clear, boss continuation and " +
    "invalid observation checks passed");

var runtimeMetrics = new RuntimeMetrics();
runtimeMetrics.RecordEvent(RuntimeEventKind.ItemUpdated);
runtimeMetrics.RecordEvent(RuntimeEventKind.ItemUpdated);
runtimeMetrics.RecordCapture(0.2f, succeeded: true);
runtimeMetrics.RecordCapture(1.2f, succeeded: false);
runtimeMetrics.RecordCatalogCapture(4f, succeeded: true);
runtimeMetrics.RecordTabletQuery(cacheHit: false, 2f, succeeded: true);
runtimeMetrics.RecordTabletQuery(cacheHit: true, 0f, succeeded: true);
runtimeMetrics.RecordTabletQuery(cacheHit: false, 4f, succeeded: false);
runtimeMetrics.RecordPresetCapture(3f, succeeded: true);
runtimeMetrics.RecordPresetCapture(5f, succeeded: false);
RuntimeMetricSnapshot metricSnapshot = runtimeMetrics.TakeSnapshotAndReset();
if (metricSnapshot.EventCounts[(int)RuntimeEventKind.ItemUpdated] != 2 ||
    metricSnapshot.Captures != 2 || metricSnapshot.FailedCaptures != 1 ||
    Math.Abs(metricSnapshot.AverageCaptureMilliseconds - 0.7f) > 0.001f ||
    Math.Abs(metricSnapshot.P50CaptureMilliseconds - 0.2f) > 0.001f ||
    Math.Abs(metricSnapshot.P95CaptureMilliseconds - 1.2f) > 0.001f ||
    metricSnapshot.CatalogCaptures != 1 ||
    metricSnapshot.FailedCatalogCaptures != 0 ||
    Math.Abs(metricSnapshot.AverageCatalogCaptureMilliseconds - 4f) > 0.001f ||
    metricSnapshot.TabletQueryCacheHits != 1 ||
    metricSnapshot.TabletQueryCacheMisses != 2 ||
    metricSnapshot.FailedTabletQueries != 1 ||
    Math.Abs(metricSnapshot.AverageTabletQueryMilliseconds - 3f) > 0.001f ||
    metricSnapshot.PresetCaptures != 2 ||
    metricSnapshot.FailedPresetCaptures != 1 ||
    Math.Abs(metricSnapshot.AveragePresetCaptureMilliseconds - 4f) > 0.001f ||
    Math.Abs(metricSnapshot.MaximumPresetCaptureMilliseconds - 5f) > 0.001f ||
    runtimeMetrics.TakeSnapshotAndReset().Captures != 0)
    throw new InvalidOperationException("runtime metrics aggregation or reset failed");
Console.WriteLine("RuntimeMetrics: events, latency percentiles and reset checks passed");

internal sealed class CallbackScope : IDisposable
{
    private readonly Action callback;
    private bool disposed;

    internal CallbackScope(Action callback)
    {
        this.callback = callback;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        callback();
    }
}
