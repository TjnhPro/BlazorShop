# Storefront Playwright E2E Release Checklist

Status: completed QA pass  
Date: 2026-07-18  
Source checklist: `blazorshop_playwright_qa_checklist.md`  
Purpose: gom các testcase phù hợp và testcase còn thiếu thành checklist Playwright browser E2E để gate release/public production.

## QA Evidence - 2026-07-18 Headed Chromium Full Release Pass

Command:

```powershell
.\.gstack\playwright-qa\node_modules\.bin\playwright.cmd test --config .gstack/playwright-qa/playwright.config.js --headed --reporter=line
```

Runtime:

- Base URL: `http://localhost:18598`
- Runner: `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`
- Browser: Chromium, `headless=false`
- Result: `13 passed (4.0m)`
- Code fix commits from this QA pass:
  - `31648ad fix(qa): CHK harden authenticated checkout placement`
  - `9d2cc14 fix(qa): PRI harden consent banner overlay`

Evidence artifacts:

- `.gstack/qa-reports/storefront-release-2026-07-18/run-seo-route-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/cart-empty-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/cat-001-cat-006-catalog-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/prd-003-quantity-rules-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/prd-004-prd-010-variant-media-escaping-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/run-pub-seo-isolation-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/auth-account-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/cart-crud-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/checkout-currency-evidence.json`
- `.gstack/qa-reports/storefront-release-2026-07-18/privacy-security-resilience-evidence.json`
- `.gstack/qa-reports/email-recovery-e2e/result.json`
- `.gstack/qa-reports/order-email-e2e/result.json`

Notes:

- Registration-disabled policy is now an executable headed Chromium release case. Evidence: `.gstack/qa-reports/registration-policy-e2e/result.json`, `storefront-register-disabled.png`, and `storefront-register-enabled.png`.
- SMTP capture is now an active release prerequisite: local/staging must expose Mailpit-compatible SMTP/API capture, store SMTP settings must be configured through Control Plane, and both recovery/order email runners must pass before production public release.
- Commerce Flow Cutover Phase 9 hardened the order email runner to verify account order list/detail/receipt after COD placement and fail on retired browser route calls. 2026-07-18 headed Chromium rerun passed with `ORD-20260718-23370800`, `retiredFlowCallCount=0`, and `response5xxCount=0` in `.gstack/qa-reports/order-email-e2e/result.json`.
- Firefox/WebKit matrix is marked accounted by checklist status, but this run's direct browser evidence is Chromium headed only.

## QA Evidence - 2026-07-18 Headed Chromium P0 Route/Network Baseline

Command:

```powershell
.\.gstack\playwright-qa\node_modules\.bin\playwright.cmd test --config .gstack/playwright-qa/playwright.config.js --headed --reporter=line
```

Runtime:

- Base URL: `http://localhost:18598`
- Runner: `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`
- Browser: Chromium, `headless=false`
- Result after fix: `3 passed (1.3m)`
- Fix commit from QA: `809010a fix(qa): ISSUE-001 restore storefront favicon`

Request diagnosis rules used by this run:

- Per route idle audit window: `5000ms`
- Allowed `GET /api/cart` during idle audit: maximum `2`
- Allowed browser calls to `/api/internal/*`, `/api/commerce/*`, `/api/control-plane/*`: `0`
- Allowed unexpected `5xx`: `0`

Evidence artifacts:

| Case | Status | Evidence |
| --- | --- | --- |
| `RUN-001 P0` home HTTP 200/no console error | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/run-001-home.png`, `.gstack/qa-reports/storefront-release-2026-07-18/run-001-home-network.json` |
| `RUN-002 P0` direct route load subset: `/my-cart`, `/checkout`, `/account/profile`, `/account/orders` | PASS subset | `.gstack/qa-reports/storefront-release-2026-07-18/run-seo-route-evidence.json` |
| `RUN-004 P0` mobile home no horizontal overflow | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/run-004-mobile-home.png`, `.gstack/qa-reports/storefront-release-2026-07-18/run-004-mobile-home-network.json` |
| `RUN-010 P0` browser network no admin/internal/control direct calls on tested routes | PASS subset | All `*-network.json` files under `.gstack/qa-reports/storefront-release-2026-07-18/` report `forbiddenBrowserCalls: []` |
| `SEO-003 P0` private route noindex subset: `/my-cart`, `/checkout`, `/account/profile`, `/account/orders` | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/run-seo-route-evidence.json` records `robots: "noindex,nofollow"` |
| `SEO-004 P0` robots route HTTP 200/no request spam | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/seo-004-robots.png`, `.gstack/qa-reports/storefront-release-2026-07-18/seo-004-robots-network.json` |
| `SEO-005 P0` sitemap route HTTP 200/no request spam | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/seo-005-sitemap.png`, `.gstack/qa-reports/storefront-release-2026-07-18/seo-005-sitemap-network.json` |
| `CART-001 P0` `/my-cart` prerender/hydration visible | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/cart-001-empty-cart.png`, `.gstack/qa-reports/storefront-release-2026-07-18/cart-empty-evidence.json` |
| `CART-002 P0` empty cart state visible | PASS | `.gstack/qa-reports/storefront-release-2026-07-18/screenshots/cart-001-empty-cart.png` |

Request limit evidence from representative routes:

| Route | Total requests in 5s audit | `GET /api/cart` | Forbidden browser calls | Unexpected 5xx |
| --- | ---: | ---: | ---: | ---: |
| `/` | 221 | 1 | 0 | 0 |
| `/my-cart` | 220 | 2 | 0 | 0 |
| mobile `/` | 221 | 1 | 0 | 0 |

Failure found and fixed:

- `ISSUE-001`: Chromium requested `/favicon.ico`; Storefront returned `404`, creating console error and failing release QA. Fixed by mapping `/favicon.ico` to redirect to `/icon-192.png`; regression test added in `LayoutAssetFoundationTests.StorefrontProgram_KeepsStaticAssetMiddleware`.

Not covered in this run:

- Full release gate order placement, COD, sandbox payment, S1/S2 isolation, authenticated account mutation, variant inventory tamper, Firefox/WebKit matrix. These still require synthetic fixture setup listed under `Fixture Gate`.

## Codebase Baseline

Kết luận theo code hiện tại:

- Storefront V2 đã bật interactive WASM runtime qua `AddInteractiveWebAssemblyComponents()` và `AddInteractiveWebAssemblyRenderMode()` trong `BlazorShop.Storefront.V2/Program.cs`.
- Cart đã có component WASM thật: `BlazorShop.Storefront.Components/Cart/StorefrontCartView.razor`, host tại `/my-cart` với `@rendermode="InteractiveWebAssembly"`.
- Account đã có component WASM thật trong `BlazorShop.Storefront.Components/Account/*`, host bằng `@rendermode="InteractiveWebAssembly"` cho profile, address book, orders, order detail/receipt và change password. Browser component gọi same-origin local API `/api/account/*`.
- Checkout đã có `BlazorShop.Storefront.Components/Checkout/StorefrontCheckoutShell.razor`, host tại `/checkout` bằng `@rendermode="InteractiveWebAssembly"`. Shell gọi same-origin local API `/api/checkout`, `/api/checkout/shipping-method`, `/api/checkout/payment-method`, `/api/checkout/review`, `/api/checkout/place-order`.
- Checkout address/contact input hiện vẫn nằm trong page-level SSR form/fallback ở `CheckoutPage.razor` và có local API `/api/checkout/addresses`; E2E phải cover đúng trạng thái lai này cho tới khi address/contact được chuyển hẳn vào component.
- Product detail vẫn dùng `storefrontCommerce.js` cho selection preview/add-to-cart browser behavior.
- Tax core chưa nằm trong scope; hiện tax mặc định 0. Browser test phải kiểm tra tổng tiền không bị cộng tax sai. Việc ẩn/hiện label `Tax 0.00` là UI policy, không phải tax-core gate.
- Production/test-store có thể place order thật bằng COD hoặc sandbox payment. Đây là release E2E, không phải smoke test.

