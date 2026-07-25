# Storefront V2 Shared Platform Functional MVP.todo

Status: in progress

Goal: biến `BlazorShop.Storefront.V2` thành storefront reference thật sự dùng lại `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime` và `BlazorShop.Storefront.Components` cho các flow ecommerce chính, nhưng vẫn giữ behavior hiện tại. Phase này chỉ QA chặt `Storefront.V2`; `BlazorShop.Storefront.Starter` và `BlazorShop.Storefront.{Name}` chỉ được định nghĩa compatibility/readiness, chưa đưa vào browser QA production.

## Current codebase context

- `BlazorShop.Storefront.V2` hiện reference `Storefront.Client`, `Storefront.Runtime`, `Storefront.Components`, `Storefront.WASM`, `Web.SharedV2` và `ServiceDefaults`.
- `Storefront.Client` đã là package/contract boundary cho generated API clients.
- `Storefront.Runtime` hiện mới có runtime options, context, capability reader, error mapper và generated-client registration. Chưa đủ facade/use-case primitive để `Storefront.V2` giảm phụ thuộc vào manual `StorefrontApiClient`.
- `Storefront.Components` hiện có feature components và browser-safe local API abstraction qua `Browser/StorefrontLocalApiClient.cs`. Phase này không ép components thuần presentational ngay; tách dần container/presentation theo từng flow để không phá WASM.
- `Storefront.V2/Configuration/StorefrontServiceCollectionExtensions.cs` đang trộn manual `StorefrontApiClient` với generated clients. Catalog/content/config đã có generated adapter, còn cart/checkout/account/payment/address/consent vẫn dựa nhiều vào manual client.
- `Storefront.V2` vẫn phải sở hữu route composition, SSR/SEO, BFF endpoint, antiforgery, cookie/session/cart token, media/deployment và store resolution. Không đẩy các trách nhiệm này xuống `Runtime`.

## Target architecture

```text
SSR/BFF server flow
Storefront.V2
  -> Storefront.Runtime
      -> Storefront.Client
          -> CommerceNode Storefront API

Browser/WASM protected flow
Storefront.Components / Storefront.WASM
  -> same-origin /api/*
      -> Storefront.V2 BFF endpoints
          -> Storefront.Runtime
              -> Storefront.Client

Presentation reuse
Storefront.V2 / Starter / Storefront.{Name}
  -> Storefront.Components
```

## Non-goals

- Không redesign UI/visual theme trong phase này.
- Không triển khai AI Generator trong phase này.
- Không QA browser production cho `Storefront.Starter` hoặc `Storefront.{Name}` trong phase này.
- Không thêm payment provider mới hoặc rewrite PayPal/Stripe.
- Không đổi business behavior của Commerce Node.
- Không xóa toàn bộ `Web.SharedV2` một lần; chỉ giảm dần business DTO leakage.
- Không ép bỏ `Storefront.Components/Browser/StorefrontLocalApiClient` ngay vì hiện account/cart/checkout WASM đang phụ thuộc vào same-origin BFF pattern này.

## Phase V2F0 - Baseline lock and migration matrix

- [x] Ghi baseline commit SHA trước khi refactor.
- [x] Inventory toàn bộ dependency hiện tại của `Storefront.V2`:
  - [x] Manual `StorefrontApiClient` method theo capability.
  - [x] Generated storefront client đang được dùng.
  - [x] `Web.SharedV2` model/DTO đang được dùng trong V2.
  - [x] Components đang gọi `/api/*` qua `StorefrontLocalApiClient`.
  - [x] BFF endpoints trong `Program.cs` hoặc endpoint extension files.
- [x] Tạo migration matrix:
  - [x] Capability.
  - [x] Current owner.
  - [x] Target owner: `Client`, `Runtime`, `Components`, hoặc `V2 host`.
  - [x] QA coverage hiện có.
  - [x] Risk level.
- [x] Đánh dấu các exception tạm thời được phép giữ manual `StorefrontApiClient`.
- [x] Không chỉnh behavior ở phase này.

### V2F0 QA gate

