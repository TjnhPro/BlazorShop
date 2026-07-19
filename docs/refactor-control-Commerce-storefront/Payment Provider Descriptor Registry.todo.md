# Payment Provider Descriptor Registry

Status: proposed
Date: 2026-07-19
Purpose: chuyen payment capability catalog tu registry hard-code sang provider-tu-cong-bo descriptor, de them provider moi nhu PayPal ma khong phai sua `PaymentProviderCapabilityRegistry`.

## Codebase Baseline

- Payment provider contract moi da co huong dung:
  - `IStorefrontPaymentProvider` trong `BlazorShop.Application/CommerceNode/Payments/CommerceNodePaymentDtos.cs`.
  - `ProviderKey`.
  - Provider operations: validate, create session, hosted session, return/cancel/webhook, authorize/capture/void/refund.
- Commerce Node DI dang register provider bang `IEnumerable<IStorefrontPaymentProvider>`:
  - `CodStorefrontPaymentProvider`.
  - `StripeStorefrontPaymentProvider`.
- `StorefrontPaymentProviderResolver` da dung `IEnumerable<IStorefrontPaymentProvider>` de resolve runtime provider theo key.
- `PaymentProviderCapabilityRegistry` da nhan `IEnumerable<IStorefrontPaymentProvider>` nhung chi lay provider key de biet installed.
- `PaymentProviderCapabilityRegistry.List()` hien van hard-code:
  - COD descriptor.
  - Stripe descriptor.
  - PayPal skeleton descriptor.
- `CommerceNodePaymentMethodService.DefaultMethods` cung dang hard-code:
  - COD.
  - Stripe.
  - PayPal skeleton.
- `CommerceNodeDbContext.HasData(PaymentMethod)` dang seed COD/Stripe/PayPal vao catalog database. Day la existing data seed, khong nen drop trong phase refactor nay.
- `PaymentWebhookSignatureVerifier` doc `RequiresWebhookSignature` tu capability registry, nen signature policy cung phai di theo provider descriptor.
- `IPayPalPaymentService` van ton tai cho legacy/shared payment area, nhung active V2 provider core chua co `PayPalStorefrontPaymentProvider`.

## Problem

Provider core hien co interface dung, nhung capability/default catalog van nam trong registry/service:

```text
IStorefrontPaymentProvider -> runtime operation
PaymentProviderCapabilityRegistry -> hard-code capability catalog
CommerceNodePaymentMethodService -> hard-code default store methods
```

He qua:

- Them provider moi van phai sua registry.
- Them PayPal provider that van phai sua hard-coded PayPal skeleton.
- Registry vua la aggregator vua la catalog owner, trai voi pattern provider plugin.
- Store default method sync van khong tu dong nhan provider moi.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Descriptor owner | Provider tu cong bo descriptor | Provider la noi hieu method type, supported operations, webhook requirement. |
| Registry role | Registry chi aggregate descriptors | Registry khong nen biet COD/Stripe/PayPal cu the. |
| Store defaults | Lay tu descriptors, khong hard-code `DefaultMethods` | Neu provider moi duoc register, store defaults co the sync additively. |
| Existing DB seed | Khong drop/remove migration seed cu | Tranh data churn va production data risk; phase nay la runtime refactor. |
| PayPal business logic | Khong implement PayPal provider that | Phase nay chi mo duong cho provider moi, khong lam payment adapter. |
| PayPal placeholder | Khong hard-code trong registry | Neu can hien PayPal chua cai, tao provider placeholder rieng hoac de existing row thanh unsupported. |
| Active semantics | Provider registered = installed; active/configured can be descriptor/config check | Khong nen xem registered class la da du secret provider. |
| API contract | Giu DTO public hien co | Refactor source of truth, khong doi response shape neu khong can. |

## Target Shape

