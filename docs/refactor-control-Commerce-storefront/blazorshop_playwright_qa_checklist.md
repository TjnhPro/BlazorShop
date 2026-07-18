# BlazorShop Storefront V2 — Playwright QA Checklist

Tài liệu này định nghĩa bộ kiểm thử end-to-end theo hành vi người dùng cho Storefront V2. Mục tiêu là phát hiện lỗi nghiệp vụ trước release, tạo regression suite cho CI và cung cấp một production smoke suite an toàn.

## 1. Phạm vi và nguồn đối chiếu

Checklist được xây dựng từ các route, controller và contract hiện có trong codebase:

- Public SSR: home, search, category, product, dynamic page, navigation, SEO, robots và sitemap.
- Customer account: register, login, logout, refresh session, confirm email, forgot/reset/change password, profile, address và order self-service.
- Commerce WASM: cart, account và checkout.
- Commerce core: product selection, variants, availability, cart session, shipping, COD, order placement và guest order lookup.
- Store core: store resolution, maintenance, branding, currency, consent và media.

Không tính vào acceptance gate của core trong phase này:

- Tax calculation và hiển thị tax.
- Stripe/PayPal hoặc provider thanh toán online.
- Theme engine và localization nhiều ngôn ngữ.
- Wishlist, compare, review và các optional business module.

Yêu cầu riêng của phase: cart và checkout **không hiển thị dòng Tax**, không gọi tax UI flow và tổng tiền phải theo công thức:

`Grand total = Subtotal + Shipping - Discount`

## 2. Mức ưu tiên và tag

| Ký hiệu | Ý nghĩa | Khi chạy |
|---|---|---|
| P0 | Luồng bán hàng hoặc bảo mật bắt buộc | Mọi PR, pre-release, production smoke phù hợp |
| P1 | Regression nghiệp vụ chính | PR liên quan và nightly |
| P2 | Edge case, resilience, responsive | Nightly hoặc trước release lớn |
| `@production-safe` | Không phá dữ liệu thật hoặc chỉ tạo dữ liệu synthetic được phép | Sau deploy production |
| `@staging-only` | Có thay đổi dữ liệu, gây lỗi có chủ ý, rate limit hoặc concurrency | Chỉ staging/test |
| `@email` | Cần test inbox hoặc email capture | Staging có mail sandbox |
| `@destructive` | Xóa, archive, đổi trạng thái hoặc tạo tải lớn | Không chạy production |
| `@mobile` | Viewport điện thoại | Chromium/WebKit mobile profile |
| `@a11y` | Accessibility assertion | Nightly/pre-release |

## 3. Dữ liệu chuẩn bị

Không dùng một sản phẩm duy nhất cho mọi testcase. Seed tối thiểu các fixture sau để mỗi rule có thể được kiểm tra độc lập.

### 3.1 Store

| Mã | Dữ liệu |
|---|---|
| S1 | Store đang active, domain chính hợp lệ, maintenance tắt |
| S2 | Store thứ hai để kiểm tra cách ly dữ liệu |
| S3 | Store maintenance bật và có maintenance message |
| S4 | Store disabled hoặc archived |
| S5 | Host/domain không được map vào store nào |

### 3.2 Product và catalog

| Mã | Dữ liệu |
|---|---|
| P1 | Simple product, published, managed stock = 20, giá 100 |
| P2 | Simple product, managed stock = 0, hide-out-of-stock = false |
| P3 | Simple product, unmanaged stock, quantity = 0 nhưng vẫn bán được |
| P4 | Product có min quantity = 2, max = 10, step = 2 |
| P5 | Variant product có ít nhất hai variant active, giá và stock khác nhau |
| P6 | Variant product có một variant inactive và một variant out-of-stock |
| P7 | Custom variation product với các required option |
| P8 | Product unpublished/archived |
| P9 | Product chưa tới `AvailableStartUtc` |
| P10 | Product đã qua `AvailableEndUtc` |
| P11 | Physical product có shipping surcharge |
| P12 | Product không yêu cầu shipping |
| P13 | Product có compare price, SEO metadata và nhiều media |
| P14 | Product thuộc category con; category cha phải tìm thấy nó |
| P15 | Product có tên chứa ký tự Unicode và ký tự HTML cần escape |

### 3.3 Account

| Mã | Dữ liệu |
|---|---|
| A1 | Customer đã confirm email, có profile, hai address và order history |
| A2 | Customer chưa confirm email |
| A3 | Customer bị lock hoặc disabled nếu policy hỗ trợ |
| A4 | Email chưa đăng ký |
| A5 | Customer thuộc S2 để kiểm tra cross-store isolation |

### 3.4 Page/navigation/SEO

| Mã | Dữ liệu |
|---|---|
| C1 | Page published, include in sitemap, có meta/canonical |
| C2 | Page published nhưng không include in sitemap |
| C3 | Page archived/unpublished |
| C4 | Page đã đổi slug và có slug history/redirect |
| C5 | Main menu có link page, category, product, external và group |
| C6 | Footer menu có legal page |

### 3.5 Shipping và checkout

| Mã | Dữ liệu |
|---|---|
| H1 | Country được giao hàng, flat rate đã cấu hình |
| H2 | Country không được giao hàng |
| H3 | Free-shipping threshold có thể đạt bằng P1/P11 |
| H4 | COD được enable và là payment method của core |
| H5 | Không có payment method khả dụng để kiểm tra failure state |

## 4. Quy ước triển khai Playwright

### 4.1 Project đề xuất

```text
tests/e2e/
├── fixtures/
│   ├── storefront.fixture.ts
│   ├── auth.fixture.ts
│   ├── catalog.fixture.ts
│   └── mail.fixture.ts
├── pages/
│   ├── home.page.ts
│   ├── product.page.ts
│   ├── cart.page.ts
│   ├── checkout.page.ts
│   └── account.page.ts
├── specs/
│   ├── 00-runtime.spec.ts
│   ├── 01-store.spec.ts
│   ├── 02-public-content.spec.ts
│   ├── 03-catalog.spec.ts
│   ├── 04-account-auth.spec.ts
│   ├── 05-account-profile.spec.ts
│   ├── 06-cart.spec.ts
│   ├── 07-checkout.spec.ts
│   ├── 08-orders.spec.ts
│   ├── 09-privacy.spec.ts
│   ├── 10-resilience.spec.ts
│   └── 99-production-smoke.spec.ts
└── playwright.config.ts
```

