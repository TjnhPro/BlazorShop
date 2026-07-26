# Storefront Runtime Hardening.todo

Status: planned

Source: autoplan after repository review of `BlazorShop.Storefront.Runtime` generated-client registration, envelope mapping, cancellation handling, runtime error primitives, facade scope, capability registration, and server-only boundary.

## Goal

Harden `BlazorShop.Storefront.Runtime` so it remains a small server/BFF integration package with compile-time-safe generated-client usage, typed response mapping, correct cancellation behavior, neutral error primitives, and capability-scoped registration.

The intended result:

- Runtime generated-client registration no longer depends on `Activator.CreateInstance`.
- Runtime facades no longer inspect generated envelopes with reflection.
- Runtime facades no longer serialize generated DTOs back into JSON just to project them.
- Caller cancellation is propagated, while real network timeout is mapped to a retryable runtime error.
- Runtime returns stable error primitives for storefront hosts to localize/render.
- Catalog/content/navigation/SEO runtime operations are split by capability.
- Storefront V2, Starter, and future `Storefront.{Name}` consumers can opt into only the runtime capabilities they need.
- `Storefront.V2.WASM` remains browser-only and cannot reference `Storefront.Runtime`.

## Current Verified Context

- [x] `docs/architecture/05-project-and-folder-guide.md` defines `BlazorShop.Storefront.Runtime` as the active minimal runtime package for store context/options, Storefront API client registration helpers, capability/configuration readers, normalized error mapping primitives, and BFF-safe result mapping primitives.
- [x] Runtime must not own Storefront V2 layout/design, CSS/assets, store-specific composition, backend business rules, provider secrets, or backend/core/API project references.
- [x] `docs/architecture/10-v2-contract-ownership.md` defines generated Storefront clients as frontend-readable contracts generated from Commerce Node Storefront OpenAPI.
- [x] `BlazorShop.Storefront.Runtime.csproj` references only `BlazorShop.Storefront.Client` plus Microsoft extension packages.
- [x] `BlazorShop.Storefront.V2.WASM.csproj` currently references `BlazorShop.Storefront.Components`, not `BlazorShop.Storefront.Runtime`.
- [x] `StorefrontRuntimeServiceCollectionExtensions.cs` registers a named generated-client `HttpClient` and sets `HttpClient.BaseAddress` from `StorefrontRuntimeOptions.CommerceNodeBaseUrl`.
- [x] `StorefrontRuntimeServiceCollectionExtensions.cs` registers all generated clients and all runtime facades in `AddStorefrontServerGeneratedClients`.
- [x] `StorefrontRuntimeServiceCollectionExtensions.cs` creates generated clients through `Activator.CreateInstance(typeof(TClient), string.Empty, httpClient)`.
- [x] `StorefrontRuntimeCartFacade.cs`, `StorefrontRuntimeAddressFacade.cs`, `StorefrontRuntimeCheckoutFacade.cs`, `StorefrontRuntimeConsentFacade.cs`, `StorefrontRuntimePaymentFacade.cs`, and `StorefrontRuntimeCatalogContentFacade.cs` read generated envelope properties with `GetProperty("Success")`, `GetProperty("Data")`, and sometimes `GetProperty("Message")`.
- [x] `StorefrontRuntimeCatalogContentFacade.cs` serializes generated DTO objects and deserializes them into target types through `JsonSerializer.Serialize` and `JsonSerializer.Deserialize`.
- [x] Runtime catch filters use `catch (Exception exception) when (exception is not OperationCanceledException || exception is TaskCanceledException)` in multiple facades and `StorefrontRuntimeExecution`.
- [x] `StorefrontRuntimeErrorMapper` maps `TaskCanceledException` to `network.timeout` without knowing whether the caller cancellation token was requested.
- [x] `StorefrontRuntimeError` currently exposes `Status`, `Code`, `Message`, `TraceId`, and `FieldErrors`; there is no `Retryable` primitive.
- [x] Runtime currently contains technical/user-facing English fallback messages such as authentication required, forbidden, not found, conflict, validation, service unavailable, and network timeout.
- [x] `IStorefrontRuntimeCatalogContentFacade` currently groups catalog, product, page/content, navigation, SEO settings, and redirect resolution behind one interface.
- [x] `Storefront.V2` uses `AddStorefrontRuntime` plus `AddStorefrontServerGeneratedClients`.
- [x] `Storefront.Starter` uses `AddStorefrontRuntime` plus `AddStorefrontGeneratedClients`.
- [x] `StorefrontSharedPlatformPackageContractTests` already checks Runtime/Core separation and that Runtime references only Client.
- [x] `StorefrontRuntimeResultPrimitiveTests` already covers runtime error mapper basics, store key execution, submit execution, and Runtime package boundary.
- [x] `StorefrontV2WASMRuntimeFoundationTests` already verifies WASM same-origin client behavior and absence of Commerce Node config in WASM startup, but it does not yet block a future WASM project reference to Runtime.
- [x] Existing dirty working tree contains OpenAPI/generated-client hardening changes in:
  - [x] `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs`
  - [x] `docs/refactor-control-Commerce-storefront/Storefront OpenAPI Generated Client Hardening.todo.md`
  - [x] `scripts/generate-storefront-client.ps1`

## Relationship To Existing Plans