- [x] `dotnet build BlazorShop.sln`.
- [x] Chạy focused architecture tests hiện có liên quan V2 boundaries.
- [x] Chạy focused Storefront V2 host/client/runtime tests hiện có.
- [x] Chạy browser baseline bằng Playwright cho các flow P0 đang chạy được.
- [x] Lưu baseline QA note vào `docs/refactor-control-Commerce-storefront/Storefront V2 Shared Platform Functional MVP.qa.md`.

## Phase V2F1 - Package contract completion

- [x] Bổ sung package metadata còn thiếu cho `Storefront.Components` nếu cần:
  - [x] `PackageId`.
  - [x] `Version`.
  - [x] `Authors`.
  - [x] `Description`.
  - [x] `RepositoryUrl` nếu repo đã dùng pattern này.
- [x] Chuẩn hóa package metadata của `Storefront.Client` và `Storefront.Runtime` theo cùng convention.
- [x] Tách rõ registration trong `Storefront.Runtime`:
  - [x] Core runtime primitives.
  - [x] Server-side generated clients.
  - [x] Không register browser/WASM generated clients trực tiếp.
- [x] Tạo package compatibility proof bằng local pack/restore build, chưa cần publish package.
- [x] Cập nhật architecture note nếu boundary/package rule thay đổi.

### V2F1 QA gate

- [x] `dotnet pack` cho `Storefront.Client`.
- [x] `dotnet pack` cho `Storefront.Runtime`.
- [x] `dotnet pack` cho `Storefront.Components`.
- [x] Boundary test: `Storefront.Client` không reference `Runtime`, `Components`, `V2`, `Web.SharedV2`, API/backend projects.
- [x] Boundary test: `Storefront.Runtime` chỉ được reference `Storefront.Client` và framework libraries cần thiết.
- [x] Boundary test: `Storefront.Components` không reference `CommerceNode.API`, `ControlPlane.*`, `Application`, `Infrastructure`, domain admin contracts, hoặc server-only APIs.

## Phase V2F2 - Runtime result and execution primitives

- [x] Thêm runtime result model trung lập:
  - [x] `StorefrontRuntimeResult<T>`.
  - [x] `StorefrontRuntimeError`.
  - [x] `StorefrontRuntimeValidationError`.
  - [x] `StorefrontRuntimeConflict`.
  - [x] Optional `StorefrontRuntimeSubmitResult<T>` cho mutation/idempotency.
- [x] Chuẩn hóa mapping lỗi từ generated client/API response:
  - [x] `401 Unauthorized`.
  - [x] `403 Forbidden`.
  - [x] `404 Not Found`.
  - [x] `409 Conflict`.
  - [x] `422 Validation`.
  - [x] `503 Unavailable`.
  - [x] Timeout/network failure.
- [x] Thêm helper đảm bảo mọi call storefront API luôn nhận `storeKey` rõ ràng.
- [x] Không đưa cookie, antiforgery, route parsing, SEO hoặc UI state vào Runtime.

### V2F2 QA gate

- [x] Unit test result mapping cho từng HTTP status.
- [x] Unit test timeout/network exception.
- [x] Unit test validation payload.
- [x] Unit test conflict/version mismatch.
- [x] Boundary test Runtime không reference Razor components hoặc V2 host.

## Phase V2F3 - Store bootstrap and configuration cutover

- [x] Tạo runtime facade cho public store bootstrap/configuration:
  - [x] Store identity.
  - [x] Branding/public logo/favicon URLs.
  - [x] Store lifecycle state: active, inactive, closed, maintenance, not ready.
  - [x] Locale/currency options.
  - [x] Public storefront feature flags.
  - [x] SEO defaults public-only.
- [x] V2 host giữ phần:
  - [x] Resolve current store.
  - [x] Route/domain handling.
  - [x] Maintenance redirect/page composition.
  - [x] SSR metadata rendering.
- [x] Thay `GeneratedStorefrontConfigurationClient` V2 adapter bằng runtime facade nếu facade đã đủ.
- [x] Xóa registration cũ chỉ sau khi không còn consumer.

### V2F3 QA gate

- [x] Browser test store active hiển thị home.
- [x] Browser test store inactive/closed/maintenance redirect hoặc render đúng maintenance page.
- [x] Browser test admin/manager access rule nếu V2 đang hỗ trợ.
- [x] Browser test missing/misconfigured store không fallback sang store khác.
- [x] Assert API/browser không trả secret setting.

## Phase V2F4 - Catalog, content, navigation and SEO cutover