### 4.2 Selector policy

Ưu tiên selector theo thứ tự:

1. `getByRole()` với accessible name.
2. `getByLabel()` cho input/select.
3. `getByText()` cho message ổn định.
4. `data-testid` cho vùng động khó định danh.

Không dùng CSS class Tailwind làm selector. Các vùng nên có test id ổn định:

- `storefront-shell`, `storefront-header`, `main-navigation`.
- `wasm-ready`, `cart-badge`, `cart-line`, `cart-summary`.
- `checkout-address-step`, `shipping-options`, `payment-methods`, `order-review`.
- `account-menu`, `order-list`, `consent-banner`.

### 4.3 Assertion chung cho mọi test

- Không có `pageerror` hoặc console error ngoài allowlist rõ ràng.
- Không có request Storefront bất ngờ trả 5xx.
- Không có request từ Storefront đi thẳng vào Commerce Admin/Internal API.
- Không lộ access token, refresh token, node credential hoặc secret trong URL/DOM/log.
- Trang private có `noindex,nofollow` và không nằm trong sitemap.
- UI không hiển thị tax hoặc `Tax 0.00` trong cart/checkout/order.
- Trace, screenshot và video chỉ lưu khi failure/retry để tránh phình artifact.

## 5. Runtime, Store và Store Resolution

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| RUN-001 | P0 | Mở `/` trên domain S1 | HTTP 200, đúng tên/logo S1, không có console error |
| RUN-002 | P0 | Reload cứng sau khi WASM đã boot | Route hiện tại giữ nguyên, WASM boot lại một lần, không duplicate handler |
| RUN-003 | P0 | Điều hướng SSR → cart WASM → account WASM → SSR product | Không mất store context; back/forward hoạt động |
| RUN-004 | P1 | Mở trực tiếp URL sâu `/my-cart` trong tab mới | SSR shell và WASM island đều render; không trắng trang |
| RUN-005 | P1 | Mở trực tiếp URL sâu `/account/orders` khi đã đăng nhập | Khôi phục session và render đúng account route |
| RUN-006 | P0 | Domain S2 | Không hiển thị branding/catalog/page/cart của S1 |
| RUN-007 | P0 | Host không được map | Bị từ chối/404 theo policy; tuyệt đối không fallback về S1 |
| RUN-008 | P0 | Store S3 đang maintenance | Public page chuyển maintenance; message đúng store |
| RUN-009 | P1 | Truy cập cart/account khi maintenance | Hành vi đúng policy; không lộ dữ liệu hoặc loop redirect |
| RUN-010 | P1 | Store S4 disabled/archived | Không bán hàng và không trả catalog của store khác |
| RUN-011 | P1 | CommerceNode tạm unavailable | Hiển thị service unavailable/retry, không biến thành false 404 |
| RUN-012 | P2 | API phản hồi chậm 3–5 giây | Có loading state, không double submit, UI không jump bất thường |
| RUN-013 | P0 | Kiểm tra network route của Storefront | Chỉ dùng `api/storefront/stores/{storeKey}/*` hoặc same-origin gateway |
| RUN-014 | P1 | Đổi store/domain trong tab khác | Token/cart của S1 không được dùng cho S2 |

## 6. Home, Navigation, Branding và Dynamic Page

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| PUB-001 | P0 | Home có page metadata slug `home` | Title/meta/canonical lấy từ store/page đúng |
| PUB-002 | P1 | Không có page metadata `home` | Home dùng metadata fallback, vẫn HTTP 200 |
| PUB-003 | P0 | Header desktop | Logo, search, main menu, account và cart không overlap |
| PUB-004 | P0 | Header mobile | Menu mở/đóng được, focus hợp lý, không clipped text |
| PUB-005 | P1 | Main navigation C5 | Mỗi item page/category/product/external đi đúng đích |
| PUB-006 | P1 | Group menu không có URL | Chỉ mở nhóm, không điều hướng sai |
| PUB-007 | P1 | Menu API unavailable | Header/footer fallback an toàn, trang vẫn dùng được |
| PUB-008 | P1 | Footer C6 | Legal links đúng, contact row trống được ẩn |
| PUB-009 | P0 | Mở `/pages/{slug}` của C1 | Render title/body an toàn, canonical đúng, HTTP 200 |
| PUB-010 | P1 | Page chứa external HTTPS link | Link render hợp lệ; target/rel đúng policy |
| PUB-011 | P0 | Page C3 archived/unpublished | HTTP 404 và không lộ nội dung cũ |
| PUB-012 | P0 | Page không tồn tại | Branded 404; không trả 200 giả |
| PUB-013 | P1 | Page C4 bằng old slug | Redirect một lần tới canonical mới, không loop |
| PUB-014 | P0 | Old slug chỉ thuộc S2 | S1 không redirect và không lộ target S2 |
| PUB-015 | P1 | Nội dung có script/event-handler nguy hiểm | Không được execute; nội dung bị reject hoặc sanitize |
| PUB-016 | P1 | Nội dung có ảnh `/media/...` | Ảnh đúng store, alt text hợp lệ, không mixed content |
| PUB-017 | P2 | Brand fields thiếu một phần | Dùng fallback đẹp, không hiện `null`/broken image |
| PUB-018 | P2 | Unicode trong brand/page title | Render và URL encode đúng |

