# Control Plane Commerce Gateway Split

## Mục Tiêu

Tách `ControlPlaneCommerceCatalog*` khỏi dạng god interface/controller/client hiện tại thành các gateway theo capability, nhưng không phá vỡ runtime V2 đang hoạt động.

Mục tiêu thực tế của phase này:

- Giữ đúng boundary bắt buộc: `ControlPlane.Web -> ControlPlane.API -> CommerceNode.API`.
- Không đưa Commerce Node base URL, node key, node secret, allowed IP, hoặc `api/commerce/*` vào Control Plane Web.
- Không đổi route public đang được Control Plane Web dùng trong phase đầu.
- Tạo transport chung cho Control Plane API gọi Commerce Node Admin để tránh copy logic.
- Tách dần service/controller/Web client theo cụm nghiệp vụ: product, category, media, order, content/page, navigation, store configuration, currency, payment, shipping, security/privacy.
- Giảm blast radius bằng các phase có thể build/test riêng.

## Codebase Baseline

Các số liệu đã kiểm tra trực tiếp:

- `BlazorShop.Application/ControlPlane/Catalog/ControlPlaneCommerceCatalogDtos.cs`
  - `IControlPlaneCommerceCatalogService` có 99 method.
  - File dài 595 dòng.
- `BlazorShop.Infrastructure/Data/ControlPlane/ControlPlaneCommerceCatalogService.cs`
  - File dài 1.938 dòng.
  - Đang chứa cả method gateway theo capability và logic transport: load store, validate node, append `storeKey`, add `X-Node-Key`/`X-Node-Secret`, parse Commerce Node envelope, multipart upload, media stream.
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/Controllers/ControlPlaneCommerceCatalogController.cs`
  - File dài 1.303 dòng.
  - Base route hiện tại là `api/control-plane/stores/{storePublicId:guid}/catalog`.
  - Đồng thời có nhiều absolute compatibility routes dạng `~/api/controlplane/commerce/stores/{storePublicId:guid}/...`.
  - Controller đang chứa products, categories, SEO, product import, media, variants, inventory, variation templates, pages, navigation, orders, payment methods, currencies, security/privacy, shipping, shipments.
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Catalog/ControlPlaneCatalogClient.cs`
  - File dài 1.780 dòng.
  - `IControlPlaneCatalogClient` có 89 method.
  - Nhiều page đang inject cùng client: `CommerceProducts`, `CommerceCategories`, `CommerceMediaLibrary`, `CommerceCurrencies`, `CommerceOrders`, `CommerceNavigation`, `CommercePaymentMethods`, `CommercePages`, `CommerceVariationTemplates`, `CommerceProductImports`.
- `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Stores/ControlPlaneStoreClient.cs`
  - Runtime store lifecycle hiện đã nằm trong `IControlPlaneStoreClient`.
  - Vì vậy phần runtime store trong `ControlPlaneCommerceCatalogController`/service là gateway API-side, còn Web-side đã có store client riêng.
- Tests hiện đang bám vào tên cũ:
  - `BlazorShop.Tests/Infrastructure/ControlPlane/ControlPlaneCommerceCatalogServiceStoreMappingTests.cs`
  - `BlazorShop.Tests/PresentationV2/ControlPlane/ControlPlaneCommerceCatalogControllerTests.cs`
  - `BlazorShop.Tests/PresentationV2/ControlPlane/ControlPlaneVariantAttributeWorkflowTests.cs`
  - `BlazorShop.Tests/PresentationV2/CommerceNode/SecurityPrivacyPhase6AdminManagementTests.cs`

## Autoplan Decisions