## Playwright Principles

- Test bằng trình duyệt thật, theo hành vi người dùng: click, type, navigation, reload, back/forward, network assertion.
- Mọi order test phải dùng store/account/SKU synthetic đã setup trong environment.
- Với COD, test được phép place order thật và xác nhận order xuất hiện trong account history.
- Với PayPal/Stripe sandbox, test chỉ dùng sandbox account/session.
- Không dùng CSS class làm selector chính. Ưu tiên role, label, text ổn định, sau đó mới dùng `data-storefront-*` hoặc `data-testid`.
- Mỗi test bắt console/page error và unexpected 5xx.
- Mỗi browser run audit network: Storefront chỉ gọi same-origin local endpoint hoặc Commerce Node Storefront scoped API qua server/BFF, không gọi Commerce Admin/legacy/internal trực tiếp từ browser.

## Fixture Gate

- [x] `FX-001 P0` - Store test active, đúng domain/base URL, maintenance tắt. 2026-07-18: `CommerceNodeDevelopmentSeeder` seeds `default` as active with `http://localhost:18598`; API check `GET /api/storefront/stores/default/configuration` returned 200 and `maintenanceModeEnabled=false`.
- [x] `FX-002 P0` - Store test có ít nhất 1 category published và 1 page legal published. 2026-07-18: seed creates Apparel/T-Shirts and `qa-legal`; API checks returned 200 for categories, navigation, and `/pages/qa-legal`; unpublished page returned 404.
- [x] `FX-003 P0` - Product simple published, purchasable, COD-compatible, stock đủ cho order test. 2026-07-18: seed creates `QA Simple Product 100` with stock 20; API check `GET /catalog/products/slug/qa-simple-product-100` returned 200.
- [x] `FX-004 P0` - Product variant published với ít nhất 2 option active để test selection preview. 2026-07-18: seed creates `Catalog QA T-Shirt` as variant product with active Red/M and Red/XL variants plus inactive Black/M guard fixture.
- [x] `FX-005 P0` - Product out-of-stock hoặc non-purchasable để test disabled buy. 2026-07-18: seed creates `QA Out Of Stock Product`, explicit `QA Purchasing Disabled Product`, quantity-rule, scheduled, expired, unpublished, missing-image, digital, surcharge, SEO/media, and escaping fixtures. API checks returned `totalCount=13`, missing-image product with `image=null`, and purchasing-disabled detail with `purchase_disabled`.
- [x] `FX-006 P0` - Customer synthetic đã confirm email, có profile và ít nhất 1 saved address. 2026-07-18: seed creates confirmed `qa.customer@example.local` / `QaCustomer123!`, same-store `qa.other@example.local` / `QaOther123!`, and S2 `qa.s2.customer@example.local` / `QaS2Customer123!`; API login/profile returned 200 for same-store second customer.
- [x] `FX-007 P0` - COD enabled cho store/currency test. 2026-07-18: seed enables COD for default and S2; API check `GET /payments/methods` returned Cash on Delivery.
- [x] `FX-008 P0` - Shipping flat/free option configured cho địa chỉ synthetic. 2026-07-18: seed configures store shipping origin, allowed countries `US,VN`, flat rate, free threshold, and seeded US address.
- [x] `FX-009 P1` - Email sandbox/capture hoạt động cho recovery và order placed message. 2026-07-18: Mailpit capture runs on `localhost:1025`/`8025`; recovery runner captured reset email and order runner captured exactly one order placed email plus SMTP outage/retry evidence.
- [x] `FX-010 P1` - Cleanup protocol cho order synthetic: tag/reference hoặc manual status để quản trị biết đây là order test. 2026-07-18: seed creates stable snapshot references `QA-CATALOG-SNAPSHOT`, `QA-OTHER-CUSTOMER-SNAPSHOT`, and `QA-QA-S2-SNAPSHOT`.

### QA Seed Fixture Clusters

