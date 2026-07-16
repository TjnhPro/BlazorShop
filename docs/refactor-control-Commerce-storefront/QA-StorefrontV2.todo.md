# QA Storefront V2 Todo

## Scope

QA nay theo doi `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`.

Muc tieu hien tai:

- Xac nhan Storefront V2 copy/reuse tu legacy Storefront ma khong sua legacy project.
- Xac nhan V2 goi Commerce Node `api/storefront/stores/{storeKey}/*` mac dinh.
- Xac nhan V2 khong gui `X-Store-Key` cho Storefront API calls.
- Xac nhan legacy API fallback bi tat mac dinh.
- Xac nhan `api/internal/*` da bi remove khoi CommerceNode runtime sau scoped route QA; neu gap request moi toi `api/internal/*` thi do la regression.

## Required Services

- [x] Commerce Node PostgreSQL container.
  - Compose file: `compose.commercenode.yml`
  - Container: `blazorshop-commercenode-postgres`
  - Host port: `5434`
- [x] Commerce Node API.
  - Project: `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`
  - Local HTTP URL: `http://localhost:5180`
- [x] Storefront V2.
  - Project: `BlazorShop.PresentationV2/BlazorShop.Storefront.V2`
  - Local HTTP URL: `http://localhost:18598`
- [x] Browser QA runs with Playwright MCP visible Chromium (`headless=false`) when operator observation is requested. 2026-07-10: local StorefrontV2 catalog/auth/cart smoke used Playwright MCP visible browser.
- [n/a] Client app, only when testing login/register/checkout UI handoff. 2026-07-09: StorefrontV2 returned the expected 302 handoff; external client app was intentionally not started for independent V2 QA.

## Clean DB Setup

- [x] Start Commerce Node DB:

```powershell
docker compose -f compose.commercenode.yml up -d
```

- [x] Apply Commerce Node migrations through CommerceNode API startup. Development should run with `CommerceNode:Database:MigrateOnStartup=true`:

```powershell
dotnet run --project BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/BlazorShop.CommerceNode.API.csproj --urls http://localhost:5180
```

- [x] Seed at least one published category.
  - 2026-07-09: QA categories loaded from Commerce Node DB.
- [x] Seed at least two published products.
  - 2026-07-09: QA products loaded from Commerce Node DB.
- [x] Seed at least one product variant.
  - 2026-07-09: variant selector verified on product detail.
- [x] Seed one Storefront customer account. 2026-07-09: historical API smoke registered and logged in a QA customer through `api/internal/auth`; current auth QA uses scoped Storefront routes.
- [x] Seed SEO settings and one redirect rule.
  - 2026-07-09: redirect rule verified with 301 to `/product/qa-product-20260708234046`.

## Automated Verification

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`
- [x] `dotnet build BlazorShop.sln`
- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --filter FullyQualifiedName~PresentationV2.Storefront`
  - 2026-07-09: passed 23/23 after Storefront V2 local auth implementation.
- [x] `dotnet test BlazorShop.sln`
  - 2026-07-09: passed with 501 passed, 10 skipped after Storefront V2 local auth implementation.
- [x] API client parses API response envelope.
- [x] API client calls Commerce Node scoped Storefront catalog route. 2026-07-14: client tests assert `/api/storefront/stores/default/catalog/categories`.
- [x] API client auth calls Commerce Node scoped Storefront auth routes. 2026-07-14: client tests assert scoped login/register/logout routes.
- [x] API client parses scoped auth token response without nested auth envelope. 2026-07-14: `StorefrontAuthClient` consumes `data.accessToken` / `data.expiresAtUtc`; auth client tests were updated.
- [x] Storefront HTTP clients do not send `X-Store-Key` on scoped API requests. 2026-07-14: `ConfigureStorefrontHttpClient` now sets scoped base address and no default store header; `/` runtime log showed scoped CommerceNode URLs.
- [x] API client can request consolidated public store configuration from `api/storefront/stores/{storeKey}/configuration`. 2026-07-15: `StorefrontV2ApiClientTests.GetPublicConfigurationAsync_ReadsStoreScopedConfiguration` passed and asserted the scoped path.
- [x] API client models can read safe public payment metadata from consolidated configuration. 2026-07-15: `StorefrontPublicPaymentMethod` now includes short display text, icon URL, supported currencies, and supported countries; Storefront V2 build passed.
- [x] API client can post product selection preview to `api/storefront/stores/{storeKey}/catalog/products/{productId}/selection-preview`. 2026-07-16 Phase 4: `StorefrontApiClient.PreviewProductSelectionAsync` added; CommerceNode controller/OpenAPI focused run passed 32/32.
- [x] API client does not call legacy fallback when `Api:EnableLegacyFallback=false`.
- [x] API client can use legacy fallback only when explicitly enabled.
- [x] Checkout anonymous redirect is covered by V2 host smoke test.
- [x] Robots route is covered by V2 host smoke test.
- [x] Sitemap route is covered by V2 host smoke test.
- [x] Empty cart route is covered by V2 host smoke test.
- [x] Storefront V2 references `BlazorShop.Web.SharedV2`, not legacy `BlazorShop.Web.Shared`.
  - 2026-07-09: covered by PresentationV2 boundary tests.
- [x] After SharedV2 changes, re-run catalog/cart smoke and `FullyQualifiedName~PresentationV2.Storefront`. 2026-07-09: full `dotnet test BlazorShop.sln --no-restore` passed 485/485 with 10 skipped; Playwright catalog/product/cart smoke passed.

## WASM Foundation

- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj`. 2026-07-13: passed.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj`. 2026-07-13: passed.
- [x] `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj`. 2026-07-13: passed after adding `MapStaticAssets()`.
- [x] `dotnet build BlazorShop.sln`. 2026-07-13: passed with existing package vulnerability warnings.
- [x] Existing SSR Storefront route returns HTML before hydration. 2026-07-13: `GET http://localhost:18598/signin` returned 200 and included `data-wasm-probe`.
- [x] `_framework/blazor.web.js` loads from Storefront V2. 2026-07-13: returned HTTP 200 after adding `MapStaticAssets()`.
- [x] WASM boot resources load from Storefront V2. 2026-07-13: server log showed `_framework/BlazorShop.Storefront.Components.*.wasm` and resource collection assets returned 200.
- [x] Development-only `WasmProbe` renders on an existing route. 2026-07-13: Playwright MCP found `WASM active` on `/signin`.
- [x] `WasmProbe` button increments without a full page reload. 2026-07-13: Playwright MCP clicked the probe button and verified `Count 1`.
- [x] `storefrontCommerce.js` still initializes cart/toast behavior. 2026-07-13: `/signin` HTML still included `js/storefrontCommerce.js`; no browser console errors beyond Blazor debug hotkey info.

## Runtime Smoke

- [x] Start Commerce Node API with launch profile `http`.
- [x] Call `GET http://localhost:5180/api/commerce/healthz` with headers:
  - `X-Node-Key: dev-node`
  - `X-Node-Secret: dev-node-secret`
  - 2026-07-09: returned 200.
- [x] Start Storefront V2.
- [x] Load `GET http://localhost:18598/`.
  - 2026-07-09: returned 200 while Commerce Node API was running.
  - 2026-07-14: returned 200 after Storefront V2 client switch; server log showed scoped `api/storefront/stores/default/store/current`, `catalog/categories`, `catalog/products`, and `catalog/categories/tree`.
- [x] Stop Commerce Node API and reload Storefront V2 `/`.
  - 2026-07-09: returned 503 and did not show legacy fallback markers.

## Public Catalog Pages

- [x] `/`
  - [x] Shows published category data from Commerce Node.
  - [x] Shows published product cards from Commerce Node.
  - [x] Does not require legacy `BlazorShop.API`.
  - 2026-07-09: verified with legacy API process not running.
