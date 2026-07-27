# Storefront V2 Manual Client Retirement Todo

Status: In Progress
Owner: Storefront Platform
Created: 2026-07-27
Scope: retire `StorefrontApiClient` manual transport from `BlazorShop.Storefront.V2`

## Context da xac minh

- `BlazorShop.Storefront.Presentation` da dang ky generated adapters cho cac Presentation contracts:
  - `IStorefrontAddressClient` -> `GeneratedStorefrontAddressClient`
  - `IStorefrontCartClient` -> `GeneratedStorefrontCartClient`
  - `IStorefrontCatalogClient` -> `GeneratedStorefrontCatalogContentClient`
  - `IStorefrontCheckoutClient` -> `GeneratedStorefrontCheckoutClient`
  - `IStorefrontContentClient` -> `GeneratedStorefrontCatalogContentClient`
  - `IStorefrontCustomerClient` -> `GeneratedStorefrontCustomerClient`
  - `IStorefrontPaymentClient` -> `GeneratedStorefrontPaymentClient`
  - `IStorefrontStoreConfigurationClient` -> `GeneratedStorefrontConfigurationClient`
  - `IStorefrontConsentClient` -> `GeneratedStorefrontConsentClient`
- `BlazorShop.Storefront.V2` van con `Services/StorefrontApiClient*.cs`.
- `StorefrontApiClient` van implement nhieu `IStorefront*Client` Presentation contracts.
- V2 composition root van dang ky `services.AddHttpClient<StorefrontApiClient>()`.
- `StorefrontApiOptions.EnableLegacyFallback` van ton tai trong options va `appsettings*.json`.
- `StorefrontApiTransport` va `StorefrontApiRoutes` van giu fallback route cu nhu `/api/public/catalog`.
- `docs/storefront-platform/storefront-client-exception-registry.md` hien noi V2 khong con manual transport exception, nen code va docs dang lech nhau.
- Source production khong con consumer hop le ro rang ngoai DI registration va chinh concrete client, nhung test suite van dung `StorefrontApiClient` lam fixture/mocking path.

## Muc tieu

- [ ] Khong con `StorefrontApiClient` trong `BlazorShop.Storefront.V2`.
- [ ] Khong class nao trong V2 implement Presentation `IStorefront*Client` contracts.
- [ ] V2 khong con manual Commerce Node Storefront API transport.
- [ ] V2 khong con legacy fallback transport hoac route constants.
- [ ] Presentation/Runtime generated transport la duong canonical duy nhat.
- [ ] V2, Starter, architecture tests, isolation gates va browser COD regression van pass.

## Khong lam trong phase nay

- [ ] Khong doi Commerce Node Storefront API contract.
- [ ] Khong rewrite cart/checkout/order/payment business flow.
- [ ] Khong doi visual layout cua Storefront V2.
- [ ] Khong tach them package moi.
- [ ] Khong xoa options host-local van can cho store key, base URL hoac auth/session neu Presentation van dung.
- [ ] Khong xoa historical docs neu noi dung duoc danh dau ro la lich su.

## Phase F1.25.0 - Baseline va guardrail do

Muc tieu: khoa blocker bang test/gate truoc khi xoa code, de viec retire khong chi la cleanup thu cong.

- [x] Chay baseline search va luu ket qua vao QA note:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|AddHttpClient<StorefrontApiClient>|LegacyCatalogBaseRoute|LegacySeoSettingsRoute" BlazorShop.PresentationV2 BlazorShop.Tests.V2 docs/storefront-platform docs/refactor-control-Commerce-storefront -g "!bin" -g "!obj"
```

- [x] Them focused architecture test cam `StorefrontApiClient` trong source V2 active:

```text
BlazorShop.PresentationV2/BlazorShop.Storefront.V2
  must not contain:
  - StorefrontApiClient
  - EnableLegacyFallback
  - AddHttpClient<StorefrontApiClient>
  - LegacyCatalogBaseRoute
  - LegacySeoSettingsRoute
```

- [x] Them focused architecture test cam V2 class implement Presentation client contracts:

```text
BlazorShop.Storefront.V2 source must not contain:
  : IStorefrontAddressClient
  : IStorefrontCartClient
  : IStorefrontCatalogClient
  : IStorefrontCheckoutClient
  : IStorefrontConsentClient
  : IStorefrontContentClient
  : IStorefrontCustomerClient
  : IStorefrontPaymentClient
  : IStorefrontStoreConfigurationClient