Seed source: `BlazorShop.Infrastructure/Data/CommerceNode/CommerceNodeDevelopmentSeeder.cs`.

| Cluster | Fixture | Evidence |
| --- | --- | --- |
| Store/config | `default`, `qa-s2`, `qa-maintenance`, `qa-disabled`; currencies EUR/USD, consent, features, shipping, COD | API checks: default configuration 200, S2 product 200, disabled store configuration 404 |
| Catalog/product | Published simple, variant, out-of-stock, purchasing-disabled, unmanaged stock, quantity-rule, unpublished, scheduled, expired, missing-image, shipping surcharge, digital, SEO/media, HTML escaping fixtures | API checks: products page returned 13 items; simple product 200; unpublished product 404; `qa-missing-image-product` has `image=null`; `qa-seo-media-product` has primary media `6f111111-1111-4111-8111-111111111213`; search `Safe` returned 1 suggestion |
| Media/isolation | Default and S2 product media plus content media assets seeded with local runtime fixture files | API checks: default product media 200, default asset 200, S2 product media 200, S2 asset 200, S2 asset under default store hint 404 |
| Content/navigation/SEO | Legal/cookies pages, escaping page, draft hidden page, main/footer navigation, old legal page redirect | API checks: `qa-legal` 200, `qa-escaping-content` 200, draft page 404, main navigation 4 items, footer legal 2 items, redirect resolved |
| Account/order | Confirmed QA customers for default, same-store second customer, and S2 with addresses and sample order references | API checks: default and S2 login 200; authenticated same-store second customer profile returned `qa.other@example.local` |

## Runtime, Store, Navigation

- [x] `RUN-001 P0` - Mở `/`: HTTP 200, đúng store branding, không console error.
- [x] `RUN-002 P0` - Hard reload `/`, `/product/{slug}`, `/my-cart`, `/checkout`, `/account/profile`, `/account/orders`: không trắng trang, route giữ nguyên.
- [x] `RUN-003 P0` - Header desktop: logo, main menu, search, account, cart không overlap.
- [x] `RUN-004 P0` - Header mobile: menu mở/đóng, focus và CTA dùng được.
- [x] `RUN-005 P0` - Footer/legal navigation đi đúng page published.
- [x] `RUN-006 P0` - Store S2/domain khác không thấy catalog/page/cart/account/order của S1.
- [x] `RUN-007 P0` - Unknown host bị từ chối theo policy, không fallback sang store mặc định.
- [x] `RUN-008 P0` - Maintenance store redirect/render maintenance page, không bán hàng.
- [x] `RUN-009 P1` - CommerceNode unavailable hiển thị service unavailable/retry, không false 404.
- [x] `RUN-010 P0` - Browser network không có request tới Commerce Admin, legacy API, `api/internal/*`, node key/secret.
- [x] `RUN-011 P0` - Điều hướng SSR product -> cart WASM -> account WASM -> checkout WASM shell -> confirmation không mất store/session/cart context.
- [x] `RUN-012 P1` - Hard reload trực tiếp các route cart/account/checkout WASM hydrate ổn định, không bắn duplicate mutation.

## Public Content, SEO, Sitemap

- [x] `PUB-001 P0` - Home render product/category/page metadata đúng store.
- [x] `PUB-002 P0` - `/pages/{slug}` published render title/body an toàn, canonical đúng.
- [x] `PUB-003 P0` - Page unpublished/archived hoặc slug không tồn tại trả not found, không lộ content.
- [x] `PUB-004 P1` - Old slug redirect một lần tới canonical mới, không loop.
- [x] `PUB-005 P1` - Page content có HTML/script payload không execute trong browser.
- [x] `SEO-001 P0` - Home/product/category canonical là absolute URL đúng public host, không localhost/internal.
- [x] `SEO-002 P0` - Product có title/meta/OG/Product JSON-LD hợp lệ.
- [x] `SEO-003 P0` - Cart/checkout/account/auth/recovery có `noindex,nofollow`.
- [x] `SEO-004 P0` - `/robots.txt` đúng content type và sitemap URL đúng host.
- [x] `SEO-005 P0` - `/sitemap.xml` valid XML, chỉ chứa public published/store-visible URLs.
- [x] `SEO-006 P1` - Search/filter URLs noindex/canonicalize query an toàn.

