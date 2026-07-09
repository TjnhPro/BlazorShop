# Phase 1 — Boundary Clarification & Architecture Baseline

## 1. Mục tiêu

Phase 1 có mục tiêu **tách ranh giới kiến trúc trên tài liệu trước**, chưa thay đổi code, chưa thay đổi database, chưa thay đổi behavior hiện tại của BlazorShop.

Mục tiêu chính:

* Làm rõ mô hình tương lai của hệ thống.
* Xác định vai trò riêng của **Control Plane**, **Commerce Node**, và **Storefront**.
* Map lại project BlazorShop hiện tại vào mô hình mới.
* Đặt convention cho API boundary.
* Đặt nguyên tắc security boundary.
* Chuẩn bị nền tảng tư duy cho các phase refactor/code tiếp theo.

Phase này là **documentation-only phase**.

---

## 2. Nguyên tắc quan trọng

Phase 1 không được làm các việc sau:

* Không sửa code.
* Không đổi route thật.
* Không đổi database schema.
* Không tạo migration mới.
* Không tách project thật.
* Không rename namespace hàng loạt.
* Không đổi authentication flow.
* Không đổi Storefront fetching flow.
* Không implement multi-store.
* Không thêm Control Plane thật.
* Không thêm Commerce Node registry thật.

Mục tiêu là **hiểu đúng và chốt boundary trước khi code**.

---

## 3. Mô hình kiến trúc mục tiêu

Hệ thống tương lai được chia thành 3 khối chính:

```text
[Control Plane]
    |
    | public HTTPS + fixed IP + API key
    v
[Commerce Node]
    |
    | internal/private API
    v
[Storefront SSR]
```

Hoặc nhìn theo vai trò:

```text
Control Plane  = quản lý platform và node
Commerce Node  = xử lý commerce thật và giữ database commerce
Storefront     = render public storefront bằng SSR
```

---

## 4. Control Plane

### Vai trò

Control Plane là nơi quản lý platform ở cấp cao.

Nó không xử lý trực tiếp nghiệp vụ bán hàng như product, order, cart, checkout. Nó chỉ điều phối và quản lý các Commerce Node.

### Thành phần dự kiến

```text
Control Plane
- Web API
- Blazor WASM admin UI
- PostgreSQL
- Admin/User auth
- Role/permission
- Commerce node registry
- Commerce node URL
- Commerce node API key / credential
- Health check
- Audit log
```

### Dữ liệu Control Plane sở hữu

```text
AdminUser
Role
Permission
CommerceNode
CommerceNodeCredential
NodeHealthSnapshot
ControlActionAuditLog
StoreRegistry metadata
```

Control Plane không nên connect trực tiếp vào database của Commerce Node.

### Cách giao tiếp với Commerce Node

Control Plane gọi Commerce Node qua:

```text
/api/controlpanel/*
```

Đây là API public về mặt network, vì Control Plane và Commerce Node có thể nằm ở server khác nhau.

Nhưng nó không phải public API cho người dùng. Nó phải được bảo vệ bằng:

```text
Fixed IP allowlist
API key
HTTPS
Rate limit
Audit log
```

---

## 5. Commerce Node

### Vai trò

Commerce Node là nơi chạy nghiệp vụ commerce thật.

Mỗi Commerce Node có thể nằm ở server/country/region khác nhau tùy mục đích của store.

Ví dụ:

```text
Commerce Node VN
Commerce Node US
Commerce Node EU
Commerce Node SG
```

### Thành phần dự kiến

```text
Commerce Node
- Web API
- PostgreSQL
- Multi-store
- Customer auth theo store
- Product/catalog/order/payment/inventory
```

### Dữ liệu Commerce Node sở hữu

```text
Store
StoreDomain
StoreSettings
ThemeSettings
Product
Category
Inventory
Order
OrderLine
Payment
Customer
CustomerAuth
Cart
Checkout
Media
SEO data
```

Commerce Node là nơi giữ dữ liệu thật của store.

---

## 6. Storefront

### Vai trò

Storefront là public rendering layer.

Storefront dùng Blazor SSR để render HTML cho browser, phục vụ SEO và trải nghiệm public shop.