## 7. SEO, Redirect, Robots và Sitemap

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| SEO-001 | P0 | Home canonical | Absolute HTTPS URL đúng domain hiện tại, không localhost/internal host |
| SEO-002 | P0 | Product P13 | Có title, description, canonical, OG và Product JSON-LD hợp lệ |
| SEO-003 | P0 | Category | Canonical đúng slug và breadcrumb structured data hợp lệ |
| SEO-004 | P1 | Search có query/filter/page | `noindex,follow`; canonical/query policy đúng |
| SEO-005 | P1 | Cart/checkout/account/auth | `noindex,nofollow` |
| SEO-006 | P0 | `/robots.txt` | Content type text/plain; sitemap URL đúng domain |
| SEO-007 | P0 | `/sitemap.xml` | XML hợp lệ, absolute HTTPS URL, không duplicate URL |
| SEO-008 | P0 | Sitemap chứa C1 | C1 xuất hiện đúng canonical |
| SEO-009 | P0 | Sitemap với C2/C3 | C2 và C3 không xuất hiện |
| SEO-010 | P0 | Sitemap với P8/P9/P10 | Product không purchasable/published theo policy không bị index sai |
| SEO-011 | P1 | Sitemap không chứa private routes | Không có signin/register/account/cart/checkout/search |
| SEO-012 | P1 | Redirect C4 | Status 301/308 đúng policy; query string an toàn được giữ nếu cần |
| SEO-013 | P1 | Redirect loop/chain seed | Không có loop; chain được rút gọn hoặc chặn |
| SEO-014 | P1 | Canonical khi có tracking query | Loại bỏ query không canonical |
| SEO-015 | P1 | 404 page | Không index và không có Product/Page JSON-LD lỗi |
| SEO-016 | P2 | Structured data ảnh media | URL ảnh public đúng host và truy cập được |

## 8. Catalog, Search và Category

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| CAT-001 | P0 | Search rỗng | Trả published products theo browse policy |
| CAT-002 | P0 | Search đúng title P1 | P1 xuất hiện |
| CAT-003 | P1 | Search chỉ trùng SKU/description | Không match nếu policy chỉ tìm title |
| CAT-004 | P1 | Search Unicode/ký tự đặc biệt | Không 500, encode đúng, kết quả ổn định |
| CAT-005 | P1 | Search category cha | Bao gồm P14 trong category con |
| CAT-006 | P1 | Category slug không tồn tại | Empty/not found state rõ ràng, không 500 |
| CAT-007 | P0 | Mở category con | Title, description, breadcrumb và direct count đúng |
| CAT-008 | P1 | Sort từng option | Thứ tự đúng và giữ filter hiện tại |
| CAT-009 | P1 | Filter min price | Không có item thấp hơn min |
| CAT-010 | P1 | Filter max price | Không có item cao hơn max |
| CAT-011 | P1 | min > max | Validation/empty state; không 500 |
| CAT-012 | P1 | Filter in-stock | Loại P2; không loại P3 nếu unmanaged stock |
| CAT-013 | P0 | Pagination next/previous | Đúng item, URL query được cập nhật |
| CAT-014 | P1 | Page vượt max backend | Backend clamp hoặc empty state theo contract, không 500 |
| CAT-015 | P1 | Page size hợp lệ | Số card đúng và URL giữ state |
| CAT-016 | P1 | Page size không hợp lệ/lớn | Backend clamp theo giới hạn |
| CAT-017 | P1 | Refresh URL có filter/sort/page | State UI được khôi phục từ URL |
| CAT-018 | P1 | Back từ product về listing | Filter/sort/page không mất |
| CAT-019 | P1 | Search suggestion | Suggestion đúng store, chọn suggestion điều hướng đúng |
| CAT-020 | P2 | Gõ search nhanh | Request cũ không ghi đè kết quả request mới |
| CAT-021 | P1 | P8/P9/P10 | Không xuất hiện trong listing/search không phù hợp |
| CAT-022 | P1 | Product card P13 | Ảnh, compare price, currency và link đúng |
| CAT-023 | P2 | Product card thiếu ảnh | Placeholder ổn định, không broken image |
| CAT-024 | P0 | Catalog S1/S2 có slug giống nhau | Mỗi domain chỉ thấy dữ liệu store tương ứng |

## 9. Product Detail và Product Selection

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| PRD-001 | P0 | Mở P1 | Tên, giá, stock, mô tả và add-to-cart đúng |
| PRD-002 | P0 | P2 out-of-stock | Nút mua disabled và có message an toàn |
| PRD-003 | P0 | P3 unmanaged stock | Vẫn cho add-to-cart dù quantity = 0 |
| PRD-004 | P0 | P4 quantity min/step | Input min=2, step=2 và default hợp lệ |
| PRD-005 | P1 | P4 quantity dưới min | Client chặn và server reject nếu bypass |
| PRD-006 | P1 | P4 quantity trên max | Client chặn và server reject nếu bypass |
| PRD-007 | P1 | P4 quantity sai step | Không add, hiện validation rõ ràng |
| PRD-008 | P0 | P5 đổi variant | Giá, SKU và stock cập nhật theo selection preview |
| PRD-009 | P0 | P5 add variant | Cart line lưu đúng variant/attribute |
| PRD-010 | P0 | P6 inactive variant | Không thể chọn/mua |
| PRD-011 | P0 | P6 out-of-stock variant | Không thể mua; variant khác vẫn hoạt động |
| PRD-012 | P0 | P7 thiếu required option | Nút mua chặn và message chỉ rõ option thiếu |
| PRD-013 | P0 | P7 chọn đầy đủ option | Selection preview thành công, cart lưu attributes |
| PRD-014 | P1 | Custom selection bị sửa qua DevTools | Server validate lại, không tin browser |
| PRD-015 | P1 | Gửi hơn 5 selected attributes | Server reject validation |
| PRD-016 | P0 | P8/P9/P10 truy cập trực tiếp | 404 hoặc unavailable đúng policy; không mua được |
| PRD-017 | P1 | Giá/stock thay đổi lúc đang mở trang | Khi add, server dùng giá/stock hiện tại và báo thay đổi nếu cần |
| PRD-018 | P1 | Double click Add to Cart | Không tạo duplicate ngoài rule merge; nút có pending state |
| PRD-019 | P1 | Add-to-cart request timeout | Cho retry an toàn, không cộng hai lần không rõ trạng thái |
| PRD-020 | P1 | P13 media gallery | Primary image đúng, media khác load, alt text hợp lệ |
| PRD-021 | P1 | Media URL sai canonical filename | Redirect/canonical behavior đúng |
| PRD-022 | P1 | Product name P15 | Text được escape, không execute HTML |
| PRD-023 | P2 | Mobile product detail | Selection, quantity và CTA dùng được không overlap |