```text
IStorefrontPaymentProvider
  - ProviderKey
  - Descriptor
  - operations

CodStorefrontPaymentProvider
  -> Descriptor: cod, offline, capture supported, no webhook signature

StripeStorefrontPaymentProvider
  -> Descriptor: stripe, redirect, capture supported, webhook signature required

PaymentProviderCapabilityRegistry
  -> IEnumerable<IStorefrontPaymentProvider>
  -> List/Get descriptors mapped to PaymentProviderCapabilityDto

CommerceNodePaymentMethodService
  -> capabilityRegistry.List()
  -> additive default StorePaymentMethod sync
```

Rules:

- `StorePaymentMethod` remains store-level configuration: enabled state, display name/order, public metadata, supported currencies/countries, settings.
- `PaymentProviderDescriptor` remains provider-level capability: method type, operation support, default display metadata, default display order, webhook requirement.
- Runtime payment operations continue to resolve through `IStorefrontPaymentProviderResolver`.
- Existing `PaymentProviderCapabilityDto` can remain public response DTO.
- Descriptor should not expose secrets, SMTP/passwords, Stripe keys, PayPal keys, private settings, or provider credentials.

## Phase 0 - Baseline And Guardrails

- [x] Confirm current hard-code points:
  - `PaymentProviderCapabilityRegistry.CreateCod`.
  - `PaymentProviderCapabilityRegistry.CreateStripe`.
  - `PaymentProviderCapabilityRegistry.CreatePayPalSkeleton`.
  - `CommerceNodePaymentMethodService.DefaultMethods`.
  - `CommerceNodeDbContext.HasData(PaymentMethod)` seed.
- [x] Confirm active providers registered in Commerce Node DI:
  - COD.
  - Stripe.
  - no active PayPal provider.
- [x] Add/adjust static guard tests:
  - registry source must not contain `CreateCod`, `CreateStripe`, or `CreatePayPalSkeleton` after refactor.
  - registry must build capabilities from `IStorefrontPaymentProvider.Descriptor`.
  - default store methods must be sourced from registry/capabilities.
- [x] Keep current tests that protect:
  - retired PayPal compatibility capture route.
  - no legacy `IPaymentHandler` in V2 active runtime.
  - Stripe webhook signature requirement.

Acceptance:

- [x] Baseline confirms exactly where provider hard-code lives.
- [x] No runtime behavior changes in Phase 0.
- [x] Focused payment tests are green before refactor.

Phase 0 evidence:

- `rg` confirmed current provider catalog hard-code lives in `PaymentProviderCapabilityRegistry.CreateCod`, `CreateStripe`, `CreatePayPalSkeleton`, `CommerceNodePaymentMethodService.DefaultMethods`, and `CommerceNodeDbContext` `PaymentMethod` seed rows.
- `BlazorShop.Infrastructure/Data/CommerceNode/DependencyInjection.cs` registers `CodStorefrontPaymentProvider` and `StripeStorefrontPaymentProvider` as `IStorefrontPaymentProvider`; no active `PayPalStorefrontPaymentProvider` registration exists.
- Guardrail coverage to preserve/refactor in later phases:
  - `PaymentProviderCapabilityRegistryTests` covers registry list/get behavior and will become the descriptor aggregation guard.
  - `PaymentProviderOperationContractTests` protects active provider operation contract and no legacy `IPaymentHandler` dependency.
  - `PaymentWebhookSignatureVerifierTests` protects provider-driven webhook signature policy.
  - `CommerceNodeStorefrontPaymentContractTests` and `StorefrontScopedPaymentWebhookHardeningTests` protect Storefront payment API/webhook contracts, including retired side-effecting compatibility behavior.
- Baseline command passed 15/15 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~PaymentProviderOperationContractTests|FullyQualifiedName~PaymentWebhookSignatureVerifierTests|FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests|FullyQualifiedName~StorefrontScopedPaymentWebhookHardeningTests" --no-restore --nologo --verbosity minimal`

## Phase 1 - Add Provider Descriptor Contract

