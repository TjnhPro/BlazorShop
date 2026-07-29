# Storefront Test Runtime Performance Closure Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-29
Autoplan mode: CEO -> Design -> Eng -> DX, auto-decisions recorded below

## Problem Statement

The full `BlazorShop.Tests.V2` Release run passed, but took too long:

```text
TRX: BlazorShop.Tests.V2/TestResults/storefront-f172-release-final-pass.trx
Result: 1641 passed, 2 skipped, 0 failed
TestRun window: 2026-07-29T12:43:07+07:00 -> 2026-07-29T12:57:44+07:00
Duration: 14m34s
```

Root cause from investigation:

- `BlazorShop.Tests.V2/TestAssemblyConfiguration.cs` disables xUnit parallelization for the entire test assembly.
- `StorefrontV2HostSmokeTests` contains 58 host smoke tests and accounts for about `756.2s` of the test runtime.
- The slowest host smoke tests use `ServiceUnavailableHandler`, which returns HTTP `503` immediately.
- Storefront V2 starts with `builder.AddServiceDefaults()`, and `BlazorShop.ServiceDefaults/Extensions.cs` applies `AddStandardResilienceHandler()` to `HttpClient` defaults.
- `503` is treated as transient by the .NET resilience pipeline, so the generated Storefront client can retry with backoff inside host smoke tests.
- Process-spawn tests also contribute runtime and hang risk:
  - `LegacyRemovalGuardrailTests` ran about `37.3s`.
  - `StorefrontStarterFoundationBoundaryTests.StarterProject_RestoresAndBuildsFromLocalStorefrontPackages` ran about `27.9s`.
  - `LegacyRemovalGuardrailTests` and `StorefrontGeneratedClientFoundationTests` still use process helpers without explicit timeout.

## Goal

Reduce Storefront-focused test runtime without weakening production runtime behavior, Storefront boundary guarantees, BFF semantics, or release confidence.

Target outcome:

- `StorefrontV2HostSmokeTests` slow tests drop from `10s-55s` into low single-digit seconds where the page flow is not intentionally measuring production retry.
- Full `BlazorShop.Tests.V2` remains green.
- No test or external process can hang indefinitely.
- Production `AddStandardResilienceHandler()` remains enabled by default.

## Non-goals

- [ ] Do not remove production HTTP resilience from active V2 runtimes.
- [ ] Do not change Commerce Node Storefront API route shape.
- [ ] Do not change Storefront V2 public page behavior.
- [ ] Do not remove host smoke coverage for auth, cart, checkout, SEO, maintenance, account, and BFF flows.
- [ ] Do not move Storefront V2 browser/application ownership across boundaries.
- [ ] Do not introduce a new test framework.
- [ ] Do not run full test commands without timeout/hang guards.

## What Already Exists

| Area | Existing asset | How this plan uses it |
| --- | --- | --- |
| Test evidence | `storefront-f172-release-final-pass.trx` | Baseline duration source. |
| Host smoke tests | `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2HostSmokeTests.cs` | Main optimization target. |
| Assembly config | `BlazorShop.Tests.V2/TestAssemblyConfiguration.cs` | Explains serial runtime; later phase may narrow parallelization scope. |
| Service defaults | `BlazorShop.ServiceDefaults/Extensions.cs` | Production resilience source; must remain default-on. |
| Storefront runtime registration | `BlazorShop.Storefront.V2/Program.cs` and Presentation hosting extensions | Explains host startup cost per WebApplicationFactory variant. |
| Process guardrail tests | `LegacyRemovalGuardrailTests`, `StorefrontGeneratedClientFoundationTests`, `StorefrontStarterFoundationBoundaryTests` | Timeout/hang-risk closure targets. |
| QA docs | `QA-StorefrontV2.todo.md` | Update with performance closure evidence after implementation. |

## Architecture Boundary

```text
StorefrontV2HostSmokeTests
  -> WebApplicationFactory<StorefrontV2.Program>
      -> BlazorShop.Storefront.V2 Program
          -> AddServiceDefaults()
              -> default HttpClient resilience
          -> AddStorefrontApplication()
              -> Storefront.Runtime generated client registrations
              -> Storefront.Presentation page/BFF/SEO/media services
```

The plan must keep this production graph intact. Test-only overrides may alter the test host configuration, but not the default production behavior.

## Autoplan Review

### Phase 1 - CEO Review

Premises:

