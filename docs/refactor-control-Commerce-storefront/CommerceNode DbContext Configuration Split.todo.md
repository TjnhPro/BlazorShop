# CommerceNode DbContext Configuration Split

Status: in-progress
Date: 2026-07-19
Purpose: tach EF Core mapping cua `CommerceNodeDbContext` ra cac `IEntityTypeConfiguration<T>` theo aggregate, giu nguyen runtime model va migration snapshot, khong doi schema.

## Codebase Baseline

- `BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDbContext.cs` hien dai khoang 2,524 dong.
- `CommerceNodeDbContext.OnModelCreating` bat dau tai `CommerceNodeDbContext.cs` va co khoang 157 lenh `modelBuilder.Entity...`.
- `CommerceNodeDbContext` dang own active V2 ecommerce persistence theo `docs/architecture/04-data-ownership.md`.
- Repo da co pattern `IEntityTypeConfiguration<T>`:
  - `BlazorShop.Infrastructure/Data/Configurations/ProductConfiguration.cs`.
  - `BlazorShop.Infrastructure/Data/Configurations/CategoryConfiguration.cs`.
  - `BlazorShop.Infrastructure/Data/Configurations/SeoRedirectConfiguration.cs`.
  - `BlazorShop.Infrastructure/Data/Configurations/Admin/*`.
- `CommerceNodeDbContext` hien dang apply mot so shared/legacy configurations bang explicit calls:
  - `CategoryConfiguration`.
  - `ProductConfiguration`.
  - `SeoRedirectConfiguration`.
  - `SeoSettingsConfiguration`.
  - `StoreSeoSettingsConfiguration`.
  - `AdminAuditLogConfiguration`.
  - `AdminSettingsConfiguration`.
- `AppDbContext` hien dang goi `ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly)`. Day la rui ro neu them Commerce Node configs cung assembly ma khong loc namespace.
- `CommerceNodeDbContextModelTests` da co nhieu model metadata tests cho table, index, FK, check constraints va seed data.
- `MigrationModelConsistencyTests` hien chi check legacy `AppDbContext`; Commerce Node can co gate tuong tu de dam bao refactor khong sinh pending model changes.
- Mot so tests dang doc text truc tiep tu `CommerceNodeDbContext.cs`, vi du:
  - Check `Order.DetailUrl` trong default message template seed.
  - Check `DbSet<StorefrontConsentState>` / `DbSet<StorefrontConsentEvent>`.

## Problem

`CommerceNodeDbContext` dang la god mapping file. Moi phase ecommerce them feature moi lai them mapping, seed, index, check constraint vao cung mot file:

```text
CommerceNodeDbContext
  DbSet declarations
  OnModelCreating
    shared config calls
    catalog overrides
    customers
    cart
    checkout
    payment
    orders
    shipment
    store
    media
    messages
    consent/security
    shipping
    seed data
    helpers
```

He qua:

- Migration review kho vi thay doi nho nam trong file lon.
- Merge conflict cao khi nhieu phase cung them mapping.
- Mapping theo aggregate kho doc va kho test rieng.
- `AppDbContext` assembly scan co the apply nham Commerce Node configs neu tach khong co filter.

## Target Shape