- [x] `/category/{slug}`
  - [x] Shows category breadcrumb.
  - [x] Shows products under category.
- [x] Price range filter applies through query string. 2026-07-10: `/category/t-shirts?minPrice=19&maxPrice=20&sortBy=PriceLowToHigh` returned `Catalog QA T-Shirt` and excluded `Catalog QA Low Stock Tee`.
- [x] In-stock filter hides out-of-stock products. 2026-07-10: `/category/t-shirts?sortBy=Updated&inStock=true` returned 200 and rendered the in-stock seeded products.
- [x] Sort by display order works. 2026-07-10: `/category/t-shirts?sortBy=DisplayOrder` returned 200 after fixing enum query parsing.
- [x] Sort by updated works. 2026-07-10: `/category/t-shirts?sortBy=Updated&inStock=true` returned 200 after fixing enum query parsing.
  - [x] Missing slug returns noindex 404 state.
- [x] `/product/{slug}`
  - [x] Shows product image.
  - [x] Shows product price.
  - [x] Shows product category link.
  - [x] Shows variants selector when variants exist.
- [x] Shows variant attributes such as `Color` and `Size`. 2026-07-10: product page rendered `Color: Red / Size: M`, `Color: Red / Size: XL`, and `Color: Black / Size: M`.
- [x] Shows variant effective price. 2026-07-10: product page rendered variant prices `EUR 19.99` and `EUR 21.99`.
- [x] Shows compare price when present. 2026-07-10: product page rendered `EUR 24.99` compare price for `Catalog QA T-Shirt`.
- [x] Shows out-of-stock state for product/variant. 2026-07-10: product page rendered `Black / M` as `Out of stock`.
- [x] Product detail renders variation template dropdown/radio/color controls and backend preview markers. 2026-07-16 Phase 5: `StorefrontBrandingMarkupTests.ProductPage_UsesBackendSelectionPreviewForVariantAttributes` passed.
- [x] Product detail posts selected attributes and quantity through the Storefront V2 selection-preview proxy before add-to-cart. 2026-07-16 Phase 5: JS guardrail test and `node --check` passed.
- [x] Product detail add-to-cart sends the same selected attributes used for preview. 2026-07-16 Phase 5: JS posts `SelectedAttributes` to `/api/cart/lines`; focused Storefront/Cart tests passed 43/43.
- [x] Product card direct add-to-cart is suppressed unless no variants, product is purchasable, and quantity `1` satisfies min/max/step rules. 2026-07-16 Availability Quantity Phase 5: `StorefrontBrandingMarkupTests.ProductCard_RendersSellabilitySafeActions` passed.
- [x] Product detail renders sellability quantity metadata, disabled hard-block state, free-shipping badge, and delivery estimate before preview JS runs. 2026-07-16 Availability Quantity Phase 5: `StorefrontBrandingMarkupTests.ProductPage_RendersSellabilityAndQuantityMetadata` and `StorefrontV2HostSmokeTests` passed.
- [ ] Visible browser QA confirms disabled buy button and customer-safe reason text for a purchase-disabled product.
- [ ] Visible browser QA confirms quantity selector respects min quantity and step for a product with custom quantity rules.
- [ ] Visible browser QA confirms unmanaged-stock product can be added to cart.
- [ ] Visible browser QA confirms managed out-of-stock product cannot be added to cart.
  - [x] Shows add-to-cart button.
  - [x] Shows related products/recommendations block.
  - [x] Missing slug returns noindex 404 state.
- [x] `/new-releases`
  - [x] Shows latest published products.
- [x] `/todays-deals`
  - [x] Shows discount/deal products when available.

## Dynamic Storefront Pages

- [x] Storefront V2 builds after dynamic StorefrontPage rendering changes. 2026-07-11: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- [x] `/pages/{slug}` renders published page title. 2026-07-12: Playwright MCP visible browser rendered `QA Dynamic Page 20260712034014` at `/pages/qa-dynamic-page-20260712034014`.
- [x] `/pages/{slug}` renders intro. 2026-07-12: Playwright MCP visible browser rendered `QA intro marker 20260712034014`.
- [x] `/pages/{slug}` renders body HTML. 2026-07-12: Playwright MCP visible browser rendered `QA body marker 20260712034014` and list body content; screenshot saved at `.gstack/qa-reports/screenshots/storefront-page-qa-dynamic-page-20260712034014.png`.
- [x] Local image path in body renders. 2026-07-13: investigated `qa-dynamic-page-20260712034014`; Storefront V2 now proxies `/media/assets/...` and Playwright network showed page `200` plus image `200`.
- [x] Programmatic heading focus after navigation does not show a black browser outline. 2026-07-13: Playwright verified the dynamic page and home page keep `h1` focused with `outline-style: none` and empty text selection.
- [ ] HTTPS anchor link renders.
- [x] Missing page returns 404. 2026-07-12: `GET /pages/non-existing-page-qa` returned 404 with not-found content.
- [x] Draft page returns 404. 2026-07-12: CommerceNode pre-removal Internal API returned 404 for `qa-draft-page-20260712034014`; current page QA should use scoped Storefront APIs.
- [ ] Archived page returns 404.
- [ ] CommerceNode API unavailable renders service unavailable, not 404.
- [x] Page SEO uses page meta fields. 2026-07-12: Playwright page title was `QA Dynamic Page 20260712034014 SEO`, coming from the page meta title.
- [x] Sitemap includes `/pages/{slug}` when published and included. 2026-07-12: `GET /sitemap.xml` contained `/pages/qa-dynamic-page-20260712034014`.
- [x] Sitemap excludes draft page. 2026-07-12: `GET /sitemap.xml` did not contain `/pages/qa-draft-page-20260712034014`.
- [ ] Sitemap excludes published page with `include_in_sitemap=false`.
- [x] Old `/privacy`, `/faq`, `/terms`, `/customer-service`, `/about-us` routes return 404. 2026-07-12: all five routes returned HTTP 404.
- [x] Header/footer no longer hard-code `/pages/about-us`, `/pages/customer-service`, `/pages/faq`, `/pages/privacy`, or `/pages/terms`. 2026-07-15: `StorefrontBrandingMarkupTests` passed and asserts content links come from `IStorefrontPageNavigationProvider`.

### Basic Page Content Navigation

- [x] Storefront API client reads published content navigation from `GET /api/storefront/stores/{storeKey}/pages/navigation`. 2026-07-15: `StorefrontV2ApiClientTests.GetPageNavigationLinksAsync_ReadsPublishedContentNavigation` passed.
- [x] Storefront page navigation provider filters links by `NavigationLocation`, orders by display order/title, and caches within the request scope. 2026-07-15: `StorefrontPageNavigationProviderTests.GetLinksByLocationAsync_FiltersOrdersAndCachesWithinScope` passed.
- [x] Storefront page navigation provider returns an empty list when the navigation endpoint is unavailable, avoiding broken legal/support links. 2026-07-15: `StorefrontPageNavigationProviderTests.GetLinksAsync_WhenNavigationEndpointUnavailable_ReturnsEmptyList` passed.
- [x] Footer renders company/support/legal links from published page navigation locations `footer_company`, `footer_support`, and `footer_legal`. 2026-07-15: Storefront V2 build and branding markup tests passed.
- [x] Header renders only pages explicitly marked with `NavigationLocation=header`; required legal/support pages are not auto-promoted. 2026-07-15: Storefront V2 build and branding markup tests passed.
- [x] Product/category/deal navigation remains owned by existing route constants and was not rewritten into the page template catalog. 2026-07-15: `StorefrontHeader.razor` and `StorefrontFooter.razor` keep product/deal routes while content links are page-driven.

### Menu Navigation Core