- [x] The problem is worth solving because a 14m local test loop slows every later Storefront phase.
- [x] The slow path is not one hung test; it is serial accumulation from expensive host smoke tests plus retry/backoff.
- [x] The highest-leverage fix is to avoid production resilience retry during fake `503` test scenarios.
- [x] Full production resilience behavior should remain covered by focused tests instead of being paid for by every host smoke test.

Alternatives considered:

| Option | Effort | Risk | Decision |
| --- | ---: | --- | --- |
| Keep tests as-is | 0 | Slow feedback remains; future phases likely repeat timeout complaints | Rejected |
| Only increase command timeout | Low | Masks root cause and still wastes 14m+ | Rejected |
| Disable resilience globally | Low | Production behavior regression | Rejected |
| Add test-host opt-out for default HTTP resilience | Medium | Must prove default production behavior remains on | Accepted |
| Replace every `503` fake immediately | Medium/high | Broad test churn before root fix is proven | Defer to follow-up phase |

NOT in scope:

- [ ] Browser Playwright suite redesign.
- [ ] Rewriting all host smoke tests into unit tests.
- [ ] Replacing `WebApplicationFactory` globally.
- [ ] Removing production `ServiceDefaults`.

Dream state delta:

```text
Current:
  Full V2 tests pass, but Storefront host smoke runtime hides retry/backoff cost.

This plan:
  Test hosts can opt out of default HTTP resilience when using deterministic fake handlers.
  Process helpers cannot hang indefinitely.
  Parallelization is narrowed instead of disabled globally.

12-month ideal:
  Fast PR gate separates source/contract/unit tests from slower host/browser/package proofs.
  Release gate still runs full fidelity tests with explicit timeout and profiling output.
```

### Phase 2 - Design Review

Status: skipped.

Reason: this plan does not change user-facing UI, visual hierarchy, interaction copy, responsive behavior, or browser-rendered storefront markup. Storefront browser QA remains relevant only as regression verification if implementation touches visible behavior unexpectedly.

### Phase 3 - Engineering Review

Key engineering risks:

- [ ] Do not accidentally disable production retry/backoff.
- [ ] Do not hide real service-unavailable page behavior by replacing every `503` test with success fakes.
- [ ] Do not make parallel tests race on package feeds, generated output folders, static process outputs, or shared WebApplicationFactory state.
- [ ] Do not run validation without timeout/hang guards.

Test diagram:

| Codepath / behavior | Existing coverage | Required closure |
| --- | --- | --- |
| Production default `AddStandardResilienceHandler()` remains on | Indirect through runtime registration | Add focused guard/assertion or source-level test. |
| Test host disables generated-client retry when explicitly configured | Missing | Add focused test proving `503` response returns quickly and without retry count inflation. |
| `StorefrontV2HostSmokeTests` page semantics | Existing but slow | Keep semantics, remove accidental retry delay. |
| Service-unavailable UI semantics | Existing host smoke tests | Keep enough `503` tests, but run with no backoff in test host. |
| Process helpers | Partial | Add async read + timeout + process-tree kill where missing. |
| Assembly parallelization | Globally disabled | Narrow to collections only after shared-state audit. |

Failure modes registry:

| Failure mode | Impact | Mitigation |
| --- | --- | --- |
| Test opt-out leaks into production | Storefront loses transient fault protection | Default-on config, explicit test-only override, guard test. |
| Retried `503` remains in some generated-client path | Tests stay slow | Add retry-count handler and duration assertions to focused tests. |
| Parallelization causes shared filesystem race | Flaky CI/local tests | Only re-enable after class-level shared resource inventory. |
| Process helper blocks forever | Agent/local run hangs | Add timeout + async stdout/stderr read to every process helper. |
| Full suite still slow after host fix | Hidden slow class remains | Add TRX profiling step and document top offenders. |

### Phase 3.5 - DX Review

Developer journey:

| Stage | Desired experience |
| --- | --- |
| 1. Pick a phase | Developer reads this todo and knows the exact next phase. |
| 2. Edit | Changes are scoped to the named files. |
| 3. Focused test | Each phase has a short timeout-guarded command. |
| 4. Runtime check | Host smoke behavior remains meaningful. |
| 5. Full test | Full suite uses hang guards and writes TRX evidence. |
| 6. Profile | TRX top offenders are compared against baseline. |
| 7. Update docs | QA checklist records before/after evidence. |
| 8. Commit | One commit per phase. |
| 9. Resume | Any agent can continue from unchecked boxes. |

TTHW target:

- Current: full confidence loop is about 14m34s and can hang if a process child blocks.
- Target after closure: focused phase loop under 2 minutes; full Release test run materially below baseline and protected by hang guards.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | CEO | Optimize test runtime now instead of accepting 14m baseline | Mechanical | P6 bias toward action | The suite passes but blocks Storefront phase velocity. | Keep as-is |
| 2 | Eng | Keep production resilience default-on | Mechanical | P1 completeness | Runtime resilience is production behavior, not test-only noise. | Global disable |
| 3 | Eng | Add explicit test-host opt-out for generated-client retry/backoff | Mechanical | P5 explicit over clever | Makes fake `503` deterministic without changing production defaults. | Rewrite all host tests first |
| 4 | Eng | Add process helper timeouts before broader parallelization | Mechanical | P1 completeness | Prevents future 20m hangs independent of performance tuning. | Rely on terminal timeout only |
| 5 | DX | Every verification command must include a hang guard | Mechanical | P5 explicit over clever | Developers can run commands without risking indefinite sessions. | Unbounded `dotnet test` |

## Phase F1.73 - Baseline Profiling And Fast Guard

Goal: lock the current performance evidence and add a repeatable way to compare improvements before changing behavior.

### Implementation

- [x] Add or document a local TRX profiling command for `BlazorShop.Tests.V2`.
  - Added `scripts/qa/measure-test-trx-durations.ps1`.
- [x] Capture class-level and test-level duration buckets from the existing TRX.
  - Existing full-suite TRX: `BlazorShop.Tests.V2/TestResults/storefront-f172-release-final-pass.trx`.
  - Buckets: `<1s` = 1601 tests / 30.9s, `1-10s` = 8 tests / 27.8s, `10-30s` = 29 tests / 582.7s, `>=30s` = 5 tests / 233.1s.
- [x] Record the known baseline in this todo:
  - [x] `StorefrontV2HostSmokeTests`: 58 tests, about `756.2s` in full-suite TRX; focused F1.73 run was 58 tests / `747.2s`.
  - [x] `>=10s` tests: 34 tests, about `815.9s` in full-suite TRX; focused host-smoke run had 32 slow tests / `740.2s`.
  - [x] Full TRX duration: `14m34s`.
- [x] Identify every `StorefrontV2HostSmokeTests` path that uses `ServiceUnavailableHandler`.
  - `StorefrontV2HostSmokeTests.cs` lines 475, 494, 519, 546, 600, 624, 649, 680, 731, 829, 872, and 1159.
- [x] Identify every process-spawn test helper and classify whether it already has timeout protection.
  - `LegacyRemovalGuardrailTests.RunProcess`: no timeout, sync stdout/stderr read.
  - `StorefrontGeneratedClientFoundationTests.RunProcess`: no timeout, sync stdout/stderr read.
  - `CommerceNodeStorefrontOpenApiContractTests.RunProcess`: no timeout, sync stdout/stderr read; outside F1.76 named scope but should be considered for a later Commerce Node contract-test hardening pass.
  - `StorefrontStarterFoundationBoundaryTests.RunProcess`: has 3-minute timeout, async stdout/stderr read, and process-tree kill.

### Tests

- [x] Do not run full suite in this phase unless explicitly needed.
- [x] Run a timeout-guarded focused duration probe:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontV2HostSmokeTests" --logger "trx;LogFileName=storefront-host-smoke-before.trx" --blame-hang --blame-hang-timeout 5m
```

Result: passed 58/58 in `12m27s`; TRX `BlazorShop.Tests.V2/TestResults/storefront-host-smoke-before.trx`.

### Acceptance Criteria

- [x] Baseline evidence is committed in this todo or a linked QA note.
- [x] Slow-test root cause is documented with file names and exact classes.
- [x] No production/runtime code changes in this phase.

### Commit

- [x] Commit message: `F1.73 profile storefront test runtime baseline`

## Phase F1.74 - Test Host Resilience Opt-out

Goal: make fake `503` Storefront host smoke tests deterministic and fast while keeping production `HttpClient` resilience enabled by default.

### Implementation

- [x] Add a narrow configuration gate around default `HttpClient` resilience registration in `BlazorShop.ServiceDefaults/Extensions.cs`.
  - [x] Default value must keep resilience enabled.
  - [x] Test-only config must be explicit, for example `ServiceDefaults:HttpClientResilience:Enabled=false`.
  - [x] Do not disable OpenTelemetry, health checks, or service discovery unless proven necessary.
- [x] Update `StorefrontV2HostSmokeTests.CreateClient(...)` to set the test-only config before the test host builds.
  - Uses `ServiceDefaults__HttpClientResilience__Enabled=false` around `CreateClient()` so `Program.cs` sees the setting before calling `AddServiceDefaults()`.
- [x] Add a counting handler around `ServiceUnavailableHandler` or a dedicated test handler to prove retry attempts are not happening in the opted-out host.
  - Added `ServiceDefaultsHttpClientResilienceTests.AddServiceDefaults_WhenHttpClientResilienceDisabled_DoesNotRetryTransientFailures`.
- [x] Keep at least one source/behavior guard proving production default remains enabled.
  - Added `ServiceDefaultsHttpClientResilienceTests.AddServiceDefaults_KeepsHttpClientResilienceEnabledByDefault`.
- [x] Do not change public Storefront routes, BFF endpoints, or page output expectations.

### Tests

- [x] Run focused host smoke tests with hang guard:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontV2HostSmokeTests" --logger "trx;LogFileName=storefront-host-smoke-f174.trx" --blame-hang --blame-hang-timeout 5m
```