- [x] Tạo runtime facades cho catalog/content:
  - [x] Product detail.
  - [x] Category listing.
  - [x] Search result.
  - [x] Page/topic content.
  - [x] Menu/navigation projection.
  - [x] Slug/redirect resolver output.
- [x] V2 host giữ phần:
  - [x] SSR route mapping.
  - [x] Canonical URL composition.
  - [x] Open Graph/JSON-LD rendering.
  - [x] Sitemap/robots endpoint composition.
  - [x] Page shell/layout.
- [x] Retire `GeneratedStorefrontCatalogContentClient` adapter sau khi runtime facade thay thế đủ.
- [x] Giữ DTO projection ổn định để không phá Razor pages/components hiện có.

### V2F4 QA gate

- [x] Playwright product detail SSR: title, price, image, canonical, add-to-cart surface.
- [x] Playwright category listing: paging, sorting, empty state.
- [x] Playwright search: normalize term, noindex search result nếu hiện có rule SEO.
- [x] Playwright page/topic route: published page render, unpublished hidden.
- [x] Playwright menu/navigation active item.
- [x] Playwright 301 redirect từ slug cũ nếu data fixture có.
- [x] Validate `/sitemap.xml` chỉ chứa store-visible/published entities.
- [x] Validate `/robots.txt` không cho index mutation endpoints.

## Phase V2F5 - Product interaction and component slice

- [x] Tách product detail UI thành component API rõ:
  - [x] Gallery 1x1 image list.
  - [x] Main image.
  - [x] Product summary.
  - [x] Price block.
  - [x] Variant/attribute selector.
  - [x] Quantity selector.
  - [x] Add-to-cart command surface.
- [x] V2 hoặc Runtime cung cấp product interaction model:
  - [x] Resolved variant/combination.
  - [x] Price.
  - [x] Availability.
  - [x] SKU/GTIN nếu có.
  - [x] Main image/gallery selection.
  - [x] Quantity constraints.
  - [x] Add-to-cart eligibility.
  - [x] Validation messages.
- [x] Components không tự gọi Commerce Node trực tiếp.
- [x] Components được phép gọi same-origin local BFF qua existing browser abstraction khi cần interactive WASM behavior.

### V2F5 QA gate

- [x] Playwright product gallery: danh sách ảnh 1x1 render đúng, chọn thumbnail đổi main image.
- [x] Playwright variant selection cập nhật price/availability/image nếu fixture có variant.
- [x] Playwright quantity min/max/step validation.
- [x] Playwright add-to-cart thành công từ product page.
- [x] Playwright unavailable product không cho add-to-cart và hiển thị reason.
- [x] Network assertion: browser không gọi trực tiếp Commerce Node host.

## Phase V2F6 - Cart runtime/BFF cutover

- [ ] Runtime facade cho cart:
  - [ ] Get current cart.
  - [ ] Add line.
  - [ ] Update quantity.
  - [ ] Remove line.
  - [ ] Clear cart.
  - [ ] Recalculate cart.
  - [ ] Cart warnings/validation state.
- [ ] V2 host giữ:
  - [ ] Guest/auth cart token cookie.
  - [ ] Same-origin `/api/cart/*` BFF endpoints.
  - [ ] Antiforgery.
  - [ ] Response mapping sang local browser DTO nếu cần.
- [ ] Components cart vẫn dùng `StorefrontLocalApiClient` cho browser mutation.
- [ ] Loại manual `StorefrontApiClient` cart methods khỏi active DI sau khi cutover.

### V2F6 QA gate

- [ ] Playwright add product to cart từ product page.
- [ ] Playwright cart badge count update.
- [ ] Playwright cart page load line items, image, selected attributes, unit price, line total.
- [ ] Playwright update quantity.
- [ ] Playwright remove item.
- [ ] Playwright clear cart.
- [ ] Playwright validation warning khi product không còn purchasable.
- [ ] Playwright `409` cart version conflict handling nếu API hỗ trợ.
- [ ] Antiforgery test: mutation thiếu token bị reject.

## Phase V2F7 - Checkout and COD order placement cutover

- [ ] Runtime facade cho checkout:
  - [ ] Start/resume checkout.
  - [ ] Review checkout.
  - [ ] Set billing address.
  - [ ] Set shipping address.
  - [ ] Select shipping method.
  - [ ] Select payment method.
  - [ ] Place order.
