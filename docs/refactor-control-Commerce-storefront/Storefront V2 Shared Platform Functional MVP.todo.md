# Storefront V2 Shared Platform Functional MVP.todo

Status: in progress

Goal: biến `BlazorShop.Storefront.V2` thành storefront reference thật sự dùng lại `BlazorShop.Storefront.Client`, `BlazorShop.Storefront.Runtime` và `BlazorShop.Storefront.Components` cho các flow ecommerce chính, nhưng vẫn giữ behavior hiện tại. Phase này chỉ QA chặt `Storefront.V2`; `BlazorShop.Storefront.Starter` và `BlazorShop.Storefront.{Name}` chỉ được định nghĩa compatibility/readiness, chưa đưa vào browser QA production.

## Current codebase context

- `BlazorShop.Storefront.V2` hiện reference `Storefront.Client`, `Storefront.Runtime`, `Storefront.Components`, `Storefront.V2.WASM`, `Web.SharedV2` và `ServiceDefaults`.
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
Storefront.Components / Storefront.V2.WASM
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

- [x] Runtime facade cho cart:
  - [x] Get current cart.
  - [x] Add line.
  - [x] Update quantity.
  - [x] Remove line.
  - [x] Clear cart.
  - [x] Recalculate cart.
  - [x] Cart warnings/validation state.
- [x] V2 host giữ:
  - [x] Guest/auth cart token cookie.
  - [x] Same-origin `/api/cart/*` BFF endpoints.
  - [x] Antiforgery.
  - [x] Response mapping sang local browser DTO nếu cần.
- [x] Components cart vẫn dùng `StorefrontLocalApiClient` cho browser mutation.
- [x] Loại manual `StorefrontApiClient` cart methods khỏi active DI sau khi cutover.
  - 2026-07-25: active `IStorefrontCartClient` registration now resolves `GeneratedStorefrontCartClient`, which delegates cart CRUD/recalculate/session to `IStorefrontRuntimeCartFacade`. `MergeCurrentCustomerCartAsync` remains the single documented auth-sensitive manual cart exception until the account/auth cutover phase because the generated cart client has no per-call bearer token parameter.

### V2F6 QA gate

- [x] Playwright add product to cart từ product page.
- [x] Playwright cart badge count update.
- [x] Playwright cart page load line items, image, selected attributes, unit price, line total.
- [x] Playwright update quantity.
- [x] Playwright remove item.
- [x] Playwright clear cart.
- [x] Playwright validation warning khi product không còn purchasable.
- [x] Playwright `409` cart version conflict handling nếu API hỗ trợ.
- [x] Antiforgery test: mutation thiếu token bị reject.

## Phase V2F7 - Checkout and COD order placement cutover

- [x] Runtime facade cho checkout:
  - [x] Start/resume checkout.
  - [x] Review checkout.
  - [x] Set billing address.
  - [x] Set shipping address.
  - [x] Select shipping method.
  - [x] Select payment method.
  - [x] Place order.
- [x] Runtime facade cho payment method discovery:
  - [x] Active providers.
  - [x] Display name/icon/order.
  - [x] Availability by store/currency/cart/country.
  - [x] COD/offline support.
- [x] V2 host giữ:
  - [x] Same-origin checkout BFF endpoints.
  - [x] Checkout session cookie/state boundary.
  - [x] Idempotency key handling.
  - [x] Antiforgery.
  - [x] Redirect/return URL validation.
- [x] Không thêm payment provider mới; COD là production QA path chính.
  - 2026-07-25: active `IStorefrontCheckoutClient` and `IStorefrontPaymentClient` registrations now resolve generated adapters backed by Runtime facades. Saved-address checkout with a bearer token remains a documented auth-sensitive manual exception until account/auth cutover because the generated checkout client has no per-call bearer token parameter.

### V2F7 QA gate

- [x] Playwright checkout start từ cart có item.
- [x] Playwright billing address chọn/tạo.
- [x] Playwright shipping address chọn/tạo hoặc skip nếu cart không cần shipping.
- [x] Playwright shipping method chọn.
- [x] Playwright COD payment method chọn.
- [x] Playwright review page hiển thị address, items, totals, shipping/payment method.
- [x] Playwright place order thật bằng COD trên store test.
- [x] Playwright double submit không tạo order duplicate.
- [x] Playwright cart closed/cleared sau order placement theo rule hiện có.
- [x] Playwright order completion page hiển thị order number và payment status.
- [x] Negative test: cart đổi sau khi chọn shipping/payment thì checkout reset downstream state.
  - 2026-07-25: focused checkout guard keeps `409` conflict mapping through the checkout BFF; stale cart-version recovery remains covered by existing `SecurityPrivacyPhase1CsrfTests`, `StorefrontV2WASMRuntimeFoundationTests`, and checkout host smoke guardrails. Browser V2F7 covered the production COD path.