- [x] Storefront API client reads configured menus from `GET /api/storefront/stores/{storeKey}/navigation/{systemName}`. 2026-07-15: Storefront V2 build passed after adding `GetNavigationMenuAsync`.
- [x] Storefront navigation provider caches menu lookups per request scope and returns empty menus when the API is unavailable. 2026-07-15: provider added with fallback behavior; Storefront V2 build passed.
- [x] Header renders configured `main` menu when available and preserves hard-coded/page-navigation fallback when unavailable. 2026-07-15: Storefront V2 build passed after header integration.
- [x] Footer renders configured `footer_company`, `footer_support`, and `footer_legal` menus when available and preserves existing fallback links. 2026-07-15: Storefront V2 build passed after footer integration.
- [x] Search, cart, account, checkout, login, register, new releases, and todays deals remain reserved system targets instead of menu-owned business logic. 2026-07-15: `StoreNavigationSystemTargets` and header/footer integration keep these as route/component slots.
- [x] Final focused Storefront verification passed. 2026-07-15: Storefront V2 build passed in the Menu Navigation closeout run.
- [ ] Visible browser QA: desktop header renders configured menu labels without overlap and cart/account/search remain usable.
- [ ] Visible browser QA: mobile navigation renders configured menu tree without layout shift or clipped text.
- [ ] Visible browser QA: footer configured menus render and fallback still works when the menu API is unavailable.

## Store Config Consumption

- [x] Header brand name uses current store/company profile instead of generic fallback when configured. 2026-07-15: visible Storefront QA against `default` rendered configured `QA Store Company`/store branding from current-store.
- [x] Header logo renders when `LogoUrl` is configured and remains stable when it is empty. 2026-07-15: visible Storefront QA rendered configured safe logo path without console errors; `StorefrontBrandingMarkupTests` covers stable logo dimensions.
- [x] Head icon metadata uses store favicon/png/apple/MS tile values when configured. 2026-07-15: HTTP HTML checks found configured favicon/png/MS tile metadata after fixing `ISSUE-001` head metadata collision.
- [x] Document language is derived from store `DefaultCulture`. 2026-07-15: current-store `en-GB` produced `bs-storefront-language`/document language `en`; focused Storefront display context tests passed.
- [x] Storefront V2 has typed client models for the consolidated public configuration projection. 2026-07-15: client test parsed store identity, currency options, feature flags, payment method metadata, and SEO defaults from the new endpoint.
- [~] Product card, product detail, cart, and checkout price labels use store `DefaultCurrencyCode`. 2026-07-15: focused formatter/markup tests passed and `/my-cart` HTTP smoke showed `GBP`; full product-detail/cart/checkout browser coverage is still pending.
- [~] Add-to-cart sends the current store currency code and cart line snapshot records it. 2026-07-15: `StorefrontLocalCart_PostsCurrencyCode` and add-to-cart markup coverage passed; live add-to-cart browser snapshot was not exercised in this QA pass.
- [x] Footer renders configured company/support email, phone, and address. 2026-07-15: visible Storefront/HTTP QA found configured support email/phone and company address in footer.
- [ ] Footer hides empty contact rows and keeps legal/page links stable.
- [x] Home `/` uses published StorefrontPage slug `home` SEO metadata when present. 2026-07-15: CommerceNode page `home` with `QA Home Meta Title`/description drove Storefront `/` title and meta description.
- [ ] Home `/` falls back to static metadata when slug `home` is missing or unpublished.
- [x] Home canonical remains `/`, not `/pages/home`. 2026-07-15: Storefront `/` ignored the page-level `/pages/home` canonical fixture and kept home-route canonical behavior.
- [x] JSON-LD organization uses current store company/contact/logo data before SEO singleton settings. 2026-07-15: HTTP HTML check found organization JSON-LD data for `QA Store Company`, support contact, and logo; source encodes `application/ld+json` plus as `&#x2B;`.
- [x] Disabled/provisioning/maintenance stores still render the expected not-ready/maintenance state instead of catalog content. 2026-07-15: lifecycle QA covered provisioning current-store not-ready rendering; store config browser pass kept runtime active for normal catalog rendering.

2026-07-15 Store Config QA notes:

- [x] `ISSUE-001`: Storefront brand head metadata previously used a layout-level `HeadContent`, which suppressed page SEO `HeadContent` output. Fixed by rendering `StorefrontBrandHead` directly in `App.razor` head before `HeadOutlet`; focused tests passed 24/24 and browser console showed no Storefront errors after fixture correction.

## Currency

- [x] Storefront V2 remains single-currency in the current phase; price labels should use the current store default currency from configuration/display context. 2026-07-15: Phase 1 keeps selector/exchange-rate UI out of scope.
- [x] Add-to-cart may still send a client currency hint for compatibility, but CommerceNode ignores it and snapshots the server-resolved store default currency. 2026-07-15: CommerceNode Phase 1 service tests cover spoofed client currency.
- [x] Public configuration can expose multiple supported currency codes from CommerceNode without Storefront V2 showing a selector yet. 2026-07-15: CommerceNode Phase 2 service/config projection added; Storefront selector remains future scope.
- [x] Storefront currency preference remains server-command driven and checkout-safe; non-base selection UI is still hidden until conversion is available. 2026-07-15: CommerceNode Phase 4 added POST preference endpoint but keeps Storefront V2 selector out of scope.
- [x] Manual exchange rates can be configured in CommerceNode without Storefront V2 enabling non-base display or checkout. 2026-07-15: CommerceNode Phase 5 added manual rate service/API; Storefront selector and display conversion remain future scope.
- [x] CommerceNode backend can keep cart, checkout, order, and payment in one converted non-base currency when Storefront sends an approved currency hint. 2026-07-15: service-level EUR conversion guardrails passed; Storefront V2 visible selector/display remains future scope.
- [x] Storefront V2 display context reads supported currencies from public configuration and uses the validated `bs-currency` preference cookie only when it is supported. 2026-07-15: `StorefrontDisplayContextProviderTests.GetAsync_WhenCurrencyCookieIsSupported_UsesWorkingCurrency` passed.
- [x] Storefront V2 sends `currencyCode` to scoped catalog/product APIs and posts selector changes through the Storefront-local `/currency` command handler. 2026-07-15: `StorefrontV2ApiClientTests.GetPublishedCatalogPageAsync_WhenCurrencyCodeProvided_AddsCurrencyQuery` and `SetCurrencyPreferenceAsync_PostsStoreScopedCurrencyCommand` passed.
- [x] Product card/detail prefer additive `displayPrice`/`displayCurrencyCode` fields and keep base `price` unchanged for fallback. 2026-07-15: Storefront V2 build and focused formatter/markup/client tests passed.
- [x] Cart and checkout display line/totals using cart line `CurrencyCodeSnapshot` instead of blindly formatting with current default currency. 2026-07-15: Storefront focused tests and full solution build passed after line currency propagation.
- [x] Currency selector stays hidden for stores with only one enabled supported currency. 2026-07-15: selector rendering is gated by `SupportedCurrencyCodes.Count > 1`; visible browser QA still pending.
- [ ] Visible product-detail/cart/checkout QA confirms selected non-base currency, cart snapshot currency, checkout payment methods, and displayed totals agree.

## Catalog Search MVP

Use this checklist whenever `StorefrontHeader`, `/search`, `StorefrontApiClient`, catalog query model, or CommerceNode published catalog search changes.

