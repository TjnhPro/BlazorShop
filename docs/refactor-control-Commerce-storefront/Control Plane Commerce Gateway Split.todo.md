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

- [x] Tạo contracts trong `BlazorShop.Application/ControlPlane/CommerceGateway/` hoặc `BlazorShop.Application/ControlPlane/Gateway/`:
  - [x] `ICommerceNodeAdminGatewayTransport`
  - [x] `CommerceNodeAdminGatewayResult<TPayload>`
  - [x] `CommerceNodeAdminMediaGatewayResult`
  - [x] `CommerceNodeAdminGatewayFailure`
  - [x] request helper model cho multipart nếu cần.
  2026-07-18 Phase 1: contract created in `BlazorShop.Application/ControlPlane/CommerceGateway/CommerceNodeAdminGatewayDtos.cs`; existing multipart request DTOs are reused.
- [x] Tạo implementation trong `BlazorShop.Infrastructure/Data/ControlPlane/`:
  - [x] `CommerceNodeAdminGatewayTransport`
- [x] Transport chịu trách nhiệm:
  - [x] load `StoreRegistry` theo `storePublicId`.
  - [x] include `Node` và `Endpoints`.
  - [x] validate store not found.
  - [x] validate archived store.
  - [x] validate node missing/disabled.
  - [x] validate node secret.
  - [x] resolve primary `control_api` endpoint.
  - [x] append `storeKey` query cho Commerce Admin route.
  - [x] add `X-Node-Key` và `X-Node-Secret`.
  - [x] send JSON request/response.
  - [x] send multipart product import/media upload.
  - [x] stream media preview bytes.
  - [x] parse standard Commerce Node envelope.
  - [x] map remote status code sang gateway failure.
  2026-07-18 Phase 1: transport owns node credential headers, storeKey query, store/node/control endpoint validation, JSON envelope parsing, media byte streaming, and upload multipart forwarding.
- [x] Đổi `ControlPlaneCommerceCatalogService` dùng transport mới.
  2026-07-18 Phase 1: catalog service constructor now takes `ICommerceNodeAdminGatewayTransport`; old `HttpRequestMessage`, store load, and node header code was removed from the service.
- [x] Giữ tên `ControlPlaneCommerceCatalogResult<TPayload>` tạm thời hoặc chuyển bằng adapter để giảm diff.
  2026-07-18 Phase 1: catalog service maps `CommerceNodeAdminGatewayResult<TPayload>` back to existing catalog result types.
- [x] Đăng ký DI:
  - [x] `AddHttpClient<ICommerceNodeAdminGatewayTransport, CommerceNodeAdminGatewayTransport>()`
  - [x] giữ `IControlPlaneCommerceCatalogService` trong phase này.
- [x] Không đổi route/controller/Web client.
  2026-07-18 Phase 1: route/controller/Web client files were not changed.

Risk controls:

- [x] Không để capability service tự biết node secret. 2026-07-18 Phase 1: node secret handling is inside `CommerceNodeAdminGatewayTransport`.
- [x] Không để Web client biết Commerce Node URL. 2026-07-18 Phase 1: no Web changes; existing boundary guard remains.
- [x] Không copy `LoadStoreAsync`, `ValidateStoreForRemoteCall`, `AppendStoreKeyQuery` sang nhiều service. 2026-07-18 Phase 1: these concerns moved into one transport.

Verification:

- [x] `ControlPlaneCommerceCatalogServiceStoreMappingTests` vẫn pass.
- [x] Thêm test trực tiếp cho `CommerceNodeAdminGatewayTransport`:
  - [x] appends `storeKey`.
  - [x] sends node credentials.
  - [x] returns validation failure for archived store.
  - [x] maps empty/malformed remote response.
  - [x] maps 404/400/409/5xx.
  2026-07-18 Phase 1: focused gateway run passed 17/17 with existing package advisory warnings.
- [x] `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj`
  2026-07-18 Phase 1: passed with 0 warnings and 0 errors. Additional `ControlPlane.API` build also passed with 0 warnings and 0 errors.

