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
using SephiriaEnhancements.ModelChecks.Features.RangedControls;
using SephiriaEnhancements.ModelChecks.Integration;
using SephiriaEnhancements.ModelChecks.Runtime.Diagnostics;
using SephiriaEnhancements.ModelChecks.Runtime.Execution;
using SephiriaEnhancements.ModelChecks.Runtime.GameBridge.Multiplayer;
using SephiriaEnhancements.ModelChecks.Runtime.Inventory;
using SephiriaEnhancements.ModelChecks.Runtime.State;

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

TownMapProjectionChecks.Run();
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
NativeCompanionPolicyChecks.Run();
DirectionalAimMathChecks.Run();
CombatRelationOutlinePolicyChecks.Run();
CombatVisualPolicyChecks.Run();
CombatVisualLocalizationChecks.Run();
DpsFormatterChecks.Run();
CombatTrackingChecks.Run();
EncounterReportSnapshotChecks.Run();
CombatInsightsInteractionChecks.Run();
ReportDisplayWindowChecks.Run();
ModShortcutsChecks.Run();
DeveloperConsoleContractChecks.Run();
InventorySnapshotChecks.Run();
InventorySettlementValidationChecks.Run();
InventorySettlementProjectorChecks.Run();
Console.WriteLine("InventoryDefaultObjective: " +
    InventoryDefaultObjectiveChecks.Run());
Console.WriteLine("InventoryArtifactLevelBoundary: " +
    InventoryArtifactLevelBoundaryChecks.Run());
Console.WriteLine("InventoryTargetReachability: " +
    InventoryTargetReachabilityChecks.Run());
Console.WriteLine("InventoryPreferenceEditor: " +
    InventoryPreferenceEditorChecks.Run());
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
