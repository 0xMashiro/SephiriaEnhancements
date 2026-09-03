using SephiriaEnhancements.ModelChecks.Configuration;
using SephiriaEnhancements.ModelChecks.Features.CombatInsights;
using SephiriaEnhancements.ModelChecks.Features.CombatRelationOutlines;
using SephiriaEnhancements.ModelChecks.Features.CombatVisuals;
using SephiriaEnhancements.ModelChecks.Features.DefeatRetry;
using SephiriaEnhancements.ModelChecks.Features.DeveloperConsole;
using SephiriaEnhancements.ModelChecks.Features.DeveloperTools;
using SephiriaEnhancements.ModelChecks.Features.Inventory;
using SephiriaEnhancements.ModelChecks.Features.KeyboardUiNavigation;
using SephiriaEnhancements.ModelChecks.Features.MapEnhancements;
using SephiriaEnhancements.ModelChecks.Features.MultiplayerAccess;
using SephiriaEnhancements.ModelChecks.Features.MultiplayerRules;
using SephiriaEnhancements.ModelChecks.Features.NativeCompanion;
using SephiriaEnhancements.ModelChecks.Features.CombatTargeting;
using SephiriaEnhancements.ModelChecks.Integration;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using SephiriaEnhancements.ModelChecks.Runtime.Execution;
using SephiriaEnhancements.ModelChecks.Runtime.GameBridge.Multiplayer;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.State;

if (args.Contains("--inventory-gpu-only"))
{
    InventoryGpuChecks.Run();
    return;
}

KeyboardPointerOwnershipChecks.Run();
if (args.Contains("--keyboard-pointer-only")) return;

RewardComboHighlightChecks.Run();
if (args.Contains("--reward-combo-only")) return;

if (args.Contains("--combat-targeting-only"))
{
    CombatTargetingChecks.Run();
    ModShortcutsChecks.Run();
    return;
}

if (args.FirstOrDefault() == "--inventory-replay")
{
    InventoryReproductionReplay.Run(args);
    return;
}
InventoryReproductionChecks.Run();
InventoryAdditiveScoreBoundChecks.Run();
if (args.Contains("--inventory-reproduction-only")) return;
if (args.FirstOrDefault() == "--inventory-known-solutions-benchmark")
{
    InventoryKnownSolutionChecks.Benchmark(args[1], args.Contains("--fixed-work"));
    return;
}
InventoryKnownSolutionChecks.Run();
if (args.Contains("--inventory-known-solutions-only")) return;

if (args.Contains("--combat-insights-only"))
{
    DpsFormatterChecks.Run();
    CombatTrackingChecks.Run();
    EncounterReportSnapshotChecks.Run();
    FloorCombatStatisticsChecks.Run();
    CombatInsightsInteractionChecks.Run();
    ReportDisplayWindowChecks.Run();
    EncounterReportLayoutChecks.Run();
    ModShortcutsChecks.Run();
    return;
}

if (args.Contains("--inventory-strategies-only"))
{
    Console.WriteLine("InventoryOptimizerContribution: " + InventoryOptimizerContributionChecks.Run());
    return;
}

if (args.Contains("--inventory-application-only"))
{
    Console.WriteLine("InventoryLocalRuntime: " + InventoryLocalRuntimeChecks.Run());
    Console.WriteLine("InventoryItemIdentity: " + InventoryItemIdentityChecks.Run());
    return;
}

if (args.Contains("--inventory-hard-only"))
{
    InventoryHardConstraintChecks.Run();
    return;
}

if (args.Contains("--inventory-preferences-only"))
{
    InventoryPreferenceComparisonChecks.Run();
    return;
}

if (args.Contains("--row-category-stats-only"))
{
    InventoryRowCategoryStatChecks.Run();
    return;
}

LoggingChecks.Run();
if (args.Contains("--logging-only")) return;
LocalizationChecks.Run();
if (args.Contains("--localization-only")) return;

Console.WriteLine("InventoryOptimizationPreferencesCodec: " +
    InventoryOptimizationPreferencesCodecChecks.Run());
Console.WriteLine("InventoryOptimizationArchitecture: " +
    InventoryOptimizationArchitectureChecks.Run());
