# Storefront OpenAPI Generated Client Hardening.todo

Status: in progress
Source: autoplan after repository review of Storefront OpenAPI snapshot ownership, generated-client determinism, generated-client base URL behavior, and `CreatedAfterUtc` query semantics.

## Goal

Make Storefront OpenAPI and `BlazorShop.Storefront.Client` production-owned platform assets instead of test-owned artifacts.

The intended result:

- `contracts/storefront/storefront.openapi.json` is the canonical committed Storefront frontend/client OpenAPI contract.
- `BlazorShop.Storefront.Client` is generated from the canonical contract, not from a test-project snapshot path.
- Regeneration is deterministic and has a release gate that fails on generated-source drift.
- Generated client base URL behavior has one source of truth.
- `CreatedAfterUtc` is handled as a real contract/date serialization issue, not hidden by page-level behavior forever.

## Current Verified Context

- [x] Storefront frontend/client Swagger is exposed by Commerce Node at `/swagger/storefront/swagger.json`.
- [x] Storefront provider callback/webhook Swagger is separated at `/swagger/storefront-provider/swagger.json`.
- [x] `CommerceNodeStorefrontOpenApiContractTests` validates OpenAPI parsing, metadata, security, schemas, snapshots, breaking-change guardrails, and generator safety.
- [x] Current full Storefront OpenAPI snapshot is stored under `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json`.
- [x] Current path snapshot is stored under `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/storefront-openapi.paths.snapshot.txt`.
- [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json` reads from the test-project snapshot path.
- [x] `scripts/generate-storefront-client.ps1` restores dotnet tools and runs NSwag config.
- [x] `StorefrontGeneratedClientFoundationTests.StorefrontClientGeneration_IsPinnedAndDeterministic` checks pinned NSwag config and generated-source markers, but it does not run regeneration and compare git diff.
- [x] `BlazorShop.Storefront.Runtime` configures generated-client `HttpClient.BaseAddress` from `StorefrontRuntimeOptions.CommerceNodeBaseUrl`.
- [x] Generated NSwag clients currently still generate a `BaseUrl` property and constructor argument because `useBaseUrl` and `generateBaseUrlProperty` are true.
- [x] Runtime creates generated clients with `string.Empty` as the generated `baseUrl` constructor argument.
- [x] `CreatedAfterUtc` exists in Domain/Application query flow and Commerce Node Storefront API contract.
- [x] `CreatedAfterUtc` is used by Commerce Node repository filtering against product `CreatedOn`.
- [x] NSwag generated query serialization currently emits `CreatedAfterUtc` with `ToString("s", InvariantCulture)`, which drops offset/UTC marker.
- [x] Storefront V2 `NewReleases.razor` currently sends only `SortBy = ProductCatalogSortBy.Newest`.
- [x] A guardrail test currently asserts `NewReleases.razor` does not send `CreatedAfterUtc`.

## Non-goals

- [x] Do not redesign Storefront V2 UI.
- [x] Do not change catalog business semantics in the same phase as moving contract ownership.
- [x] Do not remove Commerce Node OpenAPI runtime tests.
- [x] Do not make test project the source of truth for production package generation.
- [x] Do not hand-edit `Generated/StorefrontClient.g.cs` except through generation output review.
- [x] Do not move checkout/cart/payment business logic into `Storefront.Client` or `Storefront.Runtime`.
- [x] Do not add backend/core/API references to `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, `Storefront.Starter`, or generated storefronts.
- [x] Do not restore `CreatedAfterUtc` usage in New Releases until date query serialization is proven safe.

## Target Ownership

```text
CommerceNode API runtime
  /swagger/storefront/swagger.json
      validates that the runtime can produce the Storefront frontend/client contract

contracts/storefront/storefront.openapi.json
      canonical committed Storefront frontend/client contract
      production input to NSwag
      source of truth for generated Storefront client package

BlazorShop.Storefront.Client
      generated from contracts/storefront/storefront.openapi.json
      owns generated transport/contracts only

BlazorShop.Tests.V2
      verifies CommerceNode runtime OpenAPI matches canonical contract
      verifies breaking-change compatibility
      verifies generated client builds and does not drift
```

## Phase Dependency Map

```text
OCH0 Baseline and ownership lock
  -> OCH1 Canonical contract file
      -> OCH2 Contract tests target canonical contract
          -> OCH3 Generator reads canonical contract
              -> OCH4 Deterministic regeneration gate
                  -> OCH5 Single base URL source
                      -> OCH6 CreatedAfterUtc contract issue
                          -> OCH7 Docs and QA release gate
```

## Phase OCH0 - Baseline and Ownership Lock

Goal: freeze current behavior before moving files or generator inputs.

### Tasks

- [x] Record current generated-client inputs:
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json`
  - [x] `scripts/generate-storefront-client.ps1`
  - [x] `.config/dotnet-tools.json`
  - [x] `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated/StorefrontClient.g.cs`
- [x] Record current contract snapshots:
  - [x] `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json`
  - [x] `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/storefront-openapi.paths.snapshot.txt`
- [x] Record current tests that must remain valid:
  - [x] `CommerceNodeStorefrontOpenApiContractTests`
  - [x] `StorefrontGeneratedClientFoundationTests`
  - [x] `StorefrontGeneratedCatalogContentClientTests`
  - [x] `StorefrontGeneratedConfigurationClientTests`
  - [x] `StorefrontSharedPlatformPackageContractTests`
- [x] Capture current known issue:
  - [x] generated `CreatedAfterUtc` query uses `"s"` format.
  - [x] `NewReleases.razor` avoids `CreatedAfterUtc`.
- [x] Confirm there are no unintentional generator output changes before starting:

```powershell
git diff -- BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated
```

### Done When

- [x] Baseline current files are known.
- [x] The migration can distinguish intentional generator changes from drift.
- [x] No behavior change has been made.

## Phase OCH1 - Introduce Canonical Storefront Contract File

Goal: create the neutral production-owned OpenAPI contract location.

### Tasks

- [ ] Create folder:

```text
contracts/storefront/
```

- [ ] Add canonical contract file:

```text
contracts/storefront/storefront.openapi.json
```

- [ ] Copy the current frontend/client Storefront OpenAPI JSON from the existing full snapshot as the initial canonical baseline.
- [ ] Keep JSON formatting stable and deterministic.
- [ ] Decide the path snapshot ownership:
  - [ ] Keep `storefront-openapi.paths.snapshot.txt` under test snapshots for test readability.
  - [ ] Do not use path snapshot as generator input.
- [ ] Add a short `contracts/storefront/README.md` if needed to state:
  - [ ] Commerce Node runtime produces the API document.
  - [ ] canonical committed contract is used for package generation.
  - [ ] test snapshots are guardrails, not production input.

### Files Likely Touched

- `contracts/storefront/storefront.openapi.json`
- `contracts/storefront/README.md`

### Verification

```powershell
Test-Path contracts/storefront/storefront.openapi.json
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
```

### Done When

- [ ] Canonical contract file exists outside the test project.
- [ ] It matches current Storefront frontend/client OpenAPI baseline.
- [ ] Existing contract tests still pass before they are retargeted.

## Phase OCH2 - Retarget Contract Tests to Canonical Ownership

Goal: make tests prove runtime OpenAPI matches the canonical contract without letting tests own the generator input.

### Tasks

- [ ] Update `CommerceNodeStorefrontOpenApiContractTests` constants:
  - [ ] Add repository-root resolution for `contracts/storefront/storefront.openapi.json`.
  - [ ] Use canonical contract for full-document baseline comparison.
  - [ ] Keep path snapshot comparison under test snapshots if it remains useful.
- [ ] Update `BlazorShop.Tests.V2.csproj`:
  - [ ] Stop relying on copying full OpenAPI JSON snapshot from test project if canonical contract is read from repo root.
  - [ ] Keep path snapshot copy only if test still reads it through output directory.
- [ ] Preserve breaking-change guard behavior:
  - [ ] removed paths fail.
  - [ ] removed operation IDs fail.
  - [ ] removed schemas/properties fail.
  - [ ] property type changes fail.
  - [ ] optional-to-required changes fail.
  - [ ] enum value removals fail.
  - [ ] response status removals fail.
  - [ ] security scheme removals/changes fail.
- [ ] Rename variables/test messages from `SwaggerSnapshotPath` to `CanonicalStorefrontContractPath` where applicable.
- [ ] Update tests that assert old snapshot ownership:
  - [ ] `V2ProductionReadinessTests` currently expects test snapshot copy entries.
  - [ ] `StorefrontGeneratedClientFoundationTests` currently expects `"storefront-openapi.snapshot.json"` in NSwag config.
- [ ] Keep old test snapshot file temporarily until all references are removed or explicitly converted.

### Files Likely Touched

- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj`
- `BlazorShop.Tests.V2/Architecture/V2ProductionReadinessTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/Snapshots/storefront-openapi.snapshot.json`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~V2ProductionReadinessTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests"
```

### Done When

- [ ] Runtime Storefront Swagger is compared against `contracts/storefront/storefront.openapi.json`.
- [ ] No production package generation path points into `BlazorShop.Tests.V2`.
- [ ] Test snapshots remain guardrails only.

## Phase OCH3 - Retarget NSwag Generator to Canonical Contract

Goal: make `BlazorShop.Storefront.Client` generation consume the production-owned canonical contract.

### Tasks

- [ ] Update `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json`:
  - [ ] `documentGenerator.fromDocument.url` points to `../../contracts/storefront/storefront.openapi.json` or another stable relative path from the config file.
  - [ ] Keep pinned namespace `BlazorShop.Storefront.Client`.
  - [ ] Keep output `Generated/StorefrontClient.g.cs`.
  - [ ] Keep pinned NSwag runtime and options unless a later phase changes base URL.
- [ ] Update `scripts/generate-storefront-client.ps1`:
  - [ ] Keep default config path.
  - [ ] Validate canonical contract file exists before running NSwag.
  - [ ] Emit clear error message with missing path and remediation.
  - [ ] Keep `dotnet tool restore`.
  - [ ] Keep working directory at repo root.
- [ ] Update tests:
  - [ ] assert config references `contracts/storefront/storefront.openapi.json`.
  - [ ] assert config does not reference `BlazorShop.Tests.V2`.
  - [ ] assert script validates missing canonical contract path.
- [ ] Run generator once and inspect diff:
  - [ ] No generated source diff expected in this phase unless relative input path affects generated comments only.
  - [ ] Any generated diff must be reviewed line-by-line.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json`
- `scripts/generate-storefront-client.ps1`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`

### Verification

```powershell
./scripts/generate-storefront-client.ps1
git diff --exit-code -- BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
```

### Done When

- [ ] Generator input is canonical contract file.
- [ ] Generator config has no test-project path.
- [ ] Generated client still builds.

## Phase OCH4 - Add Deterministic Regeneration Gate

Goal: prove generation is deterministic by running generator and failing on generated-source drift.

### Tasks

- [ ] Add a release-gate script or test helper:

```text
scripts/qa/run-storefront-client-regeneration-gate.ps1
```

- [ ] Gate steps:
  - [ ] run `./scripts/generate-storefront-client.ps1`.
  - [ ] run `git diff --exit-code -- BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated`.
  - [ ] fail with a clear message if generated files drift.
  - [ ] tell developer to review and commit regenerated source when contract changes are intentional.
- [ ] Add optional contract diff check:
  - [ ] `git diff --exit-code -- contracts/storefront/storefront.openapi.json`.
  - [ ] make this optional if the gate is run immediately after intentionally refreshing the canonical contract.
- [ ] Update `StorefrontGeneratedClientFoundationTests`:
  - [ ] Keep fast string checks.
  - [ ] Add a test that the QA gate script exists and contains the generator and git diff commands.
  - [ ] Do not run expensive git/NSwag gate inside normal unit tests unless CI can support it reliably.
- [ ] Add CI/release checklist instruction:
  - [ ] run the regeneration gate before publishing `BlazorShop.Storefront.Client`.
  - [ ] run package consumer build after regeneration.

### Files Likely Touched

- `scripts/qa/run-storefront-client-regeneration-gate.ps1`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`
- `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### Verification

```powershell
./scripts/qa/run-storefront-client-regeneration-gate.ps1
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClientFoundationTests"
```

### Done When

- [ ] Generated-client drift is caught by a command intended for release/CI.
- [ ] Normal test suite still has a fast guard that the gate exists.
- [ ] Drift failure message explains the cause and fix.

## Phase OCH5 - Normalize Generated Client Base URL Behavior

Goal: choose one base URL source and remove implicit dual configuration.

Decision: use `HttpClient.BaseAddress` as the single source for Storefront generated clients.

### Rationale

- `Storefront.Runtime` already configures the generated-client `HttpClient.BaseAddress`.
- Storefront V2, Starter, and generated storefronts should configure Commerce Node base URL through Runtime options.
- Passing `string.Empty` into every generated client works but makes the runtime contract hard to understand.
- Keeping a separate generated `BaseUrl` property invites per-client divergence.

### Tasks

- [ ] Update `nswag.storefront.client.json`:
  - [ ] set `useBaseUrl` to `false` if NSwag supports relative URL generation with injected `HttpClient`.
  - [ ] set `generateBaseUrlProperty` to `false`.
  - [ ] keep `injectHttpClient` true.
  - [ ] keep `disposeHttpClient` false.
- [ ] Run `scripts/generate-storefront-client.ps1`.
- [ ] Review generated constructor signatures:
  - [ ] generated clients should no longer require a `string baseUrl` constructor parameter.
  - [ ] generated clients should use request URLs relative to `HttpClient.BaseAddress`.
- [ ] Update `StorefrontRuntimeServiceCollectionExtensions.CreateClient<TClient>`:
  - [ ] instantiate generated clients with only `HttpClient` if constructor changes.
  - [ ] remove `string.Empty` base URL usage.
  - [ ] keep named HttpClient registration.
- [ ] Update independent package consumer test:
  - [ ] construct a generated client with `HttpClient { BaseAddress = new Uri("https://example.invalid/") }`.
  - [ ] do not pass a base URL string.
- [ ] Update Runtime tests:
  - [ ] assert generated client registration uses `HttpClient.BaseAddress`.
  - [ ] assert no generated `BaseUrl` property is present if config supports it.
- [ ] Update any docs that mention generated client constructor shape.
- [ ] If NSwag cannot remove `BaseUrl` cleanly:
  - [ ] keep generated constructor but create a Runtime factory wrapper with a named helper method.
  - [ ] add a guard explaining why base URL must be empty.
  - [ ] mark this as a temporary compatibility exception.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated/StorefrontClient.g.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`

### Verification

```powershell
./scripts/generate-storefront-client.ps1
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"
./scripts/qa/run-storefront-client-regeneration-gate.ps1
```

### Done When

- [ ] Generated clients have one base URL source.
- [ ] Runtime generated-client registration is simpler and explicit.
- [ ] Starter/package consumer still builds from the generated package.

## Phase OCH6 - Resolve `CreatedAfterUtc` Query Contract

Goal: fix the date query contract before using it again in Storefront pages.

### Problem Statement

`CreatedAfterUtc` is not dead code. It is part of catalog query semantics and is used by backend filtering. However, the generated client currently serializes it without offset/UTC information. Storefront V2 avoids sending it from New Releases, which prevents runtime bugs but changes "new releases" from "created within a recent window" into "newest sort only".

### Tasks

- [ ] Add a focused issue note in this plan or a dedicated small todo section:
  - [ ] OpenAPI nullable date query shape.
  - [ ] `DateTime` versus `DateTimeOffset` at Commerce Node contract boundary.
  - [ ] NSwag optional query generation behavior.
  - [ ] timezone/UTC serialization format.
  - [ ] ASP.NET Core model binding behavior for offset date query strings.
- [ ] Decide target public API type:
  - [ ] Prefer `DateTimeOffset?` for Storefront API query contract if offset matters.
  - [ ] Map to Domain `DateTime? CreatedAfterUtc` only after converting to UTC.
  - [ ] Keep Domain type unchanged initially if changing it would broaden scope.
- [ ] Add Commerce Node contract test:
  - [ ] OpenAPI parameter `CreatedAfterUtc` has `type: string`.
  - [ ] OpenAPI parameter has expected date/date-time format.
  - [ ] parameter remains optional/nullable.
- [ ] Add generated-client test:
  - [ ] generated source serializes `CreatedAfterUtc` with offset/round-trip-safe format, or
  - [ ] generated source delegates conversion through a safe generated helper.
- [ ] Add API behavior test:
  - [ ] query with UTC/offset value filters products by `CreatedOn`.
  - [ ] query without value keeps current newest sort behavior.
- [ ] Only after contract is safe, update `NewReleases.razor` if business wants recent-window semantics:
  - [ ] choose a window setting or constant, for example 7/14/30 days.
  - [ ] avoid hardcoding arbitrary business policy if there is no product decision.
  - [ ] update SEO text if the page becomes a real recent-window page.
- [ ] Update or remove guard test:
  - [ ] replace `NewReleases_DoesNotSendGeneratedClientUnsafeCreatedAfterUtcQuery` with a positive test only after safe serialization is proven.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CatalogContracts.cs`
- `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CatalogMappings.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/nswag.storefront.client.json`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Client/Generated/StorefrontClient.g.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/NewReleases.razor`
- `BlazorShop.Tests.V2/PresentationV2/CommerceNode/CommerceNodeStorefrontOpenApiContractTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontComponentsHeadlessPresentationRefactorTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`
- `BlazorShop.Tests.V2/Infrastructure/CommerceNode/*`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
./scripts/qa/run-storefront-client-regeneration-gate.ps1
```

### Done When

- [ ] `CreatedAfterUtc` has a generator-safe public contract.
- [ ] Backend filtering remains covered.
- [ ] New Releases either intentionally keeps newest-sort semantics or safely restores recent-window filtering.
- [ ] No test locks in avoidance as the long-term fix.

## Phase OCH7 - Documentation, QA, and Release Gate Integration

Goal: make the new ownership and release commands discoverable.

### Tasks

- [ ] Update `docs/architecture/09-api-contract-standards.md`:
  - [ ] canonical Storefront contract lives under `contracts/storefront/storefront.openapi.json`.
  - [ ] Commerce Node runtime contract tests compare live Swagger to canonical contract.
  - [ ] generated client drift gate is required before package release.
- [ ] Update `docs/architecture/10-v2-contract-ownership.md`:
  - [ ] clarify `Storefront.Client` is generated from canonical contract, not test snapshot.
  - [ ] clarify test snapshots are breaking-change guardrails only.
- [ ] Update `docs/architecture/05-project-and-folder-guide.md`:
  - [ ] replace "generated from Commerce Node Storefront OpenAPI snapshot" wording with canonical contract wording.
  - [ ] document Runtime base URL ownership.
- [ ] Update StorefrontBuilder docs if they mention generated client source:
  - [ ] `docs/architecture/11-storefront-builder.md`
  - [ ] `docs/agents/storefront-builder.md`
  - [ ] `docs/visual-reverse-engineering-skill/*`
- [ ] Update QA checklists:
  - [ ] `QA-CommerceNode.todo.md` includes canonical contract verification.
  - [ ] `QA-StorefrontV2.todo.md` includes generated-client regeneration gate before release.
  - [ ] StorefrontBuilder QA includes package/generation guard when generated storefronts consume `Storefront.Client`.
- [ ] Add a release checklist section:
  - [ ] fetch/run Commerce Node Storefront Swagger contract tests.
  - [ ] compare runtime OpenAPI to canonical.
  - [ ] run `./scripts/qa/run-storefront-client-regeneration-gate.ps1`.
  - [ ] build `Storefront.Client`.
  - [ ] run package consumer proof.
  - [ ] run Storefront V2 focused tests.

### Files Likely Touched

- `docs/architecture/09-api-contract-standards.md`
- `docs/architecture/10-v2-contract-ownership.md`
- `docs/architecture/05-project-and-folder-guide.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/agents/storefront-builder.md`
- `docs/visual-reverse-engineering-skill/*`
- `docs/refactor-control-Commerce-storefront/QA-CommerceNode.todo.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### Verification

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Architecture|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests"
./scripts/qa/run-storefront-client-regeneration-gate.ps1
```

### Done When

- [ ] Architecture docs match implementation.
- [ ] QA files list the new release gates.
- [ ] Future agents do not infer test snapshots are production contract owners.

## Final Release Verification

Run after all implementation phases:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~CommerceNodeStorefrontOpenApiContractTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests|FullyQualifiedName~StorefrontComponentsHeadlessPresentationRefactorTests"
./scripts/qa/run-storefront-client-regeneration-gate.ps1
```

If browser-visible New Releases behavior changes in OCH6:

```powershell
./scripts/run-v2-local.ps1 -StopExisting
# Run the matching Storefront V2 Playwright release cases for catalog/new releases.
```

## Completion Checklist

- [ ] `contracts/storefront/storefront.openapi.json` exists and is canonical.
- [ ] NSwag config no longer points into `BlazorShop.Tests.V2`.
- [ ] Commerce Node contract tests compare runtime Swagger against canonical contract.
- [ ] Generated client regeneration gate exists.
- [ ] Generated client regeneration gate fails on `Generated/StorefrontClient.g.cs` drift.
- [ ] Generated clients use one base URL source.
- [ ] Runtime registration no longer depends on a confusing empty generated base URL when NSwag supports removing it.
- [ ] `CreatedAfterUtc` has a tracked contract fix path.
- [ ] New Releases semantics are intentionally chosen after the date contract fix.
- [ ] Architecture docs and QA checklists are updated.
- [ ] Storefront.Client package consumer proof still passes.

## Autoplan Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Move canonical Storefront OpenAPI to `contracts/storefront/storefront.openapi.json` | Auto-decided | API contracts need clear ownership | Test project should validate contracts, not own production generator inputs | Keep generating from `BlazorShop.Tests.V2` snapshot |
| 2 | Keep test snapshots as guardrails only | Auto-decided | Smallest safe migration | Existing breaking-change tests are valuable and should not be removed while changing ownership | Delete all snapshots immediately |
| 3 | Add regeneration gate as script/CI release gate instead of only xUnit runtime test | Auto-decided | Fast local tests plus strong release gate | Running NSwag and git diff in every normal test is heavier than a release gate, but the gate must be explicit and tested for existence | Only string-check generated files |
| 4 | Use `HttpClient.BaseAddress` as the single generated-client base URL source | Auto-decided | Runtime owns Storefront API endpoint configuration | Runtime already centralizes Commerce Node base URL and supports V2/Starter/generated storefront consumers | Pass base URL into every generated client |
| 5 | Treat `CreatedAfterUtc` as a separate contract/date serialization phase | Auto-decided | Do not mix behavior change with contract source migration | Backend filter is real, but current generated serialization is unsafe; fix the contract before changing New Releases behavior | Permanently avoid `CreatedAfterUtc` in Storefront pages |