- [x] This plan complements `Storefront OpenAPI Generated Client Hardening.todo.md`.
- [x] The OpenAPI plan owns canonical contract location, deterministic regeneration, generated source drift gate, generated client base URL behavior, and `CreatedAfterUtc` query serialization.
- [x] This Runtime plan owns DI registration shape, facade mapping safety, cancellation behavior, runtime error primitives, facade splitting, capability registration, and server-only guardrails.
- [x] If generated client constructor signatures change under the OpenAPI plan, this Runtime plan must adapt typed factories in the same or immediately following phase.

## Non-goals

- [x] Do not change Commerce Node Storefront API behavior in this plan.
- [x] Do not redesign Storefront V2 UI.
- [x] Do not move checkout/cart/order/payment business rules into Runtime.
- [x] Do not add backend/core/API project references to Runtime.
- [x] Do not add generated Storefront API clients to WASM/browser.
- [x] Do not split Runtime into multiple NuGet packages in this phase.
- [x] Do not remove existing convenience registration methods before V2 and Starter have a compatibility path.
- [x] Do not remove `Message` from `StorefrontRuntimeError` in one breaking step.
- [x] Do not hand-edit `BlazorShop.Storefront.Client/Generated/StorefrontClient.g.cs` except through generated-client regeneration phases.
- [x] Do not mix visual component refactor with Runtime hardening.

## Target Runtime Shape

```text
Storefront.V2 / Starter / Storefront.{Name}
  -> Storefront.Runtime
      -> Storefront.Client generated clients
          -> Commerce Node Storefront API over HTTP

Storefront.V2.WASM
  -> Storefront.Components
  -> same-origin BFF endpoints only
  -/-> Storefront.Runtime
  -/-> Storefront.Client generated Commerce Node clients
  -/-> CommerceNodeBaseUrl
```

```text
Runtime registration

AddStorefrontRuntime(options)
  -> core options/context/error/result primitives only

AddStorefrontCatalogRuntime()
AddStorefrontContentRuntime()
AddStorefrontNavigationRuntime()
AddStorefrontSeoRuntime()
AddStorefrontCartRuntime()
AddStorefrontCheckoutRuntime()
AddStorefrontAccountRuntime()
AddStorefrontConfigurationRuntime()
AddStorefrontPaymentRuntime()
AddStorefrontConsentRuntime()
AddStorefrontAddressRuntime()

AddStorefrontPlatformRuntime()
  -> convenience method that calls all capability registrations

AddStorefrontServerGeneratedClients()
AddStorefrontGeneratedClients()
  -> compatibility wrappers during migration
```

## Phase Dependency Map

```text
SRH0 Baseline and conflict check
  -> SRH1 Guardrails and characterization tests
      -> SRH2 Typed generated-client factories
          -> SRH3 Cancellation handling normalization
              -> SRH4 Typed envelope execution and no JSON projection
                  -> SRH5 Runtime error primitive hardening
                      -> SRH6 Catalog/content/navigation/SEO facade split
                          -> SRH7 Capability registration split
                              -> SRH8 V2/Starter adoption and docs
                                  -> SRH9 QA and release gate
```

## Phase SRH0 - Baseline And Conflict Check

Status: completed in commit pending.

Goal: freeze current Runtime behavior and avoid mixing this refactor with existing OpenAPI/generated-client work.

### Tasks

- [x] Record current git status before implementation.
- [x] Confirm existing dirty files are unrelated to this Runtime plan or intentionally part of the OpenAPI hardening plan.
- [x] Record current Runtime file inventory:
  - [x] `StorefrontRuntimeServiceCollectionExtensions.cs`
  - [x] `StorefrontRuntimeExecution.cs`
  - [x] `StorefrontRuntimeError.cs`
  - [x] `StorefrontRuntimeResult.cs`
  - [x] `StorefrontRuntimeContext.cs`
  - [x] `StorefrontRuntimeOptions.cs`
  - [x] `StorefrontRuntimeCatalogContentFacade.cs`
  - [x] `StorefrontRuntimeCartFacade.cs`
  - [x] `StorefrontRuntimeCheckoutFacade.cs`
  - [x] `StorefrontRuntimeConfigurationFacade.cs`
  - [x] `StorefrontRuntimeAddressFacade.cs`
  - [x] `StorefrontRuntimeConsentFacade.cs`
  - [x] `StorefrontRuntimePaymentFacade.cs`
- [x] Record all Runtime reflection envelope usages:
  - [x] `GetProperty("Success")`
  - [x] `GetProperty("Data")`
  - [x] `GetProperty("Message")`
- [x] Record all Runtime catch filters involving `TaskCanceledException`.
- [x] Record all consumers of runtime registration methods:
  - [x] `AddStorefrontRuntime`
  - [x] `AddStorefrontServerGeneratedClients`
  - [x] `AddStorefrontGeneratedClients`
- [x] Record current generated client constructor signatures before removing `Activator`.
- [x] Confirm `Storefront.V2.WASM.csproj` has no Runtime or Client reference before adding guardrails.

### Baseline Notes

