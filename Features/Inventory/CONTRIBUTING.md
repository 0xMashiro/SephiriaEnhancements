# Contributing an inventory search strategy

Strategies are source contributions compiled into the mod. The interfaces are
internal; this is not a public DLL plugin API. Keep game API access in the existing
integration layer. Search code uses the captured inventory and does not operate on
live Unity objects.

## Start with a working implementation

Read [MultiStartInventoryLayoutOptimizer.cs](Core/MultiStartInventoryLayoutOptimizer.cs)
for a seeded search composed with the existing bounded optimizer. It first runs the
existing search with the original budget, keeps its best result, then uses any
remaining budget for shuffled starts and local moves. It runs in Thorough mode;
small problems still use exact enumeration first. Fast and Balanced keep their
existing selection behavior. This strategy does not guarantee a better result on
every inventory or a globally optimal result.

The reusable entry point is `IInventoryLayoutOptimizer` in
[InventoryOptimizerSelector.cs](Core/InventoryOptimizerSelector.cs):

- `Metadata`: a unique ID, selection priority and supported capabilities.
- `CanOptimize(request)`: whether this strategy supports this input and effort.
- `TryOptimize(request, cancellationToken, out proposal)`: run the search or return
  `false` to let selection continue.

The request supplies `Snapshot`, the resolved user `Policy`, and `Budget`.
Build layouts with `InventoryLayoutProjection`. Its item indices correspond to
`Snapshot.Items`; preserve these identities, including duplicate item types.
Occupied cells must be unique and in range. Keep stone tablet rotations unchanged
when the item cannot rotate or the policy prohibits rotation.

## Evaluate candidates and return a result

Use `InventorySettlementProjector` to calculate modeled game effects and
`InventoryOptimizationScorer` to compare candidates against the request policy.
For a hot loop, reuse one `InventorySettlementProjectionWorkspace` with
`EvaluateForScoring`. Consume that evaluation immediately; the workspace is reused
on the next call. Use the detailed `Evaluate` method when retaining settlement data.

Return a result through `request.CreateProposal(layout, candidateEvaluations,
terminationReason, elapsedMilliseconds, searchMethod)`. It computes the baseline,
selected score, target feedback and before/after changes. It rejects invalid
layouts and forbidden rotations, retains the source layout if the submitted one
scores worse, and refuses to expose a layout that misses a Hard requirement.
Do not construct custom scores to change the meaning of user requirements.

Selection also rebuilds successful proposals against the original request, so a
strategy-supplied policy or score cannot replace it. Invalid layouts fall through
to another strategy. A handled search with unmet Hard goals returns that failure;
it is not automatically retried with a different strategy. Exceptions currently
propagate to the controller's failure handler. This is reviewed in-process code,
not a sandbox or a hard execution-time limiter.

Honor cancellation and check the time and evaluation limits inside search loops.
Composed searches must share the overall budget, rather than granting a fresh
budget to every restart. The budget counts candidate search evaluations; final
result validation is separate. Time limits are cooperative: an in-flight
evaluation and final validation can finish after the deadline.

Use a per-run `Random` with an explicit seed. For reproducible comparisons, disable
the elapsed-time limit and fix the seed and candidate budget. Do not claim an
optimality or infeasibility proof from a heuristic search. The `OptimalityProof`
capability permits reporting a proof; it does not establish one. Other capability
flags are descriptive; `CanOptimize` must implement the actual eligibility checks.

## Register and verify

1. Add the implementation under `Features/Inventory/Core`.
2. Add it to the registry initializer, or explicitly call
   `InventoryOptimizerRegistry.Register` during initialization. Discovery is not
   automatic. Priorities are descending: exact is 100, the runtime GPU strategy is 50,
   multistart is 25, and bounded fallback is 0. Avoid equal priorities. Selection uses the first handling
   strategy, not a comparison of results from all registered strategies.
3. Add a linked `<Compile Include>` to
   [the portable model project](../../../tests/ModelChecks/SephiriaEnhancements.ModelChecks.csproj).
4. Extend
   [InventoryOptimizerContributionChecks](../../../tests/ModelChecks/Features/Inventory/InventoryOptimizerContributionChecks.cs).
   Its `VerifyContract(optimizer, request)` accepts any strategy and a supported,
   feasible fixture. It checks budget accounting, item preservation, executable
   layouts, canonical scores, Hard conditions, feedback and source immutability.
   Run it on representative fixtures, not only a layout already at its optimum.
   Update the registry-order assertion when registering another built-in.
5. Add strategy-specific checks for a concrete improvement, fixed-seed
   repeatability, cancellation, disabled rotation and budget exhaustion. For
   infeasible inputs, assert the failure and its evidence separately. Use the
   exhaustive oracle on small cases to measure solution quality; a heuristic need
   not match the optimum on every case. Larger cases need representative
   performance measurements, not a claim based on one fixture.

Run focused checks from the repository root:

```powershell
dotnet run --project tests/ModelChecks/SephiriaEnhancements.ModelChecks.csproj -c Release -- --inventory-strategies-only
```

Before submitting, run the existing portable gate:

```powershell
./scripts/test.ps1
```

Portable checks use the SDK selected by `global.json` and require no private game
assemblies. The mod itself still targets `netstandard2.1`; do not introduce APIs
available only in the test runner's newer runtime. Use the README's build command
and a local game installation to verify the complete mod. Never contribute game
assemblies, extracted game source, or machine-specific paths.