## Phase 2 - Split Application Gateway Contracts

Mục tiêu: tách interface application theo capability nhưng chưa đổi controller route.

- [x] Tạo folders dưới `BlazorShop.Application/ControlPlane/CommerceGateway/`:
  - [x] `Products`
  - [x] `Categories`
  - [x] `Media`
  - [x] `Orders`
  - [x] `Content`
  - [x] `Navigation`
  - [x] `StoreConfiguration`
  - [x] `Currencies`
  - [x] `Payments`
  - [x] `Shipping`
  - [x] `SecurityPrivacy`
  - [x] `Messages`
  2026-07-18 Phase 2: `Messages` was added because current codebase has email settings, message templates, and queued-message gateway methods that do not belong in payment/shipping.
- [x] Tạo interfaces:
  - [x] `IControlPlaneProductGateway`
  - [x] `IControlPlaneCategoryGateway`
  - [x] `IControlPlaneMediaGateway`
  - [x] `IControlPlaneOrderGateway`
  - [x] `IControlPlaneContentGateway`
  - [x] `IControlPlaneNavigationGateway`
  - [x] `IControlPlaneStoreConfigurationGateway`
  - [x] `IControlPlaneCurrencyGateway`
  - [x] `IControlPlanePaymentGateway`
  - [x] `IControlPlaneShippingGateway`
  - [x] `IControlPlaneSecurityPrivacyGateway`
  - [x] `IControlPlaneMessageGateway`
- [x] Di chuyển method signatures từ `IControlPlaneCommerceCatalogService` sang interface capability tương ứng.
  2026-07-18 Phase 2: 113 method signatures were split across capability contracts. The old facade now inherits the capability interfaces and declares no direct `Task<...>` methods.
- [x] Giữ DTO đang dùng để không tạo churn.
  2026-07-18 Phase 2: DTO/result types remain in `ControlPlaneCommerceCatalogDtos.cs`.
- [x] Giữ `ControlPlaneCommerceCatalogResult<TPayload>` tạm thời nếu rename result tạo diff quá lớn.
- [x] Tạo adapter `IControlPlaneCommerceCatalogService` tạm thời nếu test/controller cũ vẫn cần, nhưng đánh dấu chỉ là compatibility trong refactor.
  2026-07-18 Phase 2: `IControlPlaneCommerceCatalogService` is now a compatibility facade over capability interfaces.

Risk controls:

- [x] Không tạo interface trống hoặc capability chưa có consumer. 2026-07-18 Phase 2: method counts are Product 31, Media 14, Messages 14, Content 11, Navigation 9, Category 8, Currency 8, Order 8, StoreConfiguration 4, Payment 2, SecurityPrivacy 2, Shipping 2.
- [x] Không gom capability mới vào `Catalog`. 2026-07-18 Phase 2: old catalog facade has no direct method declarations.
- [x] Không đổi permission policy trong phase này. 2026-07-18 Phase 2: no API/controller/authorization files changed.

Verification:

- [x] Build Application.
  2026-07-18 Phase 2: `dotnet build BlazorShop.Application/BlazorShop.Application.csproj --no-restore` passed with 0 warnings and 0 errors.
- [x] Search không còn method mới thêm vào `IControlPlaneCommerceCatalogService`.
  2026-07-18 Phase 2: old facade has 0 direct `Task<...>` members.
- [x] Test compile.
  2026-07-18 Phase 2: Infrastructure build passed with 0 warnings/errors and focused gateway tests passed 17/17 with existing package advisory warnings.

## Phase 3 - Split Infrastructure Gateway Implementations

Mục tiêu: chia `ControlPlaneCommerceCatalogService.cs` thành implementation nhỏ, mỗi file dùng transport chung.

