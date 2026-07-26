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
- `Storefront.WASM` remains browser-only and cannot reference `Storefront.Runtime`.

## Current Verified Context

- [x] `docs/architecture/05-project-and-folder-guide.md` defines `BlazorShop.Storefront.Runtime` as the active minimal runtime package for store context/options, Storefront API client registration helpers, capability/configuration readers, normalized error mapping primitives, and BFF-safe result mapping primitives.
- [x] Runtime must not own Storefront V2 layout/design, CSS/assets, store-specific composition, backend business rules, provider secrets, or backend/core/API project references.
- [x] `docs/architecture/10-v2-contract-ownership.md` defines generated Storefront clients as frontend-readable contracts generated from Commerce Node Storefront OpenAPI.
- [x] `BlazorShop.Storefront.Runtime.csproj` references only `BlazorShop.Storefront.Client` plus Microsoft extension packages.
- [x] `BlazorShop.Storefront.WASM.csproj` currently references `BlazorShop.Storefront.Components`, not `BlazorShop.Storefront.Runtime`.
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
- [x] `StorefrontWasmRuntimeFoundationTests` already verifies WASM same-origin client behavior and absence of Commerce Node config in WASM startup, but it does not yet block a future WASM project reference to Runtime.
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

Storefront.WASM
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
- [x] Confirm `Storefront.WASM.csproj` has no Runtime or Client reference before adding guardrails.

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
- `Storefront.WASM.csproj` has no `Storefront.Runtime` or `Storefront.Client` project reference.
- Focused baseline test passed: `StorefrontRuntimeResultPrimitiveTests`, `StorefrontSharedPlatformPackageContractTests`, and `StorefrontWasmRuntimeFoundationTests`.

### Files Likely Read