```

- [x] Them focused architecture test cam V2 tu dung manual Commerce Node Storefront route strings:

```text
V2 host may expose same-origin BFF endpoints.
V2 host must not construct Commerce Node API transport routes directly.
```

- [x] Chap nhan test moi fail tam thoi o phase nay neu chua xoa client.
- [x] Ghi ro trong test name rang fail nay la blocker MVP, khong phai canh bao nhe.

2026-07-27 F1.25.0 evidence:

- Baseline `rg` found active V2 manual transport in `StorefrontApiClient*.cs`, `StorefrontApiRoutes.cs`, `StorefrontApiTransport.cs`, `StorefrontServiceCollectionExtensions.cs`, `StorefrontApiOptions.cs`, `appsettings*.json`, plus test fixtures and historical docs/backlog entries.
- Added `StorefrontManualClientRetirementGuardrailTests` with three focused blocker tests:
  - `MvpBlocker_StorefrontV2ManualClientTransport_MustBeRetired`
  - `MvpBlocker_StorefrontV2Classes_MustNotImplementPresentationClientContracts`
  - `MvpBlocker_StorefrontV2Host_MustNotConstructCommerceNodeStorefrontRoutesDirectly`
- Ran `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter FullyQualifiedName~StorefrontManualClientRetirementGuardrailTests`.
- Expected red baseline: `2` failed, `1` passed. Failures prove the still-active `StorefrontApiClient`, `EnableLegacyFallback`, `AddHttpClient<StorefrontApiClient>`, and legacy route constants are release blockers.

## Phase F1.25.1 - Consumer audit va replacement map

Muc tieu: phan loai tat ca usages truoc khi xoa, tranh xoa nham fixture dang bao ve behavior that.

- [x] Audit production source:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|LegacyCatalogBaseRoute|LegacySeoSettingsRoute|AddHttpClient<StorefrontApiClient>" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"
```

- [x] Audit tests:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|LegacyCatalogBaseRoute|AddHttpClient<StorefrontApiClient>" BlazorShop.Tests.V2 -g "!bin" -g "!obj"
```

- [x] Audit docs/backlog:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|manual client|manual transport|legacy fallback" docs/storefront-platform docs/refactor-control-Commerce-storefront docs/architecture -g "*.md"
```

- [x] Lap bang replacement cho moi consumer test:

| Consumer | Current dependency | Replacement | Expected owner |
| --- | --- | --- | --- |
| `StorefrontV2HostSmokeTests.ReplaceStorefrontApiClients` | concrete `StorefrontApiClient` | fake Presentation interfaces hoac fake Runtime facade | Test fixture |
| `StorefrontV2ApiClientTests` | manual transport behavior | Runtime facade / Presentation generated adapter tests | Runtime/Presentation |
| `StorefrontDisplayContextProviderTests` | concrete client | fake `IStorefrontStoreConfigurationClient` | Presentation contract |
| `StorefrontCurrentStoreProviderTests` | concrete client | fake `IStorefrontStoreConfigurationClient` | Presentation contract |
| `StorefrontPageNavigationProviderTests` | concrete client | fake `IStorefrontContentClient` | Presentation contract |
| `StorefrontCommerceFlowCutoverTests` | reads manual client source | negative guardrail only | Architecture test |
| `StorefrontIndependenceBoundaryTests` | expects `AddHttpClient<StorefrontApiClient>` | invert assertion to forbid it | Architecture test |
| `StorefrontHostCompositionTests` | documented exception allow-list | invert assertion to no exception files | Architecture test |

- [x] Xac minh generated/Runtime path co day du nhung capability ma manual client tung che:
  - [x] current store/configuration.
  - [x] catalog/search/product/page/navigation/SEO.
  - [x] cart session, add, update, remove, recalculate, merge current customer.
  - [x] checkout start, load, address update, shipping, payment, review, place order.
  - [x] payment method discovery va payment attempt.
  - [x] customer profile, address book, order list, order detail, receipt.
  - [x] consent read/write.
