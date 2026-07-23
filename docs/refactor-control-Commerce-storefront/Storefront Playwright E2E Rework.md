# Storefront Playwright E2E Cases To Rework

Status: draft review  
Date: 2026-07-18  
Source checklist: `blazorshop_playwright_qa_checklist.md`  
Purpose: gom các testcase chưa phù hợp hoặc cần chỉnh để checklist không tạo failure giả so với codebase hiện tại.

## Code Evidence

- `BlazorShop.Storefront.V2/Program.cs` có WASM runtime host: `AddInteractiveWebAssemblyComponents()`, `AddInteractiveWebAssemblyRenderMode()`.
- `BlazorShop.Storefront.Components` có cart, account và checkout WASM component:
  - `Cart/CartView.razor`
  - `Account/AccountProfileEditor.razor`, `AccountAddressBook.razor`, `AccountOrderList.razor`, `AccountOrderDetail.razor`, `AccountChangePasswordForm.razor`
  - `Checkout/CheckoutShell.razor`
- `/my-cart` host `CartView` với `@rendermode="InteractiveWebAssembly"`.
- Account pages host các component account bằng `@rendermode="InteractiveWebAssembly"` và component gọi same-origin local API `/api/account/*`.
- Checkout page host `CheckoutShell` bằng `@rendermode="InteractiveWebAssembly"`. Shell gọi same-origin local API `/api/checkout`, `/api/checkout/shipping-method`, `/api/checkout/payment-method`, `/api/checkout/review`, `/api/checkout/place-order`.
- Checkout contact/address input vẫn nằm trong page-level SSR form/fallback ở `CheckoutPage.razor`; đây là phần chưa nên viết expectation như một WASM address-step component hoàn chỉnh.
- Checkout và order detail đang render dòng Tax trong UI, nhưng tax core không thuộc phase và hiện mặc định 0. Test không nên fail vì thiếu tax core.

## Naming And Scope Rework

| Case/section | Vấn đề | Cách chỉnh |
|---|---|---|
| Scope dòng "Commerce WASM: cart, account và checkout" | Đúng với code hiện tại nếu hiểu checkout là shell/component, không phải toàn bộ address/contact UI. | Đã chuyển sang Release checklist; giữ caveat checkout address/contact form/fallback. |
| `RUN-003` / navigation qua cart/account/checkout | Account và checkout shell đã là WASM, nên không còn phải defer. | Đã chuyển sang Release checklist thành `RUN-011` và `RUN-012`. |
| `WASM-001` | Ready marker có thể áp dụng cho cart/account/checkout component host, nhưng selector cụ thể phải bám `data-storefront-*` hiện có. | Đã chuyển sang Release checklist dưới dạng hydration/deep-link/reload; không bắt marker chưa tồn tại. |
| `WASM-003` | Duplicate mutation vẫn là rủi ro thật cho cart/account/checkout local API. | Đã chuyển sang Release checklist bằng các case cart update, account mutation, checkout place-order/idempotency. |
| `WASM-007..012` | Network failure/retry phù hợp với component gọi local JSON API, nhưng không nên yêu cầu error UI chưa có text chuẩn. | Chuyển vào Release ở mức browser observable/recoverable, chỉ thêm selector/test hook nếu role/label không đủ ổn định. |
| Checkout address-step WASM | Shell hiện chưa sở hữu form nhập contact/address; page vẫn có SSR form/fallback. | Giữ ở Rework cho tới khi address/contact input thành component hoặc local `/api/checkout/addresses` được nối vào UI component rõ ràng. |

## Tax Expectation Rework

| Case/section | Vấn đề | Cách chỉnh |
|---|---|---|
| Scope yêu cầu "cart và checkout không hiển thị dòng Tax" | Code hiện render Tax row ở checkout và order detail; tax core chưa làm, mặc định 0. | Không dùng làm blocker tax core. Chuyển thành UI policy: nếu team muốn không hiện Tax 0 thì tạo ticket UI riêng; E2E chỉ gate tổng tiền đúng. |
| Assertion chung "UI không hiển thị tax hoặc `Tax 0.00` trong cart/checkout/order" | Sẽ fail trên checkout/order detail hiện tại dù business total có thể đúng. | Đổi thành "tax không làm đổi grand total khi tax disabled/default 0"; optional assertion ẩn label tax nếu UI đã được sửa. |
| `CHK-022` | "không có tax row" không đúng code hiện tại. | Đổi expected thành "Subtotal + shipping - discount + tax(0) = grand total; không có tax charge bất ngờ". |
| `CUR-010` | "Không có label tax" không phản ánh code. | Đổi thành "Tax disabled/default 0 không ảnh hưởng grand total; nếu tax row hiện thì amount phải 0". |
| `ORD-004`, `PROD-016`, `PROD-ORD-004` nếu assert không tax row | Có thể fail vì order detail/receipt đang hiện row Tax. | Chỉ assert tax amount 0 và grand total đúng, hoặc tách UI cleanup ticket. |

## Browser Flow Rework