Execution note: after an initial timed-out build/test invocation left MSBuild/testhost child processes behind, the process tree for that invocation was stopped, `dotnet build-server shutdown` was run, the test project was rebuilt, and the host smoke subset was rerun with `--no-build` plus the same hang guard.

Result: passed 58/58 in `14s`; TRX `BlazorShop.Tests.V2/TestResults/storefront-host-smoke-f174.trx`.

- [x] Parse `storefront-host-smoke-f174.trx` and compare slowest tests against baseline.
  - F1.73 focused baseline: 58 tests / `747.2s`, slow tests `>=10s`: 32 / `740.2s`.
  - F1.74 focused result: 58 tests / `14.9s`, slow tests `>=10s`: 0 / `0s`.
- [x] Run focused ServiceDefaults guard tests if added.
  - `dotnet test ... --no-build --filter "FullyQualifiedName~ServiceDefaultsHttpClientResilienceTests" --logger "trx;LogFileName=service-defaults-resilience-f174.trx" --blame-hang --blame-hang-timeout 3m`
  - Result: passed 2/2 in `145ms`.

### Acceptance Criteria

- [x] Slow `ServiceUnavailableHandler` host smoke tests no longer spend `10s+` on retry/backoff.
- [x] Production default resilience remains on.
- [x] No Storefront V2 behavior expectations are weakened.
- [x] Focused tests pass with timeout/hang guard.

### Commit

- [x] Commit message: `F1.74 disable storefront host retry backoff in tests`

## Phase F1.75 - Storefront Host Smoke Fake Cleanup

Goal: stop using `ServiceUnavailableHandler` as a generic "do not call real Commerce Node" fallback where the test does not assert service-unavailable behavior.

### Implementation

- [ ] Review all `new ServiceUnavailableHandler()` usages in `StorefrontV2HostSmokeTests`.
- [ ] Split them into:
  - [ ] Tests that explicitly validate `503`/service-unavailable UI.
  - [ ] Tests that only need deterministic page data or no real network.
- [ ] Replace non-`503` usages with explicit fake clients/facades or minimal success handlers.
- [ ] Keep `503` tests narrow and named so future agents understand they intentionally exercise unavailable-dependency semantics.
- [ ] Avoid broad helper abstractions unless duplication remains after the split.

### Tests

- [ ] Run focused host smoke tests with hang guard:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~StorefrontV2HostSmokeTests" --logger "trx;LogFileName=storefront-host-smoke-f175.trx" --blame-hang --blame-hang-timeout 5m
```

- [ ] Parse the TRX and list any remaining `>=10s` Storefront host tests.

### Acceptance Criteria

- [ ] Tests that do not assert service-unavailable behavior no longer depend on `503` fake responses.
- [ ] Service-unavailable coverage remains explicit.
- [ ] Host smoke test runtime is lower than F1.74 or unchanged for a clearly documented reason.

### Commit

- [ ] Commit message: `F1.75 clarify storefront host smoke fakes`

## Phase F1.76 - Process Helper Timeout Closure

Goal: prevent local/CI hangs from child processes in architecture/package tests.

### Implementation

- [ ] Update `LegacyRemovalGuardrailTests.RunProcess` to:
  - [ ] Read stdout asynchronously.
  - [ ] Read stderr asynchronously.
  - [ ] Use an explicit per-process timeout.
  - [ ] Kill the entire process tree on timeout.
  - [ ] Return failure output that includes command, arguments, working directory, timeout, stdout, and stderr.
- [ ] Update `StorefrontGeneratedClientFoundationTests.RunProcess` with the same protections.
- [ ] Compare with `StorefrontStarterFoundationBoundaryTests.RunProcess` and keep behavior consistent.
- [ ] Do not change what the guardrail scripts assert.

### Tests

- [ ] Run timeout-guarded process test subset:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~LegacyRemovalGuardrailTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontStarterFoundationBoundaryTests" --logger "trx;LogFileName=process-helper-f176.trx" --blame-hang --blame-hang-timeout 6m
```