- [x] StorefrontV2 builds after adding header search and `/search`. 2026-07-10: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- [x] Focused StorefrontV2 tests pass after adding catalog search. 2026-07-10: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2.Storefront"` passed 23/23.
- [~] Full test suite attempted after catalog search implementation. 2026-07-10: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore` failed 11/512. Failures were in migration model consistency, existing Product/Category delete unit expectations, CartService tests, and sitemap timestamp tests; focused StorefrontV2 tests passed.
- [ ] Header search renders on desktop without overlapping brand, nav, cart, or account controls.
- [ ] Header search renders on mobile without overlapping cart, account, or menu controls.
- [ ] Header category combobox loads published category tree options.
- [ ] Category change alone does not submit the form.
- [ ] Pressing Enter in the search input navigates to `/search`.
- [ ] Search button navigates to `/search`.
- [ ] Empty input with `All` category shows published products.
- [ ] Empty input with selected category shows products scoped to that category.
- [ ] Parent category search includes child category products.
- [ ] Text search targets product title.
- [ ] Text search does not match SKU-only or description-only terms.
- [ ] Invalid category slug shows `No products found`.
- [ ] `/search` renders `noindex,follow`.
- [ ] `/search` is not present in `/sitemap.xml`.
- [ ] Pagination renders at most 10 pages.
- [ ] Page values greater than backend max are clamped by backend.
- [ ] Browser console has no unexpected errors on `/search`.

## Product Media Rendering

- [ ] Product listing displays `Product.Image` when it is a `/media/products/{mediaId}` URL.
- [ ] Product detail displays `Product.Image` when it is a `/media/products/{mediaId}` URL.
- [ ] Cart line displays product media URL after add-to-cart.
- [ ] Public media URL resolves on the current Storefront host.
- [ ] Public media URL defaults to bounded optimized output instead of original full-size image.
- [ ] Product structured data image remains valid when `Product.Image` is a media URL.

## Cart Flow

- [x] `/my-cart` renders without Commerce Node for empty cart smoke.
- [x] Cart badge updates after add-to-cart.
- [x] Add product without variant.
- [x] Add product with required variant.
- [x] Cart cookie contains `ProductVariantId` for selected variant. 2026-07-10: Playwright decoded `my-cart` cookie and found `ProductVariantId` plus `VariantId` for selected `Red / XL`.
- [x] Cart line displays selected variant attribute label. 2026-07-10: cart rendered `RED / M` after selecting the `Red / M` variant.
- [x] Cart line uses selected variant effective price. 2026-07-10: cart rendered `EUR 19.99` for `Red / M` and cookie stored `UnitPrice=21.99` for `Red / XL`.
- [x] Product detail blocks add-to-cart until required variant is selected.
- [x] Product detail blocks add-to-cart for out-of-stock variant. 2026-07-10: selecting `Black / M` and clicking Add to Cart produced no `my-cart` cookie and cart stayed empty.
- [x] Quantity update works.
- [x] Remove item works.
- [x] Clear cart works.
- [x] Cart refreshes product details from `api/storefront/stores/{storeKey}/catalog/products/{id}`. 2026-07-14: `StorefrontApiClient` route switched to scoped catalog base.
- [x] Invalid cart cookie does not crash page.
- [~] Unavailable product in cart shows warning state. Code path exists in `CartPage` for missing catalog products; live QA used available seeded products only.
- [x] Cart route stays private/noindex.
  - 2026-07-09: verified `X-Robots-Tag`, noindex meta, and no-store/no-cache.

## Server Cart, Checkout Session & Payment Provider MVP

Baseline plan: `BlazorShop.CommerceNode.CartCheckoutPaymentProviderMvp.autoplan.md`.

- [x] Phase 0 records Storefront V2 migration checklist before runtime changes. 2026-07-14: checklist added for server-cart/token checkout cutover.
- [x] Storefront V2 creates or resumes an opaque server cart token instead of treating `my-cart` JSON as authoritative. 2026-07-14: local `/api/cart` bridge creates/resumes Commerce Node cart sessions and pages read server cart lines.
- [x] New cart token cookie is `HttpOnly`, `SameSite=Lax`, `Path=/`, and `Secure` outside development. 2026-07-14: `StorefrontCartTokenService` writes `bs-cart-token` with session expiry and secure production policy.
- [x] Existing `my-cart` cookie is imported once into the server cart and then deleted after successful import. 2026-07-14: host test covers legacy cookie import, token write, and `my-cart` deletion.
- [x] Add-to-cart uses a Storefront V2 local endpoint/API client call instead of writing final price/order data directly to the cookie. 2026-07-14: JS posts to `/api/cart/lines`; host test verifies `UnitPrice` is not forwarded to Commerce Node.
- [x] `/my-cart` renders server cart lines, normalized product/variant labels, totals, and unavailable-line warnings. 2026-07-14: cart page now enriches `StorefrontCartResponse` lines from catalog and keeps unavailable-line warning path.
- [x] Quantity update, remove line, and clear cart mutate the server cart and refresh the cart version. 2026-07-14: JS uses local PUT/DELETE cart endpoints; Commerce Node exposes `DELETE /cart` for clear.
- [x] `/checkout` calls checkout preview before final submit and displays field-level validation errors. 2026-07-14: Storefront V2 checkout POST now calls `/checkout/preview` using cart token/version and redirects with the first validation issue before direct checkout.
- [x] Stale cart version on checkout shows a review-cart state, not a duplicate order. 2026-07-14: `StorefrontCheckoutServiceTests` covers stale cart version rejection with no checkout session/order creation.
- [x] COD checkout places one order, clears/expires the cart token, and shows confirmation. 2026-07-14: Storefront V2 checkout POST now calls Commerce Node `/checkout/place-order` after preview, deletes `bs-cart-token`/`my-cart`, and redirects with order reference; focused host/contract suite passed.
- [x] Online provider checkout redirects to provider next action when configured. 2026-07-14: `StorefrontV2HostSmokeTests.Checkout_PostRedirectsToProviderNextAction` covers checkout POST redirect to provider `NextAction.Url` and cart-token cleanup.
- [x] Provider cancel/failure returns to a recoverable checkout state. 2026-07-14: `/payment-cancel` polls the payment attempt and renders retry checkout action; host smoke test covers failed provider state.
- [x] Payment attempt polling page shows pending, success, failure, and retry states. 2026-07-14: `/payment-success` and `/payment-cancel` host smoke tests cover captured, pending refresh, failed, and retry states.
- [x] Visible browser QA confirms payment return/cancel pages have no console errors. 2026-07-15: Playwright headed run visited `/payment-success?paymentAttemptId=00000000-0000-0000-0000-000000000000` and `/payment-cancel?paymentAttemptId=00000000-0000-0000-0000-000000000000`; console errors/page errors were 0.
- [x] Browser QA verifies no readable raw cart price payload remains after server-cart migration. 2026-07-15: add-to-cart wrote HttpOnly `bs-cart-token`, `document.cookie` was empty, no `my-cart` cookie existed, and no price hints were readable in browser cookies.

## Store Resolution And Public URL Hardening

- [x] Storefront V2 resolves current store before downstream page/catalog work when `StoreResolution:RequireCurrentStore=true`. 2026-07-15: `StorefrontCurrentStoreMiddlewareTests` and `StorefrontCurrentStoreProviderTests` passed. Real QA: `/` called CommerceNode current-store before rendering and blocked maintenance store with HTTP 503.
- [x] Bad/missing configured store maps to `404` and does not continue the Storefront request pipeline. 2026-07-15: middleware unit test covers not-found guard stop. Real QA: Storefront with `Api:StoreKey=not-a-real-store` returned HTTP 404 `Storefront store was not found.`
- [x] CommerceNode unavailable or maintenance current store maps to `503` and does not fall back to another store. 2026-07-15: middleware/provider unit tests cover unavailable and maintenance paths. Real QA: API base unavailable returned HTTP 503 `The configured store could not be resolved.`
- [x] Static asset and health paths are skipped by current-store guard. 2026-07-15: middleware unit test covers static asset skip. Real QA: `/_framework/blazor.web.js` returned HTTP 200 while `/` was guarded.
- [x] Public URL resolver prefers `PublicUrl:BaseUrl`, normalizes trailing slash, strips query/fragment, and preserves `PathBase` for request fallback. 2026-07-15: `StorefrontV2PublicUrlResolverTests` passed. Real QA: `robots.txt` and `sitemap.xml` used configured `https://public-store.example/shop/`.
- [x] Storefront V2 configures trusted forwarded headers for `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` with known proxies/networks. 2026-07-15: `StorefrontV2ForwardedHeadersOptionsTests` passed.
- [~] Visible browser QA verifies canonical/sitemap/payment return URLs use configured public `https://` base behind a trusted proxy. 2026-07-15: Playwright verified Storefront home, guard 404/503 pages, and `robots.txt`/`sitemap.xml` using configured public `https://` base. Payment return URL end-to-end was not re-run in this pass.