## 10. Account — Registration, Login và Session

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| AUTH-001 | P0 | Registration policy = standard | `/register` hiển thị form |
| AUTH-002 | P0 | Registration disabled | Form không cho submit; API trả forbidden đúng message |
| AUTH-003 | P0 | Đăng ký dữ liệu hợp lệ | Tạo account một lần; hiện hướng dẫn confirm/login phù hợp |
| AUTH-004 | P0 | Password không đủ uppercase/lowercase/digit/special/8 chars | Client và server cùng reject đúng rule |
| AUTH-005 | P0 | Confirm password khác | Không submit hoặc server reject `Passwords do not match` |
| AUTH-006 | P1 | Email sai format/trim/case | Sai format bị chặn; trim/case normalize đúng |
| AUTH-007 | P0 | Email đã tồn tại | Không tạo duplicate; message không lộ dữ liệu nhạy cảm |
| AUTH-008 | P1 | Full name trống/quá dài | Validation ổn định, không 500 |
| AUTH-009 | P1 | Captcha enabled và token thiếu/sai | Không đăng ký; message recoverable |
| AUTH-010 | P1 | Bấm submit hai lần | Chỉ tạo một account |
| AUTH-011 | P0 | Confirm email link hợp lệ | Account được confirm và chuyển tới kết quả/login an toàn |
| AUTH-012 | P1 | Confirm email token sai/hỏng | Không confirm; message an toàn |
| AUTH-013 | P1 | Confirm email link dùng lại | Idempotent hoặc message đã confirm; không 500 |
| AUTH-014 | P1 | Confirm email của S2 trên S1 | Không tạo session/identity cross-store |
| AUTH-015 | P0 | Login A1 đúng | Vào return URL an toàn, account menu hiển thị đúng |
| AUTH-016 | P0 | Login sai password | Không login; thông báo generic |
| AUTH-017 | P0 | Login email không tồn tại | Message/timing gần tương đương sai password |
| AUTH-018 | P1 | Login A2 chưa confirm | Hành vi theo policy, không bypass email confirmation |
| AUTH-019 | P1 | Login A3 lock/disabled | Bị từ chối với message an toàn |
| AUTH-020 | P1 | Captcha login sai | Không login |
| AUTH-021 | P1 | Nhiều login sai | Rate limit/lockout đúng policy; không 500 |
| AUTH-022 | P0 | ReturnUrl nội bộ hợp lệ | Sau login quay về product/cart/checkout đã yêu cầu |
| AUTH-023 | P0 | ReturnUrl external/protocol-relative | Bị bỏ qua; không open redirect |
| AUTH-024 | P0 | Reload sau login | Session được khôi phục qua refresh flow |
| AUTH-025 | P1 | Access token hết hạn | Refresh cookie đổi token và request được retry một lần |
| AUTH-026 | P0 | Refresh token sai/hết hạn | Xóa session/cookie và yêu cầu login lại |
| AUTH-027 | P0 | Logout | Session bị revoke, account route không còn truy cập |
| AUTH-028 | P1 | Back browser sau logout | Không xem lại dữ liệu account từ cache |
| AUTH-029 | P1 | Logout ở tab A, dùng tab B | Tab B bị logout ở request kế tiếp |
| AUTH-030 | P0 | Cookie security | Refresh cookie HttpOnly/Secure/SameSite/path đúng policy |

## 11. Account Recovery và Change Password

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| REC-001 | P0 | Mở forgot-password | Form có email, captcha khi enable, noindex |
| REC-002 | P0 | Forgot password với A1 | Hiện generic success; một email reset được queue |
| REC-003 | P0 | Forgot password với A4 | Response UI giống A1; không lộ email tồn tại hay không |
| REC-004 | P1 | Email sai format/trống | Validation, không gọi flow reset |
| REC-005 | P1 | Forgot password nhiều lần | Rate limit; token trước xử lý theo policy |
| REC-006 | P1 | Captcha recovery sai | Không queue email |
| REC-007 | P0 | Link reset hợp lệ từ test inbox | Mở đúng store/reset page, token không bị log ra console |
| REC-008 | P0 | Reset bằng password mạnh và confirm đúng | Thành công; chuyển về login hoặc success state |
| REC-009 | P0 | Login bằng password mới | Thành công |
| REC-010 | P0 | Login bằng password cũ sau reset | Thất bại |
| REC-011 | P0 | Reset password yếu | Reject theo đầy đủ strong-password rules |
| REC-012 | P0 | Confirm password không khớp | Reject, token chưa bị consume nếu request không hợp lệ |
| REC-013 | P1 | Token reset sai/malformed | Không đổi password; generic invalid-token message |
| REC-014 | P1 | Token reset hết hạn | Không đổi password; cho phép yêu cầu link mới |
| REC-015 | P1 | Token reset dùng lại | Không đổi password lần hai |
| REC-016 | P1 | Token/email không khớp | Không đổi password |
| REC-017 | P1 | Refresh/reload reset page | Không tự submit và không lộ token trong analytics/referrer ngoài site |
| REC-018 | P1 | Reset link của S2 mở trên S1 | Không reset cross-store |
| REC-019 | P0 | Change password với current password đúng | Thành công và có success feedback |
| REC-020 | P0 | Current password sai | Không đổi password |
| REC-021 | P0 | New/confirm mismatch | Không đổi password |
| REC-022 | P0 | New password yếu | Reject theo server rules |
| REC-023 | P1 | New password giống current | Hành vi đúng policy và message rõ ràng |
| REC-024 | P0 | Sau change password, password mới login được | Thành công |
| REC-025 | P0 | Sau change password, password cũ thất bại | Thành công về mặt bảo mật |
| REC-026 | P1 | Session hiện tại/session tab khác sau change | Revoke hoặc giữ theo policy được kiểm chứng rõ |

