# StorefrontBuilder Script Runner

Thư mục này chứa script wrapper để chạy StorefrontBuilder từ đúng repo root. Script gốc vẫn nằm ở `tools/BlazorShop.AI.StorefrontBuilder\build-storefront.ps1`; file trong thư mục này chỉ giúp gọi đúng đường dẫn, đúng tham số, và tránh lỗi chạy sai working directory.

Luôn bắt đầu command bằng `powershell` hoặc `pwsh`. Không chạy `-NoProfile` như token đầu tiên trong PowerShell.

## Command Nhanh

In command StorefrontBuilder thật sự sẽ được gọi, nhưng không chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Describe
```

Chạy lập kế hoạch khô, không tạo project thật:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode plan-only -Name Demo -StoreKey sample
```

Tạo storefront dùng thử dưới `obj/storefront-builder/generated`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode generate -Name Demo -StoreKey sample -Force
```

Chạy đầy đủ bước tạo storefront và validation:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode full -Name Demo -StoreKey sample -Force
```

Validate một generated project đã có sẵn:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode validate-only -Name Demo -StoreKey sample -OutputRoot obj/storefront-builder/generated
```

## Command Portable Handoff

Chạy preflight cho portable handoff Phase 4:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode preflight-only -HandoffRoot <portable-handoff-root>
```

Tạo generation plan từ portable handoff package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode plan-only -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root>
```

Generate storefront từ portable handoff package:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode generate -Name Demo -StoreKey sample -HandoffRoot <portable-handoff-root> -Force
```

## Tham Số

- `-Mode`: chọn chế độ chạy StorefrontBuilder. Xem phần "Giải Thích Mode" bên dưới.
- `-Url`: URL tham chiếu dùng cho các mode phân tích/generate không chạy qua handoff. Mặc định: `https://reference.example`.
- `-Name`: tên project storefront được generate. Có thể truyền dạng thân thiện như `kindredcoast`, `kindred-coast`, hoặc full name `BlazorShop.Storefront.KindredCoast`; wrapper sẽ chuẩn hóa thành project name hợp lệ trước khi gọi StorefrontBuilder gốc. Mặc định: `BlazorShop.Storefront.GeneratedProof`.
- `-StoreKey`: store key của storefront được generate. Mặc định: `sample`.
- `-OutputRoot`: thư mục output của generated project, tính từ repo root nếu truyền relative path. Mặc định: `obj/storefront-builder/generated`.
- `-HandoffRoot`: thư mục portable `analysis/agent-handoff` package cho các mode Phase 4 handoff.
- `-HandoffSchemaRoot`: thư mục schema dùng để validate handoff. Mặc định: `tools/BlazorShop.AI.StorefrontReverseEngineering/Schemas`.
- `-Force`: cho phép ghi đè output generated cũ để chạy lại deterministic.
- `-SkipVisualQa`: bỏ qua thông báo runner Visual QA trong mode `full`.
- `-SkipCommerceRegression`: bỏ qua thông báo runner Commerce Regression trong mode `full`.
- `-InstallNodeDependencies`: chạy `npm ci` trong `tools\BlazorShop.AI.StorefrontBuilder` nếu thiếu `node_modules`.
- `-Describe`: chỉ in command gốc sẽ chạy rồi thoát, không thực thi StorefrontBuilder.

## Giải Thích Mode

- `analyze-only`: chỉ tạo review artifact từ `-Url` cho project output đã resolve. Dùng khi cần chạy phần phân tích/reference artifact mà không generate project mới.
- `preflight-only`: chỉ validate portable handoff package trước khi StorefrontBuilder tiêu thụ. Mode này bắt buộc có `-HandoffRoot`; không generate project.
- `plan-only`: tạo kế hoạch generate ở dạng dry-run, in danh sách file sẽ replace/patch/skip. Dùng để kiểm tra trước khi ghi file thật.
- `generate`: tạo hoặc cập nhật generated storefront project dưới `-OutputRoot`. Nếu output đã tồn tại, thường cần thêm `-Force`.
- `update`: chạy regeneration trên generated project đã có sẵn. Mode này dùng khi muốn refresh lại generated files theo scope mặc định của script gốc.
- `validate-only`: validate generated project đã tồn tại bằng static gate, schema, asset/CSS/composition/protected dependency/idempotency checks.
- `full`: chạy luồng đầy đủ: generate project, tạo artifact phụ trợ, update manifest, validate generated project. Đây là mode nên dùng khi cần proof local đầy đủ.

## Gợi Ý Sử Dụng

Generated output là artifact dùng thử, không phải source chính. Dùng `obj/storefront-builder/generated` cho các proof run local. Chỉ dùng `artifacts/storefront-builder/generated` khi workflow yêu cầu giữ artifact thủ công lâu hơn.

Nếu chỉ muốn kiểm tra command trước khi chạy thật, dùng `-Describe`. Nếu muốn kiểm tra tác động trước khi ghi file, dùng `-Mode plan-only`. Nếu muốn chạy proof đầy đủ, dùng `-Mode full -Force`.

Ví dụ chạy full cho KindredCoast:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\StorefrontBuilder\run-storefront-builder.ps1 -Mode full -Name kindredcoast -StoreKey kindredcoast -Url "https://www.kindredcoast.com/" -Force
```