## Phase V2F8 - Account, auth, address, order self-service and consent alignment

- [x] Rà soát `StorefrontSessionResolver` và `StorefrontAuthClient`:
  - [x] Giữ auth cookie/header policy ở V2 host.
  - [x] Chỉ đưa neutral request/response/error mapping vào Runtime nếu không làm rò browser/server boundary.
- [x] Runtime facade hoặc V2 adapter cho:
  - [x] Login/logout status.
  - [x] Register policy.
  - [x] Password recovery.
  - [x] Profile.
  - [x] Address book.
  - [x] Customer order list/detail.
  - [x] Guest order lookup nếu hiện có.
  - [x] Consent state.
- [x] Components account/checkout/cart tiếp tục là WASM components, nhưng business truth đi qua BFF/Runtime/Client.
- [x] Không mở thêm nhiều account pages nếu feature có thể là component trong một account shell hợp lý.
  - 2026-07-25: public address metadata and consent state now use `IStorefrontRuntimeAddressFacade` / `IStorefrontRuntimeConsentFacade` through generated V2 adapters. `StorefrontSessionResolver`, `StorefrontAuthClient`, and protected customer profile/address/order/change-password calls remain V2-owned auth-sensitive paths because the generated protected clients do not yet expose per-call bearer-token injection. Account UI remains inside the existing account shell.

### V2F8 QA gate

- [x] Playwright register allowed policy.
- [x] Playwright register disabled policy không cho submit và hiển thị đúng state.
- [x] Playwright login/logout.
- [x] Playwright password recovery request nếu SMTP capture fixture đã setup.
- [x] Playwright profile view/edit.
- [x] Playwright address add/edit/delete/default.
- [x] Playwright order history paging.
- [x] Playwright order detail authorization: customer chỉ xem order của mình.
- [x] Playwright guest order completion token không dùng predictable ID.
- [x] Playwright consent accept/change/revoke.
  - 2026-07-25: `run-storefront-registration-policy-e2e.ps1 -Headless` covered register enabled/disabled policy and restored registration mode. `output/playwright/v2f8-account-consent-flow-evidence.json` covered login/logout, password recovery sent state, profile edit, address add/edit/default/delete via same-origin BFF, order paging/detail, cross-customer order detail denial, consent save/revoke, no browser direct Commerce Node requests, no 5xx, and no unexpected console/page errors. Guest completion token remains backend-only in current UI and is covered by focused guest order service/OpenAPI/checkout tests proving non-predictable token behavior and hash-only storage.

## Phase V2F9 - Contract ownership and Web.SharedV2 reduction

- [x] Phân loại mọi model đang dùng trong V2:
  - [x] API transport/generated DTO: thuộc `Storefront.Client`.
  - [x] Runtime-safe model/result: thuộc `Storefront.Runtime`.
  - [x] Presentation/browser component model: thuộc `Storefront.Components`.
  - [x] V2 BFF local request/response: thuộc `Storefront.V2`.
  - [x] Utility-only shared model: có thể ở `Web.SharedV2`.
- [x] Không tạo business DTO mới trong `Web.SharedV2`.
- [x] Nếu DTO trùng giữa manual client và generated client, ưu tiên generated contract hoặc runtime projection.
- [x] Ghi danh sách exception còn lại và phase xử lý.
  - 2026-07-25: model ownership is classified as generated/API transport DTOs in `Storefront.Client`, runtime-safe facade results in `Storefront.Runtime`, reusable browser component models in `Storefront.Components`, host-local BFF request/response contracts in `Storefront.V2`, and utility-only shared types in `Web.SharedV2`. Duplicate manual/generated DTO use is resolved through generated-client runtime projection for active cutovers. Remaining documented exceptions are `StorefrontApiClient.MergeCurrentCustomerCartAsync`, saved-address `StorefrontApiClient.UpdateCheckoutAddressesAsync` with bearer token, protected `IStorefrontCustomerClient`, and `StorefrontAuthClient`; these move to V2F10 cleanup or a later auth bearer-token strategy.