```text
BlazorShop.Infrastructure/Data/CommerceNode/
  CommerceNodeDbContext.cs
  Configurations/
    Catalog/
      ProductCommerceNodeConfiguration.cs
      ProductVariantConfiguration.cs
      CategoryStoreScopeConfiguration.cs
      VariationTemplateConfiguration.cs
      VariationTemplateOptionConfiguration.cs
      VariationTemplateValueConfiguration.cs
    Customers/
      CommerceCustomerConfiguration.cs
      CommerceCustomerAddressConfiguration.cs
    Cart/
      CartSessionConfiguration.cs
      CartLineConfiguration.cs
    Checkout/
      CheckoutSessionConfiguration.cs
    Payments/
      PaymentAttemptConfiguration.cs
      PaymentProviderEventConfiguration.cs
      PaymentAttemptAuditLogConfiguration.cs
      PaymentMethodConfiguration.cs
      StorePaymentMethodConfiguration.cs
    Orders/
      OrderCommerceNodeConfiguration.cs
      OrderLineConfiguration.cs
      OrderHistoryEntryConfiguration.cs
    Shipping/
      ShipmentConfiguration.cs
      ShipmentItemConfiguration.cs
      ShipmentTrackingEventConfiguration.cs
      StoreShippingSettingsConfiguration.cs
    Stores/
      CommerceStoreConfiguration.cs
      CommerceStoreDomainConfiguration.cs
      StoreFeatureStateConfiguration.cs
      StoreCurrencyConfiguration.cs
      StoreCurrencyExchangeRateConfiguration.cs
    Media/
      ProductMediaConfiguration.cs
      CommerceMediaAssetConfiguration.cs
      CategoryMediaAssignmentConfiguration.cs
    Content/
      StorefrontPageConfiguration.cs
      StoreSeoSlugHistoryConfiguration.cs
      StoreNavigationMenuConfiguration.cs
      StoreNavigationMenuItemConfiguration.cs
    ProductImports/
      ProductImportJobConfiguration.cs
      ProductImportRowConfiguration.cs
    Messages/
      MessageTemplateConfiguration.cs
      QueuedMessageConfiguration.cs
      StoreEmailSettingsConfiguration.cs
    SecurityPrivacy/
      StorefrontConsentStateConfiguration.cs
      StorefrontConsentEventConfiguration.cs
      StoreSecurityPrivacySettingsConfiguration.cs
    Tasks/
      CommerceTaskConfiguration.cs
      CommerceTaskStepConfiguration.cs
    Deployment/
      StoreDeploymentConfiguration.cs
      StorefrontDeploymentImageConfiguration.cs
    Identity/
      CommerceNodeIdentityConfiguration.cs
    Seed/
      CommerceNodeSeedData.cs
    CommerceNodeModelBuilderExtensions.cs
```

`CommerceNodeDbContext.OnModelCreating` should become:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyCommerceNodeConfigurations();
}
```

Suggested extension:

```csharp
internal static class CommerceNodeModelBuilderExtensions
{
    public static ModelBuilder ApplyCommerceNodeConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CommerceNodeDbContext).Assembly,
            type => type.Namespace?.StartsWith(
                "BlazorShop.Infrastructure.Data.CommerceNode.Configurations",
                StringComparison.Ordinal) == true);

        return modelBuilder;
    }
}
```

Important: do not let `AppDbContext` discover Commerce Node configurations.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Refactor type | Mechanical mapping extraction | Goal la maintainability, khong doi schema/behavior. |
| Folder owner | `BlazorShop.Infrastructure/Data/CommerceNode/Configurations` | Commerce Node owns active V2 ecommerce persistence. |
| Apply style | Filtered `ApplyConfigurationsFromAssembly` behind extension | Tranh `AppDbContext` apply nham configs cung assembly. |
| DbSet declarations | Giu trong `CommerceNodeDbContext` | DbContext van la public persistence surface cho repositories/tests. |
| First phase | Guardrails + one small aggregate pilot | Chung minh snapshot khong doi truoc khi tach rong. |
| Seed data | Tach sau, giu literal IDs/timestamps | Seed data de sinh migration neu thay doi nho. |
| Migration output | Khong tao migration moi | Refactor dung la runtime model == snapshot. |
| Tests | Model/snapshot tests, khong Playwright | Day la persistence mapping refactor, khong doi browser behavior. |

## Phase 0 - Guardrails Before Extraction

Goal: them gate de biet refactor co doi EF model hay khong.

Tasks:

- [x] Add Commerce Node migration consistency tests tuong tu `MigrationModelConsistencyTests`:
  - [x] Create `CommerceNodeDbContext`.
  - [x] Resolve `IMigrationsModelDiffer`.
  - [x] Resolve `IDesignTimeModel`.
  - [x] Load `BlazorShop.Infrastructure.Data.CommerceNode.Migrations.CommerceNodeDbContextModelSnapshot`.
  - [x] Compare snapshot relational model with design-time relational model.
  - [x] Assert no operations.
- [x] Add a specific test for `context.Database.HasPendingModelChanges()` for `CommerceNodeDbContext` if provider/test setup supports it.
- [x] Record current counts:
  - [x] `CommerceNodeDbContext.cs` line count.
  - [x] `modelBuilder.Entity` count.
  - [x] list of entity groups to migrate.
- [x] Identify text-based tests reading `CommerceNodeDbContext.cs`.
- [x] Convert text-based tests to model metadata tests where possible before moving content.

Exit criteria:

- [x] Commerce Node model consistency test passes before extraction.
- [x] Existing `CommerceNodeDbContextModelTests` pass.
- [x] No code mapping has moved yet except test guardrails.

Phase 0 evidence:

- Added `CommerceNodeMigrationModelConsistencyTests` for:
  - `CommerceNodeDbContext.Database.HasPendingModelChanges()`.
  - Runtime relational model versus `CommerceNodeDbContextModelSnapshot`.
- Baseline inventory:
  - `CommerceNodeDbContext.cs` line count: 2164.
  - `modelBuilder.Entity` count: 157.
  - Planned migration groups remain the phase scopes in this file: payments, messages/email, store/currency/security/shipping, cart/checkout/customer/address, orders/fulfillment, catalog/media/content/imports, identity/tasks/deployment.
- Text-based test dependencies found:
  - `StorefrontOrderEmailE2ERunnerTests` looked for `Order.DetailUrl` inside `CommerceNodeDbContext.cs`.
  - `SecurityPrivacyPhase3ConsentTests` looked for `DbSet<StorefrontConsentState>` and `DbSet<StorefrontConsentEvent>` inside `CommerceNodeDbContext.cs`.
- Converted those tests to EF design-time seed/model metadata and `CommerceNodeDbContext` DbSet reflection.
- Focused command passed 41/41 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeMigrationModelConsistencyTests|FullyQualifiedName~CommerceNodeDbContextModelTests|FullyQualifiedName~StorefrontOrderEmailE2ERunnerTests|FullyQualifiedName~SecurityPrivacyPhase3ConsentTests" --no-restore --nologo --verbosity minimal`