### Thành phần dự kiến

```text
Storefront
- Blazor SSR
- Resolve store theo domain phía server
- Fetch data bằng internal API từ Commerce Node
- Render Home/Product/Category/Search/Cart shell
- SEO metadata
- Sitemap
- Robots.txt
```

### Nguyên tắc

Storefront không nên giữ business database riêng.

Storefront không nên gọi `/api/controlpanel/*`.

Storefront chỉ gọi Commerce Node thông qua:

```text
/api/internal/*
```

Luồng:

```text
Browser
→ Storefront SSR
→ Commerce Node /api/internal/*
→ Commerce DB
→ Storefront render HTML
→ Browser nhận HTML
```

Browser không biết:

```text
Commerce internal API URL
Internal API key
StoreId nội bộ
Connection string
Node private address
```

---

## 7. API Boundary Convention

Commerce Node nên chia API theo boundary rõ ràng.

### 7.1 `/api/controlpanel/*`

Dành cho:

```text
Control Plane → Commerce Node
```

Tính chất:

```text
Public URL
Không dành cho browser/customer
Chỉ Control Plane được gọi
Bảo vệ bằng fixed IP + API key
Dùng cho quản lý node/store từ xa
```

Ví dụ:

```text
GET  /api/controlpanel/health
GET  /api/controlpanel/version
GET  /api/controlpanel/capabilities
POST /api/controlpanel/stores
PUT  /api/controlpanel/stores/{storeId}/settings
POST /api/controlpanel/stores/{storeId}/sync
```

### 7.2 `/api/internal/*`

Dành cho:

```text
Storefront SSR → Commerce Node
```

Tính chất:

```text
Private/internal network
Không expose public internet
Không dành cho browser gọi trực tiếp
Dùng để render storefront
```

Ví dụ:

```text
GET /api/internal/storefront/context
GET /api/internal/catalog/categories
GET /api/internal/catalog/products
GET /api/internal/catalog/products/{slug}
GET /api/internal/catalog/categories/{slug}
GET /api/internal/sitemap
GET /api/internal/theme-settings
```

### 7.3 `/api/storefront/*`

Phase 1 chưa bắt buộc dùng.

Route này chỉ cần nếu sau này có customer-facing API cho browser/mobile/app gọi trực tiếp.

Ví dụ dùng khi có:

```text
Mobile app
Customer SPA
Customer WASM portal
AJAX-heavy cart/checkout
External customer integration
```

Nếu Storefront SSR xử lý toàn bộ customer flow thì Phase 1 chưa cần `/api/storefront/*`.

---

## 8. Security Boundary

### 8.1 Control Plane → Commerce Node

Vì `/api/controlpanel/*` phải public URL, cần bảo vệ tối thiểu bằng:

```text
HTTPS
Fixed IP allowlist của Control Plane
API key riêng cho từng Commerce Node
Rate limit
Audit log
```

Không nên chỉ dựa vào domain/header như `Origin`, `Referer`, `Host`, vì các header này có thể bị giả khi gọi server-to-server.

### 8.2 Storefront → Commerce Node

`/api/internal/*` không nên expose ra internet.

Nên chạy qua:

```text
localhost
Docker network
private LAN
private VPC
internal reverse proxy
```

Có thể thêm internal secret header để phòng cấu hình sai:

```text
X-Storefront-Internal-Key
```

### 8.3 Browser → Storefront

Browser chỉ thấy Storefront public URL.

Browser không biết Commerce internal API.

Storefront SSR resolve store theo domain ở phía server.

---

## 9. Mapping BlazorShop hiện tại sang mô hình mới

### Hiện tại

```text
BlazorShop.API
- Đang là API host chung
- Chứa public/catalog/admin/auth/cart/payment/order controllers

BlazorShop.Storefront
- Đang là Blazor SSR public storefront
- Gọi API để lấy dữ liệu public catalog

BlazorShop.Web
- Đang là Blazor WASM client
- Chứa account/admin/manager style UI

BlazorShop.Application
- Chứa DTO/service/use-case

BlazorShop.Infrastructure
- Chứa DB/repository/provider/Identity/payment/email

BlazorShop.ServiceDefaults
- Chứa hosting defaults: health check, OpenTelemetry, service discovery, HTTP resilience

BlazorShop.AppHost
- Local Aspire orchestrator
```