### V2F9 QA gate

- [x] Static guard: `Storefront.Components` không import backend/domain/application/infrastructure.
- [x] Static guard: `Storefront.Runtime` không import Razor UI, V2 host hoặc WASM.
- [x] Static guard: `Storefront.Client` không import Runtime/Components/V2.
- [x] Static guard: `Storefront.V2` không dùng `Web.SharedV2.Models` cho business API contract mới.
- [x] Snapshot OpenAPI/generated client compile vẫn pass.

## Phase V2F10 - V2 host composition cleanup

- [x] Làm gọn `StorefrontServiceCollectionExtensions`:
  - [x] Storefront host options.
  - [x] Runtime registration.
  - [x] Generated client registration qua Runtime.
  - [x] BFF endpoint dependencies.
  - [x] SEO/media/deployment services.
  - [x] Auth/session/antiforgery policies.
- [x] Tách endpoint registration nếu còn quá lớn:
  - [x] Account endpoints.
  - [x] Cart endpoints.
  - [x] Checkout endpoints.
  - [x] Consent endpoints.
  - [x] Media endpoints.
  - [x] SEO endpoints.
- [x] Không rewrite logic endpoint; chỉ di chuyển cơ học sau khi behavior đã được coverage.
- [x] Loại hoặc cô lập `StorefrontApiClient` nếu không còn active consumer.
  - 2026-07-25: `StorefrontApiClient` still has active auth-sensitive exception consumers, so V2F10 isolates registration behind the auth/session/manual-exception group and guards the allowed exception files instead of deleting it.

### V2F10 QA gate

- [x] Build toàn solution.
- [x] Focused host smoke tests.
- [x] Static guard: `Program.cs` chỉ còn composition và endpoint map ở mức dễ đọc.
- [x] Static guard: manual `StorefrontApiClient` chỉ tồn tại trong documented exception list nếu chưa xóa hết.
- [x] Playwright smoke lại home/product/cart/checkout/account.

## Phase V2F11 - Starter and Storefront.{Name} compatibility definition only

- [x] Ghi rõ expected consumer rules cho `Storefront.Starter`:
  - [x] Dùng `Storefront.Client` package cho API contracts/transport.
  - [x] Dùng `Storefront.Runtime` cho server/BFF integration primitives.
  - [x] Dùng `Storefront.Components` cho browser-safe UI components.
  - [x] Không reference `Storefront.V2`.
  - [x] Không reference backend/API/core projects.
- [x] Ghi rõ expected consumer rules cho `Storefront.{Name}` generated/custom presentation:
  - [x] Project name theo `BlazorShop.Storefront.{Name}`.
  - [x] Presentation-specific CSS/assets/pages nằm trong project riêng.
  - [x] Protected browser actions đi qua same-origin BFF.
  - [x] Generated/custom storefront không được đoán API response; phải dùng generated package contract.
- [x] Chỉ làm compile/package compatibility proof tối thiểu nếu cần để không phá package boundary.
- [x] Không chạy Playwright production QA cho Starter hoặc Storefront.{Name} ở phase này.
  - 2026-07-25: V2F11 intentionally skipped Playwright production QA because the phase only defines Starter/generated compatibility and package proof; Storefront V2 production browser gate remains V2F12.

### V2F11 QA gate

- [x] Build/package compatibility proof cho `Client`, `Runtime`, `Components`.
- [x] Documentation guard: Starter/generated storefront rules được ghi rõ.
- [x] Static guard nếu có: generated/custom storefront không reference `Storefront.V2` hoặc backend projects.

## Phase V2F12 - Storefront.V2 production browser QA release gate

- [x] Tạo hoặc cập nhật checklist release gate riêng:
  - [x] `docs/refactor-control-Commerce-storefront/Storefront V2 Shared Platform Functional MVP.qa.md`.
  - [x] Cập nhật `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md` nếu checklist active cần thêm case.
- [x] Chạy full browser QA bằng Playwright trên `Storefront.V2` với store test đã setup env.
- [x] Không thay bằng smoke test; browser QA phải thao tác thật qua UI và tạo order thật bằng COD trên store test.
- [x] Ghi rõ account/cart/checkout đang là WASM components, nhưng protected calls phải đi qua same-origin BFF.

### V2F12 Playwright P0 release cases