- Current dirty working tree before SRH0 contained only this plan and `Storefront Components Logic Only Hardening.todo.md` as untracked docs.
- Runtime inventory contains `StorefrontRuntimeServiceCollectionExtensions.cs`, `StorefrontRuntimeExecution.cs`, `StorefrontRuntimeError.cs`, `StorefrontRuntimeResult.cs`, `StorefrontRuntimeContext.cs`, `StorefrontRuntimeOptions.cs`, `StorefrontRuntimeCatalogContentFacade.cs`, `StorefrontRuntimeCartFacade.cs`, `StorefrontRuntimeCheckoutFacade.cs`, `StorefrontRuntimeConfigurationFacade.cs`, `StorefrontRuntimeAddressFacade.cs`, `StorefrontRuntimeConsentFacade.cs`, and `StorefrontRuntimePaymentFacade.cs`.
- Offenders confirmed before refactor:
  - `StorefrontRuntimeServiceCollectionExtensions.cs` uses `Activator.CreateInstance`.
  - `StorefrontRuntimeCatalogContentFacade.cs` uses `GetProperty("Success")`, `GetProperty("Data")`, and JSON serialize/deserialize projection.
  - Runtime execution and catalog content submit/read paths use the old `OperationCanceledException`/`TaskCanceledException` catch filter.
- Registration consumers confirmed:
  - `Storefront.V2` calls `AddStorefrontRuntime` and `AddStorefrontServerGeneratedClients`.
  - `Storefront.Starter` calls `AddStorefrontRuntime` and `AddStorefrontGeneratedClients`.
- Generated client constructors currently use explicit `HttpClient` constructors, for example `StorefrontCartClient(HttpClient httpClient)`.
- `Storefront.V2.WASM.csproj` has no `Storefront.Runtime` or `Storefront.Client` project reference.
- Focused baseline test passed: `StorefrontRuntimeResultPrimitiveTests`, `StorefrontSharedPlatformPackageContractTests`, and `StorefrontV2WASMRuntimeFoundationTests`.

### Files Likely Read

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
git status --short
rg -n "Activator\.CreateInstance|GetProperty\(\"" BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime BlazorShop.Tests.V2
rg -n "TaskCanceledException|OperationCanceledException" BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime BlazorShop.Tests.V2
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
```

### Done When

- [x] Baseline offenders are listed.
- [x] Existing in-flight OpenAPI changes are not overwritten.
- [x] Focused Runtime/Storefront boundary tests establish the current behavior before refactor.

## Phase SRH1 - Guardrails And Characterization Tests

Goal: add failing or characterization tests before changing Runtime internals.

Status: completed in commit pending.

### Tasks

- [x] Add a Runtime source guardrail test that blocks `Activator.CreateInstance` in `BlazorShop.Storefront.Runtime`.
- [x] Add a Runtime source guardrail test that blocks `GetProperty("Success")`, `GetProperty("Data")`, and `GetProperty("Message")` in Runtime source after typed envelope migration.
- [x] Add a Runtime source guardrail test that blocks `JsonSerializer.Serialize(source, JsonOptions)` projection inside Runtime facade mapping.
- [x] Add a Runtime cancellation characterization test:
  - [x] caller-requested cancellation propagates as `OperationCanceledException`.
  - [x] network/request timeout maps to `network.timeout`.
  - [x] timeout error is marked retryable after SRH5.
- [x] Add a Runtime DI characterization test:
  - [x] `AddStorefrontRuntime` registers core runtime primitives.
  - [x] `AddStorefrontServerGeneratedClients` can resolve each registered generated client interface.
  - [x] `AddStorefrontServerGeneratedClients` can resolve each current runtime facade.
- [x] Add a WASM project boundary test:
  - [x] `Storefront.V2.WASM.csproj` does not reference `Storefront.Runtime`.
  - [x] `Storefront.V2.WASM.csproj` does not reference `Storefront.Client`.
  - [x] WASM source does not contain `CommerceNodeBaseUrl`.
  - [x] WASM source does not contain `StorefrontRuntimeOptions`.
  - [x] WASM source does not import `BlazorShop.Storefront.Runtime`.
- [x] Keep guardrail tests initially scoped so they can be enabled phase-by-phase if the current code still violates them.
- [x] Update test names to explain the rule and remediation.

### SRH1 Notes

- Added active DI characterization for `AddStorefrontRuntime` plus `AddStorefrontServerGeneratedClients`.
- Added active WASM project/source boundary guardrail blocking Runtime, Client, `CommerceNodeBaseUrl`, and `StorefrontRuntimeOptions` usage from WASM.
- Added target guardrails for Activator, envelope reflection, JSON projection, caller cancellation, and retryable timeout as skipped tests with explicit SRH enablement notes.
- Focused QA gate passed with 42 passed and 4 intentionally skipped target guardrails.

### Files Likely Touched

- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRuntimeResultPrimitiveTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontV2WASMRuntimeFoundationTests.cs`
- New focused test file if existing files become too large.