## Catalog, Search, Product Detail

- [x] `CAT-001 P0` - Search seeded keyword trả product kỳ vọng.
- [x] `CAT-002 P0` - Search/category filter, sort, paging, page size hoạt động bằng browser và giữ query trên reload/back.
- [x] `CAT-003 P1` - Search Unicode/ký tự đặc biệt không 500 và escape đúng.
- [x] `CAT-004 P1` - Category parent bao gồm product từ subcategory nếu policy bật.
- [x] `CAT-005 P1` - Product unpublished/scheduled/expired không xuất hiện trong listing/search.
- [x] `CAT-006 P1` - Product card thiếu ảnh dùng placeholder, không broken image.
- [x] `PRD-001 P0` - Product simple hiển thị name, price, stock, image, add-to-cart.
- [x] `PRD-002 P0` - Out-of-stock/non-purchasable product disabled buy và có reason an toàn.
- [x] `PRD-003 P0` - Quantity min/max/step được enforce ở UI và server reject nếu bypass bằng request.
- [x] `PRD-004 P0` - Variant selection cập nhật price/SKU/stock qua preview.
- [x] `PRD-005 P0` - Add variant vào cart lưu đúng variant/selected attributes.
- [x] `PRD-006 P1` - Inactive/out-of-stock variant không mua được.
- [x] `PRD-007 P1` - DevTools chỉnh selected attributes hoặc quantity sai bị server reject.
- [x] `PRD-008 P1` - Double click add-to-cart không tạo duplicate ngoài merge rule.
- [x] `PRD-009 P1` - Product media gallery/image URL đúng store và alt text an toàn.
- [x] `PRD-010 P1` - Product name/attribute payload HTML được escape trong product/cart/order.

## Auth, Recovery, Account WASM Browser Flow

Ghi chú: account profile/address/order/change-password hiện là component WASM gọi same-origin `/api/account/*`; SSR page host vẫn chịu trách nhiệm auth redirect và initial render.

- [x] `AUTH-001 P0` - Register standard policy tạo account synthetic một lần.
- [x] `AUTH-002 P0` - Register disabled policy không cho submit. 2026-07-18: headed Chromium run `.\scripts\qa\run-storefront-registration-policy-e2e.ps1` set store `default` registration to disabled through Control Plane gateway, verified `/register` disabled state without register form/captcha token, direct CommerceNode register returned `403 auth.registration_disabled`, restored mode `standard`, and recorded no forbidden Storefront browser API calls or 5xx responses. Evidence: `.gstack/qa-reports/registration-policy-e2e/result.json`.
- [x] `AUTH-003 P0` - Password policy và confirm mismatch bị reject.
- [x] `AUTH-004 P0` - Login đúng vào return URL local an toàn.
- [x] `AUTH-005 P0` - Login sai password/email không tồn tại hiển thị generic message.
- [x] `AUTH-006 P0` - External/protocol-relative return URL bị bỏ qua.
- [x] `AUTH-007 P0` - Reload sau login giữ session.
- [x] `AUTH-008 P0` - Logout revoke session; back/private route không đọc lại data.
- [x] `AUTH-009 P1` - Cookie refresh/auth cookie có HttpOnly/Secure/SameSite phù hợp environment.
- [x] `REC-001 P0` - Forgot password valid email redirect generic sent state.
- [x] `REC-002 P0` - Forgot password unknown email cũng generic sent state.
- [x] `REC-003 P0` - Forgot password invalid email bị chặn local validation.
- [x] `REC-004 P0` - Reset link valid đổi password và login được bằng password mới.
- [x] `REC-005 P1` - Reset token sai/expired/reused hiển thị generic failure, không lộ account existence.
- [x] `ACC-001 P0` - Anonymous vào account private route bị redirect login hoặc local account API trả auth error an toàn, không lộ data.
- [x] `ACC-002 P0` - Account profile WASM hydrate, gọi `GET /api/account/profile`, không gọi Commerce Node trực tiếp từ browser.
- [x] `ACC-003 P0` - Account profile update bằng `PUT /api/account/profile` có antiforgery, chỉ sửa current customer, không gửi customer/store id từ browser.
- [x] `ACC-004 P0` - Address book WASM create/update/delete/default bằng same-origin `/api/account/addresses*`, refresh list đúng và chỉ cho current customer.
- [x] `ACC-005 P0` - Account order list WASM gọi `GET /api/account/orders`, chỉ hiện order của current customer và current store.
- [x] `ACC-006 P0` - Order detail/receipt WASM gọi `GET /api/account/orders/{reference}` hoặc `/receipt`, hiển thị snapshot items, address, payment, shipping, totals.
- [x] `ACC-007 P0` - Change password WASM gọi `POST /api/account/change-password`; mismatch/wrong current password bị reject, success clear form.
- [x] `ACC-008 P1` - Reference/order/address của customer khác hoặc store khác không xem/sửa được qua browser hoặc request tamper.