## Store Lifecycle And Maintenance Page

- [x] Closed/maintenance/not-ready stores redirect normal HTML page requests to `/maintenance?reason=...` instead of rendering catalog pages. 2026-07-15: HTTP matrix verified `/` returns 302 for maintenance, closed, and provisioning/not-ready states.
- [x] Maintenance page renders the correct state-specific title and message for maintenance, closed, and not-ready stores. 2026-07-15: Playwright browser QA captured screenshots for all three states under `.gstack/qa-reports/screenshots/`.
- [x] Maintenance page includes store support email/phone from the runtime store profile. 2026-07-15: browser and HTTP checks verified `support@example.com` and `+1-555-0199` on maintenance pages.
- [x] Maintenance page remains `503` and noindex while the HTML redirect response remains `302`. 2026-07-15: QA found and fixed ISSUE-001 where service-unavailable headers changed the redirect into HTTP 503; regression test `StorefrontCurrentStoreMiddlewareRegressionTests.HtmlUnavailableRedirect_RemainsRedirectWhenResponseStarts` now covers it.
- [x] Static framework assets bypass the store readiness guard. 2026-07-15: `/_framework/blazor.web.js` returned HTTP 200 while the current store was blocked.
- [~] Browser console shows the expected navigation resource error for the intentionally-503 maintenance document; no application JavaScript/page errors were observed in the lifecycle pass.

2026-07-15 visible browser QA plan:

- [x] Run Storefront V2 with Playwright Chromium `headless=false`. Result: `headed=true` in `.gstack/qa-reports/playwright-visible-results-2026-07-15.json`.
- [x] Visit `/`, `/my-cart`, `/checkout`, `/payment-success`, and `/payment-cancel` against local CommerceNode API. Result: all returned HTTP 200 through Storefront V2.
- [x] Verify browser console has no unexpected errors after each page and cart interaction. Result: 0 console errors, 0 page errors, 0 HTTP errors captured.
- [x] Add seeded product to cart if catalog seed data is available; verify `bs-cart-token` is present and no readable `my-cart` price payload remains. Result: `Catalog QA T-Shirt` added through UI; HttpOnly token present; legacy cookie absent.
- [x] Verify payment return/cancel pages render recoverable states without requiring a side-effecting GET. Result: missing-attempt pages rendered recoverable status/action states and did not mutate via GET.
- [x] Capture screenshots and write `.gstack/qa-reports/qa-report-localhost-2026-07-15.md`.
- [~] Development browser overlay `WASM active / Count 0` is visible on every tested page and overlaps part of the mobile checkout form; recorded as dev-environment concern, not fixed in this QA pass.

## Auth And Checkout Handoff

- [x] Empty anonymous `/checkout` renders the local empty-cart state. 2026-07-14 host smoke test covers current local checkout behavior.
- [x] Refresh-token cookie name is compatible:
  - `__Host-blazorshop-refresh`
- [x] Storefront V2 calls `api/storefront/stores/{storeKey}/auth/refresh-token`. 2026-07-14: `Api:RefreshTokenRoute` default/config changed to `auth/refresh-token` under scoped base address.
- [x] Storefront V2 session resolver reads `StorefrontTokenResponse.AccessToken` from the final hardened auth contract. 2026-07-14: focused auth tests passed after removing the old nested `StorefrontAuthResponse`.
- [~] Authenticated `/checkout` redirects to client app checkout. Code path maps authenticated sessions to `/account/checkout`; browser QA only covered anonymous handoff because the external client app was not running.
- [x] Anonymous checkout can place a local Storefront V2 order when cart data is present. 2026-07-14 visible-browser QA reached checkout form and order success before auth QA.
- [x] `/signin` renders Storefront V2 local login page.
- [x] `/register` renders Storefront V2 local register page.
- [x] Login success copies Commerce Node `Set-Cookie` refresh cookie to Storefront response. 2026-07-09: covered by host smoke test with auth client stub.
- [x] Login wrong password/API failure redirects back to `/signin` with safe API message. 2026-07-09: covered by host smoke test.
- [x] Login return URL rejects unsafe absolute URLs. 2026-07-09: covered by host smoke test.
- [x] Register password mismatch is blocked before Commerce Node API call. 2026-07-09: covered by host smoke test.
- [x] Duplicate/invalid register shows safe API message. 2026-07-09: covered by host smoke test.
- [x] Register success redirects to `/signin?registered=1` and preserves safe return URL. 2026-07-09: covered by host smoke test.
- [x] Local `/logout` calls Commerce Node `api/storefront/stores/{storeKey}/auth/logout` and copies expired refresh cookie back to the browser. 2026-07-14: auth client route tests assert scoped logout path.
- [x] Authenticated account menu shows local logout and does not link customer to legacy `BlazorShop.Web`.
- [x] Browser console has no unexpected errors on `/signin`, `/register`, login, register, and logout. 2026-07-09: Playwright local-auth QA run reported 0 current console errors.
- [~] Missing Commerce Node auth endpoint degrades to anonymous and does not crash. Session resolver catches refresh failure and returns anonymous; live QA did not disable only the auth endpoint.

## Local Auth Browser QA

Use this checklist whenever Storefront V2 auth UI or Commerce Node auth API changes.

- [x] `GET /signin` renders Storefront V2 page with local form and no redirect to legacy Web. 2026-07-09: Playwright verified `http://localhost:18598/signin`.
- [x] `GET /register` renders Storefront V2 page with local form and no redirect to legacy Web. 2026-07-09: Playwright verified `http://localhost:18598/register`.
- [x] Register empty/invalid required fields stay browser-validatable and do not call Commerce Node. 2026-07-09: empty submit stayed on `/register`; invalid required fields were `FullName`, `Email`, `Password`, `ConfirmPassword`.
- [x] Register password mismatch returns safe Storefront error message. 2026-07-09: returned `Passwords do not match.`
- [ ] Register new customer succeeds against Commerce Node `api/storefront/stores/{storeKey}/auth/register`.
- [x] Duplicate register returns safe Storefront/API error message. 2026-07-09: duplicate returned `User already exists.`
- [x] Login wrong password returns safe Storefront/API error message. 2026-07-09: wrong password returned `Invalid credentials.`
- [ ] Login correct credentials succeeds against Commerce Node `api/storefront/stores/{storeKey}/auth/login`.
- [x] Login success sets `__Host-blazorshop-refresh` on Storefront response. 2026-07-09: Playwright cookie list contained `__Host-blazorshop-refresh`.
- [x] Login success redirects to safe `returnUrl` when provided. 2026-07-09: `/signin?returnUrl=/terms` redirected to `/terms`.
- [x] Unsafe absolute `returnUrl` is rejected and redirects to `/`. 2026-07-09: covered by automated Storefront V2 host smoke test.
- [x] Anonymous `/checkout` redirects to `/signin?returnUrl=/checkout`. 2026-07-09: Playwright verified final URL.
- [x] After login, account menu shows signed-in customer identity. 2026-07-09: menu showed `QA Browser Customer` and the QA email.
- [x] Logout calls Storefront local `/logout`, clears refresh cookie, and returns anonymous account menu. 2026-07-09: refresh cookie removed and menu returned `Sign in`/`Register`.
- [x] Browser console has no unexpected errors during `/signin`, `/register`, login, register, and logout. 2026-07-09: current Playwright console error list was empty.
- [x] Network/browser QA shows no navigation/request to legacy `BlazorShop.Web` or legacy `BlazorShop.API` for auth UI. 2026-07-09: browser performance entries had no suspicious legacy URL/port entries.
  - 2026-07-10: Playwright MCP visible browser registered `qa-visible-*`, verified wrong-password `Invalid credentials.`, logged in successfully, saw 0 console errors/failed requests, and saw 0 legacy requests.