- [x] Add provider-level descriptor contract in `BlazorShop.Application/CommerceNode/Payments/CommerceNodePaymentDtos.cs`.
- [x] Proposed shape:

```csharp
public sealed record PaymentProviderDescriptor(
    string SystemName,
    string DisplayName,
    string? Description,
    string? IconUrl,
    int DefaultDisplayOrder,
    IReadOnlyList<string> SupportedCurrencyCodes,
    IReadOnlyList<string> SupportedCountryCodes,
    decimal? MinOrderTotal,
    decimal? MaxOrderTotal,
    string MethodType,
    bool RecurringCapable,
    bool SupportsAuthorize,
    bool SupportsCapture,
    bool SupportsVoid,
    bool SupportsRefund,
    bool SupportsPartialRefund,
    bool RequiresWebhookSignature,
    bool ActiveByDefault = true);
```

- [x] Add `PaymentProviderDescriptor Descriptor { get; }` to `IStorefrontPaymentProvider`.
- [x] Keep `ProviderKey` for resolver compatibility, but add test that `ProviderKey == Descriptor.SystemName`.
- [x] Do not add secret/config values to descriptor.
- [x] Do not change existing public DTOs or API contracts yet.

Acceptance:

- [x] All provider implementations and test fake providers compile with descriptor.
- [x] Descriptor is provider metadata only, not store config and not secret config.
- [x] Existing payment operation contracts remain unchanged.

Phase 1 evidence:

- Added `PaymentProviderDescriptor` and `IStorefrontPaymentProvider.Descriptor`; existing `PaymentProviderCapabilityDto` and public API DTOs were not changed.
- Updated COD/Stripe providers and all active fake/minimal providers to expose descriptors.
- Added operation contract tests for `ProviderKey == Descriptor.SystemName` and descriptor property names avoiding secret/password/credential/settings metadata.
- Focused command passed 71/71 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderOperationContractTests|FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~PaymentWebhookSignatureVerifierTests|FullyQualifiedName~StorefrontCheckoutServiceTests" --no-restore --nologo --verbosity minimal`

## Phase 2 - Move COD And Stripe Capabilities Into Providers

- [x] Add descriptor to `CodStorefrontPaymentProvider`:
  - system name `cod`.
  - display name `Cash on Delivery`.
  - method type `offline`.
  - default order `10`.
  - supports capture `true`.
  - webhook signature `false`.
  - active by default `true`.
- [x] Add descriptor to `StripeStorefrontPaymentProvider`:
  - system name `stripe`.
  - display name `Stripe`.
  - method type `redirect`.
  - default order `20`.
  - supports capture `true`.
  - webhook signature `true`.
  - active by default `true` to represent registered provider runtime support; store-level default enabled state remains separate and is handled in Phase 4.
- [x] Update fake/minimal provider classes in tests to expose descriptor.
- [x] Add provider descriptor tests:
  - COD descriptor matches COD operation behavior.
  - Stripe descriptor requires webhook signature.
  - descriptor system name matches provider key.

Acceptance:

- [x] COD and Stripe capability data no longer needs registry helper methods.
- [x] Existing checkout/payment tests still pass.
- [x] No PayPal runtime behavior is introduced.

Phase 2 evidence:

- COD descriptor now mirrors offline/captured behavior: `cod`, `Cash on Delivery`, `offline`, order `10`, capture supported, no webhook signature, active by default.
- Stripe descriptor now mirrors redirect/webhook behavior: `stripe`, `Stripe`, `redirect`, order `20`, capture supported, webhook signature required, inactive by default.
- No PayPal provider or PayPal runtime operation was added.
- Focused command passed 80/80 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderOperationContractTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests|FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~PaymentAttemptServiceTests" --no-restore --nologo --verbosity minimal`

## Phase 3 - Refactor Capability Registry Into Aggregator