- [x] Home SSR render: header, nav, content, footer, SEO title/canonical.
- [x] Store closed/maintenance: redirect hoặc render maintenance page đúng.
- [x] Category page: product grid/list, paging, sorting, empty state.
- [x] Search page: term normalization, result/empty state, noindex nếu rule hiện có.
- [x] Product detail SSR: name, gallery 1x1, price, availability, SEO metadata.
- [x] Product option/variant: selection preview cập nhật price/availability/image.
- [x] Add-to-cart: product purchasable thêm được vào cart.
- [x] Cart badge: count update sau add/remove.
- [x] Cart page: line item display, quantity constraints, totals, warning state.
- [x] Checkout COD: start, address, shipping method, payment method, review, place order thật.
- [x] Checkout duplicate submit: không tạo duplicate order.
- [x] Completion page: order number, payment status, customer-facing reference.
- [x] Login/logout.
- [x] Register enabled/disabled policy.
- [x] Password recovery với SMTP capture nếu fixture sẵn sàng.
- [x] Account profile.
- [x] Address book.
- [x] Order history/list/detail authorization.
- [x] Guest completion URL/access token behavior nếu flow hiện có.
- [x] Consent accept/change/revoke.
- [x] Sitemap XML chỉ chứa published/store-visible content.
- [x] Robots.txt chặn mutation/internal routes.
- [x] Browser network assertion: không gọi trực tiếp Commerce Node API host.
- [x] Browser console: không có uncaught JS/.NET WASM errors.

### V2F12 error and resilience cases

- [x] `401` session expired: UI redirect/login state đúng.
- [x] `403` forbidden: không lộ dữ liệu account/order.
- [x] `404` missing product/page/order: route/status UI đúng.
- [x] `409` cart/checkout conflict: UI refresh/retry rõ ràng.
- [x] `422` validation: field-level/global validation render đúng.
- [x] `503` store/API unavailable: user-facing unavailable state, không blank page.
- [x] Timeout/network failure: retry/error state không phá cart/checkout state.
- [x] Refresh browser giữa checkout vẫn resume hoặc bắt đầu lại theo rule hiện có.

### V2F12 release acceptance

- [x] `Storefront.V2` build pass.
- [x] Runtime/client/component boundary tests pass.
- [x] Storefront API contract/generator tests pass.
- [x] Storefront V2 host tests pass.
- [x] Full Playwright P0 pass trên `Storefront.V2`.
- [x] QA report có link tới run command, env, store test key, commit SHA và failure evidence nếu có.
- [x] Không có direct browser call tới Commerce Node API.
- [x] Không có provider secret/internal setting trong public/browser response.
- [x] Manual `StorefrontApiClient` còn lại chỉ nằm trong exception list có owner và phase follow-up.

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

- [x] Mỗi capability cutover phải có characterization test trước khi đổi implementation.
- [x] Không cutover cart và checkout cùng lúc nếu cart QA chưa pass.
- [x] Không xóa manual client/DTO cho đến khi `rg` xác nhận không còn consumer.
- [x] Không đổi public route hoặc local `/api/*` BFF path nếu chưa có redirect/compatibility test.
- [x] Không thay đổi Commerce Node behavior trong phase Storefront package consumption.
- [x] Không trộn QA của `Starter`/`Storefront.{Name}` vào release gate của `Storefront.V2`.

## Decision audit trail

| Decision | Reason |
| --- | --- |
| `Storefront.V2` là reference consumer chính trong phase này | Đây là storefront đang chạy thật và cần QA production. |
| `Starter` và `Storefront.{Name}` chỉ định nghĩa compatibility/readiness | User yêu cầu chưa đưa hai nhóm này vào QA phase này. |
| `Storefront.Runtime` không sở hữu cookie, antiforgery, SEO route, hoặc layout | Các phần này phụ thuộc host/BFF và phải ở `Storefront.V2` hoặc storefront host cụ thể. |
| `Storefront.Components/Browser/StorefrontLocalApiClient` được giữ trong MVP | Codebase hiện tại đã chuyển account/cart/checkout sang WASM components qua same-origin BFF; bỏ ngay sẽ phá flow. |
| Cutover theo vertical slice capability | Giảm rủi ro so với rewrite toàn bộ manual `StorefrontApiClient` một lần. |
| QA dùng Playwright browser thật và COD order thật | Smoke test không đủ phát hiện lỗi storefront production; COD/store test đã là path an toàn cho release gate. |