### Acceptance Criteria

- [ ] No process helper in the touched classes uses sync `ReadToEnd()` followed by unbounded `WaitForExit()`.
- [ ] Timeout failure messages are actionable.
- [ ] Focused process tests pass.

### Commit

- [ ] Commit message: `F1.76 add timeouts to storefront process guardrails`

## Phase F1.77 - Parallelization Boundary Review

Goal: reduce unnecessary serial execution without making WebApplicationFactory, package, filesystem, or process tests flaky.

### Implementation

- [ ] Inventory test classes that require serial execution:
  - [ ] Shared package feed/output folders.
  - [ ] Generated source/output folders.
  - [ ] WebApplicationFactory tests sharing mutable host state.
  - [ ] External process tests.
- [ ] Replace assembly-wide `DisableTestParallelization = true` only if safe.
- [ ] Prefer xUnit collection-level serialization for unsafe groups.
- [ ] Allow pure unit/source/static guardrail tests to run in parallel.
- [ ] Update `docs/architecture/07-deployment-and-local-run.md` if the active V2 test behavior changes.

### Tests

- [ ] Run focused architecture and Storefront tests with hang guard:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --filter "FullyQualifiedName~Architecture|FullyQualifiedName~PresentationV2.Storefront" --logger "trx;LogFileName=parallel-boundary-f177.trx" --blame-hang --blame-hang-timeout 10m
```

- [ ] If parallelization changes, run the same focused command twice to catch race flakiness.

### Acceptance Criteria

- [ ] Serial-only tests are isolated by collection or equivalent mechanism.
- [ ] Pure tests are no longer blocked by global serial execution, if safe.
- [ ] No file/package output races occur in repeated focused runs.

### Commit

- [ ] Commit message: `F1.77 narrow V2 test parallelization boundary`

## Phase F1.78 - Full Release Test Performance Closure

Goal: prove the suite still passes and record the new performance baseline.

### Implementation

- [ ] Update this todo with final timings.
- [ ] Update `QA-StorefrontV2.todo.md` with the performance closure evidence.
- [ ] If any architecture doc changed, ensure `docs/architecture/07-deployment-and-local-run.md` still reflects active V2 test behavior.
- [ ] Document any remaining slow test class with a reason and owner.

### Tests

- [ ] Run full V2 test suite with hang guard:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-restore --logger "trx;LogFileName=storefront-test-runtime-f178-final.trx" --blame-hang --blame-hang-timeout 20m
```

- [ ] Parse final TRX:
  - [ ] Total duration.
  - [ ] Top 15 tests by duration.
  - [ ] Class duration totals.
  - [ ] Count and sum of tests `>=10s`.
- [ ] Compare final numbers against baseline.

### Acceptance Criteria

- [ ] Full suite passes: `0 failed`.
- [ ] No test command was run without a hang guard.
- [ ] `StorefrontV2HostSmokeTests` runtime is materially lower than `756.2s`, or remaining cost is documented test-by-test.
- [ ] No unbounded process helper remains in the affected tests.
- [ ] QA evidence is recorded.

### Commit

- [ ] Commit message: `F1.78 close storefront test runtime performance`

## Final Verification Checklist

- [ ] `dotnet build BlazorShop.sln -c Release --no-restore`
- [ ] Focused Storefront host smoke test TRX generated with hang guard.
- [ ] Focused process helper TRX generated with hang guard.
- [ ] Full `BlazorShop.Tests.V2` Release TRX generated with hang guard.
- [ ] TRX duration comparison recorded.
- [ ] `QA-StorefrontV2.todo.md` updated.
- [ ] One commit per phase, in order.

## Resume Notes

Start at the first unchecked phase. Do not skip phases. Do not run `dotnet test` without `--blame-hang --blame-hang-timeout` and an external tool timeout.