- [ ] Runtime facade cho payment method discovery:
  - [ ] Active providers.
  - [ ] Display name/icon/order.
  - [ ] Availability by store/currency/cart/country.
  - [ ] COD/offline support.
- [ ] V2 host giữ:
  - [ ] Same-origin checkout BFF endpoints.
  - [ ] Checkout session cookie/state boundary.
  - [ ] Idempotency key handling.
  - [ ] Antiforgery.
  - [ ] Redirect/return URL validation.
- [ ] Không thêm payment provider mới; COD là production QA path chính.

### V2F7 QA gate

- [ ] Playwright checkout start từ cart có item.
- [ ] Playwright billing address chọn/tạo.
- [ ] Playwright shipping address chọn/tạo hoặc skip nếu cart không cần shipping.
- [ ] Playwright shipping method chọn.
- [ ] Playwright COD payment method chọn.
- [ ] Playwright review page hiển thị address, items, totals, shipping/payment method.
- [ ] Playwright place order thật bằng COD trên store test.
- [ ] Playwright double submit không tạo order duplicate.
- [ ] Playwright cart closed/cleared sau order placement theo rule hiện có.
- [ ] Playwright order completion page hiển thị order number và payment status.
- [ ] Negative test: cart đổi sau khi chọn shipping/payment thì checkout reset downstream state.

## Phase V2F8 - Account, auth, address, order self-service and consent alignment

- [ ] Rà soát `StorefrontSessionResolver` và `StorefrontAuthClient`:
  - [ ] Giữ auth cookie/header policy ở V2 host.
  - [ ] Chỉ đưa neutral request/response/error mapping vào Runtime nếu không làm rò browser/server boundary.
- [ ] Runtime facade hoặc V2 adapter cho:
  - [ ] Login/logout status.
  - [ ] Register policy.
  - [ ] Password recovery.
  - [ ] Profile.
  - [ ] Address book.
  - [ ] Customer order list/detail.
  - [ ] Guest order lookup nếu hiện có.
  - [ ] Consent state.
- [ ] Components account/checkout/cart tiếp tục là WASM components, nhưng business truth đi qua BFF/Runtime/Client.
- [ ] Không mở thêm nhiều account pages nếu feature có thể là component trong một account shell hợp lý.

### V2F8 QA gate

- [ ] Playwright register allowed policy.
- [ ] Playwright register disabled policy không cho submit và hiển thị đúng state.
- [ ] Playwright login/logout.
- [ ] Playwright password recovery request nếu SMTP capture fixture đã setup.
- [ ] Playwright profile view/edit.
- [ ] Playwright address add/edit/delete/default.
- [ ] Playwright order history paging.
- [ ] Playwright order detail authorization: customer chỉ xem order của mình.
- [ ] Playwright guest order completion token không dùng predictable ID.
- [ ] Playwright consent accept/change/revoke.

## Phase V2F9 - Contract ownership and Web.SharedV2 reduction

- [ ] Phân loại mọi model đang dùng trong V2:
  - [ ] API transport/generated DTO: thuộc `Storefront.Client`.
  - [ ] Runtime-safe model/result: thuộc `Storefront.Runtime`.
  - [ ] Presentation/browser component model: thuộc `Storefront.Components`.
  - [ ] V2 BFF local request/response: thuộc `Storefront.V2`.
  - [ ] Utility-only shared model: có thể ở `Web.SharedV2`.
- [ ] Không tạo business DTO mới trong `Web.SharedV2`.
- [ ] Nếu DTO trùng giữa manual client và generated client, ưu tiên generated contract hoặc runtime projection.
- [ ] Ghi danh sách exception còn lại và phase xử lý.

### V2F9 QA gate

- [ ] Static guard: `Storefront.Components` không import backend/domain/application/infrastructure.
- [ ] Static guard: `Storefront.Runtime` không import Razor UI, V2 host hoặc WASM.
- [ ] Static guard: `Storefront.Client` không import Runtime/Components/V2.
- [ ] Static guard: `Storefront.V2` không dùng `Web.SharedV2.Models` cho business API contract mới.
- [ ] Snapshot OpenAPI/generated client compile vẫn pass.