## Phase 1 - Infrastructure For Filtered Configuration Apply

Goal: them folder/config apply mechanism ma chua move nhieu mapping.

Tasks:

- [x] Add `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/CommerceNodeModelBuilderExtensions.cs`.
- [x] Implement filtered namespace scan for only `BlazorShop.Infrastructure.Data.CommerceNode.Configurations`.
- [x] Update `CommerceNodeDbContext.OnModelCreating` to call extension only after pilot configs exist.
- [x] Keep explicit existing shared config calls temporarily:
  - [x] `CategoryConfiguration`.
  - [x] `ProductConfiguration`.
  - [x] `SeoRedirectConfiguration`.
  - [x] `SeoSettingsConfiguration`.
  - [x] `StoreSeoSettingsConfiguration`.
  - [x] `AdminAuditLogConfiguration`.
  - [x] `AdminSettingsConfiguration`.
- [x] Add a test proving `AppDbContext` model does not include Commerce Node-only table names from new configs.
- [x] Do not change `AppDbContext` unless this test shows assembly scan risk.

Exit criteria:

- [x] `AppDbContext` legacy model unchanged.
- [x] `CommerceNodeDbContext` model unchanged.
- [x] No migration generated by EF due to this phase.

Phase 1 evidence:

- Added namespace-filtered `ApplyCommerceNodeConfigurations()` under `BlazorShop.Infrastructure.Data.CommerceNode.Configurations`.
- `CommerceNodeDbContext` now calls the filtered extension while existing shared config calls remain explicit.
- Updated `AppDbContext.ApplyConfigurationsFromAssembly` to scan only `BlazorShop.Infrastructure.Data.Configurations`, preventing future Commerce Node aggregate configs from leaking into the legacy/default context.
- Added `CommerceNodeConfigurationBoundaryTests` to lock both filtered scans.
- Focused command passed 40/40 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~CommerceNodeConfigurationBoundaryTests|FullyQualifiedName~CommerceNodeMigrationModelConsistencyTests|FullyQualifiedName~MigrationModelConsistencyTests|FullyQualifiedName~CommerceNodeDbContextModelTests" --no-restore --nologo --verbosity minimal`

## Phase 2 - Pilot Aggregate: Payments

Goal: tach mot aggregate co mapping ro rang nhung khong qua lon.

Scope:

- [x] `PaymentAttemptConfiguration`.
- [x] `PaymentProviderEventConfiguration`.
- [x] `PaymentAttemptAuditLogConfiguration`.
- [x] `PaymentMethodConfiguration`.
- [x] `StorePaymentMethodConfiguration`.

Tasks:

- [x] Move exact fluent API from `CommerceNodeDbContext` into the new configuration files.
- [x] Move `PaymentMethod.HasData(...)` seed into `PaymentMethodConfiguration` or `Seed/CommerceNodeSeedData.cs`.
- [x] Keep existing seed IDs, display order, provider system names, active flags and timestamps exactly.
- [x] Remove moved payment blocks from `OnModelCreating`.
- [x] Run focused tests:
  - [x] `dotnet test BlazorShop.Tests --filter Payment`.
  - [x] `dotnet test BlazorShop.Tests --filter CommerceNodeDbContextModelTests`.
  - [x] Commerce Node migration consistency test.

Exit criteria:

- [x] `PaymentAttempt`, `PaymentProviderEvent`, `PaymentAttemptAuditLog`, `PaymentMethod`, `StorePaymentMethod` model metadata unchanged.
- [x] No pending model changes.
- [x] `CommerceNodeDbContext` line count drops.

Phase 2 evidence:

- Added payment configuration files under `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/Payments`.
- Moved `PaymentMethod` seed into `PaymentMethodConfiguration` with the existing IDs/keys/names/descriptions/default flags/sort order unchanged.
- Removed payment blocks from `CommerceNodeDbContext.OnModelCreating` while keeping DbSet declarations.
- `CommerceNodeDbContext.cs` line count dropped from 2164 to 1975.
- `modelBuilder.Entity` count dropped from 157 to 151.
- Focused command passed 139/141 with 2 existing skips:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~Payment|FullyQualifiedName~CommerceNodeDbContextModelTests|FullyQualifiedName~CommerceNodeMigrationModelConsistencyTests" --no-restore --nologo --verbosity minimal`

## Phase 3 - Messages And Email Settings

Goal: tach transactional message/email mapping va sua test text-based dang doc DbContext.

Scope:

- [x] `MessageTemplateConfiguration`.
- [x] `QueuedMessageConfiguration`.
- [x] `StoreEmailSettingsConfiguration`.
- [x] `CommerceNodeSeedData` for default message templates.

Tasks:

- [x] Move `CreateDefaultMessageTemplates` and `CreateMessageTemplate` into seed helper.
- [x] Preserve default template IDs, public IDs, system names, subjects, bodies, created/updated timestamps.
- [x] Preserve `Order.DetailUrl` seed token.
- [x] Update tests that search `Order.DetailUrl` in `CommerceNodeDbContext.cs` to read model seed data or the new seed helper file.
- [x] Verify `MessageTemplate_HasTransactionalTemplateMappingAndSeeds`.
- [x] Verify `StoreEmailSettings_HasOneSettingsRowPerStoreAndSecretSafeColumns`.

Exit criteria:

- [x] Message/email model metadata unchanged.
- [x] Default template seed data unchanged.
- [x] No test depends on message seed text being inside `CommerceNodeDbContext.cs`.

Phase 3 evidence:

- Added message/email config files under `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/Messages`.
- Added `CommerceNodeSeedData` under `Configurations/Seed` for default transactional message templates.
- Preserved default template IDs, public IDs, system names, subject/body text, descriptions, and `2026-07-17T00:00:00Z` timestamps.
- Preserved `{{Order.DetailUrl}}` in the order placed template body.
- `StorefrontOrderEmailE2ERunnerTests` already reads design-time seed data after Phase 0 and no longer depends on seed text being in `CommerceNodeDbContext.cs`.
- `CommerceNodeDbContext.cs` line count dropped from 1975 to 1760.
- `modelBuilder.Entity` count dropped from 151 to 148.
- Focused command passed 150/150 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~Message|FullyQualifiedName~Email|FullyQualifiedName~CommerceNodeDbContextModelTests|FullyQualifiedName~CommerceNodeMigrationModelConsistencyTests" --no-restore --nologo --verbosity minimal`

## Phase 4 - Store Runtime, Currency, Security, Shipping

Goal: tach store configuration groups da duoc them trong cac phase MVP-to-real-use gan day.

Scope:

- [x] `CommerceStoreConfiguration`.
- [x] `CommerceStoreDomainConfiguration`.
- [x] `StoreFeatureStateConfiguration`.
- [x] `StoreCurrencyConfiguration`.
- [x] `StoreCurrencyExchangeRateConfiguration`.
- [x] `StorefrontConsentStateConfiguration`.
- [x] `StorefrontConsentEventConfiguration`.
- [x] `StoreSecurityPrivacySettingsConfiguration`.
- [x] `StoreShippingSettingsConfiguration`.