### QA Gate

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
```

### Done When

- [x] Tests describe the target Runtime safety rules.
- [x] Tests fail only for intentional current offenders or pass after the matching refactor phase.
- [x] Failure messages identify the exact offending file and target remediation.

## Phase SRH2 - Replace Activator With Typed Generated-Client Factories

Goal: make generated-client constructor drift a compile-time error.

Status: completed in commit pending.

### Tasks

- [x] Split generated-client registration into a focused file if `StorefrontRuntimeServiceCollectionExtensions.cs` becomes too large:
  - [x] `StorefrontRuntimeGeneratedClientRegistration.cs`
  - [x] or a partial `StorefrontRuntimeServiceCollectionExtensions.GeneratedClients.cs` if the existing class is kept partial.
- [x] Keep named `HttpClient` registration as the single place that applies:
  - [x] `StorefrontRuntimeOptions.CommerceNodeBaseUrl`
  - [x] caller-supplied `configureHttpClient`
  - [x] future tracing/correlation/retry handler wiring.
- [x] Replace generic `CreateClient<TClient>` with explicit typed factory registrations for every generated client currently registered:
  - [x] `IStorefrontAddressClient`
  - [x] `IStorefrontAuthClient`
  - [x] `IStorefrontCartClient`
  - [x] `IStorefrontCatalogClient`
  - [x] `IStorefrontCheckoutClient`
  - [x] `IStorefrontConfigurationClient`
  - [x] `IStorefrontConsentClient`
  - [x] `IStorefrontContactClient`
  - [x] `IStorefrontCurrencyClient`
  - [x] `IStorefrontCustomerAddressesClient`
  - [x] `IStorefrontCustomerProfileClient`
  - [x] `IStorefrontNavigationClient`
  - [x] `IStorefrontNewsletterClient`
  - [x] `IStorefrontOrdersClient`
  - [x] `IStorefrontPagesClient`
  - [x] `IStorefrontPaymentsClient`
  - [x] `IStorefrontRecommendationsClient`
  - [x] `IStorefrontSeoClient`
  - [x] `IStorefrontStoreClient`
- [x] If generated constructors still require `(string baseUrl, HttpClient httpClient)`:
  - [x] call constructors explicitly with the current `string.Empty` argument.
  - [x] add a comment pointing to `Storefront OpenAPI Generated Client Hardening.todo.md` OCH5 for base URL cleanup.
  - [x] do not hide this behind reflection.
- [x] If OCH5 has already removed the generated base URL constructor parameter:
  - [x] call constructors explicitly with `HttpClient` only.
  - [x] update independent package consumer tests accordingly.
- [x] Ensure all typed factories share one helper for creating the named `HttpClient`, not one `IHttpClientFactory` call pattern duplicated with different client names.
- [x] Keep existing public registration method names in this phase:
  - [x] `AddStorefrontServerGeneratedClients`
  - [x] `AddStorefrontGeneratedClients`
- [x] Do not introduce capability registration yet; that is SRH7.

### SRH2 Notes

- `StorefrontRuntimeServiceCollectionExtensions.cs` now registers each generated client through an explicit constructor call.
- All generated client constructors already use `HttpClient` only after the earlier OpenAPI client hardening, so no `string.Empty` compatibility argument was needed.
- The named `StorefrontGenerated` `HttpClient` remains the single `CommerceNodeBaseUrl`/caller configuration point.
- Existing wrapper method names were preserved and capability registration was deferred to SRH7.
- Client/Runtime builds passed, and focused shared package/generated-client tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs`
- Optional new runtime registration file.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontGeneratedClientFoundationTests.cs` if constructor shape changes.

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests"
```

### Done When

- [x] Runtime source contains no `Activator.CreateInstance`.
- [x] A generated client constructor change fails at compile time.
- [x] V2 and Starter can still use the current registration methods.

## Phase SRH3 - Normalize Cancellation Handling

Goal: distinguish caller cancellation from network timeout consistently.

Status: completed in commit pending.

### Tasks

- [x] Update `StorefrontRuntimeExecution.ExecuteAsync` and `ExecuteSubmitAsync` to accept and use the caller `CancellationToken` in exception handling.
- [x] Use the rule:
  - [x] if `OperationCanceledException` occurs and `cancellationToken.IsCancellationRequested` is true, rethrow.
  - [x] if `TaskCanceledException` occurs and caller token is not canceled, map to `network.timeout`.
  - [x] if `TimeoutException` occurs, map to `network.timeout`.
  - [x] if `HttpRequestException` occurs, map to `network.failure`.
- [x] Update every Runtime facade local executor to either:
  - [x] use `StorefrontRuntimeExecution`, or
  - [x] implement the same cancellation rule with the explicit caller token.
- [x] Remove catch filters shaped like:

```csharp
catch (Exception exception)
    when (exception is not OperationCanceledException || exception is TaskCanceledException)
```

- [x] Make cancellation tests cover at least:
  - [x] `StorefrontRuntimeExecution.ExecuteAsync`.
  - [x] one submit facade path.
  - [x] one read facade path.
- [x] Do not swallow cancellation in UI/server layers that rely on request abort behavior.

### SRH3 Notes

- `StorefrontRuntimeExecution` now rethrows caller-requested cancellation and only maps non-caller `TaskCanceledException`/timeouts through the error mapper.
- Runtime facade-local executors now receive the caller token and use the same cancellation rule.
- Added tests for core read execution, core submit execution, cart submit facade cancellation, and address read facade cancellation.
- Source scan found no old `OperationCanceledException`/`TaskCanceledException` catch filter after the change.
- Runtime build passed and focused runtime primitive tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeExecution.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeError.cs`
- Runtime facade files that currently catch `TaskCanceledException`.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRuntimeResultPrimitiveTests.cs`

### QA Gate

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests"
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
```

### Done When

- [x] Caller cancellation is no longer converted into timeout.
- [x] Real timeout still produces a normalized runtime error.
- [x] Runtime source has no old cancellation catch filter.

## Phase SRH4 - Replace Reflection Envelope Mapping With Typed Selectors

Goal: map generated response envelopes without reflection, dynamic typing, or JSON projection.

Status: completed in commit pending.

### Tasks

- [x] Introduce a small internal typed envelope executor in Runtime, for example:
  - [x] `StorefrontRuntimeEnvelopeExecutor`
  - [x] or generic methods on `StorefrontRuntimeExecution`
- [x] The executor should accept typed selectors:

```csharp
Func<TEnvelope, bool?> successSelector
Func<TEnvelope, TData?> dataSelector
Func<TEnvelope, string?> messageSelector
```

- [x] Executor must also receive:
  - [x] `IStorefrontRuntimeContext`
  - [x] generated-client call delegate using `storeKey` and `CancellationToken`
  - [x] fallback code/message
  - [x] caller cancellation token
  - [x] optional idempotency key for submit results.
- [x] Replace reflection envelope mapping in:
  - [x] `StorefrontRuntimeCartFacade.cs`
  - [x] `StorefrontRuntimeAddressFacade.cs`
  - [x] `StorefrontRuntimeCheckoutFacade.cs`
  - [x] `StorefrontRuntimeConsentFacade.cs`
  - [x] `StorefrontRuntimePaymentFacade.cs`
  - [x] `StorefrontRuntimeCatalogContentFacade.cs`
- [x] For each generated call, use the concrete generated envelope type returned by NSwag.
- [x] Do not create handwritten clone DTOs just to simplify selectors.
- [x] Remove `JsonSerializer.Serialize`/`Deserialize` projection from `StorefrontRuntimeCatalogContentFacade`.
- [x] If generated `Data` type differs from current Runtime return type:
  - [x] first prefer returning the generated DTO type directly from Runtime.
  - [x] if a Runtime projection remains necessary, use explicit property mapping.
  - [x] keep mapping small and covered by tests.
- [x] Add tests or source guardrails that Runtime no longer contains:
  - [x] `GetProperty("Success")`
  - [x] `GetProperty("Data")`
  - [x] `GetProperty("Message")`
  - [x] JSON serialize/deserialize projection.

### SRH4 Notes

- Added `StorefrontRuntimeEnvelopeExecutor` with typed selectors for success/data/message and shared cancellation/error mapping.
- Runtime facades now pass concrete generated envelope selectors instead of reading `Success`, `Data`, and `Message` through reflection.
- Catalog/content no longer JSON roundtrips generated DTOs; generated `Data` is returned directly where types match.
- Source guardrail is active and also blocks `dynamic`.
- Runtime source scan found no envelope reflection, JSON projection, `dynamic`, or `Activator.CreateInstance`.
- Runtime build and focused Runtime/shared/generated catalog/configuration tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeExecution.cs`
- Optional new `StorefrontRuntimeEnvelopeExecutor.cs`
- All Runtime facade files listed above.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRuntimeResultPrimitiveTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests"
```

### Done When

- [x] Runtime envelope success/data/message mapping is compile-time checked.
- [x] Runtime source has no reflection envelope mapping.
- [x] Catalog/content facade no longer serializes generated DTOs for projection.
- [x] Existing V2 behavior remains unchanged.

## Phase SRH5 - Harden Runtime Error Primitives Without Forcing Store Copy

Goal: keep Runtime errors stable and technical while storefront hosts own final visible copy.

Status: completed in commit pending.

### Tasks

- [x] Decide the non-breaking error shape migration:
  - [x] keep `Message` for compatibility in the first implementation phase.
  - [x] add `DefaultMessage` as an alias or new property if the record shape can stay source-compatible.
  - [x] add `Retryable`.
  - [x] keep `Status`, `Code`, `TraceId`, and `FieldErrors`.
- [x] If changing the positional `StorefrontRuntimeError` constructor would create too much churn:
  - [x] keep constructor shape.
  - [x] add computed properties or factory methods first.
  - [x] migrate call sites in a later phase.
- [x] Normalize error code ownership:
  - [x] API-provided code is preserved.
  - [x] network timeout uses `network.timeout`.
  - [x] network failure uses `network.failure`.
  - [x] fallback unavailable uses `storefront.unavailable`.
  - [x] invalid local input uses `request.invalid`.
- [x] Define `Retryable` rules:
  - [x] `network.timeout` is retryable.
  - [x] `network.failure` is retryable only if current policy treats transient HTTP failures as retryable.
  - [x] validation, forbidden, unauthorized, not found, and conflict are not retryable by default.
- [x] Rename internal comments/docs to describe message as fallback technical copy, not final UI copy.
- [x] Add a short host guidance section:
  - [x] Storefront host maps `Status`/`Code` to localized copy.
  - [x] Storefront host chooses toast, inline error, full error page, or retry CTA.
  - [x] Runtime does not decide language or final UX.
- [x] Update tests:
  - [x] timeout exposes `Retryable = true`.
  - [x] validation preserves `FieldErrors`.
  - [x] conflict preserves conflict primitive.
  - [x] API message still survives as fallback.

### SRH5 Notes

- `StorefrontRuntimeError` keeps its positional constructor and adds computed `DefaultMessage` and `Retryable` properties.
- `network.timeout` and `network.failure` are retryable; validation/auth/not-found/conflict errors remain non-retryable by default.
- Architecture docs now state that Runtime provides technical fallback state while hosts own localization, placement, and retry CTA behavior.
- Runtime build passed and runtime primitive tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeError.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeResult.cs`
- Runtime facades that construct fallback errors.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRuntimeResultPrimitiveTests.cs`
- `docs/architecture/05-project-and-folder-guide.md`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests"
```