- [x] Change `PaymentProviderCapabilityRegistry` to store descriptors from `IEnumerable<IStorefrontPaymentProvider>`.
- [x] `List()` returns provider descriptors mapped to `PaymentProviderCapabilityDto`.
- [x] `Get(systemName)` resolves only from provider descriptors.
- [x] Remove registry-owned hard-coded methods:
  - `CreateCod`.
  - `CreateStripe`.
  - `CreatePayPalSkeleton`.
- [x] Define installed/active mapping clearly:
  - `Installed = true` for registered provider descriptor.
  - `Active = descriptor.ActiveByDefault` unless a later phase adds runtime configuration health.
- [x] If provider list contains duplicate `SystemName`, fail clearly at startup/registry construction or pick deterministic failure in `Get/List`.
- [x] Add tests:
  - fake provider descriptor appears in `List()` without registry edit.
  - unknown provider returns validation failure.
  - duplicate provider keys are rejected.
  - PayPal is not listed unless a PayPal provider is registered.

Acceptance:

- [x] Adding a new `IStorefrontPaymentProvider` implementation plus DI registration is enough for registry discovery.
- [x] Registry source no longer names COD, Stripe, or PayPal.
- [x] Webhook signature verifier still reads signature requirement correctly through registry.

Phase 3 evidence:

- Moved `PaymentProviderCapabilityRegistry` to its own file and removed registry-owned `CreateCod`, `CreateStripe`, and `CreatePayPalSkeleton` factories.
- Registry now validates `ProviderKey == Descriptor.SystemName`, rejects duplicate descriptor keys, sorts by descriptor display order/name, and maps descriptor metadata into the existing `PaymentProviderCapabilityDto`.
- PayPal is no longer synthesized by runtime discovery; existing store rows remain handled as unsupported by `CommerceNodePaymentMethodService`.
- Active semantics are explicit for this phase: registered provider descriptor means `Installed = true`; capability `Active = descriptor.ActiveByDefault`. Stripe uses `ActiveByDefault = true` so configured stores can still enable/use Stripe, while store default enabled state remains separate.
- Focused registry/payment method command passed 17/17 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~PaymentWebhookSignatureVerifierTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~CommerceNodePaymentMethodServiceCacheTests" --no-restore --nologo --verbosity minimal`
- Focused checkout/payment command passed 80/80 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~PaymentAttemptServiceTests|FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests|FullyQualifiedName~StorefrontScopedPaymentWebhookHardeningTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests" --no-restore --nologo --verbosity minimal`

## Phase 4 - Move Store Payment Defaults To Provider Descriptors

- [x] Replace `CommerceNodePaymentMethodService.DefaultMethods` hard-code with defaults generated from provider descriptors.
- [x] Ensure default sync remains additive:
  - create missing `StorePaymentMethod` rows for provider descriptors.
  - do not delete existing rows when a provider is removed from DI.
  - do not overwrite admin customized display name/order/metadata/settings.
- [x] Default enabled behavior:
  - COD follows descriptor active/default enabled behavior.
  - Stripe remains disabled by default unless descriptor explicitly says otherwise.
  - providers not registered do not get new default rows.
- [x] Existing PayPal rows:
  - Keep existing database rows.
  - Map them to unsupported capability if no PayPal provider registered.
  - Enabling unsupported PayPal must continue to fail.
- [x] Consider whether `PaymentMethods` catalog table should also sync from descriptors at runtime. If needed, sync additively only; do not drop or rewrite seed data.
- [x] Add tests:
  - new fake provider creates missing store method.
  - removed/unregistered provider row is retained but maps unsupported.
  - admin custom display metadata is not overwritten by default sync.
  - PayPal existing row cannot be enabled until provider exists.

Acceptance:

- [x] Store manager can see/add provider-backed payment methods without service hard-code.
- [x] Existing configured store methods are preserved.
- [x] No migration/data deletion required.

Phase 4 evidence:

- Added `EnabledByDefault` to `PaymentProviderDescriptor` so provider active/usable state stays separate from store default enabled state without changing public `PaymentProviderCapabilityDto`.
- `CommerceNodePaymentMethodService.EnsureDefaultsAsync` now reads provider descriptors through `IPaymentProviderCapabilityRegistry.ListDescriptors()` and additively creates only missing `StorePaymentMethod` rows.
- COD descriptor sets `EnabledByDefault = true`; Stripe remains active/usable but disabled by default.
- Existing unsupported rows, including PayPal rows from older data, are retained and mapped to unsupported capability; enabling them still fails with `Payment provider is not installed or active.`
- `PaymentMethods` catalog seed was left unchanged and no migration/data deletion was introduced.
- Focused provider/default command passed 28/28 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~CommerceNodePaymentMethodServiceCacheTests|FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~PaymentProviderOperationContractTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests" --no-restore --nologo --verbosity minimal`
- Focused checkout/payment command passed 100/100 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~StorefrontCartServiceTests|FullyQualifiedName~PaymentAttemptServiceTests|FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests" --no-restore --nologo --verbosity minimal`

## Phase 5 - Optional PayPal Placeholder Decision

- [x] Decide whether Control Plane should show PayPal as future/unavailable before a real PayPal provider exists.
- [x] If yes, create explicit `PayPalPlaceholderStorefrontPaymentProvider` - not chosen for this phase:
  - registered as `IStorefrontPaymentProvider`.
  - descriptor says `ActiveByDefault = false`.
  - operations return unsupported/not configured.
  - no provider secrets or PayPal API calls.
- [x] If no, remove PayPal skeleton from runtime discovery:
  - existing DB rows remain.
  - new stores only get registered provider defaults.
  - manager sees unsupported state for old PayPal rows.
- [x] Do not implement real PayPal create/capture/refund behavior in this phase.

Acceptance:

- [x] There is no hidden PayPal hard-code in registry.
- [x] PayPal visibility is an explicit provider registration decision.
- [x] Future real PayPal provider can replace placeholder by DI registration and descriptor/operations without registry edit.

Phase 5 evidence:

- Decision: do not add `PayPalPlaceholderStorefrontPaymentProvider` in this phase. PayPal should only appear as a provider-backed capability after an explicit provider registration exists.
- Removed PayPal skeleton from development store payment method defaults so new seeded stores no longer receive a PayPal store method from local seeding.
- Kept `CommerceNodeDbContext` `PaymentMethods` catalog seed and existing rows untouched; older PayPal `StorePaymentMethod` rows still map unsupported and cannot be enabled.
- `rg` confirmed no `PayPalStorefrontPaymentProvider`, no `PayPalPlaceholderStorefrontPaymentProvider`, and no `CreatePayPalSkeleton` runtime discovery remains.
- Focused command passed 76/76 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~StorefrontCheckoutServiceTests" --no-restore --nologo --verbosity minimal`

## Phase 6 - Storefront And Control Plane Contract Stability

- [x] Verify `StorefrontScopedPaymentsController.GetPaymentMethods` response remains safe public metadata only.
- [x] Verify `StorefrontScopedConfigurationController` public configuration projection still excludes settings/secret data.
- [x] Verify Control Plane payment method manager still receives:
  - capability installed/active.
  - method type.
  - operation support flags.
  - settings configured status.
- [x] Update Swagger/OpenAPI snapshots only if generated schema changes due to descriptor contract leaking into public DTOs. Expected: no public schema change.
- [x] Add static test that descriptor is not returned directly from public Storefront API if it would expose internal metadata.

Acceptance:

- [x] Storefront payment method API shape stays stable.
- [x] Control Plane payment manager behavior stays stable.
- [x] No provider secret or settings JSON appears in public responses.

Phase 6 evidence:

- Storefront payment/configuration controllers still map payment methods to Storefront contracts and do not return `PaymentProviderDescriptor`.
- Control Plane manager still uses `StorePaymentMethodDto`, which includes capability installed/active, method type, operation flags, and settings configured status without raw settings JSON in response payload.
- No Swagger/OpenAPI snapshot update was required because public DTO shapes used by Storefront payment/configuration endpoints did not change.
- Added static contract guard to ensure Storefront payment/configuration controller and contract sources do not expose `PaymentProviderDescriptor` or `SettingsJson`.
- Focused command passed 16/16 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests|FullyQualifiedName~StorefrontScopedPaymentWebhookHardeningTests" --no-restore --nologo --verbosity minimal`

## Phase 7 - Focused Verification

- [x] Run focused provider/core tests:
  - `PaymentProviderCapabilityRegistryTests`.
  - `PaymentProviderOperationContractTests`.
  - `PaymentWebhookSignatureVerifierTests`.
  - `StripeStorefrontPaymentProviderTests`.
  - `CommerceNodePaymentMethodSecretBoundaryTests`.
- [x] Run focused checkout/payment tests:
  - `StorefrontCheckoutServiceTests`.
  - `PaymentAttemptServiceTests`.
  - `CommerceNodeStorefrontPaymentContractTests`.
  - `StorefrontScopedPaymentWebhookHardeningTests`.