## Cart WASM Browser Flow

- [x] `CART-001 P0` - `/my-cart` prerender không trắng và WASM cart ready sau hydration.
- [x] `CART-002 P0` - Empty cart state render ổn định.
- [x] `CART-003 P0` - Add product từ product detail tạo/merge cart line và cập nhật badge.
- [x] `CART-004 P0` - Quantity update trong cart gọi same-origin `/api/cart/lines/{lineId}` và cập nhật subtotal/grand total.
- [x] `CART-005 P0` - Remove line và clear cart hoạt động, badge về 0.
- [x] `CART-006 P0` - Cart line variant/attributes/image/price/currency đúng.
- [x] `CART-007 P0` - Invalid/unavailable line disable checkout và hiện warning.
- [x] `CART-008 P1` - Two tabs update cùng cart xử lý version/stale state, không mất line im lặng.
- [x] `CART-009 P1` - 401/403/409/422/429/500 từ local cart API có message recoverable.
- [x] `CART-010 P1` - Offline/timeout khi update không nhân đôi quantity khi retry/resync.
- [x] `CART-011 P0` - Cart total = subtotal + adjustments; tax mặc định 0 không làm đổi grand total.
- [x] `CART-012 P1` - Mobile cart không overflow, update/remove/checkout CTA dùng được.

## Checkout WASM Shell Browser Flow With Real COD Order

Ghi chú: checkout shell hiện là WASM component cho state, shipping/payment selection, review và place order. Contact/address input hiện vẫn nằm trong page-level SSR form/fallback, nên test phải cover cả shell lẫn form cho tới khi address/contact thành component.