Tasks:

- [x] Move exact table names, column names, max lengths, defaults, indexes and check constraints.
- [x] Preserve check constraints:
  - [x] store status.
  - [x] default currency code length.
  - [x] consent/security settings where present.
  - [x] shipping surcharge policy where present.
- [x] Update tests that read `DbSet<StorefrontConsentState>` / `DbSet<StorefrontConsentEvent>` from `CommerceNodeDbContext.cs`; DbSet lines should remain, but if tests inspect mapping text, move to model metadata.
- [x] Run focused tests:
  - [x] `dotnet test BlazorShop.Tests --filter StoreFeatureState`.
  - [x] `dotnet test BlazorShop.Tests --filter StoreCurrency`.
  - [x] `dotnet test BlazorShop.Tests --filter SecurityPrivacy`.
  - [x] `dotnet test BlazorShop.Tests --filter StoreShippingSettings`.

Exit criteria:

- [x] Store lifecycle/readiness model unchanged.
- [x] Currency model unchanged.
- [x] Consent/security/privacy model unchanged.
- [x] Shipping settings model unchanged.
- [x] No pending model changes.

Phase 4 evidence:

- Added store runtime configuration files under `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/Stores`.
- Added consent/security configuration files under `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/SecurityPrivacy`.
- Added `StoreShippingSettingsConfiguration` under `BlazorShop.Infrastructure/Data/CommerceNode/Configurations/Shipping`.
- Removed the moved store, currency, consent/security/privacy and shipping settings blocks from `CommerceNodeDbContext.OnModelCreating`; DbSet declarations remain.
- Preserved existing check constraints for store status, store default currency code, feature key, currency code/rounding, exchange rate currency/rate/effective windows, and store domain status.
- Consent/security and shipping settings had no existing check constraints for settings or surcharge policy in the source mapping, so this phase did not add new constraints.
- `CommerceNodeDbContext.cs` line count is now 1761.
- `modelBuilder.Entity` count dropped from 148 to 139.
- Focused command passed 119/119 tests:
  - `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~StoreFeatureState|FullyQualifiedName~StoreCurrency|FullyQualifiedName~SecurityPrivacy|FullyQualifiedName~StoreShippingSettings|FullyQualifiedName~CommerceNodeDbContextModelTests|FullyQualifiedName~CommerceNodeMigrationModelConsistencyTests" --no-restore --nologo --verbosity minimal`

## Phase 5 - Cart, Checkout, Customer, Address

Goal: tach customer/cart/checkout tables theo runtime flow V2.

Scope:

- [ ] `CommerceCustomerConfiguration`.
- [ ] `CommerceCustomerAddressConfiguration`.
- [ ] `CartSessionConfiguration`.
- [ ] `CartLineConfiguration`.
- [ ] `CheckoutSessionConfiguration`.

Tasks:

- [ ] Move exact mapping blocks.
- [ ] Preserve token hash indexes and cart uniqueness.
- [ ] Preserve checkout `jsonb` columns and default JSON SQL.
- [ ] Preserve cart/customer FK delete behaviors.
- [ ] Run focused tests:
  - [ ] `dotnet test BlazorShop.Tests --filter StorefrontCart`.
  - [ ] `dotnet test BlazorShop.Tests --filter StorefrontCheckout`.
  - [ ] `dotnet test BlazorShop.Tests --filter StorefrontCustomer`.
  - [ ] `dotnet test BlazorShop.Tests --filter Address`.

Exit criteria:

- [ ] Cart/checkout/customer tests pass.
- [ ] No schema diff.
- [ ] No change to V2 cart/checkout runtime APIs.

## Phase 6 - Orders And Fulfillment

Goal: tach order placement/read model persistence mapping.

Scope:

- [ ] `OrderCommerceNodeConfiguration`.
- [ ] `OrderLineConfiguration`.
- [ ] `OrderHistoryEntryConfiguration`.
- [ ] `ShipmentConfiguration`.
- [ ] `ShipmentItemConfiguration`.
- [ ] `ShipmentTrackingEventConfiguration`.

Tasks:

- [ ] Preserve existing legacy table casing where present:
  - [ ] `Shipments`.
  - [ ] `ShipmentItems`.
  - [ ] `ShipmentTrackingEvents`.