| Decision | Chọn | Lý do |
| --- | --- | --- |
| Transport chung trước hay tách controller trước | Transport chung trước | Đây là phần rủi ro nhất vì giữ node credentials và store scoping. Tách controller trước dễ copy logic gateway. |
| Đổi route ngay không | Không | Control Plane Web đang gọi `api/controlplane/commerce/stores/{storePublicId}/...`. Đổi route sớm sẽ tạo breaking change không cần thiết. |
| Tạo nhiều interface nhưng một implementation lớn không | Không | Chỉ cắt interface facade không giảm maintainability nếu file 1.938 dòng vẫn giữ toàn bộ behavior. |
| Dùng generic gateway quá trừu tượng không | Không | Transport nên generic vừa đủ cho HTTP/envelope/multipart. Capability gateway vẫn explicit method theo domain. |
| Tách theo mọi entity nhỏ không | Không | Product và variant/inventory/import có thể đi cùng Catalog/Product gateway trong phase đầu để tránh quá nhiều file nhỏ. |
| Có xóa DTO cũ ngay không | Không | DTO/result cũ nên được giữ hoặc di chuyển có kiểm soát để tránh phá nhiều references. |

## Kiến Trúc Đích

```text
ControlPlane.Web pages
  -> capability Web clients
     - IControlPlaneProductClient
     - IControlPlaneCategoryClient
     - IControlPlaneMediaClient
     - IControlPlaneOrderClient
     - IControlPlaneContentClient
     - IControlPlaneNavigationClient
     - IControlPlaneCurrencyClient
     - IControlPlanePaymentClient
     - IControlPlaneShippingClient
     - IControlPlaneSecurityPrivacyClient
  -> IControlPlaneApiClient
  -> ControlPlane.API capability controllers
     - ControlPlaneCommerceProductsController
     - ControlPlaneCommerceCategoriesController
     - ControlPlaneCommerceMediaController
     - ControlPlaneCommerceOrdersController
     - ControlPlaneCommerceContentController
     - ControlPlaneCommerceNavigationController
     - ControlPlaneCommerceStoreConfigurationController
     - ControlPlaneCommerceCurrenciesController
     - ControlPlaneCommercePaymentsController
     - ControlPlaneCommerceShippingController
     - ControlPlaneCommerceSecurityPrivacyController
  -> capability gateway services
     - IControlPlaneProductGateway
     - IControlPlaneCategoryGateway
     - IControlPlaneMediaGateway
     - IControlPlaneOrderGateway
     - IControlPlaneContentGateway
     - IControlPlaneNavigationGateway
     - IControlPlaneStoreConfigurationGateway
     - IControlPlaneCurrencyGateway
     - IControlPlanePaymentGateway
     - IControlPlaneShippingGateway
     - IControlPlaneSecurityPrivacyGateway
  -> ICommerceNodeAdminGatewayTransport
  -> CommerceNode.API api/commerce/*
```

## Naming Quyết Định

Tên nên dùng:

- `ICommerceNodeAdminGatewayTransport`
- `CommerceNodeAdminGatewayTransport`
- `CommerceNodeAdminGatewayResult<TPayload>`
- `CommerceNodeAdminMediaGatewayResult`

Không dùng:

- `ICommerceNodeGatewayTransport` quá rộng nếu sau này có Storefront gateway khác.
- `Catalog` cho payment, shipping, order, page, navigation, security/privacy.
- `Manager` cho transport service vì nó không quản lý domain, nó chỉ vận chuyển request qua boundary.

## Capability Split Đề Xuất

### Product Gateway

Bao gồm:

- Products query/get/create/update/archive.
- Product import template/upload/job/rows/errors.
- Product variants.
- Inventory product/variant stock.
- Variation templates.
- Product/category SEO nếu muốn giữ gần catalog trong phase đầu.

Lý do: Đây vẫn là catalog thật. Tách nhỏ hơn ngay từ đầu có thể tạo quá nhiều dependency injection và controller files.

### Category Gateway

Bao gồm:

- Categories list/tree/create/update/archive.
- Category media assignment.
- Category SEO nếu không để trong Product gateway.

### Media Gateway

Bao gồm:

- Product media list/import/order/primary/delete/retry/preview.
- Media asset list/get/upload/update/replace/delete/preview.

### Order Gateway

Bao gồm:

- Order query/get.
- Admin note.
- Shipping status.
- Complete/cancel.
- Shipment get/upsert.

### Content Gateway

Bao gồm:

- Storefront pages list/get/create/update/archive.
- Page templates/template status/draft/map/clear.
- Page navigation update.

### Navigation Gateway

Bao gồm:

- Navigation menus list/get/create/update.
- Navigation items create/update/archive/order.
- System target options.

### Store Configuration Gateway

Bao gồm:

- Runtime store get/update/activate/deactivate.
- Có thể mở rộng sau cho store lifecycle/config nếu không thuộc `ControlPlaneStoreClient`.

### Currency Gateway

Bao gồm:

- Currencies list/update.
- Exchange rates list/upsert/disable.
- Exchange-rate providers list/fetch/update-task.

### Payment Gateway

Bao gồm:

- Payment methods list/update.
- Không thêm provider secret vào public/Web contract.

### Shipping Gateway

Bao gồm:

- Shipping settings get/update.
- Shipment vẫn nên ở Order gateway vì nó là fulfillment state của order.

### Security Privacy Gateway

Bao gồm:

- Security/privacy settings get/update.
- Register policy, captcha/consent settings nếu cùng settings object.

### SEO Gateway Optional

Bao gồm:

- Product/category SEO get/update.
- SEO slug generate/validate/history.

Quyết định phase đầu: không bắt buộc tách SEO riêng nếu làm tăng số file và dependency quá sớm. Nếu tách, cần giữ route cũ.

## Phase 0 - Safety Net Và Inventory

- [x] Chốt danh sách method hiện tại của `IControlPlaneCommerceCatalogService` theo capability.
  2026-07-18 Phase 0 inventory: interface currently has 113 `Task<...>` methods. Method families are product/catalog/import/variant/inventory/SEO, category/media, media assets/product media previews, runtime store configuration, pages/content, navigation, orders/shipments, payments/email/message templates/queued messages, currencies/exchange rates, security/privacy, and shipping settings.
- [x] Chốt danh sách method hiện tại của `IControlPlaneCatalogClient` theo page/capability.
  2026-07-18 Phase 0 inventory: Web client currently has 108 async task methods and one shared route helper `api/controlplane/commerce/stores/{storePublicId:D}/{path}`. Active page families are products, categories, media library, currencies, orders, navigation, payment methods, pages, variation templates, product imports, security/privacy, shipping, and email/messages.
- [x] Ghi mapping route hiện tại từ `ControlPlaneCommerceCatalogController`.
  2026-07-18 Phase 0 inventory: controller has 147 `[Http*]` route attributes, base route `api/control-plane/stores/{storePublicId:guid}/catalog`, compatibility route root `~/api/controlplane/commerce/stores/{storePublicId:guid}/...`, and one product-import template compatibility route without store id.
- [x] Ghi mapping test hiện tại đang bảo vệ gateway:
  - [x] storeKey forwarding.
  - [x] node credential forwarding.
  - [x] Web không gọi `api/commerce`.
  - [x] product import template route/header.
  - [x] security/privacy route/policy checks.
  2026-07-18 Phase 0 inventory: `ControlPlaneCommerceCatalogServiceStoreMappingTests` has 10 gateway forwarding tests, `ControlPlaneCommerceCatalogControllerTests` has 2 controller/template route tests, `ControlPlaneVariantAttributeWorkflowTests` has 5 Web workflow/boundary tests, and `SecurityPrivacyPhase6AdminManagementTests` guards security/privacy management.
- [x] Thêm hoặc cập nhật test inventory để fail nếu Control Plane Web chứa:
  - [x] `api/commerce`
  - [x] `api/storefront`
  - [x] `CommerceNodeApi`
  - [x] `X-Node-Key`
  - [x] `X-Node-Secret`
  2026-07-18 Phase 0: `ControlPlaneWeb_UsesControlPlaneCommerceGatewayRoutesOnly` now rejects node credential header names in Control Plane Web source in addition to Commerce Node route/base-url strings.
- [x] Không sửa route, behavior, hoặc DI trong phase này nếu chỉ tạo safety net.
  2026-07-18 Phase 0: no route, runtime behavior, or DI changes were made.

Verification:

- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneCommerceCatalogServiceStoreMappingTests|FullyQualifiedName~ControlPlaneVariantAttributeWorkflowTests"`
  2026-07-18 Phase 0: passed 15/15 with existing package advisory warnings.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
  2026-07-18 Phase 0: passed with 0 warnings and 0 errors.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
  2026-07-18 Phase 0: passed with 0 warnings and 0 errors; Tailwind emitted existing Browserslist update notice.