- [x] Tạo implementation files trong `BlazorShop.Infrastructure/Data/ControlPlane/CommerceGateway/`:
  - [x] `ControlPlaneProductGateway.cs`
  - [x] `ControlPlaneCategoryGateway.cs`
  - [x] `ControlPlaneMediaGateway.cs`
  - [x] `ControlPlaneOrderGateway.cs`
  - [x] `ControlPlaneContentGateway.cs`
  - [x] `ControlPlaneNavigationGateway.cs`
  - [x] `ControlPlaneStoreConfigurationGateway.cs`
  - [x] `ControlPlaneCurrencyGateway.cs`
  - [x] `ControlPlanePaymentGateway.cs`
  - [x] `ControlPlaneShippingGateway.cs`
  - [x] `ControlPlaneSecurityPrivacyGateway.cs`
  - [x] `ControlPlaneMessageGateway.cs`
  2026-07-18 Phase 3: added `ControlPlaneCommerceGatewayBase` and one implementation per capability, including Messages for email/message-template/queued-message methods.
- [x] Move method bodies theo capability.
  2026-07-18 Phase 3: 113 method bodies were moved out of `ControlPlaneCommerceCatalogService` into capability gateway classes.
- [x] Move query builders theo capability:
  - [x] `BuildProductQuery` vào Product gateway.
  - [x] `BuildInventoryQuery` vào Product gateway.
  - [x] `BuildProductImportQuery`/`BuildProductImportRowsQuery` vào Product gateway.
  - [x] `BuildStorefrontPageQuery` vào Content gateway.
  - [x] `BuildOrderQuery` vào Order gateway.
  - [x] media preview/list query builders vào Media gateway.
  - [x] common `ToQueryString`/`AddIfPresent` vào small internal helper nếu duplication xuất hiện.
  2026-07-18 Phase 3: query builders and send/result adapters live in `ControlPlaneCommerceGatewayBase` to avoid duplicating transport/query helper code across gateway classes.
- [x] Đăng ký DI cho từng gateway.
- [x] Giữ adapter `ControlPlaneCommerceCatalogService` nếu cần để không đổi controller cùng phase.
- [x] Nếu adapter còn lại, adapter chỉ delegate sang gateway mới, không chứa transport logic.
  2026-07-18 Phase 3: facade delegates to capability interfaces and static test rejects `new HttpRequestMessage`, node headers, and `AppendStoreKeyQuery`.

Risk controls:

- [x] Không để nhiều gateway tự build base URL/node headers. 2026-07-18 Phase 3: only `CommerceNodeAdminGatewayTransport` owns base URL/node headers.
- [x] Không tạo static global HttpClient. 2026-07-18 Phase 3: gateway implementations use DI transport only.
- [x] Không trộn ControlPlaneDbContext query vào capability gateway ngoài transport. 2026-07-18 Phase 3: capability classes do not inject `ControlPlaneDbContext`.

Verification:

- [x] Store mapping tests được đổi để test từng gateway.
  2026-07-18 Phase 3: existing facade tests now exercise capability gateways through the facade and shared transport.