### Done When

- [x] Runtime errors expose enough state for localized Storefront UX.
- [x] Runtime keeps technical fallback copy without owning final storefront copy.
- [x] Existing V2 and Starter consumers still compile.

## Phase SRH6 - Split Catalog Content Facade By Capability

Goal: reduce `IStorefrontRuntimeCatalogContentFacade` scope without changing endpoint behavior.

Status: completed in commit pending.

### Target Interfaces

- [x] `IStorefrontRuntimeCatalogFacade`
  - [x] published categories
  - [x] category tree
  - [x] category by slug
  - [x] catalog products
  - [x] filter metadata
  - [x] search suggestions
  - [x] product by slug
  - [x] product by ID
  - [x] product selection preview if current consumers treat it as catalog/product runtime.
- [x] `IStorefrontRuntimeContentFacade`
  - [x] published page by slug/system name
  - [x] page navigation links if currently page-owned.
- [x] `IStorefrontRuntimeNavigationFacade`
  - [x] menu tree/menu items
  - [x] active navigation support if Runtime owns it.
- [x] `IStorefrontRuntimeSeoFacade`
  - [x] SEO settings/defaults
  - [x] redirect resolution
  - [x] canonical/route resolver helpers if currently in Runtime.

### Tasks

- [x] Record current consumers of `IStorefrontRuntimeCatalogContentFacade`.
- [x] Create new capability-specific interfaces in Runtime.
- [x] Move implementation code mechanically into capability-specific classes:
  - [x] `StorefrontRuntimeCatalogFacade`
  - [x] `StorefrontRuntimeContentFacade`
  - [x] `StorefrontRuntimeNavigationFacade`
  - [x] `StorefrontRuntimeSeoFacade`
- [x] Keep old `IStorefrontRuntimeCatalogContentFacade` as a compatibility adapter for one phase if consumers are numerous.
- [x] Mark compatibility adapter with an implementation comment and test, not a permanent abstraction.
- [x] Update V2 generated adapters or services to inject the new specific facade where the consuming class only needs one capability.
- [x] Do not change public Storefront API routes or generated DTOs.
- [x] Do not add a new NuGet package.
- [x] Keep tests for old and new registrations until V2/Starter migration is complete.

### SRH6 Notes

- Current old-facade consumers were `GeneratedStorefrontCatalogContentClient`, generated catalog tests, shared package DI tests, and plan docs.
- Added `IStorefrontRuntimeCatalogFacade`, `IStorefrontRuntimeContentFacade`, `IStorefrontRuntimeNavigationFacade`, and `IStorefrontRuntimeSeoFacade`.
- Kept `IStorefrontRuntimeCatalogContentFacade` as a compatibility adapter interface extending the four capability interfaces for one migration phase.
- Added `StorefrontRuntimeCatalogFacade`, `StorefrontRuntimeContentFacade`, `StorefrontRuntimeNavigationFacade`, and `StorefrontRuntimeSeoFacade` as the primary capability facade classes.
- The compatibility implementation remains `StorefrontRuntimeCatalogContentFacade` in this phase to avoid behavioral churn; the new capability facade classes delegate to the compatibility facade until the adapter is removed.
- V2 generated catalog/content adapter now injects the four capability interfaces instead of the compatibility god facade.
- Runtime/V2 builds passed and focused Runtime/shared/catalog tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeCatalogContentFacade.cs`
- New Runtime facade files for catalog/content/navigation/SEO.
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests"
```

### Done When

- [x] Runtime no longer has one catalog/content/navigation/SEO god facade as the primary API.
- [x] Existing V2 and Starter flows keep working.
- [x] Compatibility adapter has a planned removal trigger.

## Phase SRH7 - Split Capability Registration

Goal: let hosts opt into runtime capabilities while preserving current all-in registration convenience.

Status: completed in commit pending.

### Tasks

- [x] Add registration methods:
  - [x] `AddStorefrontCatalogRuntime`
  - [x] `AddStorefrontContentRuntime`
  - [x] `AddStorefrontNavigationRuntime`
  - [x] `AddStorefrontSeoRuntime`
  - [x] `AddStorefrontCartRuntime`
  - [x] `AddStorefrontCheckoutRuntime`
  - [x] `AddStorefrontAccountRuntime`
  - [x] `AddStorefrontConfigurationRuntime`
  - [x] `AddStorefrontPaymentRuntime`
  - [x] `AddStorefrontConsentRuntime`
  - [x] `AddStorefrontAddressRuntime`
- [x] Each capability registration should register only:
  - [x] generated clients needed by that capability.
  - [x] facades needed by that capability.
  - [x] small shared helpers required by the capability.
- [x] Add `AddStorefrontPlatformRuntime` as the intentional all-capability registration.
- [x] Keep `AddStorefrontServerGeneratedClients` as compatibility wrapper to `AddStorefrontPlatformRuntime` during migration.
- [x] Keep `AddStorefrontGeneratedClients` as compatibility wrapper for Starter during migration.
- [x] Add tests that prove:
  - [x] catalog-only registration does not register checkout/payment/cart facades.
  - [x] cart-only registration registers cart client/facade and shared runtime primitives.
  - [x] platform registration resolves all current facades.
  - [x] old wrapper methods still work.
- [x] Avoid implicit duplicate registration conflicts:
  - [x] repeated calls should be safe or documented.
  - [x] prefer `TryAdd` only when it matches current DI behavior.