Console.WriteLine("InventoryOptimizerContribution: " + InventoryOptimizerContributionChecks.Run());
Console.WriteLine("InventoryArtifactIntent: " +
    InventoryArtifactIntentEditorChecks.Run());
InventoryPriorityQueueChecks.Run();
InventoryAutomaticGoalChecks.Run();
InventoryPreferenceComparisonChecks.Run();
InventoryHardConstraintChecks.Run();
Console.WriteLine("InventoryHudInteraction: " +
    InventoryHudInteractionChecks.Run());
Console.WriteLine("KeyboardSelectionRecovery: " +
    KeyboardSelectionRecoveryPolicyChecks.Run());
Console.WriteLine("InventoryLocalRuntime: " +
    InventoryLocalRuntimeChecks.Run());
Console.WriteLine("InventoryItemIdentity: " +
    InventoryItemIdentityChecks.Run());
Console.WriteLine("InventoryEntityTargets: " +
    InventoryEntityTargetChecks.Run());
Console.WriteLine("NativeUiActionCatalog: " +
    NativeUiActionCatalogChecks.Run());

TownMapProjectionChecks.Run();
MapEnhancementsLocalizationChecks.Run();
DeveloperPlayerDamagePolicyChecks.Run();
AmbientExecutionContextChecks.Run();
MultiplayerExtensionDiscoveryChecks.Run();
MidRunAdmissionChecks.Run();
MultiplayerRulePolicyChecks.Run();
MultiplayerRulesSessionChecks.Run();
Console.WriteLine("MultiplayerRulesLifecycle: " + MultiplayerRulesLifecycleChecks.Run());
Console.WriteLine("LocalGameplayContext: " + LocalGameplayContextChecks.Run());
MultiplayerRulesLocalizationChecks.Run();
OptionsCategoryChecks.Run();
MultiplayerRulePresentationGroupsChecks.Run();
MultiplayerRuleCatalogChecks.Run();
ActiveExplorationRulesPayloadCodecChecks.Run();
if (args.Contains("--multiplayer-rules-only"))
    return;

DefeatRetryPolicyChecks.Run();
RetryCheckpointsChecks.Run();
NativeCompanionPolicyChecks.Run();
CombatTargetingChecks.Run();
CombatRelationOutlinePolicyChecks.Run();
CombatVisualPolicyChecks.Run();
CombatVisualLocalizationChecks.Run();
DpsFormatterChecks.Run();
CombatTrackingChecks.Run();
EncounterReportSnapshotChecks.Run();
FloorCombatStatisticsChecks.Run();
CombatInsightsInteractionChecks.Run();
ReportDisplayWindowChecks.Run();
EncounterReportLayoutChecks.Run();
ModShortcutsChecks.Run();
DeveloperConsoleContractChecks.Run();
InventorySnapshotChecks.Run();
InventorySettlementValidationChecks.Run();
InventorySettlementProjectorChecks.Run();
InventoryRowCategoryStatChecks.Run();
Console.WriteLine("InventoryPositionEffects: " + InventoryPositionEffectChecks.Run());
Console.WriteLine("NativeInventoryEffectAccess: " + NativeInventoryEffectAccessChecks.Run());
Console.WriteLine("InventoryDefaultObjective: " +
    InventoryDefaultObjectiveChecks.Run());
Console.WriteLine("InventoryArtifactLevelBoundary: " +
    InventoryArtifactLevelBoundaryChecks.Run());
Console.WriteLine("InventoryTargetReachability: " +
    InventoryTargetReachabilityChecks.Run());
Console.WriteLine("InventoryComboTargetEditor: " +
    InventoryComboTargetEditorChecks.Run());
InventoryOptimizerChecks.Run();
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
InventoryOptimizationPolicyChecks.Run();
InventoryOptimizationLocalizationChecks.Run();
InventorySearchBudgetChecks.Run();
InventoryArrangementLifecyclePolicyChecks.Run();
StoneTabletSnapshotChecks.Run();
NativePresetSnapshotChecks.Run();
InventoryCatalogSnapshotChecks.Run();
RuntimeStateHubChecks.Run();
EncounterLifecycleHubChecks.Run();
RuntimeMetricsChecks.Run();