- [x] Run active V2 build:
  - `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.
  - `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`.
  - `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`.
- [x] Run Playwright only if payment method visibility/checkout UI changes:
  - Storefront checkout still lists COD.
  - COD order can still be placed.
  - Control Plane payment methods page still opens and shows capability state.

Acceptance:

- [x] New provider descriptor fake test proves no registry edit is required for provider discovery.
- [x] COD checkout remains green.
- [x] Stripe hosted provider behavior remains green.
- [x] PayPal compatibility capture route remains retired.

Phase 7 evidence:

- Provider/core tests passed 31/31:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~PaymentProviderCapabilityRegistryTests|FullyQualifiedName~PaymentProviderOperationContractTests|FullyQualifiedName~PaymentWebhookSignatureVerifierTests|FullyQualifiedName~StripeStorefrontPaymentProviderTests|FullyQualifiedName~CommerceNodePaymentMethodSecretBoundaryTests" --no-restore --nologo --verbosity minimal`
- Checkout/payment tests passed 76/76:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StorefrontCheckoutServiceTests|FullyQualifiedName~PaymentAttemptServiceTests|FullyQualifiedName~CommerceNodeStorefrontPaymentContractTests|FullyQualifiedName~StorefrontScopedPaymentWebhookHardeningTests" --no-restore --nologo --verbosity minimal`
- Active V2 builds passed with 0 warnings and 0 errors:
  - `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj`
  - `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
  - `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
  - `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
- Headed checkout/order E2E passed:
  - `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`
  - `.\scripts\qa\run-storefront-order-email-e2e.ps1`
  - Result artifact: `.gstack/qa-reports/order-email-e2e/result.json`.
  - Success COD order: `ORD-20260719-FFC6BF28`.
  - Network guardrails: `requestCount=3310`, `response5xxCount=0`, `retiredFlowCallCount=0`.

## Not In Scope

- [ ] Implementing real PayPal adapter.
- [ ] Adding Stripe account/secret management in descriptor.
- [ ] Adding provider configuration health dashboard.
- [ ] Dropping or rewriting `PaymentMethods` seed data.
- [ ] Deleting existing `StorePaymentMethod` rows.
- [ ] Changing public payment API response contracts unless tests require a safe additive field.
- [ ] Reintroducing `IPaymentHandler` or direct PayPal capture route.
- [ ] Moving payment provider operations out of Commerce Node runtime.

## Failure Modes Registry

| Failure mode | Risk | Mitigation |
| --- | --- | --- |
| Descriptor exposes secret/config values | High | Descriptor is static public-safe metadata only; settings stay in store payment config. |
| Stripe marked active even when secret missing | Medium | Keep active/default semantics conservative; defer config health to separate phase. |
| Existing PayPal rows disappear from manager | Medium | Do not delete rows; unsupported rows map safely. |
| New providers do not create store defaults | Medium | Source default sync from registry descriptors and test with fake provider. |
| Registry accepts duplicate provider keys | High | Add duplicate-key failure test. |
| Public API schema changes unintentionally | Medium | Run OpenAPI contract/snapshot tests. |
| Webhook signature policy regresses | High | Preserve verifier tests for Stripe required/COD optional. |
| Admin custom payment metadata overwritten | High | Add default-sync preservation test. |

## Test Diagram

| Codepath | Existing coverage | New/updated coverage |
| --- | --- | --- |
| Provider operation defaults | `PaymentProviderOperationContractTests` | Add descriptor/key consistency checks. |
| Capability registry list/get | `PaymentProviderCapabilityRegistryTests` | Replace hard-coded COD/Stripe/PayPal expectations with provider descriptor aggregation tests. |
| Webhook signature requirement | `PaymentWebhookSignatureVerifierTests` | Verify signature policy comes from descriptor. |
| Store payment default sync | `CommerceNodePaymentMethodSecretBoundaryTests`, cache tests | Add fake provider default row and preserve custom admin metadata tests. |
| Storefront payment methods | Storefront payment contract/API tests | Confirm public metadata still safe and stable. |
| Checkout provider availability | `StorefrontCheckoutServiceTests` | Confirm COD still available and unsupported provider cannot be enabled/used. |
| Control Plane payment manager | Control Plane payment client/page tests if present | Confirm capability flags remain visible. |

## Implementation Checklist

- [x] Phase 0 complete.
- [x] Phase 1 complete.
- [x] Phase 2 complete.
- [x] Phase 3 complete.
- [x] Phase 4 complete.
- [x] Phase 5 decision complete.
- [x] Phase 6 complete.
- [x] Phase 7 release gate complete.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Scope | Provider owns descriptor | Mechanical | DRY | Provider already owns operation behavior; duplicating capability in registry is the root issue. | Registry-owned COD/Stripe/PayPal descriptors. |
| 2 | Registry | Registry aggregates provider descriptors only | Mechanical | Explicit over clever | Adding provider should require DI registration, not editing registry code. | Static provider catalog in registry. |
| 3 | Store defaults | Generate default store payment methods from descriptors | Mechanical | Completeness | Removing registry hard-code alone still leaves manager/default setup hard-coded. | Keeping `CommerceNodePaymentMethodService.DefaultMethods`. |
| 4 | Data | Keep existing seeds/rows | Mechanical | Pragmatic | Refactor should not risk data loss or migration churn. | Dropping PayPal seed/rows in this phase. |
| 5 | PayPal | No real PayPal adapter in this phase | Mechanical | Scope control | Goal is registry extensibility; PayPal operations need separate provider implementation and QA. | Implementing PayPal create/capture/refund now. |
| 6 | Public API | Preserve public DTO shape | Mechanical | Compatibility | Storefront and Control Plane clients already consume current safe DTOs. | Exposing descriptor directly as public contract. |

## Release Gate

- [x] `IStorefrontPaymentProvider` exposes descriptor.
- [x] COD and Stripe providers expose correct descriptors.
- [x] `PaymentProviderCapabilityRegistry` contains no provider-specific factory methods.
- [x] Registry discovers fake/new provider through `IEnumerable<IStorefrontPaymentProvider>`.
- [x] Store payment defaults are sourced from descriptors, not a hard-coded array.
- [x] Existing PayPal rows are not deleted and cannot be enabled without active provider support.
- [x] Public Storefront configuration/payment method responses contain no secrets/settings JSON.
- [x] Focused provider/payment/checkout tests pass.
- [x] Active V2 builds pass.
