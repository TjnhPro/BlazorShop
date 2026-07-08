# BlazorShop Commerce Node Foundation Todo

## Muc tieu

Xay dung foundation cho Commerce Node theo huong nang cap, khong refactor legacy `BlazorShop.Presentation`.
Commerce Node la boundary moi nam trong `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.

MVP nay chi tap trung vao viec Control Plane co the dang ky node, luu credential node, va probe node qua API `api/commerce/*`.
Chua trien khai heartbeat worker, capability snapshot sender, store assignment sync, action polling, action result reporting.

## Nguyen tac kien truc

- Giu Layered Architecture hien co: Domain entity, Application DTO/service contract, Infrastructure EF/service/client, API/Web chi orchestration.
- Khong doi ten project legacy va khong refactor `BlazorShop.Presentation`.
- Control Plane chu dong goi Commerce Node. Commerce Node khong phu thuoc Control Plane trong MVP.
- Commerce Node API dung prefix `api/commerce/*`.
- Response pattern dung envelope da chuan hoa: `success`, `message`, `data`.
- HTTP status van co y nghia cho Web client/typed client; UI doc `Success` va `Message` de hien thi loi.
- Credential MVP don gian: `node_key`, `node_secret`, allow IP. Chua ma hoa secret trong phase nay.
- Khong log raw secret, khong tra raw secret ra list/detail API sau khi luu.

## Autoplan danh gia nhanh

### Product

Huong nay phu hop hon so voi viec dua worker/sync vao ngay phase dau. Gia tri can co truoc la Control Plane nhin thay node co song hay khong, va node co duong API doc lap de phat trien tiep store/order fetch.

### Engineering

Can tach ro 2 loai credential:

- `commerce_node_credential` hien tai luu `secret_hash`, phu hop voi flow generate/rotate mot lan reveal.
- MVP moi can Control Plane gui secret goc sang Commerce Node de probe `api/commerce/healthz`.

Vi vay khong nen ep dung bang credential hash hien co cho MVP nay. Them truong `node_secret` vao `commerce_node` la cach it code va ro nghia nhat hien tai. Day la quyet dinh MVP, khong phai thiet ke bao mat cuoi cung.

### DX

Developer/deploy operator chi can:

1. Deploy Commerce Node len VPS.
2. Set env `CommerceNode__NodeKey`, `CommerceNode__NodeSecret`, `CommerceNode__AllowedControlPlaneIps__0`.
3. Vao Control Plane tao/edit node voi cung `node_key`, `node_secret`, `control_api_url`.
4. Bam probe node va doc trang thai.

## So do runtime

```text
Control Plane Web
  -> Control Plane API
    -> ControlPlaneDbContext
      -> commerce_node + commerce_node_endpoint + node_health_snapshot
    -> CommerceNodeControlClient
      -> GET {control_api_url}/api/commerce/healthz
         Headers:
           X-Node-Key: <node_key>
           X-Node-Secret: <node_secret>

Commerce Node API
  -> IP allowlist middleware
  -> node key/secret middleware
  -> api/commerce/healthz
```

## Database design

### Database pham vi

Commerce Node dung database rieng cho nghiep vu ecom. MVP `healthz` chua can doc DB, nhung foundation van phai tao boundary DB rieng de cac phase store/order/product sau khong quay lai dung `AppDbContext` legacy.

Control Plane database chi luu registry/secret/health cua node. Commerce Node database luu nghiep vu ecom cua node.

Control Plane database hien co:

- `commerce_node`
- `commerce_node_endpoint`
- `commerce_node_credential`
- `node_health_snapshot`
- `node_capability_snapshot`

Commerce Node database foundation:

- Connection string rieng: `CommerceNodeConnection`
- DbContext rieng: `CommerceNodeDbContext`
- Tai su dung entity/configuration ecom hien co khi phu hop.
- Khong keo `AppDbContext` vao Commerce Node vi `AppDbContext` gom ca Identity/auth va legacy storefront/admin concerns.

### Commerce Node DB MVP

MVP nay chi can tao context boundary va connection validation. Chua bat buoc tao migration ecom neu chua co table moi.

DbSet nen bat dau tu cac entity ecom legacy can phuc vu node:

- `Category`
- `Product`
- `ProductVariant`
- `Order`
- `OrderLine`
- `OrderItem`
- `PaymentMethod`
- `NewsletterSubscriber`
- `SeoSettings`
- `SeoRedirect`

Khong dua `AppUser`, `RefreshToken`, Control Plane auth tables, hoac Control Plane registry tables vao `CommerceNodeDbContext`.

### Thay doi bang `commerce_node`

Them cac cot:

| Column | Type | Nullable | Muc dich |
|---|---:|---:|---|
| `node_secret` | `text` | yes | Secret plain text MVP de Control Plane goi Commerce Node. Khong tra ra list/detail. |
| `node_secret_updated_at` | `timestamp with time zone` | yes | Thoi diem tao/cap nhat secret. |

Ghi chu:

- `node_secret` nullable de migration khong pha node cu.
- Khi tao node moi cho Commerce Node MVP, service validate bat buoc co `node_secret`.
- Khi update node, neu user de trong secret thi giu secret cu; neu nhap secret moi thi cap nhat va set `node_secret_updated_at`.
- `commerce_node_credential` giu nguyen de khong pha code credential/rotate hien co, nhung khong dung cho probe MVP.
- `AllowedControlPlaneIps` khong luu DB trong MVP; day la env o Commerce Node side.

### Bang `commerce_node_endpoint`

Giu nguyen.

`kind = 'control_api'` se tro den base URL cua Commerce Node, vi Control Plane la ben probe node.
Vi du:

```text
https://node-01.example.com
```

Client se tu append:

```text
/api/commerce/healthz
```

### Bang `node_health_snapshot`

Giu nguyen va tiep tuc dung de luu ket qua probe.

Mapping de xuat:

| Probe result | `node_health_snapshot.status` | `commerce_node.status` |
|---|---|---|
| 200 + `success=true` + data status healthy | `healthy` | `healthy` |
| 200 + `success=true` + data status warning | `warning` | `warning` |
| 401 | `down` | `down` |
| 403 | `down` | `down` |
| timeout | `timeout` | `down` |
| invalid JSON/envelope | `malformed` | `down` |
| exception | `down` | `down` |

`DependencyStatusJson` co the luu `data.dependencies` neu Commerce Node tra ve. MVP co the de `null`.

### EF migration

Ten migration de xuat:

```text
ControlPlaneCommerceNodeMvpCredential
```

Thay doi entity:

- `CommerceNode.NodeSecret`
- `CommerceNode.NodeSecretUpdatedAt`

Thay doi DTO:

- `CreateControlPlaneNodeRequest.NodeSecret`
- `UpdateControlPlaneNodeRequest.NodeSecret`
- `ControlPlaneNodeSummary.HasNodeSecret`
- `ControlPlaneNodeSummary.NodeSecretUpdatedAt`
- `ControlPlaneNodeDetail.HasNodeSecret`
- `ControlPlaneNodeDetail.NodeSecretUpdatedAt`

Khong them `NodeSecret` vao summary/detail response.

## Commerce Node API design

### Project

Tao project moi:

```text
BlazorShop.PresentationV2/BlazorShop.CommerceNode.API
```

Loai project:

```text
ASP.NET Core Web API
```

Project reference de xuat:

- `BlazorShop.Application` neu can dung shared response contracts.
- `BlazorShop.Domain` chi khi can model/domain constant.
- `BlazorShop.Web.Shared` neu co helper response/options/logging dung duoc ma khong keo dependency Web UI khong can thiet.

Khong reference `BlazorShop.Presentation`.

### Configuration

Options section:

```json
{
  "CommerceNode": {
    "NodeKey": "node-01",
    "NodeSecret": "dev-secret",
    "AllowedControlPlaneIps": [ "127.0.0.1", "::1" ]
  }
}
```

Env khi deploy VPS:

```text
CommerceNode__NodeKey=node-01
CommerceNode__NodeSecret=<secret>
CommerceNode__AllowedControlPlaneIps__0=<control-plane-public-ip>
```

Options class:

```csharp
public sealed class CommerceNodeOptions
{
    public string NodeKey { get; set; } = string.Empty;
    public string NodeSecret { get; set; } = string.Empty;
    public string[] AllowedControlPlaneIps { get; set; } = [];
}
```

Validation:

- `NodeKey` bat buoc.
- `NodeSecret` bat buoc.
- `AllowedControlPlaneIps` bat buoc trong Production.
- Development co the cho phep `127.0.0.1` va `::1`.

### Headers

Control Plane goi Commerce Node voi:

```text
X-Node-Key: <node_key>
X-Node-Secret: <node_secret>
```

Middleware validate:

1. Lay remote IP tu `HttpContext.Connection.RemoteIpAddress`.
2. Neu co allowlist va IP khong nam trong danh sach thi tra `403`.
3. Validate `X-Node-Key` bang `CommerceNode:NodeKey`.
4. Validate `X-Node-Secret` bang `CommerceNode:NodeSecret`.
5. Sai/missing header tra `401`.

MVP chi can exact IP match. CIDR/range de phase hardening neu can.

### Response envelope

Thanh cong:

```json
{
  "success": true,
  "message": "Commerce Node is healthy.",
  "data": {
    "nodeKey": "node-01",
    "status": "healthy",
    "checkedAt": "2026-07-08T00:00:00Z",
    "version": "1.0.0",
    "environment": "Production"
  }
}
```

Credential sai:

```json
{
  "success": false,
  "message": "Invalid Commerce Node credential.",
  "data": null
}
```

IP khong duoc phep:

```json
{
  "success": false,
  "message": "Control Plane IP is not allowed.",
  "data": null
}
```

### Endpoint MVP

| Method | Route | Auth | Status | Muc dich |
|---|---|---|---|---|
| GET | `/api/commerce/healthz` | Node key/secret + allow IP | 200/401/403/500 | Kiem tra node song va credential dung |

`healthz` chua can goi database. Chi can tra runtime status va metadata toi thieu.

### Route reserve cho phase sau

Chua trien khai trong MVP, chi giu convention:

| Method | Route | Phase sau |
|---|---|---|
| GET | `/api/commerce/stores` | Control Plane fetch store truc tiep tu node |
| GET | `/api/commerce/stores/{storeId}` | Store status/detail |
| GET | `/api/commerce/orders` | Order query |
| GET | `/api/commerce/orders/{orderId}` | Order detail |

## Control Plane API/Web changes

### Node create/edit

Cap nhat create node:

- Nhap `NodeKey`
- Nhap `NodeSecret`
- Nhap `Name`
- Nhap `ControlApiUrl`
- Nhap `Description`

Cap nhat edit node:

- Cho sua `Name`
- Cho sua `Description`
- Cho sua `ControlApiUrl`
- Cho cap nhat `NodeSecret`
- Neu secret input de trong thi giu secret cu

UI khong hien raw secret sau khi save. Chi hien:

- `Has secret: Yes/No`
- `Secret updated at`

### Probe flow

`ControlPlaneHealthService.ProbeAsync` giu vai tro hien tai, nhung `CommerceNodeControlClient` can:

- Goi `{controlApiBaseUrl}/api/commerce/healthz`
- Gui `X-Node-Key` va `X-Node-Secret`
- Parse envelope `{ success, message, data }`
- Map `data.status` sang health status
- Luu `node_health_snapshot`

Can doi interface client tu:

```csharp
ProbeAsync(string controlApiBaseUrl, CancellationToken cancellationToken)
```

thanh:

```csharp
ProbeAsync(
    string controlApiBaseUrl,
    string nodeKey,
    string nodeSecret,
    CancellationToken cancellationToken)
```

Neu `node_secret` null/empty thi service tra validation:

```text
Node does not have a Commerce Node secret configured.
```

### Audit log

Ghi audit cho:

- Create node co secret
- Update node secret
- Probe node
- Disable node

Khong ghi raw secret vao audit payload.

## Phase plan

### Phase 0 - Baseline va scope lock

- [ ] Doc lai `CommerceNode`, `CommerceNodeEndpoint`, `CommerceNodeCredential`, `NodeHealthSnapshot`.
- [ ] Xac nhan `commerce_node_credential` hash khong dung cho probe MVP.
- [ ] Xac nhan route prefix Commerce Node la `api/commerce/*`.
- [ ] Xac nhan cac task sau out of scope: heartbeat worker, capability snapshot sender, store assignment sync, action polling, action result reporting.

Commit de xuat:

```text
docs(commerce-node): add foundation plan
```

### Phase 1 - Commerce Node API shell

- [ ] Tao folder `BlazorShop.PresentationV2`.
- [ ] Tao project `BlazorShop.CommerceNode.API`.
- [ ] Add project vao solution.
- [ ] Cau hinh launch/appsettings toi thieu.
- [ ] Add DI skeleton theo pattern hien co.
- [ ] Add `CommerceNodeConnection` placeholder cho DB ecom rieng.
- [ ] Bao dam project build doc lap.

Commit de xuat:

```text
feat(commerce-node): add api shell
```

### Phase 2 - Commerce Node options va response envelope

- [ ] Tao `CommerceNodeOptions`.
- [ ] Bind `CommerceNode` configuration section.
- [ ] Validate `NodeKey`, `NodeSecret`, `AllowedControlPlaneIps`.
- [ ] Tao `CommerceNodeDbContext` rieng cho ecom data boundary, khong dung `AppDbContext`.
- [ ] Dang ky `CommerceNodeDbContext` bang `CommerceNodeConnection`.
- [ ] Tai su dung response envelope pattern hien co neu phu hop.
- [ ] Neu helper trong `BlazorShop.Web.Shared` co the dung ma khong tao dependency xau, thi reference va dung lai.
- [ ] Neu `BlazorShop.Web.Shared` keo UI/web dependency khong phu hop API runtime, tao helper nho trong Commerce Node va ghi ro ly do.

Commit de xuat:

```text
feat(commerce-node): add options and api response envelope
```

### Phase 3 - Commerce Node credential/IP guard

- [ ] Tao middleware/action filter validate remote IP.
- [ ] Tao middleware/action filter validate `X-Node-Key`.
- [ ] Tao middleware/action filter validate `X-Node-Secret`.
- [ ] Dung constant-time compare cho secret neu co the.
- [ ] Tra envelope loi voi HTTP 401 khi credential sai.
- [ ] Tra envelope loi voi HTTP 403 khi IP khong duoc phep.
- [ ] Khong log raw secret.

Commit de xuat:

```text
feat(commerce-node): protect commerce api with node credentials
```

### Phase 4 - Commerce healthz endpoint

- [ ] Tao `GET /api/commerce/healthz`.
- [ ] Tra envelope success.
- [ ] Data gom `nodeKey`, `status`, `checkedAt`, `version`, `environment`.
- [ ] Khong can database.
- [ ] Kiem thu endpoint voi valid header.
- [ ] Kiem thu endpoint voi missing/wrong header.

Commit de xuat:

```text
feat(commerce-node): add authenticated health endpoint
```

### Phase 5 - Control Plane database migration

- [ ] Them `NodeSecret` vao `CommerceNode`.
- [ ] Them `NodeSecretUpdatedAt` vao `CommerceNode`.
- [ ] Map EF columns `node_secret`, `node_secret_updated_at`.
- [ ] Tao migration `ControlPlaneCommerceNodeMvpCredential`.
- [ ] Cap nhat model snapshot.
- [ ] Dam bao migration khong anh huong auth tables va legacy commerce tables.

Commit de xuat:

```text
feat(control-plane): store commerce node mvp secret
```

### Phase 6 - Control Plane node API/UI update

- [ ] Cap nhat create request co `NodeSecret`.
- [ ] Cap nhat update request co optional `NodeSecret`.
- [ ] Validate create bat buoc co secret.
- [ ] Update secret chi khi input khong rong.
- [ ] Summary/detail tra `HasNodeSecret`, `NodeSecretUpdatedAt`.
- [ ] UI create node them secret input.
- [ ] UI edit node them secret input dang optional.
- [ ] UI khong hien raw secret.
- [ ] Audit log create/update secret khong ghi raw secret.

Commit de xuat:

```text
feat(control-plane): configure commerce node secrets
```

### Phase 7 - Control Plane probe client update

- [ ] Doi `ICommerceNodeControlClient.ProbeAsync` nhan `nodeKey`, `nodeSecret`.
- [ ] Doi client goi `/api/commerce/healthz` thay vi `/health`.
- [ ] Gui headers `X-Node-Key`, `X-Node-Secret`.
- [ ] Parse response envelope.
- [ ] Map HTTP 401/403 thanh health snapshot `down` voi error code ro rang.
- [ ] Map malformed envelope thanh `malformed`.
- [ ] Bo doc `/capabilities` trong MVP nay, hoac de optional neu endpoint khong ton tai thi khong warning.
- [ ] Cap nhat `ControlPlaneHealthService.ProbeAsync` truyen secret tu node.

Commit de xuat:

```text
feat(control-plane): probe commerce node healthz endpoint
```

### Phase 8 - QA va documentation

- [ ] Cap nhat `QA-ControlPlane.todo.md` them Commerce Node section.
- [ ] Test clean DB tren PostgreSQL port `5433`.
- [ ] Test create node voi secret.
- [ ] Test edit node giu secret khi input empty.
- [ ] Test edit node doi secret.
- [ ] Test probe success voi Commerce Node local.
- [ ] Test wrong secret tra 401 va Control Plane luu health down.
- [ ] Test disallowed IP tra 403 neu co the reproduce local.
- [ ] Test disabled node khong probe.
- [ ] Test secret khong xuat hien trong list/detail response va UI.
- [ ] Build/test solution.

Commit de xuat:

```text
test(commerce-node): verify mvp health probe flow
```

## Cau hoi can chot truoc khi implement

1. `AllowedControlPlaneIps` MVP chi exact IP match hay can CIDR ngay tu dau?
   - De xuat: exact IP match truoc, CIDR de hardening.
2. Khi update node, secret input rong se giu secret cu hay clear secret?
   - De xuat: rong la giu secret cu; muon clear can action rieng.
3. Co can an/vo hieu hoa UI credential rotate hash hien co cho Commerce Node MVP khong?
   - De xuat: giu nguyen neu dang dung, nhung label/plan can noi ro no chua phuc vu `api/commerce/healthz`.
4. `ControlApiUrl` nen luu base URL hay full healthz URL?
   - De xuat: luu base URL, client tu append `/api/commerce/healthz`.

## Definition of Done

- Commerce Node API project build duoc va chay doc lap.
- `GET /api/commerce/healthz` dung prefix `api/commerce/*`, co envelope, co credential/IP guard.
- Control Plane tao/edit node co node secret nhung khong expose raw secret.
- Control Plane probe goi dung Commerce Node endpoint voi headers.
- Health snapshot luu duoc ket qua success/failure.
- QA checklist duoc cap nhat va cac muc MVP duoc verify.

## Decision Audit Trail

| # | Decision | Classification | Rationale | Rejected |
|---|---|---|---|---|
| 1 | Tao project moi `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API` | Auto-decided | Giu legacy Presentation bat dong va tao boundary moi cho v2. | Refactor/rename `BlazorShop.Presentation`. |
| 2 | Control Plane chu dong goi Commerce Node | User-approved | Phu hop deploy flow VPS: set env node, sau do Control Plane tao/edit node va ping healthz. | Commerce Node heartbeat worker trong MVP. |
| 3 | Dung `/api/commerce/healthz` lam endpoint MVP | User-approved | Du de xac minh node song, credential dung, network thong. | Capability/store/action APIs trong phase nay. |
| 4 | Them `node_secret` vao `commerce_node` cho MVP | Engineering decision | Bang credential hash hien co khong cho Control Plane lay secret goc de goi node. | Dung `commerce_node_credential.secret_hash` cho probe. |
| 5 | Allow IP nam o Commerce Node env, khong luu DB | Engineering decision | Allowlist la cau hinh runtime cua node, khong can Control Plane DB trong MVP. | Tao bang allowlist trong Control Plane ngay phase dau. |