## Phase V2F10 - V2 host composition cleanup

- [ ] Làm gọn `StorefrontServiceCollectionExtensions`:
  - [ ] Storefront host options.
  - [ ] Runtime registration.
  - [ ] Generated client registration qua Runtime.
  - [ ] BFF endpoint dependencies.
  - [ ] SEO/media/deployment services.
  - [ ] Auth/session/antiforgery policies.
- [ ] Tách endpoint registration nếu còn quá lớn:
  - [ ] Account endpoints.
  - [ ] Cart endpoints.
  - [ ] Checkout endpoints.
  - [ ] Consent endpoints.
  - [ ] Media endpoints.
  - [ ] SEO endpoints.
- [ ] Không rewrite logic endpoint; chỉ di chuyển cơ học sau khi behavior đã được coverage.
- [ ] Loại hoặc cô lập `StorefrontApiClient` nếu không còn active consumer.

### V2F10 QA gate

- [ ] Build toàn solution.
- [ ] Focused host smoke tests.
- [ ] Static guard: `Program.cs` chỉ còn composition và endpoint map ở mức dễ đọc.
- [ ] Static guard: manual `StorefrontApiClient` chỉ tồn tại trong documented exception list nếu chưa xóa hết.
- [ ] Playwright smoke lại home/product/cart/checkout/account.

## Phase V2F11 - Starter and Storefront.{Name} compatibility definition only

- [ ] Ghi rõ expected consumer rules cho `Storefront.Starter`:
  - [ ] Dùng `Storefront.Client` package cho API contracts/transport.
  - [ ] Dùng `Storefront.Runtime` cho server/BFF integration primitives.
  - [ ] Dùng `Storefront.Components` cho browser-safe UI components.
  - [ ] Không reference `Storefront.V2`.
  - [ ] Không reference backend/API/core projects.
- [ ] Ghi rõ expected consumer rules cho `Storefront.{Name}` generated/custom presentation:
  - [ ] Project name theo `BlazorShop.Storefront.{Name}`.
  - [ ] Presentation-specific CSS/assets/pages nằm trong project riêng.
  - [ ] Protected browser actions đi qua same-origin BFF.
  - [ ] Generated/custom storefront không được đoán API response; phải dùng generated package contract.
- [ ] Chỉ làm compile/package compatibility proof tối thiểu nếu cần để không phá package boundary.
- [ ] Không chạy Playwright production QA cho Starter hoặc Storefront.{Name} ở phase này.

### V2F11 QA gate

- [ ] Build/package compatibility proof cho `Client`, `Runtime`, `Components`.
- [ ] Documentation guard: Starter/generated storefront rules được ghi rõ.
- [ ] Static guard nếu có: generated/custom storefront không reference `Storefront.V2` hoặc backend projects.

## Phase V2F12 - Storefront.V2 production browser QA release gate

- [ ] Tạo hoặc cập nhật checklist release gate riêng:
  - [ ] `docs/refactor-control-Commerce-storefront/Storefront V2 Shared Platform Functional MVP.qa.md`.
  - [ ] Cập nhật `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` nếu checklist active cần thêm case.
- [ ] Chạy full browser QA bằng Playwright trên `Storefront.V2` với store test đã setup env.
- [ ] Không thay bằng smoke test; browser QA phải thao tác thật qua UI và tạo order thật bằng COD trên store test.
- [ ] Ghi rõ account/cart/checkout đang là WASM components, nhưng protected calls phải đi qua same-origin BFF.

### V2F12 Playwright P0 release cases