## Phase 1 - Extract Commerce Node Admin Gateway Transport

Mục tiêu: kéo phần shared transport ra khỏi `ControlPlaneCommerceCatalogService` nhưng chưa tách capability service.

- [ ] Tạo contracts trong `BlazorShop.Application/ControlPlane/CommerceGateway/` hoặc `BlazorShop.Application/ControlPlane/Gateway/`:
  - [ ] `ICommerceNodeAdminGatewayTransport`
  - [ ] `CommerceNodeAdminGatewayResult<TPayload>`
  - [ ] `CommerceNodeAdminMediaGatewayResult`
  - [ ] `CommerceNodeAdminGatewayFailure`
  - [ ] request helper model cho multipart nếu cần.
- [ ] Tạo implementation trong `BlazorShop.Infrastructure/Data/ControlPlane/`:
  - [ ] `CommerceNodeAdminGatewayTransport`
- [ ] Transport chịu trách nhiệm:
  - [ ] load `StoreRegistry` theo `storePublicId`.
  - [ ] include `Node` và `Endpoints`.
  - [ ] validate store not found.
  - [ ] validate archived store.
  - [ ] validate node missing/disabled.
  - [ ] validate node secret.
  - [ ] resolve primary `control_api` endpoint.
  - [ ] append `storeKey` query cho Commerce Admin route.
  - [ ] add `X-Node-Key` và `X-Node-Secret`.
  - [ ] send JSON request/response.
  - [ ] send multipart product import/media upload.
  - [ ] stream media preview bytes.
  - [ ] parse standard Commerce Node envelope.
  - [ ] map remote status code sang gateway failure.
- [ ] Đổi `ControlPlaneCommerceCatalogService` dùng transport mới.
- [ ] Giữ tên `ControlPlaneCommerceCatalogResult<TPayload>` tạm thời hoặc chuyển bằng adapter để giảm diff.
- [ ] Đăng ký DI:
  - [ ] `AddHttpClient<ICommerceNodeAdminGatewayTransport, CommerceNodeAdminGatewayTransport>()`
  - [ ] giữ `IControlPlaneCommerceCatalogService` trong phase này.
- [ ] Không đổi route/controller/Web client.

Risk controls:

- [ ] Không để capability service tự biết node secret.
- [ ] Không để Web client biết Commerce Node URL.
- [ ] Không copy `LoadStoreAsync`, `ValidateStoreForRemoteCall`, `AppendStoreKeyQuery` sang nhiều service.

Verification:

- [ ] `ControlPlaneCommerceCatalogServiceStoreMappingTests` vẫn pass.
- [ ] Thêm test trực tiếp cho `CommerceNodeAdminGatewayTransport`:
  - [ ] appends `storeKey`.
  - [ ] sends node credentials.
  - [ ] returns validation failure for archived store.
  - [ ] maps empty/malformed remote response.
  - [ ] maps 404/400/409/5xx.
- [ ] `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj`

## Phase 2 - Split Application Gateway Contracts

Mục tiêu: tách interface application theo capability nhưng chưa đổi controller route.

- [ ] Tạo folders dưới `BlazorShop.Application/ControlPlane/CommerceGateway/`:
  - [ ] `Products`
  - [ ] `Categories`
  - [ ] `Media`
  - [ ] `Orders`
  - [ ] `Content`
  - [ ] `Navigation`
  - [ ] `StoreConfiguration`
  - [ ] `Currencies`
  - [ ] `Payments`
  - [ ] `Shipping`
  - [ ] `SecurityPrivacy`
- [ ] Tạo interfaces:
  - [ ] `IControlPlaneProductGateway`
  - [ ] `IControlPlaneCategoryGateway`
  - [ ] `IControlPlaneMediaGateway`
  - [ ] `IControlPlaneOrderGateway`
  - [ ] `IControlPlaneContentGateway`
  - [ ] `IControlPlaneNavigationGateway`
  - [ ] `IControlPlaneStoreConfigurationGateway`
  - [ ] `IControlPlaneCurrencyGateway`
  - [ ] `IControlPlanePaymentGateway`
  - [ ] `IControlPlaneShippingGateway`
  - [ ] `IControlPlaneSecurityPrivacyGateway`