### SRH7 Notes

- Added all requested capability registration methods plus `AddStorefrontPlatformRuntime`.
- `AddStorefrontServerGeneratedClients` and `AddStorefrontGeneratedClients` now delegate to `AddStorefrontPlatformRuntime`.
- Registrations use `TryAddScoped` for generated clients/facades so repeated capability calls stay stable.
- Catalog/content/navigation/SEO currently share `AddCatalogContentRuntimeCore` because the compatibility adapter still has a shared constructor; this keeps checkout/payment/cart out of catalog-only registration and leaves deeper adapter removal for the planned follow-up.
- Added DI tests for catalog-only, cart-only, platform registration, and wrapper delegation.
- Runtime build passed and shared platform package tests passed.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/StorefrontRuntimeServiceCollectionExtensions.cs`
- Optional new registration files.
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests"
```

### Done When

- [x] Hosts can register only the Runtime capabilities they need.
- [x] Existing all-in Runtime registration still works.
- [x] DI tests prove capability boundaries.

## Phase SRH8 - V2, Starter, Docs, And Boundary Adoption

Goal: make active consumers use the new Runtime shape without breaking existing local run behavior.

### Tasks

- [x] Update `Storefront.V2` registration:
  - [x] prefer `AddStorefrontPlatformRuntime` if V2 still needs all current capabilities.
  - [x] or use explicit capability registration if V2 composition is already split.
  - [x] do not call both old and new all-in wrappers.
- [x] Update `Storefront.Starter` registration:
  - [x] use `AddStorefrontPlatformRuntime` for starter simplicity, or
  - [x] use a minimal explicit set if Starter has intentionally narrow capabilities.
- [x] Update generated storefront docs/rules so future `Storefront.{Name}` projects:
  - [x] use Runtime only from server/BFF project.
  - [x] use Components/Browser for WASM/browser behavior.
  - [x] do not guess API response shape.
  - [x] do not reference Commerce Node API, Control Plane API, Application, Domain, Infrastructure, or `Web.SharedV2`.
- [x] Update architecture docs:
  - [x] `docs/architecture/05-project-and-folder-guide.md`
  - [x] `docs/architecture/10-v2-contract-ownership.md`
  - [x] `docs/architecture/11-storefront-builder.md` if generated storefront guidance changes.
- [x] Update QA checklist:
  - [x] `QA-StorefrontV2.todo.md` includes Runtime DI/cancellation/envelope/browser-boundary cases.
  - [x] StorefrontBuilder QA docs include server-only Runtime consumption rule if generated storefronts are involved.
- [x] Decide whether to mark old wrapper names obsolete:
  - [x] only after V2 and Starter call the new preferred method.
  - [x] do not remove wrappers until a later cleanup phase.

### Files Likely Touched

- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs`
- `docs/architecture/05-project-and-folder-guide.md`
- `docs/architecture/10-v2-contract-ownership.md`
- `docs/architecture/11-storefront-builder.md`
- `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`

### QA Gate

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
```

### Done When

- [x] Active server storefront consumers use the new preferred Runtime registration path.
- [x] Browser/WASM boundary remains explicit.
- [x] Docs explain Runtime as server-only BFF integration.

### SRH8 Notes - 2026-07-26

- `Storefront.V2` and `Storefront.Starter` now call `AddStorefrontPlatformRuntime`; the old all-in wrapper names remain compatibility APIs for later cleanup.
- StorefrontBuilder, architecture, agent, and QA docs now state that Runtime is server/BFF-only and browser/WASM code must use same-origin endpoints plus browser-safe Components contracts/headless behavior.
- Verification passed:
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore`
  - `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore`
  - `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"` passed `31/31`.
  - Starter package-boundary verification required repacking `BlazorShop.Storefront.Runtime` `1.0.0-local` into the ignored local feed and force-restoring Starter into an isolated repo-local NuGet package cache because the global NuGet cache still contained the older local package binary.

## Phase SRH9 - Full QA And Release Gate

Goal: prove hardening did not break Storefront V2 runtime flows or package consumers.

### Static And Unit Verification

- [x] Run Runtime focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests"
```

- [x] Run generated client tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests"
```

- [x] Run architecture/boundary tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Storefront"
```

- [x] Run builds:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
```

### Browser Verification If Runtime Flow Changes Touch V2

- [n/a] Start V2 local runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [n/a] Run targeted Playwright Storefront V2 browser QA:
  - [n/a] home/store bootstrap loads with current store context.
  - [n/a] catalog page loads products through Runtime generated client.
  - [n/a] product detail loads product and SEO data through Runtime generated client.
  - [n/a] add-to-cart works.
  - [n/a] cart WASM component calls same-origin BFF only.
  - [n/a] checkout start/review/place-order COD still works if checkout Runtime code changed.
  - [n/a] account WASM component calls same-origin BFF only if account registration/runtime changed.
  - [n/a] payment result/order completion still resolves through Storefront API.
  - [n/a] network assertion: browser does not call Commerce Node host directly.
  - [n/a] network assertion: browser does not call Control Plane.

### Done When