## 12. Account Profile và Addresses

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| ACC-001 | P0 | Anonymous mở `/account` | Redirect `/signin` với safe return URL |
| ACC-002 | P0 | A1 mở profile | Dữ liệu đúng customer/store, không lộ internal id |
| ACC-003 | P0 | Update full name/first/last/company/phone | Lưu và reload vẫn đúng |
| ACC-004 | P1 | Update email | Hành vi confirm lại email đúng policy |
| ACC-005 | P1 | Preferred currency hợp lệ | Storefront/cart hiển thị currency phù hợp |
| ACC-006 | P1 | Preferred currency không hỗ trợ | Reject/fallback có cảnh báo |
| ACC-007 | P1 | Field quá dài/HTML payload | Validation/escape, không XSS |
| ACC-008 | P0 | Tạo address hợp lệ | Address xuất hiện sau reload |
| ACC-009 | P0 | Update address | Dữ liệu mới hiển thị ở account và checkout |
| ACC-010 | P0 | Delete non-default address | Xóa đúng address |
| ACC-011 | P1 | Delete default address | Hành vi rõ ràng; default được chuyển hoặc bỏ theo policy |
| ACC-012 | P0 | Set default shipping | Chỉ một default shipping và checkout preselect đúng |
| ACC-013 | P0 | Set default billing | Chỉ một default billing |
| ACC-014 | P1 | Tạo address vừa shipping vừa billing default | Hai flag lưu đúng |
| ACC-015 | P0 | Required field trống | Client/server reject |
| ACC-016 | P1 | Country code sai length/không hỗ trợ | Reject hoặc normalized đúng |
| ACC-017 | P1 | Postal code/phone theo configuration | Required/optional đúng store settings |
| ACC-018 | P1 | Email address field sai | Validation, không 500 |
| ACC-019 | P1 | Update/delete address không thuộc customer | 404/forbidden, không lộ address |
| ACC-020 | P0 | A1 cố truy cập address A5/S2 | Bị chặn cross-store |
| ACC-021 | P2 | Hai tab cùng update address | Conflict/last-write behavior rõ; UI không báo success sai |
| ACC-022 | P1 | Account page mobile | Menu, form và action buttons dùng được |

## 13. Cart WASM

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| CART-001 | P0 | Guest lần đầu mở cart | Tạo/resume cart session; empty state đúng |
| CART-002 | P0 | Add P1 từ product | Badge tăng, cart có đúng product/price/quantity |
| CART-003 | P0 | Add P1 lần hai | Merge cùng line và tăng quantity theo rule |
| CART-004 | P0 | Add hai product khác nhau | Hai line độc lập; tổng đúng |
| CART-005 | P0 | Add P5 variant khác nhau | Mỗi variant là line đúng, không merge sai |
| CART-006 | P0 | Add P7 custom attributes khác nhau | Line key/personalization tách đúng |
| CART-007 | P0 | Update quantity hợp lệ | Line total, subtotal, badge cập nhật từ server result |
| CART-008 | P0 | Quantity 0/âm/chữ | UI chặn; API không nhận mutation sai |
| CART-009 | P0 | P4 quantity min/max/step | Cart áp dụng cùng rule product page |
| CART-010 | P0 | Quantity vượt stock | Reject/revert UI, có message rõ |
| CART-011 | P0 | P3 unmanaged stock | Cart vẫn checkout allowed |
| CART-012 | P0 | Remove một line | Chỉ line mục tiêu bị xóa; tổng cập nhật |
| CART-013 | P0 | Clear cart | Có confirmation nếu thiết kế yêu cầu; cart rỗng |
| CART-014 | P0 | Reload sau mutation | Cart giữ nguyên từ server token/session |
| CART-015 | P1 | Đóng/mở browser context với persisted storage | Cart guest tồn tại theo lifetime policy |
| CART-016 | P0 | Cart S1 trên domain S2 | Không load/merge cart S1 |
| CART-017 | P1 | Cart token sai/hết hạn | Tạo cart mới hoặc recovery state; không 500 |
| CART-018 | P0 | Login khi guest cart có item | Merge/attach vào customer đúng một lần |
| CART-019 | P0 | Customer cart và guest cart có cùng product | Quantity merge đúng, không duplicate order line bất ngờ |
| CART-020 | P1 | Logout rồi xem cart | Hành vi cart theo policy; không lộ customer-only cart |
| CART-021 | P0 | Product bị unpublished sau khi đã add | Cart cảnh báo unavailable và chặn checkout |
| CART-022 | P0 | Product hết stock sau khi add | Cart cảnh báo và chặn checkout |
| CART-023 | P0 | Giá product đổi sau khi add | Recalculate/validation hiển thị giá server quyết định; không dùng giá DOM cũ |
| CART-024 | P1 | Currency rate hết hạn/thay đổi | Recalculate hoặc yêu cầu re-add theo contract, không mixed snapshot |
| CART-025 | P1 | Hai tab update cùng cart | Version/conflict xử lý; không mất line im lặng |
| CART-026 | P1 | Double click quantity/remove | Chỉ mutation hợp lệ cuối cùng được áp dụng |
| CART-027 | P1 | API 409 cart changed | Reload cart và message rõ; không báo success sai |
| CART-028 | P1 | API timeout nhưng mutation đã thành công | Retry/resync không nhân đôi quantity |
| CART-029 | P1 | Offline khi update, rồi online | Pending/error state được dọn; resync từ server |
| CART-030 | P0 | Cart totals | Subtotal và grand total đúng; không có dòng tax |
| CART-031 | P1 | Shipping chưa tính tại cart | Label đúng “estimate” hoặc không hiển thị sai total checkout |
| CART-032 | P0 | Checkout button với invalid line | Disabled và giải thích lý do |
| CART-033 | P0 | Checkout button với cart hợp lệ | Điều hướng checkout và giữ cart/session |
| CART-034 | P1 | Cart line media URL | Ảnh đúng store và không broken |
| CART-035 | P2 | Cart 50+ lines theo limit | Limit server được tôn trọng, UI vẫn responsive |
| CART-036 | P2 | Mobile cart | Update/remove/summary/CTA dùng được, không horizontal overflow |