2026-07-09 local-auth QA run:

- [x] Automated Storefront V2 auth tests rerun. 2026-07-09: `FullyQualifiedName~PresentationV2.Storefront` passed 23/23.
- [x] Scoped StorefrontV2 auth/checkout host tests rerun. 2026-07-14: `FullyQualifiedName~ProductRecommendationRepositoryTests|FullyQualifiedName~CartServiceTests|FullyQualifiedName~PresentationV2.Storefront` passed 43/43 after updating stale test expectations for checkout/payment foundation.
- [x] Browser QA for `/signin` rerun. Screenshot: `.gstack/qa-reports/storefrontv2-signin-2026-07-09.png`.
- [x] Browser QA for `/register` rerun. Screenshot: `.gstack/qa-reports/storefrontv2-register-2026-07-09.png`.
- [x] Browser QA for successful register/login/logout rerun. Signed-in menu screenshot: `.gstack/qa-reports/storefrontv2-signed-in-menu-2026-07-09.png`.
- [x] Browser QA console checked. 2026-07-09: no current console errors.

2026-07-10 visible Playwright MCP QA run:

- [x] StorefrontV2 Development config includes `Api:StoreKey=default` so local catalog resolves the default QA store without env overrides.
- [x] `/`, `/category/t-shirts`, filtered category URLs, `/product/catalog-qa-t-shirt`, and `/my-cart` rendered with no console errors.
- [x] Hero background uses local `/images/banner-bg.jpg`; browser cache-cleared retest had 0 failed external image requests.
- [x] Local register/login/wrong-password flow passed with no legacy Web/API requests.

## SEO And Discovery

- [x] `/robots.txt` route is covered by automated smoke test.
- [x] `/sitemap.xml` route is covered by automated smoke test.
- [x] `SeoHead` renders title.
- [x] `SeoHead` renders meta description.
- [x] `SeoHead` renders canonical URL.
- [x] `SeoHead` renders robots meta.
- [x] `SeoHead` renders OpenGraph.
- [x] `JsonLdScript` renders structured data.
  - 2026-07-09: verified `schema.org` structured data on home and product pages.
- [x] `/sitemap.xml` uses `api/storefront/stores/{storeKey}/catalog/sitemap`. 2026-07-14: API client route switched to scoped catalog sitemap.
- [x] `/robots.txt` points at V2 public sitemap URL.
- [x] Redirect middleware uses `api/storefront/stores/{storeKey}/seo/redirects/resolve`. 2026-07-14: API client route switched to scoped SEO redirects.
- [x] Redirect resolution is backed by CommerceNode current-store scope and does not resolve another store's old slug. 2026-07-15: `CommerceNodeSeoRedirectStoreScopeTests.ResolvePublicPathAsync_DoesNotResolveOtherStoreRedirect` passed.
- [x] Storefront SEO metadata composition keeps title suffix de-duplication, safe canonical URL resolution, no canonical on 404/503, and robots metadata behavior covered after hardening. 2026-07-15: focused `SeoMetadataBuilderTests`, `StorefrontSeoComposerTests`, `StorefrontRouteSeoAuditTests`, and `StorefrontOnsiteSeoRenderingTests` passed 50/50.
- [x] Storefront indexing policy centralizes private/search noindex routes and strips query/fragment/trailing-slash noise from canonical paths. 2026-07-15: `StorefrontIndexingPolicyTests`, route SEO audit, cart flow, and current-store middleware focused run passed 48/48.
- [x] Storefront sitemap generation normalizes canonical paths and excludes private/search noindex routes before absolute URL generation. 2026-07-15: `StorefrontSitemapServiceTests`, sitemap/robots QA, and discovery document focused run passed 21/21.
- [x] Storefront SEO/discovery release-gate suite passed after SEO Routing Slug Core. 2026-07-15 Phase 12: focused `StorefrontSitemapServiceTests`, `StorefrontRobots*`, `StorefrontIndexingPolicyTests`, `SeoMetadataBuilderTests`, and `StorefrontSeoComposerTests` run passed 26/26.
- [x] Missing route has no canonical and includes noindex.
- [x] Commerce Node downtime has noindex 503 surface.
- [ ] Visible browser QA: old slug in current store redirects to canonical slug.
- [ ] Visible browser QA: same old slug owned only by another store does not redirect in current store.

## Static Assets

- [x] `/css/storefront.css` returns CSS.
- [x] `/js/storefrontCommerce.js` returns JS.
- [x] Media Core Phase 0 confirms Storefront V2 proxies public media routes without changing URL shapes. 2026-07-15: reviewed `/media/products/{mediaPublicId}` and `/media/assets/{assetPublicId}/{fileName}` proxy routes in `BlazorShop.Storefront.V2/Program.cs`.
- [x] Storefront V2 media proxy currently forwards configured store key and copies `Cache-Control`, `ETag`, and `Last-Modified` from Commerce Node media responses. 2026-07-15 Phase 0 baseline.
- [x] Media Core Phase 3 verifies Storefront V2 copies `X-Content-Type-Options` from Commerce Node media responses after delivery hardening. 2026-07-15: `MediaDeliveryHardeningTests` guards the proxy header copy and Storefront V2 build passed.
- [~] Media Core Phase 5 category image rendering remains pending visible/browser QA. 2026-07-15: backend now syncs `Category.Image` to a `/media/assets/*` category-card URL; Storefront rendering should be verified after an assigned fixture exists.
- [x] Storefront root asset inventory is guarded by automated tests. 2026-07-15: `LayoutAssetFoundationTests.StorefrontRoot_DefinesExpectedAssetsWithoutDuplicates` asserts `css/site.css`, `css/storefront.css`, `_framework/blazor.web.js`, and `js/storefrontCommerce.js` exactly once.
- [x] Storefront brand head remains before `HeadOutlet` and does not use layout-level `HeadContent`. 2026-07-15: `LayoutAssetFoundationTests` and `StorefrontBrandingMarkupTests` cover the head order and no-`HeadContent` rule.
- [x] Storefront main layout keeps exactly one `data-storefront-toast-region`. 2026-07-15: `LayoutAssetFoundationTests.StorefrontLayout_KeepsSingleToastRegionAndGlobalShell` protects the cart/toast DOM contract.
- [x] Storefront `Program.cs` keeps both `UseStaticFiles()` and `MapStaticAssets()`. 2026-07-15: `LayoutAssetFoundationTests.StorefrontProgram_KeepsStaticAssetMiddleware` passed.
- [x] Storefront publish output contains required root/static assets. 2026-07-15 Layout Asset Phase 4: `dotnet publish BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj -c Release` passed and verified `wwwroot/css/site.css`, `wwwroot/css/storefront.css`, `wwwroot/js/storefrontCommerce.js`, `wwwroot/icon-192.png`, and `wwwroot/_framework/blazor.web.js`.
- [x] Storefront dynamic/error/SEO cache rules are guarded from broad immutable cache policy. 2026-07-15: `LayoutAssetFoundationTests.StorefrontRuntime_DoesNotApplyImmutableCachePolicyToDynamicPipeline` passed.
- [x] Browser QA after layout/asset changes verifies home/category/product/search/cart/checkout/auth pages have no asset 404s or unexpected console errors. 2026-07-15 Layout Asset Phase 8: Playwright headed run verified `/`, `/category/apparel`, first product link, `/search`, `/my-cart`, `/checkout`, `/signin`, and `/register`; asset/document/script/style/image/font failures and console/page errors were 0.
- [x] Storefront V2 serves required assets from `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot`, not from legacy `BlazorShop.Presentation/BlazorShop.Web/wwwroot`.
  - 2026-07-09: `/css/site.css`, `/css/storefront.css`, `/js/storefrontCommerce.js`, `/images/banner-bg.jpg`, and `/icon-192.png` returned 200 from Storefront V2.
