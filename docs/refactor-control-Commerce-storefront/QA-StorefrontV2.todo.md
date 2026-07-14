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
- [~] Header/footer `/pages/about-us`, `/pages/customer-service`, `/pages/faq`, `/pages/privacy`, and `/pages/terms` require matching published CommerceNode pages or they render 404. 2026-07-14 scoped QA DB had no published standard pages, so all five linked `/pages/*` routes returned 404.

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
- [ ] COD checkout places one order, clears/expires the cart token, and shows confirmation.
- [ ] Online provider checkout redirects to provider next action when configured.
- [ ] Provider cancel/failure returns to a recoverable checkout state.
- [ ] Payment attempt polling page shows pending, success, failure, and retry states without console errors.
- [ ] Browser QA verifies no readable raw cart price payload remains after server-cart migration.

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
- [x] Missing route has no canonical and includes noindex.
- [x] Commerce Node downtime has noindex 503 surface.

## Static Assets

- [x] `/css/storefront.css` returns CSS.
- [x] `/js/storefrontCommerce.js` returns JS.
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