## 14. Checkout WASM — Session, Address và Shipping

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| CHK-001 | P0 | Mở checkout với cart rỗng | Empty state, không tạo order/session vô nghĩa |
| CHK-002 | P0 | Mở checkout với cart invalid | Chặn, chỉ rõ line cần sửa |
| CHK-003 | P0 | Guest checkout nếu policy cho phép | Có thể nhập contact/address và tiếp tục |
| CHK-004 | P0 | Authenticated checkout A1 | Load customer và saved addresses đúng |
| CHK-005 | P0 | Chọn saved address | Form/review dùng đúng address |
| CHK-006 | P0 | Chuyển từ saved sang manual address | Field manual bật đúng, không giữ ID cũ |
| CHK-007 | P0 | Required contact/address trống | Không qua bước tiếp theo |
| CHK-008 | P1 | Email/phone/postal invalid | Validation theo configuration |
| CHK-009 | P1 | Country thay đổi | State list/default được cập nhật và state cũ không còn |
| CHK-010 | P0 | Physical cart tới H1 | Có shipping option hợp lệ |
| CHK-011 | P0 | Physical cart tới H2 | Hiện không giao được và cho sửa address |
| CHK-012 | P0 | Cart chỉ có P12 | Không bắt chọn shipping method |
| CHK-013 | P0 | Flat rate | Shipping total đúng settings/currency |
| CHK-014 | P0 | P11 surcharge | Surcharge cộng một lần đúng policy |
| CHK-015 | P1 | Nhiều quantity P11 | Sum/highest surcharge đúng configured policy |
| CHK-016 | P0 | Đạt free-shipping threshold H3 | Shipping = 0 và rule match đúng |
| CHK-017 | P1 | Subtotal giảm dưới threshold | Shipping được tính lại |
| CHK-018 | P1 | Đổi address sau khi chọn shipping | Shipping option cũ bị invalidate/recalculate |
| CHK-019 | P1 | Shipping API timeout | Có retry, không mất checkout session |
| CHK-020 | P1 | Resume checkout bằng reload/back/forward | Current step và dữ liệu đã lưu được phục hồi |
| CHK-021 | P1 | Cancel checkout session | Cart vẫn xử lý theo policy; session không place order được |
| CHK-022 | P0 | Checkout totals | Subtotal + shipping - discount = grand total; không có tax row |
| CHK-023 | P1 | Non-base currency checkout | Product, shipping, grand total cùng currency/snapshot |
| CHK-024 | P1 | Address thuộc customer khác bị inject | Server reject, không lộ address |

## 15. Checkout WASM — Payment, Review và Place Order

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| PAY-001 | P0 | H4 COD available | COD xuất hiện và chọn được |
| PAY-002 | P0 | H5 không có payment method | Chặn place order với message cấu hình |
| PAY-003 | P1 | Payment method disabled giữa flow | Review/place order revalidate và chặn |
| PAY-004 | P0 | Review cart/address/shipping/payment | Snapshot hiển thị đúng dữ liệu đã chọn |
| PAY-005 | P0 | Terms required nhưng chưa accept | Không place order |
| PAY-006 | P0 | Accept terms rồi place COD order | Tạo đúng một order và chuyển confirmation |
| PAY-007 | P0 | Double click Place Order | Chỉ một order do idempotency |
| PAY-008 | P0 | Request place order timeout sau khi server đã tạo | Retry cùng key trả order cũ, không tạo order thứ hai |
| PAY-009 | P0 | Reload confirmation/submit lại form | Không tạo order mới |
| PAY-010 | P0 | Cart version thay đổi trước place order | Conflict; yêu cầu review lại |
| PAY-011 | P0 | Giá thay đổi trước place order | Server revalidate theo policy, không charge/order bằng giá giả |
| PAY-012 | P0 | Stock hết trước place order | Không tạo order, cart giữ để người dùng sửa |
| PAY-013 | P0 | Hai browser mua stock cuối cùng | Tối đa một order thành công; order còn lại nhận out-of-stock |
| PAY-014 | P1 | Checkout session hết hạn | Không place; có đường quay lại cart/start lại |
| PAY-015 | P1 | Checkout session của S1 gửi vào S2 | Reject cross-store |
| PAY-016 | P1 | Checkout session của user khác | Reject unauthorized/not found |
| PAY-017 | P1 | Server 500 tại place order | Không hiện success giả; retry an toàn |
| PAY-018 | P0 | Order confirmation | Reference, amount và currency đúng; không lộ internal ID |
| PAY-019 | P0 | Guest confirmation | Dùng guest access token an toàn; URL không lộ secret ngoài phạm vi cần thiết |
| PAY-020 | P1 | Guest access token sai/hết hạn | Không xem order |
| PAY-021 | P1 | Confirmation page refresh | Vẫn xem đúng order, không tạo mutation |
| PAY-022 | P1 | Browser back về checkout sau success | Không thể place lại |
| PAY-023 | P1 | Transactional message order placed | Queue đúng một message, failure email không rollback order |
| PAY-024 | P1 | COD order state | Payment/order status đúng quy ước core |

## 16. Order Self-Service, Receipt và Shipment

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| ORD-001 | P0 | A1 mở order list | Chỉ order của A1 và S1 |
| ORD-002 | P1 | Pagination/order list empty | Đúng state, không 500 |
| ORD-003 | P0 | Mở order detail bằng reference | Items, totals, address, payment và shipping snapshot đúng |
| ORD-004 | P0 | Mở receipt | Nội dung printable, noindex, không có tax row |
| ORD-005 | P0 | Reference không thuộc A1 | 404/forbidden, không lộ order |
| ORD-006 | P0 | Order A5/S2 trên S1 | Không truy cập được |
| ORD-007 | P1 | Product sau đó đổi tên/giá/ảnh | Order vẫn hiển thị snapshot lúc đặt |
| ORD-008 | P1 | Order shipment chưa tạo | Detail hiển thị pending phù hợp |
| ORD-009 | P1 | Order shipped/tracking event | Tracking, carrier, status và history đúng |
| ORD-010 | P1 | Tracking URL external | Scheme an toàn, không `javascript:` |
| ORD-011 | P0 | Guest lookup đúng reference/email/token | Trả đúng order safe projection |
| ORD-012 | P0 | Guest lookup sai email/token | Generic not found, không leak existence |
| ORD-013 | P1 | Guest token hết hạn | Không truy cập order |
| ORD-014 | P1 | Logout khi đang xem order | Request tiếp theo redirect login, không cache private data |
| ORD-015 | P2 | Mobile order detail/receipt | Không overflow; totals và actions đọc được |

## 17. Currency và Tổng tiền

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| CUR-001 | P0 | Default currency S1 | Product/cart/checkout/order dùng cùng currency |
| CUR-002 | P1 | Chọn supported currency | Listing/product/cart cập nhật đúng |
| CUR-003 | P1 | Reload sau chọn currency | Preference được giữ |
| CUR-004 | P1 | Currency không hỗ trợ | Fallback default và có behavior rõ |
| CUR-005 | P1 | Variant price conversion | Làm tròn unit/line/order đúng metadata |
| CUR-006 | P1 | Shipping rate conversion | Cùng currency checkout, không double-convert |
| CUR-007 | P0 | Payment/order snapshot | Amount payment bằng checkout grand total |
| CUR-008 | P1 | Rate expired giữa cart và checkout | Recalculate/conflict theo policy |
| CUR-009 | P1 | Mixed rate snapshots trong cart | Chặn checkout và hướng dẫn re-add/recalculate |
| CUR-010 | P0 | Tax disabled | Không có label tax; tax không làm đổi grand total |