### Mapping mục tiêu

```text
BlazorShop.API
→ Tương lai trở thành Commerce Node API

BlazorShop.Storefront
→ Giữ vai trò Storefront SSR

BlazorShop.Web
→ Tạm thời là Admin/Manager UI
→ Sau này có thể tách thành Control Plane UI hoặc Node Manager UI

BlazorShop.Application
→ Tạm thời là Commerce Application layer

BlazorShop.Infrastructure
→ Tạm thời là Commerce Infrastructure layer

Control Plane
→ Chưa tồn tại trong code hiện tại
→ Phase sau mới tạo riêng
```

---

## 10. Scope của Phase 1

Phase 1 chỉ làm các đầu việc tài liệu:

### 10.1 Architecture Boundary Document

Tạo document mô tả:

```text
Control Plane
Commerce Node
Storefront
Data ownership
Communication boundary
Security boundary
```

### 10.2 Current Project Mapping

Tạo document map project hiện tại của BlazorShop vào mô hình mới.

### 10.3 API Boundary Convention

Tạo convention cho route tương lai:

```text
/api/controlpanel/*
/api/internal/*
/api/storefront/* optional
```

### 10.4 Security Boundary Convention

Tạo nguyên tắc bảo vệ:

```text
Controlpanel API = public URL nhưng private access
Internal API = private network
Customer API = optional public customer boundary
```

### 10.5 Phase 2 Backlog

Tạo danh sách việc code cho Phase 2, nhưng chưa thực hiện ở Phase 1.

---

## 11. Out of Scope

Phase 1 không làm:

```text
Không tạo Control Plane project
Không tạo Commerce Node project mới
Không tách API host thật
Không đổi controller route
Không thêm StoreId
Không thêm Store table
Không thêm Node table
Không thêm API key middleware
Không thêm IP allowlist middleware
Không đổi Storefront fetching logic
Không đổi database
Không đổi deployment
```

---

## 12. Deliverables

Kết thúc Phase 1 cần có:

```text
docs/phase-01-boundary-baseline.md
docs/architecture/control-plane-commerce-node-storefront.md
docs/architecture/api-boundary-conventions.md
docs/architecture/security-boundary-conventions.md
docs/architecture/current-blazorshop-project-mapping.md
docs/phase-02-backlog.md
```

---

## 13. Acceptance Criteria

Phase 1 được xem là hoàn thành khi:

* Có tài liệu mô hình Control Plane / Commerce Node / Storefront.
* Có mapping rõ project BlazorShop hiện tại vào mô hình mới.
* Có định nghĩa rõ `/api/controlpanel/*`, `/api/internal/*`, `/api/storefront/*`.
* Có nguyên tắc security boundary.
* Có danh sách việc Phase 2.
* Repo vẫn chạy như cũ.
* Không có code change bắt buộc.
* Không có migration mới.
* Không có behavior change.

---

## 14. Gợi ý Phase 2

Phase 2 mới bắt đầu chuẩn bị code nhẹ.

Tên gợi ý:

```text
Phase 2 — Commerce Node Boundary Preparation
```

Scope Phase 2 có thể gồm:

```text
Tạo folder Controllers/Internal
Tạo folder Controllers/ControlPanel
Tạo route convention mới
Tạo placeholder policy/middleware
Tạo config cho Control Plane IP/API key
Tạo Internal API key config cho Storefront
Tách DTO prefix nếu cần
Chưa implement multi-store sâu
```

---

## 15. Kết luận

Phase 1 là phase nền tảng để tránh refactor sai hướng.

Nguyên tắc chính:

```text
Understand first
Document boundary
Then refactor
Then implement multi-store
```

Mục tiêu của Phase 1 không phải làm hệ thống chạy khác đi, mà là làm rõ hệ thống **sẽ được tách như thế nào** trước khi bắt đầu code.