- [ ] Di chuyển method signatures từ `IControlPlaneCommerceCatalogService` sang interface capability tương ứng.
- [ ] Giữ DTO đang dùng để không tạo churn.
- [ ] Giữ `ControlPlaneCommerceCatalogResult<TPayload>` tạm thời nếu rename result tạo diff quá lớn.
- [ ] Tạo adapter `IControlPlaneCommerceCatalogService` tạm thời nếu test/controller cũ vẫn cần, nhưng đánh dấu chỉ là compatibility trong refactor.

Risk controls:

- [ ] Không tạo interface trống hoặc capability chưa có consumer.
- [ ] Không gom capability mới vào `Catalog`.
- [ ] Không đổi permission policy trong phase này.

Verification:

- [ ] Build Application.
- [ ] Search không còn method mới thêm vào `IControlPlaneCommerceCatalogService`.
- [ ] Test compile.

## Phase 3 - Split Infrastructure Gateway Implementations

Mục tiêu: chia `ControlPlaneCommerceCatalogService.cs` thành implementation nhỏ, mỗi file dùng transport chung.

- [ ] Tạo implementation files trong `BlazorShop.Infrastructure/Data/ControlPlane/CommerceGateway/`:
  - [ ] `ControlPlaneProductGateway.cs`
  - [ ] `ControlPlaneCategoryGateway.cs`
  - [ ] `ControlPlaneMediaGateway.cs`
  - [ ] `ControlPlaneOrderGateway.cs`
  - [ ] `ControlPlaneContentGateway.cs`
  - [ ] `ControlPlaneNavigationGateway.cs`
  - [ ] `ControlPlaneStoreConfigurationGateway.cs`
  - [ ] `ControlPlaneCurrencyGateway.cs`
  - [ ] `ControlPlanePaymentGateway.cs`
  - [ ] `ControlPlaneShippingGateway.cs`
  - [ ] `ControlPlaneSecurityPrivacyGateway.cs`
- [ ] Move method bodies theo capability.
- [ ] Move query builders theo capability:
  - [ ] `BuildProductQuery` vào Product gateway.
  - [ ] `BuildInventoryQuery` vào Product gateway.
  - [ ] `BuildProductImportQuery`/`BuildProductImportRowsQuery` vào Product gateway.
  - [ ] `BuildStorefrontPageQuery` vào Content gateway.
  - [ ] `BuildOrderQuery` vào Order gateway.
  - [ ] media preview/list query builders vào Media gateway.
  - [ ] common `ToQueryString`/`AddIfPresent` vào small internal helper nếu duplication xuất hiện.
- [ ] Đăng ký DI cho từng gateway.
- [ ] Giữ adapter `ControlPlaneCommerceCatalogService` nếu cần để không đổi controller cùng phase.
- [ ] Nếu adapter còn lại, adapter chỉ delegate sang gateway mới, không chứa transport logic.

Risk controls:

- [ ] Không để nhiều gateway tự build base URL/node headers.
- [ ] Không tạo static global HttpClient.
- [ ] Không trộn ControlPlaneDbContext query vào capability gateway ngoài transport.

Verification:

- [ ] Store mapping tests được đổi để test từng gateway.
- [ ] Thêm test đảm bảo old adapter không chứa `new HttpRequestMessage` nếu adapter còn lại.
- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneCommerceCatalogServiceStoreMappingTests"`
- [ ] `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj`

## Phase 4 - Split Control Plane API Controllers

Mục tiêu: controller theo capability, giữ route hiện tại.