- [ ] Home SSR render: header, nav, content, footer, SEO title/canonical.
- [ ] Store closed/maintenance: redirect hoặc render maintenance page đúng.
- [ ] Category page: product grid/list, paging, sorting, empty state.
- [ ] Search page: term normalization, result/empty state, noindex nếu rule hiện có.
- [ ] Product detail SSR: name, gallery 1x1, price, availability, SEO metadata.
- [ ] Product option/variant: selection preview cập nhật price/availability/image.
- [ ] Add-to-cart: product purchasable thêm được vào cart.
- [ ] Cart badge: count update sau add/remove.
- [ ] Cart page: line item display, quantity constraints, totals, warning state.
- [ ] Checkout COD: start, address, shipping method, payment method, review, place order thật.
- [ ] Checkout duplicate submit: không tạo duplicate order.
- [ ] Completion page: order number, payment status, customer-facing reference.
- [ ] Login/logout.
- [ ] Register enabled/disabled policy.
- [ ] Password recovery với SMTP capture nếu fixture sẵn sàng.
- [ ] Account profile.
- [ ] Address book.
- [ ] Order history/list/detail authorization.
- [ ] Guest completion URL/access token behavior nếu flow hiện có.
- [ ] Consent accept/change/revoke.
- [ ] Sitemap XML chỉ chứa published/store-visible content.
- [ ] Robots.txt chặn mutation/internal routes.
- [ ] Browser network assertion: không gọi trực tiếp Commerce Node API host.
- [ ] Browser console: không có uncaught JS/.NET WASM errors.

### V2F12 error and resilience cases

- [ ] `401` session expired: UI redirect/login state đúng.
- [ ] `403` forbidden: không lộ dữ liệu account/order.
- [ ] `404` missing product/page/order: route/status UI đúng.
- [ ] `409` cart/checkout conflict: UI refresh/retry rõ ràng.
- [ ] `422` validation: field-level/global validation render đúng.
- [ ] `503` store/API unavailable: user-facing unavailable state, không blank page.
- [ ] Timeout/network failure: retry/error state không phá cart/checkout state.
- [ ] Refresh browser giữa checkout vẫn resume hoặc bắt đầu lại theo rule hiện có.

### V2F12 release acceptance

- [ ] `Storefront.V2` build pass.
- [ ] Runtime/client/component boundary tests pass.
- [ ] Storefront API contract/generator tests pass.
- [ ] Storefront V2 host tests pass.
- [ ] Full Playwright P0 pass trên `Storefront.V2`.
- [ ] QA report có link tới run command, env, store test key, commit SHA và failure evidence nếu có.
- [ ] Không có direct browser call tới Commerce Node API.
- [ ] Không có provider secret/internal setting trong public/browser response.
- [ ] Manual `StorefrontApiClient` còn lại chỉ nằm trong exception list có owner và phase follow-up.

## Suggested verification commands

```powershell
dotnet build BlazorShop.sln
dotnet test BlazorShop.sln --filter "FullyQualifiedName~Storefront"
.\scripts\run-v2-local.ps1 -StopExisting
```

Playwright command cần bám theo test harness hiện có trong repo khi triển khai phase QA. Nếu tạo script mới, dùng tên rõ ràng:

```powershell
.\scripts\qa\run-storefront-v2-shared-platform-release-gate.ps1
```

## Risk controls

- [ ] Mỗi capability cutover phải có characterization test trước khi đổi implementation.
- [ ] Không cutover cart và checkout cùng lúc nếu cart QA chưa pass.
- [ ] Không xóa manual client/DTO cho đến khi `rg` xác nhận không còn consumer.
- [ ] Không đổi public route hoặc local `/api/*` BFF path nếu chưa có redirect/compatibility test.
- [ ] Không thay đổi Commerce Node behavior trong phase Storefront package consumption.
- [ ] Không trộn QA của `Starter`/`Storefront.{Name}` vào release gate của `Storefront.V2`.

## Decision audit trail

| Decision | Reason |
| --- | --- |
| `Storefront.V2` là reference consumer chính trong phase này | Đây là storefront đang chạy thật và cần QA production. |
| `Starter` và `Storefront.{Name}` chỉ định nghĩa compatibility/readiness | User yêu cầu chưa đưa hai nhóm này vào QA phase này. |
| `Storefront.Runtime` không sở hữu cookie, antiforgery, SEO route, hoặc layout | Các phần này phụ thuộc host/BFF và phải ở `Storefront.V2` hoặc storefront host cụ thể. |
| `Storefront.Components/Browser/StorefrontLocalApiClient` được giữ trong MVP | Codebase hiện tại đã chuyển account/cart/checkout sang WASM components qua same-origin BFF; bỏ ngay sẽ phá flow. |
| Cutover theo vertical slice capability | Giảm rủi ro so với rewrite toàn bộ manual `StorefrontApiClient` một lần. |
| QA dùng Playwright browser thật và COD order thật | Smoke test không đủ phát hiện lỗi storefront production; COD/store test đã là path an toàn cho release gate. |