- [x] Runtime source contains no `Activator.CreateInstance`.
- [x] Runtime source contains no envelope `GetProperty` reflection.
- [x] Runtime source contains no JSON serialize/deserialize DTO projection.
- [x] Runtime source contains no old cancellation catch filter.
- [x] Runtime error primitives expose retry/localization-ready state.
- [x] Runtime facade interfaces are capability-scoped.
- [x] Runtime registration is capability-scoped with compatibility wrapper.
- [x] Storefront.V2.WASM cannot reference Runtime or Commerce Node generated clients.
- [x] Storefront V2 and Starter build.
- [x] Focused Storefront tests pass.
- [n/a] Browser QA passes if V2 runtime behavior was exercised.

### SRH9 Notes - 2026-07-26

- Builds passed for `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime`, `BlazorShop.Storefront.Components`, `BlazorShop.Storefront.V2.WASM`, `BlazorShop.Storefront.V2`, and `BlazorShop.Storefront.Starter`.
- Runtime focused tests passed `51/51`.
- Generated client tests passed `16/16`.
- Architecture/Storefront boundary gate passed `781/783` with `2` existing skipped cart service tests.
- Fixed the Starter package-boundary test to restore into an isolated repo-local NuGet package cache, preventing stale global `1.0.0-local` Runtime packages from hiding current package output.
- Browser QA was not rerun in SRH9 because this phase did not change browser-visible route, endpoint, cart, checkout, account, or media behavior. Browser/WASM boundary remains covered by `StorefrontV2WASMRuntimeFoundationTests` and the broader Architecture/Storefront gate.
- Static Runtime source scan found no `Activator.CreateInstance`, envelope `GetProperty`, JSON serialize/deserialize projection, old cancellation catch filter, or `dynamic` usage.

## Suggested Implementation Order

1. SRH0 baseline and conflict check.
2. SRH1 guardrails and characterization tests.
3. SRH2 typed generated-client factories.
4. SRH3 cancellation handling normalization.
5. SRH4 typed envelope execution and JSON projection removal.
6. SRH5 error primitive hardening.
7. SRH6 catalog/content/navigation/SEO facade split.
8. SRH7 capability registration split.
9. SRH8 V2/Starter/docs adoption.
10. SRH9 focused QA and release gate.

## Final Verification Commands

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/BlazorShop.Storefront.V2.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontV2WASMRuntimeFoundationTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests"
```

If Runtime changes are released with generated-client OpenAPI changes, also run:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
```

If V2 runtime behavior changes are browser-visible, also run Storefront V2 Playwright release cases from the active release QA checklist.

## Completion Checklist

- [x] Runtime generated-client registration is typed and compile-time-safe.
- [x] Runtime generated-client registration still supports V2 and Starter.
- [x] Runtime envelope mapping is typed.
- [x] Runtime does not use reflection to read generated envelopes.
- [x] Runtime does not JSON roundtrip generated DTOs for projection.
- [x] Runtime cancellation propagates caller aborts.
- [x] Runtime timeout mapping is explicit and test-covered.
- [x] Runtime error result supports localized storefront UX through stable primitives.
- [x] Runtime facades are capability-scoped.
- [x] Capability registration methods exist.
- [x] All-in registration remains available as a compatibility wrapper.
- [x] WASM/server boundary guardrails prevent Runtime usage in browser project.
- [x] Architecture docs and QA checklists are updated.
- [x] Storefront V2, Starter, Runtime, Client, Components, and WASM builds pass.

## Autoplan Decision Audit Trail

| # | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- |
| 1 | Keep Runtime as a single server/BFF package while hardening internals | Auto-decided | Preserve current architecture boundary | Current docs define Runtime as minimal server-side generated-client registration and BFF-safe primitives; splitting packages now adds churn without a second proven need | Split Runtime into multiple NuGet packages immediately |
| 2 | Add guardrails before refactoring `Activator`, reflection, cancellation, and WASM boundary | Auto-decided | Characterize before changing shared infrastructure | Runtime has V2, Starter, and future generated storefront consumers; tests must prove the current contract before internals move | Refactor first and rely on build failures |
| 3 | Replace `Activator.CreateInstance` with explicit typed factories | Auto-decided | Compile-time safety over short generic code | Generated constructor drift should fail build, not DI resolution at runtime | Keep reflection factory and add runtime smoke test only |
| 4 | Use typed envelope selectors instead of DTO clone layer | Auto-decided | Generated OpenAPI client is the contract owner | Runtime can map known generated response types directly without reflection or handwritten API DTO copies | Create a parallel handwritten envelope DTO hierarchy |
| 5 | Treat caller cancellation differently from timeout | Auto-decided | Correct runtime semantics | Request aborts should propagate; only non-caller timeout should be mapped to `network.timeout` | Continue mapping all `TaskCanceledException` as timeout |
| 6 | Keep `Message` temporarily and add localization-ready primitives gradually | Auto-decided | Avoid breaking active consumers | V2 and tests currently read `Message`; adding `Retryable`/fallback semantics is safer than removing message in one phase | Remove all English messages from Runtime immediately |
| 7 | Split `CatalogContentFacade` by capability without changing Storefront API behavior | Auto-decided | Reduce maintainability risk with mechanical refactor | Current facade groups catalog, pages, navigation, SEO, and redirect resolution; capability interfaces improve maintenance while preserving behavior | Rewrite catalog/content runtime flows |
| 8 | Add capability registration while preserving wrapper methods | Auto-decided | Backward-compatible migration | V2 and Starter currently call existing methods; wrappers prevent breaking active hosts while enabling more explicit future consumption | Remove old registration methods immediately |