- [ ] Tạo controllers:
  - [ ] `ControlPlaneCommerceProductsController`
  - [ ] `ControlPlaneCommerceCategoriesController`
  - [ ] `ControlPlaneCommerceMediaController`
  - [ ] `ControlPlaneCommerceOrdersController`
  - [ ] `ControlPlaneCommerceContentController`
  - [ ] `ControlPlaneCommerceNavigationController`
  - [ ] `ControlPlaneCommerceStoreConfigurationController`
  - [ ] `ControlPlaneCommerceCurrenciesController`
  - [ ] `ControlPlaneCommercePaymentsController`
  - [ ] `ControlPlaneCommerceShippingController`
  - [ ] `ControlPlaneCommerceSecurityPrivacyController`
- [ ] Mỗi controller inject đúng gateway capability.
- [ ] Giữ route templates hiện tại:
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/products`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/categories`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/pages`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/...`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/orders`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/security-privacy`
  - [ ] `~/api/controlplane/commerce/stores/{storePublicId:guid}/shipping/settings`
- [ ] Giữ legacy/base relative `api/control-plane/stores/{storePublicId}/catalog/*` cho product/category routes nếu tests/consumer còn dùng.
- [ ] Extract response mapping helper:
  - [ ] `ControlPlaneCommerceGatewayResponseMapper`
  - [ ] hoặc extension method trong API project.
- [ ] Product import CSV helper có thể ở Product controller hoặc small private helper file.
- [ ] Media preview response handling nằm ở Media controller.
- [ ] Xóa hoặc làm rỗng `ControlPlaneCommerceCatalogController` chỉ sau khi không còn action.

Permission review:

- [ ] Product/category/variant/inventory/import giữ `StoresRead/StoresWrite` trước khi có permission catalog riêng.
- [ ] Pages dùng `CommercePagesRead/Write`.
- [ ] Navigation dùng `CommerceNavigationRead/Write`.
- [ ] Currency/shipping dùng `CommerceSettingsRead/Write`.
- [ ] Exchange providers/payment provider management dùng `CommerceProvidersRead/Write` nếu hợp lý.
- [ ] Security/privacy dùng `CommerceSecurityPrivacyRead/Write`.
- [ ] Không giảm permission hiện có.

Verification:

- [ ] Controller route reflection tests cập nhật theo controller mới.
- [ ] `DownloadProductImportTemplate_ReturnsCanonicalParserHeader` vẫn pass.
- [ ] API build pass.
- [ ] OpenAPI/contract tests pass nếu Control Plane API có snapshot/contract tests liên quan.

## Phase 5 - Split Control Plane Web Clients

Mục tiêu: Web page inject đúng client theo capability, vẫn dùng `IControlPlaneApiClient`.

- [ ] Tạo service folders trong `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/`:
  - [ ] `Products`
  - [ ] `Categories`
  - [ ] `Media`
  - [ ] `Orders`
  - [ ] `Content`
  - [ ] `Navigation`
  - [ ] `Currencies`
  - [ ] `Payments`
  - [ ] `Shipping`
  - [ ] `SecurityPrivacy`
- [ ] Tạo clients:
  - [ ] `IControlPlaneProductClient`
  - [ ] `IControlPlaneCategoryClient`
  - [ ] `IControlPlaneMediaClient`
  - [ ] `IControlPlaneOrderClient`
  - [ ] `IControlPlaneContentClient`
  - [ ] `IControlPlaneNavigationClient`
  - [ ] `IControlPlaneCurrencyClient`
  - [ ] `IControlPlanePaymentClient`
  - [ ] `IControlPlaneShippingClient`
  - [ ] `IControlPlaneSecurityPrivacyClient`
- [ ] Di chuyển methods từ `ControlPlaneCatalogClient`.
- [ ] Mỗi client chỉ gọi `IControlPlaneApiClient`.
- [ ] Giữ `CommerceRoute(storePublicId, path)` helper nếu cần, nhưng đặt trong shared internal helper không chứa Commerce Node URL.
- [ ] Cập nhật DI trong `ControlPlane.Web/Program.cs`.
- [ ] Cập nhật page injections:
  - [ ] `CommerceProducts.razor` dùng Product/Category/Media nếu cần.
  - [ ] `CommerceCategories.razor` dùng Category/Media.
  - [ ] `CommerceMediaLibrary.razor` dùng Media.
  - [ ] `CommerceCurrencies.razor` dùng Currency.
  - [ ] `CommerceOrders.razor` dùng Order.
  - [ ] `CommerceNavigation.razor` dùng Navigation.
  - [ ] `CommercePaymentMethods.razor` dùng Payment.
  - [ ] `CommercePages.razor` dùng Content/Navigation nếu cần.
  - [ ] `CommerceVariationTemplates.razor` dùng Product hoặc dedicated variation client nếu tách.
  - [ ] `CommerceProductImports.razor` dùng Product import client.