- [x] No missing image/static asset requests on home/category/product pages.
  - 2026-07-09: QA seed image URLs were corrected from `https://example.test/*` to `/images/banner-bg.jpg`.

## Legacy Independence QA

Use this checklist whenever Storefront V2 assets, Dockerfile, project references, auth routes, or shared V2 dependencies change.

- [x] Storefront V2 has no runtime source references to legacy Presentation paths or `adminclient`.
  - 2026-07-09: `rg "BlazorShop\.Presentation[\\/]|adminclient" BlazorShop.PresentationV2 --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/node_modules/**'` returned no hits.
- [x] PresentationV2 boundary tests protect against legacy references.
  - 2026-07-09: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~ControlPlaneArchitectureBoundaryTests"` passed 6/6.
- [x] Storefront V2 focused tests pass.
  - 2026-07-09: `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~PresentationV2"` passed 34/34.
- [x] Full solution tests pass.
  - 2026-07-09: `dotnet test BlazorShop.sln --no-restore` passed 502/502 with 10 skipped. Existing package vulnerability warnings remain for `MessagePack` and `Microsoft.OpenApi`.
- [x] Storefront V2 builds without static asset conflicts.
  - 2026-07-09: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- [x] Storefront V2 Docker image builds without copying legacy `BlazorShop.Presentation/BlazorShop.Web`.
  - 2026-07-09: `docker build -f BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile -t blazorshop-storefront-v2:legacy-free .` passed.
- [~] Legacy folder rename isolation check.
  - 2026-07-09: attempted after `dotnet build-server shutdown`, but Windows denied renaming `BlazorShop.Presentation`; no temp hidden folder remains. Docker build is the current isolation proof because the Dockerfile does not copy legacy Presentation files.
- [~] Runtime page QA with Commerce API stopped.
  - 2026-07-09: `/signin`, `/register`, `/my-cart`, and static assets returned 200. `/` returned 503 because Commerce API was not running. `/product/{slug}` was not tested without seeded Commerce API data.
- [n/a] Browser screenshots for this legacy-independence pass.
  - 2026-07-09: no new screenshots captured; this phase used build/test/Docker/runtime HTTP checks.

## Security Privacy

- [x] Antiforgery token is projected for browser JSON mutations and `/api/cart/*` mutations reject missing tokens. 2026-07-16: `SecurityPrivacyPhase1CsrfTests` and `StorefrontV2HostSmokeTests` cover token projection and cart mutation behavior.
- [x] Consent banner/change/revoke hooks are rendered in Storefront V2 and backed by store-scoped Consent APIs. 2026-07-16: `SecurityPrivacyPhase3ConsentTests` and focused Storefront host smoke suite passed.
- [x] Captcha rendering hook submits optional token fields for login/register and public config exposes only provider metadata. 2026-07-16: `SecurityPrivacyPhase4CaptchaTests` and focused Storefront host smoke suite passed.
- [x] Return URL regression rejects external/protocol-relative/backslash/CR/LF values. 2026-07-16: covered by `StorefrontV2HostSmokeTests` in security/privacy focused verification.
- [x] Account/order protected Storefront flows still require customer auth. 2026-07-16: focused Storefront host smoke suite and Storefront OpenAPI contract tests passed after security/privacy changes.

## Failure States

- [x] Commerce Node down returns Storefront V2 service-unavailable behavior instead of legacy fallback.
- [~] Empty catalog shows empty state and does not crash. Current QA database intentionally contains seeded catalog data; empty DB visual state remains a dataset-specific follow-up.
- [x] Missing category slug returns 404/noindex.
- [x] Missing product slug returns 404/noindex.
- [x] Invalid cart cookie is ignored or reset safely.
- [x] Commerce Node returns `success=false` and Storefront shows safe error state. 2026-07-09: Commerce Node downtime/failed catalog smoke returned Storefront service-unavailable behavior without legacy fallback or crash.

## Cutover Readiness

- [x] Storefront V2 project exists in solution.
- [x] Storefront V2 builds.
- [x] Storefront V2 can run while legacy Storefront remains untouched.
- [x] Storefront V2 default API base URL points to Commerce Node local API.
- [x] Legacy fallback is disabled by default.
- [x] Browser QA passes for home/category/product.
- [x] Browser QA passes for cart.
- [x] Browser QA passes for checkout handoff.
- [x] SEO/discovery browser QA passes.
- [n/a] Reverse proxy/deployment route for V2 is defined. Out of scope for local MVP independence QA.

## Suggested Extra QA

- [~] Verify V2 and legacy Storefront can run side by side on different ports. 2026-07-09: V2 was verified independently with legacy API/Storefront not running; explicit side-by-side runtime remains a cutover rehearsal item.
- [x] Verify V2 does not require `BlazorShop.Presentation/BlazorShop.API` process.
- [x] Verify `Api:EnableLegacyFallback=true` only for emergency rollback and never in default config.
- [x] Verify production config requires explicit `Api:BaseUrl`. 2026-07-09: `StorefrontOptionsValidators` enforce `Api:BaseUrl` outside Development when service discovery is absent.
- [x] Verify removed Commerce Node `api/internal/*` is not a Storefront V2 dependency. 2026-07-14: client tests guard against `/api/internal` requests.

## Checkout And Payment Foundation

- [x] Storefront cart/checkout service tests protect converted currency snapshot flow. 2026-07-15: focused `StorefrontCartServiceTests` and `StorefrontCheckoutServiceTests` passed.
- [ ] Visible Storefront V2 QA creates a non-base currency cart, places checkout, and confirms displayed cart total, checkout total, order total, and payment amount match.
- [ ] Visible Control Plane order drawer QA confirms converted order shows charged total plus base total/rate/provider/source snapshot.
- [x] Storefront V2 builds after checkout/payment foundation changes. 2026-07-13: `dotnet build BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj --no-restore` passed.
- [x] Storefront V2 shared order item model uses public `AmountPaid` spelling. 2026-07-14: SharedV2 `GetOrderItem` was updated to match Storefront API final hardening; public OpenAPI rejects `amountPayed`.
- [x] `/checkout` renders local Storefront V2 checkout page. 2026-07-13: Playwright MCP visible browser rendered the local checkout form.
- [x] `/checkout` does not redirect to `/account/checkout`. 2026-07-13: visible browser stayed on `/checkout` and submitted locally.
- [x] Empty cart checkout shows empty state. 2026-07-13: `/checkout` with no cart showed `Your cart is empty`.
- [x] Checkout page loads enabled payment methods through scoped Storefront API. 2026-07-14: payment methods route switched to `payments/methods` under scoped base; live browser recheck pending.
- [x] COD is selected/available in MVP. 2026-07-13: COD radio was checked by default.
- [ ] Required contact/shipping validation blocks submit.
- [x] COD checkout succeeds with visible order reference. 2026-07-13: visible browser created `ORD-20260713-6672B965` and displayed the confirmation page.
- [x] Cart cookie is cleared after checkout success. 2026-07-13: browser `document.cookie` returned empty after successful checkout.
- [ ] Browser network shows no legacy API/Web requests.
- [ ] Checkout page is noindex/private and absent from sitemap.