- [ ] Preserve order snapshot columns and precision.
- [ ] Preserve guest access token/reference fields and indexes.
- [ ] Preserve order history customer visibility defaults.
- [ ] Run focused tests:
  - [ ] `dotnet test BlazorShop.Tests --filter Order`.
  - [ ] `dotnet test BlazorShop.Tests --filter Shipment`.
  - [ ] Commerce Node migration consistency test.

Exit criteria:

- [ ] Order placement/query tests pass.
- [ ] Shipment tests pass.
- [ ] No pending model changes.

## Phase 7 - Catalog, Media, Content, Imports

Goal: tach cac cum con lai co nhieu index/constraint lien quan storefront catalog.

Scope:

- [ ] `ProductCommerceNodeConfiguration`.
- [ ] `ProductVariantConfiguration`.
- [ ] `CategoryStoreScopeConfiguration`.
- [ ] `VariationTemplateConfiguration`.
- [ ] `VariationTemplateOptionConfiguration`.
- [ ] `VariationTemplateValueConfiguration`.
- [ ] `ProductMediaConfiguration`.
- [ ] `CommerceMediaAssetConfiguration`.
- [ ] `CategoryMediaAssignmentConfiguration`.
- [ ] `StorefrontPageConfiguration`.
- [ ] `StoreSeoSlugHistoryConfiguration`.
- [ ] `StoreNavigationMenuConfiguration`.
- [ ] `StoreNavigationMenuItemConfiguration`.
- [ ] `ProductImportJobConfiguration`.
- [ ] `ProductImportRowConfiguration`.

Tasks:

- [ ] Merge Product mapping carefully because `ProductConfiguration` already exists and Commerce Node adds overrides.
- [ ] Decide one of:
  - [ ] Keep shared `ProductConfiguration` then add `ProductCommerceNodeConfiguration` for Commerce Node-only additions.
  - [ ] Move all Commerce Node product mapping into a single Commerce Node config and stop applying shared `ProductConfiguration` from Commerce Node.
- [ ] Avoid duplicate indexes when shared and Commerce Node configs both configure Product.
- [ ] Preserve media usage table names and unique indexes.
- [ ] Preserve storefront page slug/template/navigation mapping.
- [ ] Preserve product import indexes/check constraints.
- [ ] Run focused tests:
  - [ ] `dotnet test BlazorShop.Tests --filter Product`.
  - [ ] `dotnet test BlazorShop.Tests --filter Category`.
  - [ ] `dotnet test BlazorShop.Tests --filter Media`.
  - [ ] `dotnet test BlazorShop.Tests --filter StorefrontPage`.
  - [ ] `dotnet test BlazorShop.Tests --filter Navigation`.

Exit criteria:

- [ ] Catalog model metadata unchanged.
- [ ] Media/content/import tests pass.
- [ ] No duplicate index or conflicting default warnings.

## Phase 8 - Identity, Tasks, Deployment, And Cleanup

Goal: tach infrastructure support mappings va don sach DbContext.

Scope:

- [ ] `CommerceNodeIdentityConfiguration`.
- [ ] `RefreshTokenConfiguration`.
- [ ] `CommerceTaskConfiguration`.
- [ ] `CommerceTaskStepConfiguration`.
- [ ] `StoreDeploymentConfiguration`.
- [ ] `StorefrontDeploymentImageConfiguration`.

Tasks:

- [ ] Preserve Identity column max lengths for login/token providers.
- [ ] Preserve refresh token indexes and columns.
- [ ] Preserve task table names/check constraints/task step mapping.
- [ ] Preserve default `StorefrontDeploymentImage` seed.
- [ ] Remove unused `using` statements from `CommerceNodeDbContext`.
- [ ] Ensure `CommerceNodeDbContext` contains mostly:
  - [ ] constructor.
  - [ ] DbSet declarations.
  - [ ] short `OnModelCreating`.
- [ ] Run full infrastructure test set.

Exit criteria:

- [ ] `CommerceNodeDbContext.cs` is small and readable.
- [ ] No moved mapping remains duplicated in `OnModelCreating`.
- [ ] No pending model changes.

## Phase 9 - Final Verification And Release Gate

Goal: prove refactor did not change schema, migration snapshot, runtime behavior, or tests.

Verification:

- [ ] `dotnet test BlazorShop.Tests --filter CommerceNodeDbContextModelTests`.
- [ ] `dotnet test BlazorShop.Tests --filter MigrationModelConsistencyTests` after Commerce Node consistency tests are added.
- [ ] `dotnet test BlazorShop.Tests --filter CommerceNode`.
- [ ] Full `dotnet test` before final commit.
- [ ] Optional manual check:
  - [ ] Run `dotnet ef migrations add CommerceNodeConfigurationSplitCheck --context CommerceNodeDbContext --project BlazorShop.Infrastructure --startup-project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
  - [ ] Confirm generated migration is empty.
  - [ ] Delete the check migration before commit.

Release gate:

- [ ] No real migration committed for this refactor.
- [ ] `CommerceNodeDbContextModelSnapshot.cs` unchanged.
- [ ] Runtime relational model equals snapshot model.
- [ ] `AppDbContext` model unchanged.
- [ ] `CommerceNodeDbContext` still owns all DbSet declarations.
- [ ] New configuration files are internal and under Commerce Node namespace.
- [ ] No feature behavior changes.

## Failure Modes Registry

| Risk | Symptom | Prevention |
| --- | --- | --- |
| `AppDbContext` applies Commerce Node configs | Legacy model gets new tables/indexes or pending model changes | Use namespace-filtered apply for Commerce Node and test AppDbContext model remains unchanged. |
| EF model changes accidentally | Empty refactor generates migration operations | Add Commerce Node migration consistency test before moving mappings. |
| Seed data changes | EF generates UpdateData/DeleteData/InsertData | Keep literal IDs/timestamps/system names exact; verify design-time seed data. |
| Product config order changes | Duplicate/missing indexes or default values | Treat Product as a special phase; avoid broad scan until behavior is locked. |
| Text-based tests fail | Tests look for moved strings in old DbContext file | Convert to model metadata or new seed/config file checks. |
| Check constraints lost | Database accepts invalid status/mode values | Model tests assert design-time check constraints by name. |
| Table casing changes | Runtime queries/migrations target wrong table | Preserve exact `ToTable` names including legacy PascalCase shipment tables. |
| Too-large mechanical PR | Review becomes as hard as the old file | Split by aggregate phases and verify after each phase. |

## Test Diagram

```text
CommerceNodeDbContext
  -> ApplyCommerceNodeConfigurations()
      -> filtered namespace scan
      -> aggregate IEntityTypeConfiguration<T>
      -> seed helper

Verification
  -> CommerceNodeDbContextModelTests
  -> Commerce Node model snapshot consistency
  -> focused aggregate tests
  -> no generated migration

Safety boundary
  -> AppDbContext does not apply Commerce Node configs
```

## Implementation Checklist

- [x] Phase 0 guardrails complete.
- [x] Phase 1 filtered configuration apply complete.
- [x] Phase 2 payments pilot complete.
- [x] Phase 3 messages/email complete.
- [x] Phase 4 store/currency/security/shipping complete.
- [ ] Phase 5 cart/checkout/customer/address complete.
- [ ] Phase 6 orders/fulfillment complete.
- [ ] Phase 7 catalog/media/content/imports complete.
- [ ] Phase 8 identity/tasks/deployment cleanup complete.
- [ ] Phase 9 final verification complete.

## Not In Scope

- [ ] Do not change domain entity properties.
- [ ] Do not change table names, column names, indexes, constraints, defaults, precision, FK delete behavior, or seed values.
- [ ] Do not generate or commit a real migration.
- [ ] Do not move Commerce Node persistence to another DbContext.
- [ ] Do not touch legacy `AppDbContext` behavior except to protect it from accidental config scan if needed.
- [ ] Do not refactor repositories/services while moving EF mapping.
- [ ] Do not combine this with feature work.
- [ ] Do not run Playwright for this refactor unless a later phase changes visible UI behavior.

## Decision Audit Trail

- Mechanical extraction is approved because the current file is a maintainability bottleneck.
- Namespace-filtered configuration discovery is required because `AppDbContext` scans the same assembly.
- `DbSet<>` declarations stay in `CommerceNodeDbContext` to preserve repository/test ergonomics.
- Seed data moves only after model consistency tests exist.
- Product mapping is deferred because shared `ProductConfiguration` and Commerce Node overrides currently both configure `Product`.
- Migration snapshot must remain unchanged throughout the refactor.