- [ ] Giữ `IControlPlaneCatalogClient` adapter tạm nếu cần để giảm diff, nhưng không để page còn inject adapter sau phase này.

Risk controls:

- [ ] Web không chứa `api/commerce`.
- [ ] Web không chứa `X-Node-Key` hoặc `X-Node-Secret`.
- [ ] Không đổi UX hoặc page route.

Verification:

- [ ] `ControlPlaneVariantAttributeWorkflowTests.ControlPlaneWeb_UsesControlPlaneCommerceGatewayRoutesOnly` cập nhật để scan tất cả capability clients.
- [ ] Build ControlPlane Web.
- [ ] Optional Playwright/manual later: mở từng manager page và xác nhận load state.

## Phase 6 - Remove Old Catalog Facades

Mục tiêu: khi các capability controllers/clients đã hoạt động, xóa lớp god interface cũ khỏi active path.

- [ ] Xóa hoặc retire `IControlPlaneCommerceCatalogService`.
- [ ] Xóa hoặc retire `ControlPlaneCommerceCatalogService` nếu không còn adapter cần thiết.
- [ ] Xóa hoặc retire `IControlPlaneCatalogClient`.
- [ ] Xóa hoặc retire `ControlPlaneCatalogClient`.
- [ ] Xóa `ControlPlaneCommerceCatalogController` nếu không còn action.
- [ ] Nếu route `api/control-plane/stores/{storePublicId}/catalog/*` còn cần compatibility, giữ bằng action trong capability controllers, không bằng controller god cũ.
- [ ] Cập nhật namespaces/docs/tests không còn nói mọi thứ là catalog.

Verification:

- [ ] `rg "IControlPlaneCommerceCatalogService|ControlPlaneCommerceCatalogService|IControlPlaneCatalogClient|ControlPlaneCatalogClient|ControlPlaneCommerceCatalogController" BlazorShop.Application BlazorShop.Infrastructure BlazorShop.PresentationV2 BlazorShop.Tests`
- [ ] Chỉ còn references trong historical docs hoặc intentional migration notes.
- [ ] Focused tests pass.

## Phase 7 - Permission And Contract Cleanup

Mục tiêu: sau khi tách capability, kiểm tra permission và OpenAPI quality.

- [ ] Review operation names/summaries nếu Control Plane OpenAPI đang expose controller mới.
- [ ] Đảm bảo protected endpoints vẫn có security metadata.
- [ ] Đảm bảo no side-effecting GET.
- [ ] Đảm bảo request bodies required cho POST/PUT.
- [ ] Đánh giá có cần permission riêng cho catalog product/category/order/media không:
  - [ ] Nếu chưa cần trong MVP, giữ `StoresRead/StoresWrite` cho catalog/admin chung.
  - [ ] Nếu thêm permission mới, phải có migration/seed/authorization tests.
- [ ] Không thêm permission mới chỉ vì tách file.

Verification:

- [ ] `ControlPlaneAuthorizationTests`
- [ ] Controller reflection/security tests.
- [ ] API build.

## Phase 8 - QA Checklist And Documentation Update

- [ ] Cập nhật `docs/architecture/06-feature-map.md`:
  - [ ] đổi wording từ "Catalog gateway" thành "Commerce Admin gateway capabilities".
  - [ ] ghi rõ transport chung ở Control Plane API -> Commerce Node.
- [ ] Cập nhật `docs/architecture/03-runtime-boundaries.md` nếu cần:
  - [ ] Control Plane API vẫn là boundary duy nhất gọi Commerce Node.
  - [ ] Control Plane Web vẫn UI-only.