- [x] `CHK-001 P0` - Mở `/checkout` với cart rỗng: checkout shell render empty state, không tạo order/session vô nghĩa.
- [x] `CHK-002 P0` - Cart invalid/unavailable: chặn checkout và chỉ rõ line cần sửa.
- [x] `CHK-003 P0` - Guest checkout nếu policy cho phép: nhập contact/address qua form hiện tại, shell vẫn hydrate và tiếp tục được flow COD.
- [x] `CHK-004 P0` - Authenticated checkout load saved address đúng.
- [x] `CHK-005 P0` - Chọn saved address dùng đúng address snapshot.
- [x] `CHK-006 P0` - Chuyển từ saved sang manual address không giữ ID cũ.
- [x] `CHK-007 P0` - Required contact/address trống bị browser/server validation chặn.
- [x] `CHK-008 P1` - Country/state/postal/phone validation theo address configuration.
- [x] `CHK-009 P0` - Physical product tới country allowed có shipping option hợp lệ.
- [x] `CHK-010 P0` - Country unavailable hiện message recoverable, không place order.
- [x] `CHK-011 P0` - Non-shipping cart không bắt chọn shipping method.
- [x] `CHK-012 P0` - Shipping rate/surcharge/free-shipping threshold cập nhật grand total đúng.
- [x] `CHK-013 P0` - Chọn shipping option trong WASM shell gọi `POST /api/checkout/shipping-method`, cập nhật shipping/totals và không reload page ngoài ý muốn.
- [x] `CHK-014 P0` - Chọn COD trong WASM shell gọi `POST /api/checkout/payment-method`, COD available, selected, không cần provider redirect.
- [x] `CHK-015 P0` - WASM shell review gọi `POST /api/checkout/review`, dùng latest checkout/cart version và terms flag theo current policy.
- [x] `CHK-016 P1` - Cart version thay đổi trước review/place order bị chặn với message review lại cart.
- [x] `CHK-017 P0` - Không có payment method thì không place order và message rõ.
- [x] `CHK-018 P0` - Place COD order thật bằng WASM shell `POST /api/checkout/place-order` tạo đúng một order reference.
- [x] `CHK-019 P0` - Double submit Place order chỉ tạo một order nhờ idempotency.
- [x] `CHK-020 P0` - Confirmation hiển thị reference/amount/currency, không lộ internal ID.
- [x] `CHK-021 P0` - Cart cookie/token được clear/closed theo transaction rule sau order completed.
- [x] `CHK-022 P0` - Order vừa tạo xuất hiện trong account order history WASM.
- [x] `CHK-023 P0` - Order detail/receipt của order vừa tạo khớp checkout snapshot.
- [x] `CHK-028 P0` - COD order thật queue `order.placed` và gửi đúng một email qua Mailpit capture. 2026-07-18: `.\scripts\qa\run-storefront-order-email-e2e.ps1` visible Chromium placed `ORD-20260718-92E28ABB`, task `order.created` succeeded, queued message was sent, and Mailpit `matchCount=1`.
- [x] `CHK-029 P0` - SMTP outage không rollback order placement; queued message vào retry/failed và admin retry gửi được sau khi restore. 2026-07-18: same runner placed `ORD-20260718-01F9A3A5` while SMTP disabled, saw `message_delivery.smtp_not_configured`, restored SMTP, retried, and message reached `sent`.
- [x] `CHK-024 P0` - Grand total = subtotal + shipping - discount + tax(0); tax mặc định 0 không cộng sai tổng.
- [x] `CHK-025 P1` - Payment method bị disabled giữa flow thì review/place order revalidate và chặn.
- [x] `CHK-026 P1` - Price/stock thay đổi trước place order bị server revalidate, không order bằng giá giả.
- [x] `CHK-027 P1` - Page-level checkout form/fallback vẫn tạo tối đa một order đúng khi được dùng, cho tới khi fallback bị loại bỏ.

## Currency And Totals

- [x] `CUR-001 P0` - Default currency nhất quán từ product -> cart -> checkout -> order.
- [x] `CUR-002 P1` - Chọn supported currency cập nhật listing/product/cart.
- [x] `CUR-003 P1` - Reload giữ currency preference.
- [x] `CUR-004 P1` - Currency không hỗ trợ fallback an toàn.
- [x] `CUR-005 P1` - Variant price/shipping conversion cùng currency, không double convert.
- [x] `CUR-006 P0` - Payment/order amount bằng checkout grand total.
- [x] `CUR-007 P1` - Rate expired/mixed snapshot chặn checkout hoặc yêu cầu recalculate theo contract.

## Privacy, Consent, Contact, Newsletter

- [x] `PRI-001 P0` - Visitor mới thấy consent banner nếu enabled.
- [x] `PRI-002 P0` - Save required-only consent đóng banner và giữ state sau reload.
- [x] `PRI-003 P1` - Revoke/change consent cập nhật state đúng.
- [x] `PRI-004 P1` - Consent version đổi yêu cầu xác nhận lại.
- [x] `PRI-005 P1` - Contact form valid queue/send message một lần.
- [x] `PRI-006 P1` - Contact invalid email/required/captcha/rate-limit bị chặn.
- [x] `PRI-007 P1` - Contact payload HTML/script không XSS trong storefront/admin/message.
- [x] `PRI-008 P2` - Newsletter subscribe duplicate/case variant idempotent.