## 18. Consent, Contact và Newsletter Surface

Newsletter là optional module, nhưng endpoint/UI đang tồn tại thì vẫn cần smoke để tránh làm hỏng storefront.

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| PRI-001 | P0 | Visitor mới và consent enabled | Banner xuất hiện đúng version |
| PRI-002 | P0 | Save required-only consent | Banner đóng, state giữ sau reload |
| PRI-003 | P1 | Save optional categories | Chỉ category đã chọn được bật |
| PRI-004 | P1 | Revoke consent | State/event cập nhật và banner trở lại theo policy |
| PRI-005 | P1 | Consent version đổi | Yêu cầu xác nhận lại |
| PRI-006 | P1 | Cookie/local state bị hỏng | Recovery an toàn, không 500 |
| PRI-007 | P1 | S1/S2 consent | Không dùng nhầm consent giữa store nếu policy store-scoped |
| PRI-008 | P1 | Contact form hợp lệ | Queue/gửi message một lần và success feedback |
| PRI-009 | P1 | Contact required field/email invalid | Reject, không queue |
| PRI-010 | P1 | Contact captcha sai/rate limit | Bị chặn an toàn |
| PRI-011 | P1 | Contact payload HTML/script | Escape/sanitize, không XSS trong admin/message |
| PRI-012 | P2 | Newsletter subscribe email hợp lệ | Thành công/idempotent theo policy |
| PRI-013 | P2 | Newsletter duplicate/case variant | Không tạo subscriber trùng |
| PRI-014 | P2 | Newsletter email invalid/captcha sai | Reject an toàn |

## 19. WASM Hydration, Resilience và Network Failure

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| WASM-001 | P0 | Chờ WASM ready trên cart/account/checkout | Ready marker xuất hiện trong timeout; không trắng UI |
| WASM-002 | P0 | SSR markup trước hydration | Nội dung không nhấp nháy thành sai store/giá |
| WASM-003 | P1 | Hydration | Không gửi duplicate cart/account/checkout mutation |
| WASM-004 | P1 | Điều hướng nhiều lần giữa WASM routes | Event handlers không nhân đôi; memory/network ổn định |
| WASM-005 | P1 | JS/WASM bundle cache cũ sau deploy | Version/cache busting tránh boot mismatch |
| WASM-006 | P1 | Bundle 404/corrupt | Có failure UI/fallback, không spinner vô hạn |
| WASM-007 | P1 | API offline khi app đã boot | Hiện offline/retry, giữ input chưa submit |
| WASM-008 | P1 | API online trở lại | Retry/resync thành công |
| WASM-009 | P1 | 401 giữa mutation | Refresh một lần rồi retry; nếu thất bại chuyển login |
| WASM-010 | P1 | 403/404/409/422 | Mỗi loại có message/action phù hợp, không generic 500 |
| WASM-011 | P1 | 429 rate limit | Hiện retry-later, không spam request |
| WASM-012 | P1 | 500/503 | Không mất cart/form; có correlation/retry support an toàn |
| WASM-013 | P2 | Slow 3G | Loading skeleton/disabled button, không double action |
| WASM-014 | P2 | Hai tab cùng account/cart | State stale được phát hiện hoặc refresh đúng |
| WASM-015 | P1 | Browser back/forward | Component state và URL đồng bộ |
| WASM-016 | P1 | Deep link + refresh mọi WASM route | Host trả app/shell đúng, không server 404 |
| WASM-017 | P2 | Storage quota/private mode | App vẫn hoạt động hoặc báo lỗi recoverable |
| WASM-018 | P1 | Console/network audit | Không có legacy API, Commerce Admin API hay node credentials |

## 20. Security và Isolation qua hành vi trình duyệt

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| SEC-001 | P0 | Sửa `storeKey` trong request | Không đọc cart/account/order store khác |
| SEC-002 | P0 | Sửa cart line/address/order ID | Không thao tác object của user khác |
| SEC-003 | P0 | Gọi account/order API không JWT | 401, không redirect HTML từ API |
| SEC-004 | P0 | Refresh cookie không có access token | Chỉ refresh endpoint sử dụng được theo policy |
| SEC-005 | P1 | Mutation form thiếu/sai antiforgery | Bị chặn |
| SEC-006 | P1 | XSS payload trong search/profile/address/custom attribute | Không execute ở Storefront, order hoặc admin |
| SEC-007 | P1 | Open redirect trong signin/register/recovery return URL | Chỉ local safe URL được dùng |
| SEC-008 | P1 | Path traversal/media filename bất thường | Không đọc file ngoài media scope |
| SEC-009 | P1 | Media asset S2 từ host S1 | 404/forbidden |
| SEC-010 | P1 | Sensitive data audit | Không có password/token/secret trong HTML, URL, console, trace attachment |
| SEC-011 | P1 | Cache headers trang account/order | Không public-cache private data |
| SEC-012 | P1 | Browser history sau logout | Private response không còn đọc được |
| SEC-013 | P1 | CORS từ origin lạ | Không cho credentialed storefront mutation ngoài policy |
| SEC-014 | P1 | Rate limit login/recovery/cart/contact | Đúng endpoint/policy, không chặn nhầm public GET |

## 21. Responsive, Accessibility và Browser Matrix

Chạy P0 trên Chromium, Firefox và WebKit desktop. Chạy `@mobile` trên viewport 390×844 và 768×1024.