- [ ] Cập nhật `docs/architecture/05-project-and-folder-guide.md` nếu thêm folder gateway mới.
- [ ] Cập nhật `docs/refactor-control-Commerce-storefront/QA-ControlPlane.todo.md`:
  - [ ] gateway split giữ storeKey forwarding.
  - [ ] node credentials không lộ vào Web.
  - [ ] manager pages vẫn dùng Control Plane API.
  - [ ] product/category/media/order/page/navigation/currency/payment/shipping/security manager pages vẫn load qua gateway mới.
- [ ] Nếu route public không đổi, ghi rõ đây là internal refactor, không phải API breaking change.

Verification:

- [ ] `rg "Catalog gateway" docs/architecture docs/refactor-control-Commerce-storefront`
- [ ] docs không còn mô tả sai mọi capability là catalog.

## Phase 9 - Release Verification

Focused build/test:

- [ ] `dotnet build BlazorShop.Application/BlazorShop.Application.csproj`
- [ ] `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj`
- [ ] `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj`
- [ ] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneCommerceCatalogServiceStoreMappingTests|FullyQualifiedName~ControlPlaneAuthorizationTests|FullyQualifiedName~ControlPlaneCommerceCatalogControllerTests|FullyQualifiedName~ControlPlaneVariantAttributeWorkflowTests|FullyQualifiedName~SecurityPrivacyPhase6AdminManagementTests"`

Manual/browser QA nếu có thay đổi Web page injections:

- [ ] Run V2 local environment.
- [ ] Login Control Plane.
- [ ] Open Products manager.
- [ ] Open Categories manager.
- [ ] Open Media Library.
- [ ] Open Pages manager.
- [ ] Open Navigation manager.
- [ ] Open Orders manager.
- [ ] Open Payment Methods.
- [ ] Open Currencies.
- [ ] Open Shipping settings.
- [ ] Open Security/Privacy settings.
- [ ] Confirm no browser request goes directly to Commerce Node `api/commerce/*`.

## Không Thuộc Scope

- [ ] Không rewrite Commerce Node admin endpoints.
- [ ] Không đổi database schema.
- [ ] Không đổi Storefront V2.
- [ ] Không đổi payment/order/cart flow.
- [ ] Không đổi public storefront API.
- [ ] Không thêm permission mới nếu chỉ đang refactor gateway.
- [ ] Không đổi route Web đang dùng trong phase đầu.
- [ ] Không tạo ABP/module architecture.
- [ ] Không tạo shared visual shell hoặc shared Control Plane/Storefront UI framework.

## Rủi Ro Chính

| Risk | Ảnh hưởng | Cách giảm |
| --- | --- | --- |
| Copy logic transport sang nhiều gateway | Node credentials/storeKey xử lý lệch nhau | Phase 1 bắt buộc transport chung trước. |
| Đổi route quá sớm | Control Plane Web hoặc tests fail hàng loạt | Giữ route hiện tại qua các capability controllers. |
| Tách quá nhỏ | Nhiều file/client không có consumer rõ | Tách theo manager/capability, không theo mọi method. |
| Permission bị giảm vô tình | Admin thấy/ghi được thứ không đúng quyền | Permission review riêng ở Phase 7. |
| Test cũ bị xóa thay vì di chuyển | Mất bảo vệ boundary | Chuyển test theo capability trước khi xóa facade. |
| Adapter cũ sống mãi | God interface vẫn tồn tại sau refactor | Phase 6 có gate `rg` bắt buộc. |

## Release Gate

Phase này chỉ được xem là hoàn thành khi:

- [ ] Không còn active page inject `IControlPlaneCatalogClient`.
- [ ] Không còn active controller inject `IControlPlaneCommerceCatalogService`.
- [ ] Không còn active transport logic trong class catalog cũ.
- [ ] Mọi Control Plane Web client vẫn chỉ gọi Control Plane API.
- [ ] Mọi Commerce Node Admin call vẫn đi qua Control Plane API transport với node credentials server-side.
- [ ] Store-scoped Commerce Admin requests vẫn append `storeKey`.
- [ ] Focused build/test pass.
- [ ] QA-ControlPlane todo được cập nhật.
- [ ] Architecture docs không còn mô tả mọi capability là catalog gateway.