- `BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/*`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Program.cs`
- `BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/*`

### Verification

```powershell
git status --short
rg -n "Activator\.CreateInstance|GetProperty\(\"" BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime BlazorShop.Tests.V2
rg -n "TaskCanceledException|OperationCanceledException" BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime BlazorShop.Tests.V2
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [x] Baseline offenders are listed.
- [x] Existing in-flight OpenAPI changes are not overwritten.
- [x] Focused Runtime/Storefront boundary tests establish the current behavior before refactor.

## Phase SRH1 - Guardrails And Characterization Tests

Goal: add failing or characterization tests before changing Runtime internals.

### Tasks

- [ ] Add a Runtime source guardrail test that blocks `Activator.CreateInstance` in `BlazorShop.Storefront.Runtime`.
- [ ] Add a Runtime source guardrail test that blocks `GetProperty("Success")`, `GetProperty("Data")`, and `GetProperty("Message")` in Runtime source after typed envelope migration.
- [ ] Add a Runtime source guardrail test that blocks `JsonSerializer.Serialize(source, JsonOptions)` projection inside Runtime facade mapping.
- [ ] Add a Runtime cancellation characterization test:
  - [ ] caller-requested cancellation propagates as `OperationCanceledException`.
  - [ ] network/request timeout maps to `network.timeout`.
  - [ ] timeout error is marked retryable after SRH5.
- [ ] Add a Runtime DI characterization test:
  - [ ] `AddStorefrontRuntime` registers core runtime primitives.
  - [ ] `AddStorefrontServerGeneratedClients` can resolve each registered generated client interface.
  - [ ] `AddStorefrontServerGeneratedClients` can resolve each current runtime facade.
- [ ] Add a WASM project boundary test:
  - [ ] `Storefront.WASM.csproj` does not reference `Storefront.Runtime`.
  - [ ] `Storefront.WASM.csproj` does not reference `Storefront.Client`.
  - [ ] WASM source does not contain `CommerceNodeBaseUrl`.
  - [ ] WASM source does not contain `StorefrontRuntimeOptions`.
  - [ ] WASM source does not import `BlazorShop.Storefront.Runtime`.
- [ ] Keep guardrail tests initially scoped so they can be enabled phase-by-phase if the current code still violates them.
- [ ] Update test names to explain the rule and remediation.

### Files Likely Touched

- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontSharedPlatformPackageContractTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontRuntimeResultPrimitiveTests.cs`
- `BlazorShop.Tests.V2/PresentationV2/Storefront/StorefrontWasmRuntimeFoundationTests.cs`
- New focused test file if existing files become too large.

### QA Gate

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [ ] Tests describe the target Runtime safety rules.
- [ ] Tests fail only for intentional current offenders or pass after the matching refactor phase.
- [ ] Failure messages identify the exact offending file and target remediation.

## Phase SRH2 - Replace Activator With Typed Generated-Client Factories

Goal: make generated-client constructor drift a compile-time error.

### Tasks

- [ ] Split generated-client registration into a focused file if `StorefrontRuntimeServiceCollectionExtensions.cs` becomes too large:
  - [ ] `StorefrontRuntimeGeneratedClientRegistration.cs`
  - [ ] or a partial `StorefrontRuntimeServiceCollectionExtensions.GeneratedClients.cs` if the existing class is kept partial.
- [ ] Keep named `HttpClient` registration as the single place that applies:
  - [ ] `StorefrontRuntimeOptions.CommerceNodeBaseUrl`
  - [ ] caller-supplied `configureHttpClient`
  - [ ] future tracing/correlation/retry handler wiring.
- [ ] Replace generic `CreateClient<TClient>` with explicit typed factory registrations for every generated client currently registered:
  - [ ] `IStorefrontAddressClient`
  - [ ] `IStorefrontAuthClient`
  - [ ] `IStorefrontCartClient`
  - [ ] `IStorefrontCatalogClient`
  - [ ] `IStorefrontCheckoutClient`
  - [ ] `IStorefrontConfigurationClient`
  - [ ] `IStorefrontConsentClient`
  - [ ] `IStorefrontContactClient`
  - [ ] `IStorefrontCurrencyClient`
  - [ ] `IStorefrontCustomerAddressesClient`
  - [ ] `IStorefrontCustomerProfileClient`
  - [ ] `IStorefrontNavigationClient`
  - [ ] `IStorefrontNewsletterClient`
  - [ ] `IStorefrontOrdersClient`
  - [ ] `IStorefrontPagesClient`
  - [ ] `IStorefrontPaymentsClient`
  - [ ] `IStorefrontRecommendationsClient`
  - [ ] `IStorefrontSeoClient`
  - [ ] `IStorefrontStoreClient`
- [ ] If generated constructors still require `(string baseUrl, HttpClient httpClient)`:
  - [ ] call constructors explicitly with the current `string.Empty` argument.
  - [ ] add a comment pointing to `Storefront OpenAPI Generated Client Hardening.todo.md` OCH5 for base URL cleanup.
  - [ ] do not hide this behind reflection.
- [ ] If OCH5 has already removed the generated base URL constructor parameter:
  - [ ] call constructors explicitly with `HttpClient` only.
  - [ ] update independent package consumer tests accordingly.
- [ ] Ensure all typed factories share one helper for creating the named `HttpClient`, not one `IHttpClientFactory` call pattern duplicated with different client names.
- [ ] Keep existing public registration method names in this phase:
  - [ ] `AddStorefrontServerGeneratedClients`
  - [ ] `AddStorefrontGeneratedClients`
- [ ] Do not introduce capability registration yet; that is SRH7.

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

- [ ] Runtime source contains no `Activator.CreateInstance`.
- [ ] A generated client constructor change fails at compile time.
- [ ] V2 and Starter can still use the current registration methods.

## Phase SRH3 - Normalize Cancellation Handling

Goal: distinguish caller cancellation from network timeout consistently.

### Tasks

- [ ] Update `StorefrontRuntimeExecution.ExecuteAsync` and `ExecuteSubmitAsync` to accept and use the caller `CancellationToken` in exception handling.
- [ ] Use the rule:
  - [ ] if `OperationCanceledException` occurs and `cancellationToken.IsCancellationRequested` is true, rethrow.
  - [ ] if `TaskCanceledException` occurs and caller token is not canceled, map to `network.timeout`.
  - [ ] if `TimeoutException` occurs, map to `network.timeout`.
  - [ ] if `HttpRequestException` occurs, map to `network.failure`.
- [ ] Update every Runtime facade local executor to either:
  - [ ] use `StorefrontRuntimeExecution`, or
  - [ ] implement the same cancellation rule with the explicit caller token.
- [ ] Remove catch filters shaped like:

```csharp
catch (Exception exception)
    when (exception is not OperationCanceledException || exception is TaskCanceledException)
```

- [ ] Make cancellation tests cover at least:
  - [ ] `StorefrontRuntimeExecution.ExecuteAsync`.
  - [ ] one submit facade path.
  - [ ] one read facade path.
- [ ] Do not swallow cancellation in UI/server layers that rely on request abort behavior.

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

- [ ] Caller cancellation is no longer converted into timeout.
- [ ] Real timeout still produces a normalized runtime error.
- [ ] Runtime source has no old cancellation catch filter.

## Phase SRH4 - Replace Reflection Envelope Mapping With Typed Selectors

Goal: map generated response envelopes without reflection, dynamic typing, or JSON projection.

### Tasks

- [ ] Introduce a small internal typed envelope executor in Runtime, for example:
  - [ ] `StorefrontRuntimeEnvelopeExecutor`
  - [ ] or generic methods on `StorefrontRuntimeExecution`
- [ ] The executor should accept typed selectors:

```csharp
Func<TEnvelope, bool?> successSelector
Func<TEnvelope, TData?> dataSelector
Func<TEnvelope, string?> messageSelector
```

- [ ] Executor must also receive:
  - [ ] `IStorefrontRuntimeContext`
  - [ ] generated-client call delegate using `storeKey` and `CancellationToken`
  - [ ] fallback code/message
  - [ ] caller cancellation token
  - [ ] optional idempotency key for submit results.
- [ ] Replace reflection envelope mapping in:
  - [ ] `StorefrontRuntimeCartFacade.cs`
  - [ ] `StorefrontRuntimeAddressFacade.cs`
  - [ ] `StorefrontRuntimeCheckoutFacade.cs`
  - [ ] `StorefrontRuntimeConsentFacade.cs`
  - [ ] `StorefrontRuntimePaymentFacade.cs`
  - [ ] `StorefrontRuntimeCatalogContentFacade.cs`
- [ ] For each generated call, use the concrete generated envelope type returned by NSwag.
- [ ] Do not create handwritten clone DTOs just to simplify selectors.
- [ ] Remove `JsonSerializer.Serialize`/`Deserialize` projection from `StorefrontRuntimeCatalogContentFacade`.
- [ ] If generated `Data` type differs from current Runtime return type:
  - [ ] first prefer returning the generated DTO type directly from Runtime.
  - [ ] if a Runtime projection remains necessary, use explicit property mapping.
  - [ ] keep mapping small and covered by tests.
- [ ] Add tests or source guardrails that Runtime no longer contains:
  - [ ] `GetProperty("Success")`
  - [ ] `GetProperty("Data")`
  - [ ] `GetProperty("Message")`
  - [ ] JSON serialize/deserialize projection.

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

- [ ] Runtime envelope success/data/message mapping is compile-time checked.
- [ ] Runtime source has no reflection envelope mapping.
- [ ] Catalog/content facade no longer serializes generated DTOs for projection.
- [ ] Existing V2 behavior remains unchanged.

## Phase SRH5 - Harden Runtime Error Primitives Without Forcing Store Copy

Goal: keep Runtime errors stable and technical while storefront hosts own final visible copy.

### Tasks

- [ ] Decide the non-breaking error shape migration:
  - [ ] keep `Message` for compatibility in the first implementation phase.
  - [ ] add `DefaultMessage` as an alias or new property if the record shape can stay source-compatible.
  - [ ] add `Retryable`.
  - [ ] keep `Status`, `Code`, `TraceId`, and `FieldErrors`.
- [ ] If changing the positional `StorefrontRuntimeError` constructor would create too much churn:
  - [ ] keep constructor shape.
  - [ ] add computed properties or factory methods first.
  - [ ] migrate call sites in a later phase.
- [ ] Normalize error code ownership:
  - [ ] API-provided code is preserved.
  - [ ] network timeout uses `network.timeout`.
  - [ ] network failure uses `network.failure`.
  - [ ] fallback unavailable uses `storefront.unavailable`.
  - [ ] invalid local input uses `request.invalid`.
- [ ] Define `Retryable` rules:
  - [ ] `network.timeout` is retryable.
  - [ ] `network.failure` is retryable only if current policy treats transient HTTP failures as retryable.
  - [ ] validation, forbidden, unauthorized, not found, and conflict are not retryable by default.
- [ ] Rename internal comments/docs to describe message as fallback technical copy, not final UI copy.
- [ ] Add a short host guidance section:
  - [ ] Storefront host maps `Status`/`Code` to localized copy.
  - [ ] Storefront host chooses toast, inline error, full error page, or retry CTA.
  - [ ] Runtime does not decide language or final UX.
- [ ] Update tests:
  - [ ] timeout exposes `Retryable = true`.
  - [ ] validation preserves `FieldErrors`.
  - [ ] conflict preserves conflict primitive.
  - [ ] API message still survives as fallback.

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

- [ ] Runtime errors expose enough state for localized Storefront UX.
- [ ] Runtime keeps technical fallback copy without owning final storefront copy.
- [ ] Existing V2 and Starter consumers still compile.

## Phase SRH6 - Split Catalog Content Facade By Capability

Goal: reduce `IStorefrontRuntimeCatalogContentFacade` scope without changing endpoint behavior.

### Target Interfaces

- [ ] `IStorefrontRuntimeCatalogFacade`
  - [ ] published categories
  - [ ] category tree
  - [ ] category by slug
  - [ ] catalog products
  - [ ] filter metadata
  - [ ] search suggestions
  - [ ] product by slug
  - [ ] product by ID
  - [ ] product selection preview if current consumers treat it as catalog/product runtime.
- [ ] `IStorefrontRuntimeContentFacade`
  - [ ] published page by slug/system name
  - [ ] page navigation links if currently page-owned.
- [ ] `IStorefrontRuntimeNavigationFacade`
  - [ ] menu tree/menu items
  - [ ] active navigation support if Runtime owns it.
- [ ] `IStorefrontRuntimeSeoFacade`
  - [ ] SEO settings/defaults
  - [ ] redirect resolution
  - [ ] canonical/route resolver helpers if currently in Runtime.

### Tasks

- [ ] Record current consumers of `IStorefrontRuntimeCatalogContentFacade`.
- [ ] Create new capability-specific interfaces in Runtime.
- [ ] Move implementation code mechanically into capability-specific classes:
  - [ ] `StorefrontRuntimeCatalogFacade`
  - [ ] `StorefrontRuntimeContentFacade`
  - [ ] `StorefrontRuntimeNavigationFacade`
  - [ ] `StorefrontRuntimeSeoFacade`
- [ ] Keep old `IStorefrontRuntimeCatalogContentFacade` as a compatibility adapter for one phase if consumers are numerous.
- [ ] Mark compatibility adapter with an implementation comment and test, not a permanent abstraction.
- [ ] Update V2 generated adapters or services to inject the new specific facade where the consuming class only needs one capability.
- [ ] Do not change public Storefront API routes or generated DTOs.
- [ ] Do not add a new NuGet package.
- [ ] Keep tests for old and new registrations until V2/Starter migration is complete.

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

- [ ] Runtime no longer has one catalog/content/navigation/SEO god facade as the primary API.
- [ ] Existing V2 and Starter flows keep working.
- [ ] Compatibility adapter has a planned removal trigger.

## Phase SRH7 - Split Capability Registration

Goal: let hosts opt into runtime capabilities while preserving current all-in registration convenience.

### Tasks

- [ ] Add registration methods:
  - [ ] `AddStorefrontCatalogRuntime`
  - [ ] `AddStorefrontContentRuntime`
  - [ ] `AddStorefrontNavigationRuntime`
  - [ ] `AddStorefrontSeoRuntime`
  - [ ] `AddStorefrontCartRuntime`
  - [ ] `AddStorefrontCheckoutRuntime`
  - [ ] `AddStorefrontAccountRuntime`
  - [ ] `AddStorefrontConfigurationRuntime`
  - [ ] `AddStorefrontPaymentRuntime`
  - [ ] `AddStorefrontConsentRuntime`
  - [ ] `AddStorefrontAddressRuntime`
- [ ] Each capability registration should register only:
  - [ ] generated clients needed by that capability.
  - [ ] facades needed by that capability.
  - [ ] small shared helpers required by the capability.
- [ ] Add `AddStorefrontPlatformRuntime` as the intentional all-capability registration.
- [ ] Keep `AddStorefrontServerGeneratedClients` as compatibility wrapper to `AddStorefrontPlatformRuntime` during migration.
- [ ] Keep `AddStorefrontGeneratedClients` as compatibility wrapper for Starter during migration.
- [ ] Add tests that prove:
  - [ ] catalog-only registration does not register checkout/payment/cart facades.
  - [ ] cart-only registration registers cart client/facade and shared runtime primitives.
  - [ ] platform registration resolves all current facades.
  - [ ] old wrapper methods still work.
- [ ] Avoid implicit duplicate registration conflicts:
  - [ ] repeated calls should be safe or documented.
  - [ ] prefer `TryAdd` only when it matches current DI behavior.

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

- [ ] Hosts can register only the Runtime capabilities they need.
- [ ] Existing all-in Runtime registration still works.
- [ ] DI tests prove capability boundaries.

## Phase SRH8 - V2, Starter, Docs, And Boundary Adoption

Goal: make active consumers use the new Runtime shape without breaking existing local run behavior.

### Tasks

- [ ] Update `Storefront.V2` registration:
  - [ ] prefer `AddStorefrontPlatformRuntime` if V2 still needs all current capabilities.
  - [ ] or use explicit capability registration if V2 composition is already split.
  - [ ] do not call both old and new all-in wrappers.
- [ ] Update `Storefront.Starter` registration:
  - [ ] use `AddStorefrontPlatformRuntime` for starter simplicity, or
  - [ ] use a minimal explicit set if Starter has intentionally narrow capabilities.
- [ ] Update generated storefront docs/rules so future `Storefront.{Name}` projects:
  - [ ] use Runtime only from server/BFF project.
  - [ ] use Components/Browser for WASM/browser behavior.
  - [ ] do not guess API response shape.
  - [ ] do not reference Commerce Node API, Control Plane API, Application, Domain, Infrastructure, or `Web.SharedV2`.
- [ ] Update architecture docs:
  - [ ] `docs/architecture/05-project-and-folder-guide.md`
  - [ ] `docs/architecture/10-v2-contract-ownership.md`
  - [ ] `docs/architecture/11-storefront-builder.md` if generated storefront guidance changes.
- [ ] Update QA checklist:
  - [ ] `QA-StorefrontV2.todo.md` includes Runtime DI/cancellation/envelope/browser-boundary cases.
  - [ ] StorefrontBuilder QA docs include server-only Runtime consumption rule if generated storefronts are involved.
- [ ] Decide whether to mark old wrapper names obsolete:
  - [ ] only after V2 and Starter call the new preferred method.
  - [ ] do not remove wrappers until a later cleanup phase.

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
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

### Done When

- [ ] Active server storefront consumers use the new preferred Runtime registration path.
- [ ] Browser/WASM boundary remains explicit.
- [ ] Docs explain Runtime as server-only BFF integration.

## Phase SRH9 - Full QA And Release Gate

Goal: prove hardening did not break Storefront V2 runtime flows or package consumers.

### Static And Unit Verification

- [ ] Run Runtime focused tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests"
```

- [ ] Run generated client tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests"
```

- [ ] Run architecture/boundary tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~Architecture|FullyQualifiedName~Storefront"
```

- [ ] Run builds:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
```

### Browser Verification If Runtime Flow Changes Touch V2

- [ ] Start V2 local runtime:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Run targeted Playwright Storefront V2 browser QA:
  - [ ] home/store bootstrap loads with current store context.
  - [ ] catalog page loads products through Runtime generated client.
  - [ ] product detail loads product and SEO data through Runtime generated client.
  - [ ] add-to-cart works.
  - [ ] cart WASM component calls same-origin BFF only.
  - [ ] checkout start/review/place-order COD still works if checkout Runtime code changed.
  - [ ] account WASM component calls same-origin BFF only if account registration/runtime changed.
  - [ ] payment result/order completion still resolves through Storefront API.
  - [ ] network assertion: browser does not call Commerce Node host directly.
  - [ ] network assertion: browser does not call Control Plane.

### Done When

- [ ] Runtime source contains no `Activator.CreateInstance`.
- [ ] Runtime source contains no envelope `GetProperty` reflection.
- [ ] Runtime source contains no JSON serialize/deserialize DTO projection.
- [ ] Runtime source contains no old cancellation catch filter.
- [ ] Runtime error primitives expose retry/localization-ready state.
- [ ] Runtime facade interfaces are capability-scoped.
- [ ] Runtime registration is capability-scoped with compatibility wrapper.
- [ ] Storefront.WASM cannot reference Runtime or Commerce Node generated clients.
- [ ] Storefront V2 and Starter build.
- [ ] Focused Storefront tests pass.
- [ ] Browser QA passes if V2 runtime behavior was exercised.

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
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj --no-restore
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~StorefrontRuntimeResultPrimitiveTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontWasmRuntimeFoundationTests|FullyQualifiedName~StorefrontGeneratedClientFoundationTests|FullyQualifiedName~StorefrontGeneratedCatalogContentClientTests|FullyQualifiedName~StorefrontGeneratedConfigurationClientTests"
```

If Runtime changes are released with generated-client OpenAPI changes, also run:

```powershell
.\scripts\qa\run-storefront-client-regeneration-gate.ps1
```

If V2 runtime behavior changes are browser-visible, also run Storefront V2 Playwright release cases from the active release QA checklist.

## Completion Checklist

- [ ] Runtime generated-client registration is typed and compile-time-safe.
- [ ] Runtime generated-client registration still supports V2 and Starter.
- [ ] Runtime envelope mapping is typed.
- [ ] Runtime does not use reflection to read generated envelopes.
- [ ] Runtime does not JSON roundtrip generated DTOs for projection.
- [ ] Runtime cancellation propagates caller aborts.
- [ ] Runtime timeout mapping is explicit and test-covered.
- [ ] Runtime error result supports localized storefront UX through stable primitives.
- [ ] Runtime facades are capability-scoped.
- [ ] Capability registration methods exist.
- [ ] All-in registration remains available as a compatibility wrapper.
- [ ] WASM/server boundary guardrails prevent Runtime usage in browser project.
- [ ] Architecture docs and QA checklists are updated.
- [ ] Storefront V2, Starter, Runtime, Client, Components, and WASM builds pass.

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