- [x] Neu phat hien gap that, bo sung vao `BlazorShop.Storefront.Presentation` hoac `BlazorShop.Storefront.Runtime` truoc khi xoa V2 manual client.
- [x] Khong them adapter moi trong V2 de thay the `StorefrontApiClient`.

2026-07-27 F1.25.1 evidence:

- Production audit found `StorefrontApiClient` only in V2 DI registration, `StorefrontApiOptions.EnableLegacyFallback`, `appsettings*.json`, and the manual client partial/route/transport files.
- Test audit found concrete-client fixtures in `StorefrontV2HostSmokeTests`, `StorefrontV2ApiClientTests`, provider tests, and cutover/ownership guardrails that still read or require the manual source.
- Docs audit found active docs/backlog entries that must be converted in F1.25.5, plus historical foundation/shared-platform plans that may keep marked historical mentions.
- Runtime/Presentation replacement coverage exists:
  - `GeneratedStorefrontConfigurationClient` and `StorefrontRuntimeConfigurationFacade` cover current store/configuration/currency.
  - `GeneratedStorefrontCatalogContentClient` and catalog/content/navigation/SEO runtime facades cover catalog/search/product/page/navigation/SEO.
  - `GeneratedStorefrontCartClient` and `StorefrontRuntimeCartFacade` cover cart session, CRUD, recalculate, validate, and merge current customer with bearer support.
  - `GeneratedStorefrontCheckoutClient` and `StorefrontRuntimeCheckoutFacade` cover checkout start/load/address update/shipping/payment/review/place order with optional bearer support.
  - `GeneratedStorefrontPaymentClient` and `StorefrontRuntimePaymentFacade` cover payment methods and attempts.
  - `GeneratedStorefrontCustomerClient`, Runtime account generated clients, `StorefrontAuthClient`, and `StorefrontSessionResolver` cover profile/address/order/receipt/auth/session flows.
  - `GeneratedStorefrontConsentClient` and `StorefrontRuntimeConsentFacade` cover consent read/save/revoke.
- No capability gap was found that requires a new V2 adapter.

## Phase F1.25.2 - Chuyen test fixture ra khoi concrete client

Muc tieu: tests phai mo phong Presentation/Runtime boundary, khong tiep tuc dung V2 manual transport lam mock backend.

- [x] Refactor `StorefrontV2HostSmokeTests`:
  - [x] Xoa helper dang `new StorefrontApiClient(...)`.
  - [x] Xoa registrations bind `IStorefront*Client` ve concrete `StorefrontApiClient`.
  - [x] Dung fake/stub Presentation client interfaces cho host smoke.
  - [x] Chi mock data can cho route/render dang test.
  - [x] Giu lai coverage cho account/cart/checkout/order rendering.
- [x] Refactor `StorefrontV2ApiClientTests`:
  - [x] Xoa tests chi verify manual route/fallback behavior.
  - [x] Chuyen behavior can giu sang Runtime facade tests neu behavior thuoc transport/result mapping.
  - [x] Chuyen behavior can giu sang Presentation generated adapter tests neu behavior thuoc contract projection.
  - [x] Xoa constructor helper `CreateApiClient`.
- [x] Refactor provider/page/navigation tests:
  - [x] `StorefrontDisplayContextProviderTests` dung fake `IStorefrontStoreConfigurationClient`.
  - [x] `StorefrontCurrentStoreProviderTests` dung fake `IStorefrontStoreConfigurationClient`.
  - [x] `StorefrontPageNavigationProviderTests` dung fake `IStorefrontContentClient`.
- [x] Refactor cutover/ownership tests:
  - [x] `StorefrontHostCompositionTests` khong con allow-list `Services/StorefrontApiClient*.cs`.
  - [x] `StorefrontIndependenceBoundaryTests` doi assertion thanh forbid `AddHttpClient<StorefrontApiClient>`.
  - [x] `StorefrontContractOwnershipTests` xoa assertion yeu cau manual exceptions trong QA docs.
  - [x] `StorefrontCommerceFlowCutoverTests` xoa source read cua manual client va thay bang assertion generated/Presentation path.