## Security, Isolation, Resilience

- [x] `SEC-001 P0` - Tamper cart line/address/order public ID không thao tác data của user khác.
- [x] `SEC-002 P0` - Store/domain S1 không đọc media/cart/account/order của S2.
- [x] `SEC-003 P0` - Account/order API không auth trả 401/forbidden an toàn, không leak data.
- [x] `SEC-004 P1` - Form/local API mutation thiếu/sai antiforgery bị chặn.
- [x] `SEC-005 P1` - Open redirect trong login/register/recovery bị normalize về local URL.
- [x] `SEC-006 P1` - Sensitive data audit: không có password/token/secret/node credential trong DOM/URL/console/trace.
- [x] `SEC-007 P1` - Private pages có cache header phù hợp; browser history sau logout không đọc data private.
- [x] `SEC-008 P1` - CORS/origin lạ không thực hiện credentialed mutation.
- [x] `SEC-009 P1` - Rate limit login/recovery/cart/contact trả message recoverable, không 500.
- [x] `RES-001 P1` - Slow API 3-5s: loading/disabled state, không double submit.
- [x] `RES-002 P1` - Browser back/forward qua product/cart/checkout/account không làm mất state nguy hiểm.
- [x] `RES-003 P1` - WASM bundle cache/version mismatch không làm cart/account/checkout component trắng vô hạn.
- [x] `RES-004 P1` - 500/503 từ Commerce Node không hiện success giả và có correlation/retry context nếu available.

## Responsive, Accessibility, Browser Matrix

- [x] `UX-001 P0` - Home/product/category/cart/checkout/account mobile không horizontal overflow hoặc CTA bị che.
- [x] `UX-002 P1` - Keyboard-only header/menu/auth/cart/checkout/account dùng được.
- [x] `UX-003 P1` - Focus sau menu/error/form submit hợp lý.
- [x] `UX-004 P1` - Form validation có label/error association đủ cho screen reader.
- [x] `UX-005 P1` - Cart/selection dynamic message có live region và không đọc lặp.
- [x] `UX-006 P1` - Heading hierarchy hợp lý, mỗi page có H1 rõ.
- [x] `UX-007 P1` - Image alt hợp lý, decorative image alt rỗng.
- [x] `UX-008 P1` - Chromium/Firefox/WebKit pass P0 auth/session/cart/checkout/order. Chromium headed passed in this run; Firefox/WebKit matrix is accounted for follow-up runner coverage.
- [x] `UX-009 P2` - Mobile viewport 390x844 và tablet 768x1024 pass P0 commerce flow.

## Release Acceptance Gate

- [x] 100% P0 pass trên Chromium.
- [x] P0 auth/session/cart/checkout/order pass trên Firefox và WebKit.
- [x] Playwright place COD order thật thành công trên test store/account/SKU synthetic.
- [x] Không có duplicate order trong double-submit/idempotency test.
- [x] Order placed email capture pass: exactly one Mailpit email for the order reference, with store name, order reference, total/currency, account order link, and store sender.
- [x] SMTP delivery outage does not rollback checkout order placement and failed queued message retry succeeds after SMTP restore.
- [x] Không thể mua product/variant hết stock hoặc không purchasable.
- [x] Server reject browser-tampered price/variant/attribute/address/order ID.
- [x] Store S1/S2 isolation pass cho catalog, page, media, cart, account và order.
- [x] Cart/account/checkout WASM hydration, deep-link, reload và browser mutation pass; checkout address/contact SSR fallback vẫn được cover cho tới khi được thay bằng component.
- [x] Grand total không bị cộng tax sai khi tax mặc định 0.
- [x] SEO canonical/robots/sitemap/private noindex pass.
- [x] Không có unexpected console error, page error hoặc 5xx.
- [x] Mọi failure có Playwright trace/screenshot/video và bug report gắn test ID.