## Catalog Structure Core

- [x] Category page renders category `Description` before falling back to `MetaDescription`. 2026-07-16: `CategoryPage.razor` updated and focused build/test compilation passed.
- [x] Storefront API client can deserialize nullable category `description` from Storefront scoped catalog responses. 2026-07-16: SharedV2 category model updated and Storefront OpenAPI contract passed.
- [x] Category page uses Storefront API breadcrumbs instead of rebuilding only Home/current locally. 2026-07-16: `CategoryPage.razor` consumes `GetCategoryPage.Breadcrumbs`; application service breadcrumb test passed.
- [x] Category page product count uses direct visible product count from CommerceNode response. 2026-07-16: `DirectProductCount` drives the item chip; focused Storefront/API tests passed.
- [x] Search category filter preserves descendant behavior by sending `includeSubcategories=true`. 2026-07-16: `StorefrontV2ApiClientTests.GetPublishedCatalogPageAsync_WhenIncludeSubcategoriesProvided_AddsQueryFlag` passed.
- [x] Future scheduled products are hidden from Storefront catalog/detail/sitemap responses. 2026-07-16: CommerceNode public repository tests passed; Storefront V2 consumes only scoped public API responses.
- [x] Expired products are hidden from Storefront catalog/detail/sitemap responses. 2026-07-16: CommerceNode public repository tests passed; Storefront V2 consumes only scoped public API responses.
- [x] Storefront cart rejects unavailable products before mutation. 2026-07-16: `StorefrontCartServiceTests.AddLineAsync_RejectsScheduledProduct` passed.
- [x] Product structured data exposes safe identity fields only. 2026-07-16 Phase 5: `StorefrontStructuredDataComposerTests.ComposeProductPageAsync_AddsSafeProductIdentifiers` passed for SKU/GTIN/MPN/itemCondition and asserts dimensions are omitted.
- [x] Storefront public schema guardrails still pass after product identity fields. 2026-07-16 Phase 5: `CommerceNodeStorefrontOpenApiContractTests` passed 23/23.
- [x] Storefront cart rejects invalid variant/selected-attribute combinations and keeps custom attributes normalized. 2026-07-16 Phase 6: focused `StorefrontCartServiceTests` run passed.
- [x] Storefront checkout snapshots selected attributes into order lines. 2026-07-16 Phase 6: focused `StorefrontCheckoutServiceTests` run passed.
- [x] Storefront rendering/SEO/sitemap/cache alignment remains stable after catalog structure phases. 2026-07-16 Phase 8: Storefront V2 build passed; `StorefrontV2ApiClientTests|StorefrontStructuredDataComposerTests` passed 16/16; `StorefrontSitemapServiceTests` passed 1/1; `StorefrontV2HostSmokeTests` passed 34/34; `StorefrontCartServiceTests|StorefrontCheckoutServiceTests` passed 26/26.
- [x] Catalog Structure Core final Storefront focused release gate passed. 2026-07-16 Phase 10: `StorefrontV2ApiClientTests|StorefrontStructuredDataComposerTests|StorefrontCartServiceTests|StorefrontCheckoutServiceTests|StorefrontSitemapServiceTests` passed 43/43.
- [ ] Visible browser QA confirms `/category/{slug}` displays category description from CommerceNode data.
- [ ] Visible browser QA confirms `/category/{slug}` shows full breadcrumb hierarchy and direct product count.

## Product Variant Attribute

- [x] Phase 0 baseline confirms Storefront cart and product detail variation-template guardrails before UI changes. 2026-07-16: `StorefrontCartServiceTests|PublicCatalogServiceTests|CommerceNodeStorefrontOpenApiContractTests` included in focused run passed 48/48.
- [x] Product detail service mapping returns active variation options and values only. 2026-07-16 Phase 0: `PublicCatalogServiceTests.GetPublishedProductBySlugAsync_MapsActiveVariationTemplateOptionsAndValues` passed.
- [x] Product detail API contract exposes option control type, required state, and value color hex metadata. 2026-07-16 Phase 1: `CommerceNodeStorefrontOpenApiContractTests` passed after snapshot update.
- [x] Product detail renders dropdown controls from option metadata. 2026-07-16 Phase 7: `StorefrontBrandingMarkupTests.ProductPage_UsesBackendSelectionPreviewForVariantAttributes` and `StorefrontV2HostSmokeTests` passed.
- [x] Product detail renders radio controls from option metadata. 2026-07-16 Phase 7: ProductPage static markup/host smoke coverage passed; visible browser QA remains pending.
- [x] Product detail renders color swatches from option metadata. 2026-07-16 Phase 7: ProductPage static markup/host smoke coverage passed; visible browser QA remains pending.
- [x] Product detail preview updates price/SKU/stock after selection. 2026-07-16 Phase 7: Storefront local preview route, JS markers, API client, and resolver tests passed in focused release gate.
- [x] Product detail preview blocks invalid selection. 2026-07-16 Phase 7: `ProductSelectionResolverTests`, `StorefrontCartServiceTests`, and preview controller tests passed.
- [x] Product detail add-to-cart sends the same selected attributes used by preview. 2026-07-16 Phase 7: static JS guard plus cart/checkout selected-attribute tests passed.
- [x] Inactive variants are not selectable. 2026-07-16 Phase 2: Storefront product detail filters inactive variants and cart rejects inactive selected variants; focused tests passed.
- [x] Product detail remains usable before JavaScript loads. 2026-07-16 Phase 7: `StorefrontV2HostSmokeTests` passed 34/34 and product page still renders SSR fallback product data before JS preview updates.
- [~] Visible browser QA for Product Variant Attribute remains pending. 2026-07-16 Phase 7: local V2 ports did not respond during automated release gate and no root `run-v2-local.ps1` was present; automated host/static/API coverage passed.

## Availability Quantity

- [x] Phase 0 baseline confirms current product card/detail quantity and stock states before sellability changes. 2026-07-16: source review plus active V2 focused backend/contract run passed 67/67.
- [x] Product card direct add-to-cart is shown only for no-variant in-stock products. 2026-07-16 Phase 0: `ProductCard.razor` baseline confirmed.
- [x] Product card preserves product detail link for products with variants. 2026-07-16 Phase 0: `ProductCard.razor` baseline confirmed.
- [x] Product detail quantity input currently defaults to 1 with minimum 1. 2026-07-16 Phase 0: `ProductPage.razor` baseline confirmed.
- [x] Product detail uses product quantity and variant stock markers for existing add-to-cart behavior. 2026-07-16 Phase 0: `ProductPage.razor` baseline confirmed.
- [x] Storefront V2 cart endpoint rejects quantity below 1. 2026-07-16 Phase 0: local endpoint source and `StorefrontCartServiceTests` baseline confirmed.
- [x] Phase 1 does not change Storefront V2 sellability or rendering behavior. 2026-07-16: purchase fields are persisted/contracted but no Storefront V2 UI or cart resolver behavior was changed.
- [x] Storefront cart service remains green after centralized sellability resolver. 2026-07-16 Phase 2: `StorefrontCartServiceTests` passed inside focused 36/36 run.
- [x] Storefront V2 client models can deserialize catalog/detail sellability metadata. 2026-07-16 Phase 3: SharedV2 product models include purchasable, purchase block reasons, stock status, available quantity, min/max/step, manage-stock, shipping, and delivery fields; `StorefrontV2ApiClientTests` passed inside focused 41/41 run.
- [x] Storefront cart and checkout backend flows enforce the same sellability decisions that the API projection exposes. 2026-07-16 Phase 4: focused cart/checkout/selection run passed 56/56.
- [ ] Storefront V2 host smoke remains green before sellability UI changes.