- [x] Thêm test đảm bảo old adapter không chứa `new HttpRequestMessage` nếu adapter còn lại.
- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter "FullyQualifiedName~ControlPlaneCommerceCatalogServiceStoreMappingTests"`
  2026-07-18 Phase 3: passed 18/18 with existing package advisory warnings.
- [x] `dotnet build BlazorShop.Infrastructure/BlazorShop.Infrastructure.csproj`
  2026-07-18 Phase 3: passed with 0 warnings and 0 errors.

## Phase 4 - Split Control Plane API Controllers

Mục tiêu: controller theo capability, giữ route hiện tại.

- [x] Tạo controllers:
  - [x] `ControlPlaneCommerceProductsController`
  - [x] `ControlPlaneCommerceCategoriesController`
  - [x] `ControlPlaneCommerceMediaController`
  - [x] `ControlPlaneCommerceOrdersController`
  - [x] `ControlPlaneCommerceContentController`
  - [x] `ControlPlaneCommerceNavigationController`
  - [x] `ControlPlaneCommerceStoreConfigurationController`
  - [x] `ControlPlaneCommerceCurrenciesController`
  - [x] `ControlPlaneCommercePaymentsController`
  - [x] `ControlPlaneCommerceShippingController`
  - [x] `ControlPlaneCommerceSecurityPrivacyController`
  2026-07-18 Phase 4: controllers were created under `BlazorShop.ControlPlane.API/Controllers/CommerceGateway/`; `ControlPlaneCommerceMessagesController` was also added because email settings, message templates, and queued messages are a separate capability in current code.
- [x] Mỗi controller inject đúng gateway capability.
  2026-07-18 Phase 4: controllers now inject capability gateways such as `IControlPlaneProductGateway`, `IControlPlaneMediaGateway`, `IControlPlaneSecurityPrivacyGateway`, and `IControlPlaneMessageGateway`; controller code no longer references `IControlPlaneCommerceCatalogService`.
- [x] Giữ route templates hiện tại:
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/products`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/categories`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/media/assets`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/pages`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/navigation/...`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/orders`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/payment-methods`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/currencies`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/security-privacy`
  - [x] `~/api/controlplane/commerce/stores/{storePublicId:guid}/shipping/settings`
  2026-07-18 Phase 4: route attributes were moved with their original templates unchanged.
- [x] Giữ legacy/base relative `api/control-plane/stores/{storePublicId}/catalog/*` cho product/category routes nếu tests/consumer còn dùng.
  2026-07-18 Phase 4: capability controllers keep the old base route attribute, while compatibility absolute routes remain unchanged.
- [x] Extract response mapping helper:
  - [x] `ControlPlaneCommerceGatewayResponseMapper`
  - [x] hoặc extension method trong API project.
  2026-07-18 Phase 4: shared response/file/error mapping is centralized in `ControlPlaneCommerceGatewayControllerBase`.
- [x] Product import CSV helper có thể ở Product controller hoặc small private helper file.
  2026-07-18 Phase 4: product import template, upload, and CSV error helpers live with `ControlPlaneCommerceProductsController` through the shared base helpers.
- [x] Media preview response handling nằm ở Media controller.
  2026-07-18 Phase 4: media/product-media preview endpoints are owned by `ControlPlaneCommerceMediaController`.
- [x] Xóa hoặc làm rỗng `ControlPlaneCommerceCatalogController` chỉ sau khi không còn action.
  2026-07-18 Phase 4: `ControlPlaneCommerceCatalogController` is an empty shell and `OldCatalogController_DoesNotOwnCommerceActions` guards that it has zero HTTP actions.

Permission review:

- [x] Product/category/variant/inventory/import giữ `StoresRead/StoresWrite` trước khi có permission catalog riêng.
- [x] Pages dùng `CommercePagesRead/Write`.
- [x] Navigation dùng `CommerceNavigationRead/Write`.
- [x] Currency/shipping dùng `CommerceSettingsRead/Write`.
- [x] Exchange providers/payment provider management dùng `CommerceProvidersRead/Write` nếu hợp lý.
- [x] Security/privacy dùng `CommerceSecurityPrivacyRead/Write`.
- [x] Không giảm permission hiện có.
  2026-07-18 Phase 4: generated capability controllers preserve the action-level authorization attributes from the previous catalog controller; security/privacy and email static guard tests now read the new controller files.

Verification:

- [x] Controller route reflection tests cập nhật theo controller mới.
- [x] `DownloadProductImportTemplate_ReturnsCanonicalParserHeader` vẫn pass.
- [x] API build pass.
- [x] OpenAPI/contract tests pass nếu Control Plane API có snapshot/contract tests liên quan.
  2026-07-18 Phase 4: no Control Plane OpenAPI snapshot gate was found for this split; focused controller/static tests passed 35/35 and `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed with 0 warnings/errors.

## Phase 5 - Split Control Plane Web Clients

Mục tiêu: Web page inject đúng client theo capability, vẫn dùng `IControlPlaneApiClient`.

- [x] Tạo service folders trong `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/Services/Commerce/`:
  - [x] `Products`
  - [x] `Categories`
  - [x] `Media`
  - [x] `Orders`
  - [x] `Content`
  - [x] `Navigation`
  - [x] `Currencies`
  - [x] `Payments`
  - [x] `Shipping`
  - [x] `SecurityPrivacy`
  2026-07-18 Phase 5: folders were added under `Services/Commerce`; extra `Messages` and `Seo` folders were added because email/message settings and generic slug policy are active page capabilities, and `ProductImports` lives under `Products`.
- [x] Tạo clients:
  - [x] `IControlPlaneProductClient`
  - [x] `IControlPlaneCategoryClient`
  - [x] `IControlPlaneMediaClient`
  - [x] `IControlPlaneOrderClient`
  - [x] `IControlPlaneContentClient`
  - [x] `IControlPlaneNavigationClient`
  - [x] `IControlPlaneCurrencyClient`
  - [x] `IControlPlanePaymentClient`
  - [x] `IControlPlaneShippingClient`
  - [x] `IControlPlaneSecurityPrivacyClient`
  2026-07-18 Phase 5: capability clients were created and registered; additional `IControlPlaneMessageClient`, `IControlPlaneProductImportClient`, and `IControlPlaneSeoClient` avoid forcing unrelated pages through a broad product/catalog client.
- [x] Di chuyển methods từ `ControlPlaneCatalogClient`.
  2026-07-18 Phase 5: active page methods were copied into capability clients. The old adapter remains temporarily for Phase 6 removal.
- [x] Mỗi client chỉ gọi `IControlPlaneApiClient`.
  2026-07-18 Phase 5: clients inherit shared `ControlPlaneCommerceClientBase`, which owns only `IControlPlaneApiClient` and route/query helpers.
- [x] Giữ `CommerceRoute(storePublicId, path)` helper nếu cần, nhưng đặt trong shared internal helper không chứa Commerce Node URL.
  2026-07-18 Phase 5: `ControlPlaneCommerceClientBase` centralizes `CommerceRoute(storePublicId, path)` with `api/controlplane/commerce/stores/{storePublicId:D}`.
- [x] Cập nhật DI trong `ControlPlane.Web/Program.cs`.
- [x] Cập nhật page injections:
  - [x] `CommerceProducts.razor` dùng Product/Category/Media nếu cần.
  - [x] `CommerceCategories.razor` dùng Category/Media.
  - [x] `CommerceMediaLibrary.razor` dùng Media.
  - [x] `CommerceCurrencies.razor` dùng Currency.
  - [x] `CommerceOrders.razor` dùng Order.
  - [x] `CommerceNavigation.razor` dùng Navigation.
  - [x] `CommercePaymentMethods.razor` dùng Payment.
  - [x] `CommercePages.razor` dùng Content/Navigation nếu cần.
  - [x] `CommerceVariationTemplates.razor` dùng Product hoặc dedicated variation client nếu tách.
  - [x] `CommerceProductImports.razor` dùng Product import client.
  2026-07-18 Phase 5: pages now inject capability clients; generic SEO calls use `IControlPlaneSeoClient`, and email/message management uses `IControlPlaneMessageClient`.
- [x] Giữ `IControlPlaneCatalogClient` adapter tạm nếu cần để giảm diff, nhưng không để page còn inject adapter sau phase này.
  2026-07-18 Phase 5: adapter is still registered temporarily, but `rg` found no page-level `@inject IControlPlaneCatalogClient` or `CatalogClient.` usage.

Risk controls:

- [x] Web không chứa `api/commerce`.
- [x] Web không chứa `X-Node-Key` hoặc `X-Node-Secret`.
- [x] Không đổi UX hoặc page route.
  2026-07-18 Phase 5: page routes and markup were not intentionally changed; scan of Web `.cs/.razor/.json` found no direct Commerce Node route or node credential header strings.

Verification:

- [x] `ControlPlaneVariantAttributeWorkflowTests.ControlPlaneWeb_UsesControlPlaneCommerceGatewayRoutesOnly` cập nhật để scan tất cả capability clients.
- [x] Build ControlPlane Web.
- [x] Optional Playwright/manual later: mở từng manager page và xác nhận load state.
  2026-07-18 Phase 5: focused tests passed 14/14 and `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj --no-restore` passed with 0 warnings/errors. Browser smoke is deferred to the release QA checklist because this phase only split Web client bindings.

## Phase 6 - Remove Old Catalog Facades

Mục tiêu: khi các capability controllers/clients đã hoạt động, xóa lớp god interface cũ khỏi active path.

- [x] Xóa hoặc retire `IControlPlaneCommerceCatalogService`.
- [x] Xóa hoặc retire `ControlPlaneCommerceCatalogService` nếu không còn adapter cần thiết.
- [x] Xóa hoặc retire `IControlPlaneCatalogClient`.
- [x] Xóa hoặc retire `ControlPlaneCatalogClient`.
- [x] Xóa `ControlPlaneCommerceCatalogController` nếu không còn action.
- [x] Nếu route `api/control-plane/stores/{storePublicId}/catalog/*` còn cần compatibility, giữ bằng action trong capability controllers, không bằng controller god cũ.
- [x] Cập nhật namespaces/docs/tests không còn nói mọi thứ là catalog.
  2026-07-18 Phase 6: removed the active god service, Web catalog adapter, and empty catalog controller. Shared result DTOs remain because capability gateways still use `ControlPlaneCommerceCatalogResult<TPayload>` and related response/result records.

Verification:

- [x] `rg "IControlPlaneCommerceCatalogService|ControlPlaneCommerceCatalogService|IControlPlaneCatalogClient|ControlPlaneCatalogClient|ControlPlaneCommerceCatalogController" BlazorShop.Application BlazorShop.Infrastructure BlazorShop.PresentationV2 BlazorShop.Tests`
- [x] Chỉ còn references trong historical docs hoặc intentional migration notes.
- [x] Focused tests pass.
  2026-07-18 Phase 6: active source `rg` returned no matches. Application, Infrastructure, ControlPlane API, and ControlPlane Web builds passed with 0 warnings/errors after rerunning Application outside the earlier parallel compiler lock. Focused tests passed 34/34 with existing package advisory warnings.

## Phase 7 - Permission And Contract Cleanup

Mục tiêu: sau khi tách capability, kiểm tra permission và OpenAPI quality.

- [x] Review operation names/summaries nếu Control Plane OpenAPI đang expose controller mới.
- [x] Đảm bảo protected endpoints vẫn có security metadata.
- [x] Đảm bảo no side-effecting GET.
- [x] Đảm bảo request bodies required cho POST/PUT.
- [x] Đánh giá có cần permission riêng cho catalog product/category/order/media không:
  - [x] Nếu chưa cần trong MVP, giữ `StoresRead/StoresWrite` cho catalog/admin chung.
  - [x] Nếu thêm permission mới, phải có migration/seed/authorization tests.
- [x] Không thêm permission mới chỉ vì tách file.
  2026-07-18 Phase 7: Control Plane API currently uses default Swagger generation without a dedicated Control Plane OpenAPI snapshot/custom operation metadata layer. This phase preserved existing route names and policies, added explicit `[FromBody]` for command request DTOs, and added reflection tests for controller authorization, no side-effecting GET, and explicit command bodies. No new permissions were introduced.

Verification:

- [x] `ControlPlaneAuthorizationTests`
- [x] Controller reflection/security tests.
- [x] API build.
  2026-07-18 Phase 7: no dedicated `ControlPlaneAuthorizationTests` class was found; equivalent coverage was added in `ControlPlaneCommerceGatewayContractTests`. Focused tests passed 19/19 with existing package advisory warnings, and `dotnet build BlazorShop.PresentationV2/BlazorShop.ControlPlane.API/BlazorShop.ControlPlane.API.csproj --no-restore` passed with 0 warnings/errors.

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