- [x] Chay focused test subset sau khi refactor fixtures:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests|FullyQualifiedName~StorefrontHostCompositionTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontContractOwnershipTests|FullyQualifiedName~StorefrontCommerceFlowCutoverTests"
```

2026-07-27 F1.25.2 evidence:

- `StorefrontV2HostSmokeTests` no longer uses `StorefrontApiClient` fixture construction or binds `IStorefront*Client` to the retired concrete client. Account smoke coverage now uses `StubStorefrontCustomerClient`; non-account HTTP backend dependencies still use `ConfigureStorefrontGeneratedClient` with focused handlers.
- Deleted `StorefrontV2ApiClientTests.cs` because it only asserted handwritten transport/fallback route behavior. Runtime facade and Presentation generated adapter assertions now carry the retained behavior surface.
- Provider tests now use typed fake Presentation interfaces:
  - `StorefrontDisplayContextProviderTests` and `StorefrontCurrentStoreProviderTests` fake `IStorefrontStoreConfigurationClient`.
  - `StorefrontPageNavigationProviderTests` fakes `IStorefrontContentClient`.
- Cutover/ownership guardrails were inverted away from manual client exception allow-lists and source reads. They now point at Runtime/Presentation generated paths and will go green after F1.25.3/F1.25.4 remove the remaining production source.
- Verification:
  - `dotnet build BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore -v:minimal` passed with existing `MessagePack` NU1902/NU1903 warnings.
  - Provider slice passed: `11` passed, `0` failed.
  - Account smoke slice passed: `7` passed, `0` failed.
  - Guardrail/cutover slice compiled and ran: `29` passed, `3` expected failures still blocking on F1.25.3/F1.25.4 source and DI removal.

## Phase F1.25.3 - Go bo DI registration va legacy fallback options

Muc tieu: cat duong resolve DI cua manual client truoc khi xoa files.

- [x] Sua `BlazorShop.Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs`:
  - [x] Xoa `services.AddHttpClient<StorefrontApiClient>()`.
  - [x] Dam bao V2 host van goi Presentation/Runtime registration canonical.
  - [x] Khong dang ky bat ky concrete V2 transport nao cho `IStorefront*Client`.
- [x] Sua `BlazorShop.Storefront.V2/Options/StorefrontApiOptions.cs`:
  - [x] Xoa `EnableLegacyFallback`.
  - [x] Giu nhung option con can cho host configuration neu con consumer hop le.
  - [x] Neu ca `StorefrontApiOptions` chi phuc vu manual client, lap follow-up xoa ca options sau khi kiem tra `Api` section usage.
- [x] Sua `appsettings.json` va `appsettings.Development.json`:
  - [x] Xoa `Api:EnableLegacyFallback`.
  - [x] Giu `Api:BaseUrl` neu Runtime/Presentation host configuration van can de goi Commerce Node.
  - [x] Giu `Api:RefreshTokenRoute` neu auth/session resolver van can.
- [x] Chay `rg` de dam bao khong con DI registration:

```powershell
rg -n "AddHttpClient<StorefrontApiClient>|EnableLegacyFallback" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.Tests.V2 -g "!bin" -g "!obj"
```

2026-07-27 F1.25.3 evidence:

- Removed `services.AddHttpClient<StorefrontApiClient>()` from V2 composition while keeping canonical `AddStorefrontRuntime(...)`, `AddStorefrontPlatformRuntime()`, and `AddStorefrontPresentation(...)`.
- Removed `EnableLegacyFallback` from `StorefrontApiOptions` and from V2 `appsettings.json` / `appsettings.Development.json`.
- Kept `Api:BaseUrl`, `Api:StoreKey`, and `Api:RefreshTokenRoute`; `StorefrontApiEndpointResolver`, `StorefrontStoreKeyResolver`, and `StorefrontSessionResolver` still consume them.
- Retired client source is still present until F1.25.4, but its constructor no longer reads fallback options so the intermediate phase builds.
- `rg -n "AddHttpClient<StorefrontApiClient>|EnableLegacyFallback" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 BlazorShop.Tests.V2 -g "!bin" -g "!obj"` returned no matches.
- `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore -v:minimal` passed.

## Phase F1.25.4 - Xoa manual client source

Muc tieu: xoa hoan toan handwritten V2 application transport va route fallback.

- [ ] Xoa cac files:
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Address.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Cart.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Catalog.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Checkout.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Configuration.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Consent.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Content.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Customer.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiClient.Payment.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiTransport.cs`
  - [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Services/StorefrontApiRoutes.cs`
- [ ] Sau khi xoa, chay source gate:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|LegacyCatalogBaseRoute|LegacySeoSettingsRoute" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"
```

- [ ] Expected: no matches.
- [ ] Build V2 de bat compile errors som:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
```

## Phase F1.25.5 - Cap nhat docs, registry va checklist

Muc tieu: docs khong con mo ta transitional exception da xoa.

- [ ] Cap nhat `docs/storefront-platform/storefront-client-exception-registry.md`:
  - [ ] Giu trang thai `none | none`.
  - [ ] Xoa references xem `StorefrontApiClient` la exception hien tai.
  - [ ] Ghi evidence moi: V2 source gate forbids `StorefrontApiClient`.
- [ ] Cap nhat `docs/storefront-platform/storefront-v2-generated-client-backlog.md`:
  - [ ] Mark address/cart/checkout/consent/customer/payment manual path la retired.
  - [ ] Neu con gap generated-client thuc su, ghi bang Runtime/Presentation backlog, khong ghi V2 manual client.
- [ ] Cap nhat `docs/refactor-control-Commerce-storefront/Storefront Presentation Cutover Completion.todo.md`:
  - [ ] Them link toi plan nay nhu prerequisite cleanup truoc khi dong cutover.
  - [ ] Khong lap lai toan bo F1.25 trong file cu.
- [ ] Cap nhat `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`:
  - [ ] Xoa muc QA yeu cau manual `StorefrontApiClient`.
  - [ ] Them gate V2 khong co manual transport.
  - [ ] Them browser network assertion: browser chi goi same-origin BFF/static endpoints, khong goi Commerce Node API direct.
- [ ] Cap nhat historical QA/plans neu can:
  - [ ] Giua noi dung da qua va hien tai bang dong "Historical note".
  - [ ] Khong de historical note bi tests doc nhu active exception.

## Phase F1.25.6 - Build, unit va architecture verification

Muc tieu: chung minh xoa manual client khong lam hong host composition va package boundaries.

- [ ] Build core storefront packages:

```powershell
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Client/BlazorShop.Storefront.Client.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Runtime/BlazorShop.Storefront.Runtime.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/BlazorShop.Storefront.Presentation.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj
dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/BlazorShop.Storefront.Starter.csproj
```

- [ ] Chay focused Storefront architecture tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontPresentationCutoverGuardrailTests|FullyQualifiedName~StorefrontHostCompositionTests|FullyQualifiedName~StorefrontIndependenceBoundaryTests|FullyQualifiedName~StorefrontContractOwnershipTests|FullyQualifiedName~StorefrontSharedPlatformPackageContractTests|FullyQualifiedName~StorefrontCommerceFlowCutoverTests"
```

- [ ] Chay focused V2 host smoke tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontV2HostSmokeTests"
```

- [ ] Chay Starter host smoke tests:

```powershell
dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --filter "FullyQualifiedName~StorefrontStarterHostSmokeTests"
```

- [ ] Chay isolation gates neu scripts ton tai trong repo:

```powershell
.\scripts\qa\run-storefront-foundation-isolation-gate.ps1
.\scripts\qa\run-storefront-builder-isolation-gate.ps1
```

- [ ] Chay source gates cuoi:

```powershell
rg -n "StorefrontApiClient|EnableLegacyFallback|LegacyCatalogBaseRoute|LegacySeoSettingsRoute" BlazorShop.PresentationV2/BlazorShop.Storefront.V2 -g "!bin" -g "!obj"
rg -n "AddHttpClient<StorefrontApiClient>|GetRequiredService<StorefrontApiClient>|new StorefrontApiClient" BlazorShop.PresentationV2 BlazorShop.Tests.V2 -g "!bin" -g "!obj"
```

- [ ] Expected:
  - [ ] V2 source: no matches.
  - [ ] Tests: no active fixture still instantiates/registers concrete `StorefrontApiClient`.
  - [ ] Docs: only historical notes may mention old client.

## Phase F1.25.7 - Browser COD va network regression

Muc tieu: vi cart/account/checkout/order flow tung dung manual client trong tests, phai verify bang trinh duyet that, khong chi smoke.

- [ ] Start V2 local stack:

```powershell
.\scripts\run-v2-local.ps1 -StopExisting
```

- [ ] Browser QA bang Playwright:
  - [ ] Home page render dung store hien tai.
  - [ ] Product list/detail render dung catalog data.
  - [ ] Add-to-cart tu product detail.
  - [ ] Cart page load, update quantity, remove item.
  - [ ] Checkout start tu cart.
  - [ ] Guest checkout COD place order that tren store test.
  - [ ] Order completed page hien thi order number/reference.
  - [ ] Account login.
  - [ ] Customer account order list/detail doc duoc order cua user dung.
  - [ ] Password recovery UI route load va submit theo policy hien tai.
  - [ ] Register disabled policy khong cho submit neu store config khoa dang ky.
- [ ] Browser network audit:
  - [ ] Browser khong goi direct Commerce Node route `api/storefront/stores/{storeKey}/*`.
  - [ ] Browser chi goi same-origin BFF/static/media routes cua Storefront host.
  - [ ] Checkout mutation requests co antiforgery/session behavior dung theo Storefront Presentation.
  - [ ] No unexpected legacy routes:
    - [ ] `/api/public/catalog`
    - [ ] `/api/seo/settings`
    - [ ] legacy cart/checkout/order compatibility routes neu khong con active.
- [ ] Luu QA ket qua vao `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` hoac release E2E checklist phu hop.
- [ ] Stop local stack neu workflow yeu cau:

```powershell
.\scripts\stop-v2-local.ps1
```

## Final definition of done

- [ ] `BlazorShop.Storefront.V2` khong con file/class/string `StorefrontApiClient`.
- [ ] `BlazorShop.Storefront.V2` khong co class implement Presentation `IStorefront*Client`.
- [ ] `BlazorShop.Storefront.V2` khong dang ky `AddHttpClient<StorefrontApiClient>`.
- [ ] `BlazorShop.Storefront.V2` khong con `EnableLegacyFallback`.
- [ ] `BlazorShop.Storefront.V2` khong con legacy route fallback constants.
- [ ] Presentation DI graph host-independent pass.
- [ ] Runtime generated-client/facade tests pass.
- [ ] V2 host smoke pass.
- [ ] Starter host smoke pass.
- [ ] Storefront architecture tests pass.
- [ ] Storefront isolation gates pass.
- [ ] Browser COD checkout/order regression pass.
- [ ] Browser network audit pass.
- [ ] Docs/registry/checklists khong con active exception noi V2 co manual transport.

## Risk controls

- [ ] Migrate test fixtures truoc khi delete source de tranh mat coverage.
- [ ] Khong xoa `Api:BaseUrl` neu no van la host configuration input cho Runtime/Presentation.
- [ ] Khong xoa `Api:RefreshTokenRoute` neu auth/session resolver con doc.
- [ ] Neu build fail do capability gap, fix gap trong `Storefront.Presentation` hoac `Storefront.Runtime`, khong tao lai V2 transport.
- [ ] Neu can giu mot historical doc mention, mark ro `Historical note` va exclude khoi active guardrail input.
- [ ] Khong them compatibility alias moi de "qua test"; test phai di theo canonical generated path.

## Autoplan decision audit

| Decision | Ket luan | Ly do |
| --- | --- | --- |
| Xoa hay obsolete `StorefrontApiClient` | Xoa | V2 host khong phai public package contract; giu obsolete van tao duong reactivation. |
| Dat replacement o dau | Presentation/Runtime | Day la layer so huu contract adapters va server/BFF integration primitives. |
| Test fixture dung gi | Fake Presentation interfaces hoac Runtime facades | Tests can verify host behavior ma khong phu thuoc manual transport. |
| Legacy fallback lam gi | Xoa cung manual client | Fallback chi phuc vu path cu va mau thuan voi V2 route ownership. |
| QA can browser khong | Co | Cart/account/checkout/order tung lien quan manual client; smoke/build khong du de bat loi integration. |
| Co rewrite business flow khong | Khong | Phase nay la retire transport path, khong doi commerce behavior. |