| ID | Pri | Testcase | Kết quả mong đợi |
|---|---:|---|---|
| UX-001 | P0 | Home/product/category/cart/checkout/account mobile | Không horizontal overflow hoặc CTA bị che |
| UX-002 | P1 | Keyboard-only header/menu/auth/cart/checkout | Mọi action dùng được bằng keyboard |
| UX-003 | P1 | Focus sau menu/dialog/error | Focus chuyển hợp lý và không bị mất |
| UX-004 | P1 | Form validation | Error được liên kết input và đọc bởi screen reader |
| UX-005 | P1 | Dynamic cart/selection message | Có live region phù hợp, không đọc lặp |
| UX-006 | P1 | Heading hierarchy | Một H1 và thứ tự heading hợp lý |
| UX-007 | P1 | Image accessibility | Product/content image có alt phù hợp; decorative image alt rỗng |
| UX-008 | P1 | Color contrast | CTA, error, disabled và text đạt chuẩn mục tiêu |
| UX-009 | P1 | Zoom 200% | Nội dung và action vẫn dùng được |
| UX-010 | P2 | Reduced motion | Không animation gây cản trở; preference được tôn trọng |
| UX-011 | P1 | Loading button | Disabled/aria-busy phù hợp, không mất label |
| UX-012 | P1 | Browser matrix auth cookie | Login/refresh/logout hoạt động trên Chromium/Firefox/WebKit |
| UX-013 | P1 | Browser matrix WASM | Boot/deep link/reload hoạt động trên ba engine |
| UX-014 | P2 | Visual regression | Snapshot vùng ổn định, mask giá/ngày/dữ liệu động |

## 22. Production Smoke Suite

Chỉ chạy trên store/account synthetic được cấp phép. Không thử rate-limit, XSS, concurrency stock cuối cùng, archive/delete dữ liệu thật hoặc payment provider thật trên production.

### 22.1 Production-safe P0

- [ ] `PROD-001 @production-safe` — `/` trả 200, đúng store/branding.
- [ ] `PROD-002 @production-safe` — header, navigation và footer link cốt lõi hoạt động.
- [ ] `PROD-003 @production-safe` — search một seeded keyword trả product kỳ vọng.
- [ ] `PROD-004 @production-safe` — category seeded mở được và breadcrumb đúng.
- [ ] `PROD-005 @production-safe` — product seeded mở được, price/stock/CTA hiện đúng.
- [ ] `PROD-006 @production-safe` — dynamic page legal mở được.
- [ ] `PROD-007 @production-safe` — old slug synthetic redirect đúng canonical.
- [ ] `PROD-008 @production-safe` — robots và sitemap hợp lệ, đúng host.
- [ ] `PROD-009 @production-safe` — media seeded tải thành công.
- [ ] `PROD-010 @production-safe` — WASM ready trên cart/account/checkout shell.
- [ ] `PROD-011 @production-safe` — login synthetic account thành công.
- [ ] `PROD-012 @production-safe` — reload giữ session.
- [ ] `PROD-013 @production-safe` — profile/order list synthetic đọc được.
- [ ] `PROD-014 @production-safe` — logout và protected-route redirect đúng.
- [ ] `PROD-015 @production-safe` — add P1 synthetic vào synthetic cart, update/remove và kết thúc cart rỗng.
- [ ] `PROD-016 @production-safe` — cart/checkout không hiển thị tax.
- [ ] `PROD-017 @production-safe` — checkout preview synthetic tới review, không place order.
- [ ] `PROD-018 @production-safe` — no console error và không có 5xx trong toàn smoke.
- [ ] `PROD-019 @production-safe` — canonical/OG không chứa localhost/internal host.
- [ ] `PROD-020 @production-safe` — Storefront không gọi Commerce Admin/legacy API.

### 22.2 Synthetic order smoke có kiểm soát

Chỉ bật khi production có test store hoặc SKU synthetic riêng và quy trình dọn order:

- [ ] `PROD-ORD-001` — tạo cart synthetic.
- [ ] `PROD-ORD-002` — chọn address/shipping/COD synthetic.
- [ ] `PROD-ORD-003` — place đúng một order với idempotency key.
- [ ] `PROD-ORD-004` — confirmation/reference/totals đúng, không tax.
- [ ] `PROD-ORD-005` — order xuất hiện trong account history.
- [ ] `PROD-ORD-006` — đánh dấu/cancel test order bằng quy trình vận hành được phê duyệt.

## 23. Staging-only Failure/Abuse Suite

- [ ] Concurrent checkout với stock = 1.
- [ ] Double-submit place order và retry sau timeout.
- [ ] Login/recovery/contact/cart rate-limit.
- [ ] Token reset/confirm malformed, expired và reused.
- [ ] Cross-store cart/address/order/media ID manipulation.
- [ ] XSS payload trong profile/address/search/page data.
- [ ] CommerceNode unavailable/restart giữa cart và checkout.
- [ ] Expired cart/checkout/currency-rate snapshot.
- [ ] Payment method bị disable giữa checkout.
- [ ] Shipping country unavailable và provider timeout.
- [ ] Product archive, price change và stock change khi item đang trong cart.
- [ ] WASM bundle version mismatch và API 401/409/429/503.

## 24. Release Acceptance Gate

Một release core chỉ được pass khi:

- [ ] 100% P0 pass trên Chromium.
- [ ] P0 auth/session/cart/checkout pass trên Firefox và WebKit.
- [ ] Không có P0/P1 security isolation failure.
- [ ] Không có duplicate order trong idempotency/concurrency suite.
- [ ] Không thể mua product/variant hết stock hoặc không purchasable.
- [ ] Cart và order không tin giá/variant/attribute bị sửa từ browser.
- [ ] Account recovery valid/invalid/expired/reused-token suite pass.
- [ ] Cart/account/checkout WASM deep-link, reload và refresh-token suite pass.
- [ ] Store S1/S2 isolation pass cho catalog, page, media, cart, account và order.
- [ ] Không có tax trong UI và công thức tổng tiền đúng.
- [ ] SEO canonical/robots/sitemap/private noindex suite pass.
- [ ] Không có unexpected console error, page error hoặc 5xx.
- [ ] Trace/screenshot của mọi failure đã được review hoặc gắn issue.

## 25. Báo cáo lỗi chuẩn

Mỗi failure nên lưu:

- Test ID và commit/deployment version.
- Store key, browser, viewport và environment.
- Dữ liệu fixture đã dùng, không ghi password/token thật.
- Expected và actual result.
- Playwright trace, screenshot cuối và request/response liên quan đã redact.
- Correlation/trace ID từ API nếu có.
- Phân loại: storefront rendering, WASM state, API contract, business rule, security isolation hoặc data/setup.
- Regression test cần bổ sung sau khi fix.