| Case/section | Vấn đề | Cách chỉnh |
|---|---|---|
| `CHK/PAY` section title "Checkout WASM" | Checkout shell là WASM, nhưng contact/address input vẫn là page-level form/fallback. | Title trong Release đã đổi thành "Checkout WASM Shell Browser Flow"; address/contact expectation phải ghi rõ caveat. |
| `CHK-020 Resume checkout bằng reload/back/forward` | Shell có checkout state/version nhưng UI chưa phải full stepper WASM. | Test reload/back giữ cart/session/version và không double place; không assert step restoration chưa có UI. |
| `CHK-021 Cancel checkout session` | Browser UI cancel checkout chưa thấy trong page hiện tại. | Chuyển sang API/contract test hoặc defer tới khi UI có cancel action. |
| `PAY-004 Review cart/address/shipping/payment` | Shell có review command, nhưng address/contact vẫn từ form/fallback. | Test review command và confirmation snapshot; không assert full multi-step review UI chưa có. |
| `PAY-005 Terms required` | Chưa xác nhận có terms checkbox/config UI trong checkout hiện tại. | Chỉ enable khi setting terms/legal acknowledgement tồn tại trong code/UI. |
| `PAY-019..021 Guest confirmation` | Backend guest lookup có thể có, nhưng browser UI guest order lookup chưa được xác nhận. | Chuyển sang missing UI testcase hoặc defer cho phase guest order self-service page. |
| `PROD-017` "checkout preview synthetic, không place order" | User yêu cầu E2E thật vẫn place COD order trên test store. | Đổi thành "place COD order synthetic thật và cleanup theo protocol". |
| Production Smoke Suite | User không muốn smoke vì không phát hiện lỗi thật. | Không dùng làm release gate chính; đổi thành Release E2E suite bằng browser thật. |

## Feature Availability Rework

| Case/section | Vấn đề | Cách chỉnh |
|---|---|---|
| `CAT-019`, `CAT-020` Search suggestion/instant search | API client có suggestion, nhưng chưa xác nhận UI autocomplete trong browser. | Chuyển thành API/contract hoặc missing UI test; browser E2E chỉ test search form hiện có. |
| `PRD-021` Media URL sai canonical filename | Product media route hiện dùng `/media/products/{mediaPublicId}`; không thấy filename canonical behavior. | Đổi thành "media URL đúng store, cache/version query hợp lệ, không broken image". |
| `ACC-004` Email update confirmation | Account profile WASM có email field/update, chưa thấy email-change confirmation flow trong browser UI. | Không gate release nếu chưa có policy; test profile update bình thường. |
| `ACC-005/006` Preferred currency trong account profile | Account profile WASM có currency input, nhưng storefront currency preference cũng có route riêng. | Test currency selector/preference ở header/currency flow; không bắt account profile là source duy nhất. |
| `ORD-011..013` Guest lookup | Browser route/page chưa thấy trong Storefront V2 pages. | Chuyển thành missing browser UI nếu cần production guest self-service; không gate account-only order history. |
| `PRI-008..011` Contact form | Contact endpoint/API có thể có, nhưng user đã quyết định contact form sẽ chuyển sang WASM component sau, không phải full page mapping. | Chỉ test nếu contact component/page đang enabled trong current store; nếu chưa có UI thì defer. |
| `PRI-012..014` Newsletter | Optional module; không nên block release nếu store không enable newsletter UI. | Tag P2/conditional by feature flag. |
| `RUN-012`, `WASM-013` slow 3G skeleton | Không phải mọi SSR page có skeleton. | Test no double submit/recoverable state; skeleton chỉ assert nơi UI có loading state thật. |

## Selector Rework

| Item | Vấn đề | Cách chỉnh |
|---|---|---|
| `data-testid` policy | Code hiện dùng nhiều role/label/text và `data-storefront-*`, không có bộ `data-testid` đầy đủ. | Playwright ưu tiên role/label/text; chỉ thêm `data-testid` khi vùng động khó định danh. |
| Suggested ids `account-menu`, `order-list`, `checkout-address-step` | Một số chưa tồn tại hoặc chưa ổn định; account hiện có `data-storefront-account-*`, checkout shell có `data-storefront-checkout-shell`. | Không viết test phụ thuộc id chưa có; tạo ticket nhỏ thêm test hooks nếu selector bằng role/label hoặc `data-storefront-*` hiện có không đủ ổn định. |
| Cart selectors | Cart có `data-storefront-cart-*` và component state. | Dùng selector hiện có trước khi thêm id mới. |
| Checkout selectors | Checkout có shell `data-storefront-checkout-shell`, cart version marker, label/form và `data-storefront-manual-address*`. | Dùng `getByLabel`, `getByRole`, và `data-storefront-*` hiện có; không thêm expectation cho `checkout-address-step` khi component chưa tồn tại. |

## Cases To Defer Until Implementation Exists

- [ ] Checkout address/contact WASM component input flow nếu muốn bỏ page-level SSR form/fallback.
- [ ] Checkout multi-step WASM stepper nếu UI sau này tách thành billing/shipping/payment/review từng bước.
- [ ] Checkout cancel session browser action.
- [ ] Guest order lookup browser page if production requires guest self-service outside confirmation URL.
- [ ] Instant search autocomplete browser interaction if UI is added.
- [ ] Terms/legal acknowledgement browser gate if setting and checkbox are added.
- [ ] Contact WASM component E2E if contact form is moved into WASM.
- [ ] Newsletter browser E2E if newsletter UI is enabled for the store.
- [ ] Strict "no Tax row visible" assertion after UI cleanup hides tax label while tax core remains disabled.

## Cases To Keep But Retag

- Cart WASM tests remain P0/P1 because cart component exists and owns browser mutations.
- Account WASM tests remain P0/P1 because profile, address, order list/detail, receipt and password components exist and call local `/api/account/*`.
- Checkout/payment/order tests remain P0/P1 as checkout WASM shell E2E and must place COD order thật on synthetic test data; address/contact stays covered as current page-level form/fallback.
- Security isolation tests remain P0/P1 even if they use browser request interception/API calls, because they catch real cross-store/customer leaks.
- SEO/private noindex tests remain P0/P1 because Storefront V2 owns robots/sitemap/canonical/private page headers.
