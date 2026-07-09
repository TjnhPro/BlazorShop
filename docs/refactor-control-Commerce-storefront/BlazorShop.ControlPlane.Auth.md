# BlazorShop ControlPlane Auth Plan

## Muc tieu

Thiet ke login/logout cho `BlazorShop.ControlPlane` theo huong nang cap, khong viet lai auth tu dau, khong pha logic legacy trong `BlazorShop.Presentation/BlazorShop.Web`.

ControlPlane la huong phat trien moi, nhung auth nen tan dung cac pattern da co:

- Identity va `IAuthenticationService` trong `BlazorShop.Application`.
- JWT + refresh token flow hien co.
- Token/session/browser storage helper trong `BlazorShop.Web.SharedV2`.
- Auth state, route guard, refresh-on-401 pattern da duoc chung minh trong `BlazorShop.Web`.

Khong tham chieu truc tiep `BlazorShop.Web` hoac `BlazorShop.Web.Shared` legacy tu ControlPlane. Logic UI/client state dung chung di qua `BlazorShop.Web.SharedV2` hoac adapter ControlPlane nho, ro rang.

## Quyet dinh kien truc

| Hang muc | Quyet dinh |
| --- | --- |
| UI | Blazor WASM cho `BlazorShop.ControlPlane.Web`. |
| Auth backend | Dung chung `BlazorShop.Application` auth service va ASP.NET Core Identity hien co. |
| Auth UI pattern | Dua theo `BlazorShop.Web/Authentication`, nhung dieu chinh route, text, layout, Tailwind va FontAwesome cho ControlPlane. |
| Operational DB | `ControlPlaneConnection` dung PostgreSQL tren port `5433`. |
| Auth/Identity DB | Khong dung ten mo ho `DefaultConnection` cho ControlPlane; dung `AuthConnection` cho `AppDbContext` va tro cung database PostgreSQL `blazorshop_controlplane` port `5433`. |
| Node auth | Khong lien quan den login/logout ControlPlane UI. Node API key la boundary rieng. |
| Legacy safety | `BlazorShop.Presentation/BlazorShop.Web` giu nguyen chay doc lap, khong bi sua route/global constant theo cach lam vo legacy. |

## DB topology can chot

Hien tai `BlazorShop.ControlPlane.API/appsettings.Development.json` dang co 2 connection string:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=blazorshop;Username=postgres;Password=postgres",
"ControlPlaneConnection": "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev"
```

`ControlPlaneConnection` da dung port `5433`, nhung auth van di qua `DefaultConnection` vi Identity duoc dang ky tu infrastructure cu. Neu local chi host PostgreSQL tren `5433`, login se fail/timeout khi app tim `5432`. Van de can sua la ten/config connection, khong phai tach them database moi.

Phuong an de xuat:

- Giu `ControlPlaneConnection` cho bang ControlPlane operational: nodes, stores, health, audit, credentials.
- Doi auth config sang `AuthConnection` de doc vao biet no phuc vu `AppDbContext`/Identity.
- `AuthConnection` va `ControlPlaneConnection` cung tro toi PostgreSQL `Host=localhost;Port=5433;Database=blazorshop_controlplane`.
- Giu 2 DbContext rieng:
  - `AppDbContext`: Identity/Auth tables nhu `AspNetUsers`, `AspNetRoles`, refresh token/user auth tables.
  - `ControlPlaneDbContext`: ControlPlane domain tables nhu nodes, stores, heartbeats, audit, credentials.
- Khong tao database `blazorshop_auth` trong phase nay vi auth chi them vai table va user muon database gon.
- Docker dev chi can dam bao database `blazorshop_controlplane` tren port `5433` co du migration cua ca 2 DbContext.

## Autoplan review summary

| Lens | Danh gia | Dieu chinh trong plan |
| --- | --- | --- |
| CEO/founder | Dung muc tieu nang cap, khong lam lai. Auth la enabling feature cho ControlPlane, khong phai product surface can sang tao lai. | Reuse-first, chi them adapter khi endpoint ControlPlane khac legacy. |
| Design | Login/logout can ro rang, it ma du thong tin; khong tao landing page. | Trang login truc tiep, layout gon, co error/loading/authenticated state. |
| Engineering | Rui ro lon nhat la copy auth provider sang V2 roi drift logic. | Tach logic session/auth state dung chung vao `BlazorShop.Web.SharedV2`, ControlPlane chi giu route/client rieng. |
| DX | Dev hay loi do sai DB port va cookie secure tren HTTP. | Phase rieng cho docker/config/seed admin va checklist loi thuong gap. |

## Kien truc muc tieu

```text
ControlPlane.Web (WASM)
  Login page / Logout action / Protected routes
  ControlPlane auth client
  Shared auth state + token/session helpers
          |
          v
ControlPlane.API
  api/control-plane/auth/login
  api/control-plane/auth/refresh-token
  api/control-plane/auth/logout
  api/control-plane/auth/me
          |
          +--> BlazorShop.Application IAuthenticationService
          |       |
          |       v
          |   Identity/AppDbContext via AuthConnection on port 5433
          |
          +--> ControlPlane profile/audit via ControlPlaneConnection on port 5433
```

## Phase 0 - Baseline va chot boundary

Muc tieu: xac nhan ro phan nao reuse, phan nao ControlPlane-specific.

Checklist:

- [ ] Ghi lai cac class auth pattern trong legacy:
  - `Login.razor`, `Login.razor.cs`
  - `Logout.razor`
  - `CustomAuthStateProvider`
  - `RefreshTokenHandler`
  - `BrowserCredentialsHandler`
  - `AuthenticationSessionRefresher`
  - `AuthenticationSessionBootstrapper`
  - `AuthenticationSessionSyncService`
  - `ProtectedRouteRedirectResolver`
- [ ] Ghi lai cac helper dung chung da o `BlazorShop.Web.SharedV2`:
  - `TokenService`
  - `HttpClientHelper`
  - browser storage/cookie services
  - auth DTOs nhu `LoginUser`, `LoginResponse`
- [ ] Xac nhan ControlPlane khong tham chieu truc tiep `BlazorShop.Web`.
- [ ] Xac nhan chi duoc reuse qua `BlazorShop.Web.SharedV2` hoac qua class moi trong `BlazorShop.ControlPlane.Web`.

Output:

- Danh sach file se move/extract.
- Danh sach file chi copy layout/UI co dieu chinh.
- Khong co code behavior moi trong phase nay.

## Phase 1 - Sua topology DB dev cho auth

Muc tieu: login ControlPlane khong con phu thuoc PostgreSQL `5432`, va config auth co ten ro rang.

Checklist:

- [ ] Cap nhat dev connection string cho `BlazorShop.ControlPlane.API`:

```json
"AuthConnection": "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev",
"ControlPlaneConnection": "Host=localhost;Port=5433;Database=blazorshop_controlplane;Username=blazorshop_controlplane;Password=blazorshop_controlplane_dev"
```

- [ ] Cap nhat infrastructure DI de `AppDbContext` uu tien `AuthConnection`, fallback `DefaultConnection` de khong pha legacy.
- [ ] Cap nhat design-time factory de migrations Identity co the doc `AuthConnection`.
- [ ] Cap nhat docker/runbook neu co noi nao con noi ControlPlane auth dung `5432`.
- [ ] Chay migration Identity/AppDbContext vao `blazorshop_controlplane`.
- [ ] Chay migration ControlPlaneDbContext vao `blazorshop_controlplane`.
- [ ] Seed admin toi thieu cho local dev neu chua co.
- [ ] Document lenh dev run trong `control-plane-runbook.md`.

Acceptance:

- API start khong tim `localhost:5432`.
- Login endpoint khong timeout khi PostgreSQL `5433` dang chay.
- `AuthConnection` va `ControlPlaneConnection` ro rang khac y nghia, du co the cung database.
- Legacy API van co the dung `DefaultConnection` qua fallback.

## Phase 2 - Tach/reuse auth client state vao shared

Muc tieu: khong duplicate logic session trong ControlPlane.

Checklist:

- [ ] Move hoac extract cac provider generic tu `BlazorShop.Web` sang `BlazorShop.Web.SharedV2`, neu khong phu thuoc UI legacy:
  - `CustomAuthStateProvider`
  - `RefreshTokenHandler`
  - `BrowserCredentialsHandler`
  - `AuthenticationStateNotifier`
  - `AuthenticatedClientStateCleaner`
  - `AuthenticationSessionRefresher`
  - `AuthenticationSessionBootstrapper`
  - `AuthenticationSessionEventPublisher`
  - `AuthenticationSessionSyncService`
- [ ] Neu class nao dang phu thuoc legacy route/constant, doi sang options:
  - token storage key
  - login route
  - refresh endpoint strategy
  - logout cleanup behavior
- [ ] Khong sua `Constant.Authentication.Login` legacy thanh ControlPlane route vi se lam vo `BlazorShop.Web`.
- [ ] Them ControlPlane-specific auth route constants rieng, vi du:

```csharp
public static class ControlPlaneAuthenticationRoutes
{
    public const string Login = "control-plane/auth/login";
    public const string RefreshToken = "control-plane/auth/refresh-token";
    public const string Logout = "control-plane/auth/logout";
    public const string Me = "control-plane/auth/me";
}
```

Acceptance:

- Legacy Web build van pass.
- ControlPlane Web co the register shared auth state provider ma khong reference project legacy.
- Cac endpoint legacy va ControlPlane khong dung chung hard-coded route sai ngu canh.

## Phase 3 - ControlPlane auth API contract

Muc tieu: backend endpoint hien co du dung cho WASM login/logout.

Hien co:

- `POST api/control-plane/auth/login`
- `POST api/control-plane/auth/refresh-token`
- `POST api/control-plane/auth/logout`
- `GET api/control-plane/auth/me`
- Refresh token duoc tra qua HttpOnly cookie `__Host-blazorshop-controlplane-refresh`.
- Audit da ghi `auth.login`, `auth.refresh`, `auth.logout`.

Checklist:

- [ ] Kiem tra `Secure = true` cua refresh cookie co phu hop local HTTP hay khong.
- [ ] Neu dev dung `http://localhost:5281`, can co cach dev-friendly:
  - chay HTTPS cho ControlPlane Web/API, hoac
  - conditional cookie secure theo environment, neu chap nhan tradeoff local only.
- [ ] Dam bao CORS allow origin ControlPlane Web.
- [ ] Dam bao private client gui credentials/cookie khi refresh-token.
- [ ] Dam bao login response khong leak refresh token neu dung HttpOnly cookie.
- [ ] Dam bao `me` tra profile du de layout hien actor.

Acceptance:

- Login thanh cong tao JWT trong session storage.
- Refresh thanh cong dua tren cookie.
- Logout xoa cookie server side va xoa local token.
- Audit co log success/failure cho auth.

## Phase 4 - ControlPlane Web registration

Muc tieu: ControlPlane Web co day du DI auth nhu legacy, nhung base URL va route rieng.

Checklist:

- [ ] Dang ky `AddCascadingAuthenticationState()`.
- [ ] Dang ky `AddAuthorizationCore()`.
- [ ] Dang ky `AuthenticationStateProvider`.
- [ ] Dang ky notifier/session refresher/bootstrapper/sync cleaner.
- [ ] Dang ky `BrowserCredentialsHandler` cho public client neu can cookie.
- [ ] Dang ky `RefreshTokenHandler` cho private client.
- [ ] Dang ky `IControlPlaneAuthenticationClient` hoac adapter tu shared auth service voi route ControlPlane.
- [ ] Sau `builder.Build()`, goi `IAuthenticationSessionBootstrapper.RestoreAsync()` truoc `RunAsync()`.

Acceptance:

- Refresh F5 khong mat auth state neu token con hop le.
- Private API call gap 401 co thu refresh mot lan cho request GET hoac request an toan.
- Khong co service registration duplicated/khong dung.

## Phase 5 - Login/logout UI

Muc tieu: tao UI auth dung pattern cu nhung phu hop ControlPlane.

Checklist:

- [ ] Tao `/login` hoac `/authentication/login` cho ControlPlane. Uu tien route ngan: `/login`.
- [ ] Login form dung `LoginUser` DTO co san.
- [ ] Co loading state, validation, error message khi sai password.
- [ ] Neu da login, redirect ve dashboard hoac route dang bi block.
- [ ] Tao logout action/page, vi du `/logout`, goi API logout roi clear local state.
- [ ] Layout hien actor/email va nut logout.
- [ ] Dung Tailwind va FontAwesome theo UI ControlPlane.
- [ ] Khong them register flow neu ControlPlane admin duoc seed/quan ly rieng.

Acceptance:

- Sai password hien loi, khong redirect.
- Dung password redirect dashboard.
- Logout quay ve login va protected page bi chan.

## Phase 6 - Route protection va permission-aware UX

Muc tieu: toan bo ControlPlane UI mac dinh yeu cau auth.

Checklist:

- [ ] Doi `Routes.razor` tu `RouteView` sang `AuthorizeRouteView`.
- [ ] Them `NotAuthorized` redirect ve login, giu return URL.
- [ ] Cac page Dashboard, Nodes, Stores, Health, Audit yeu cau `[Authorize]`.
- [ ] Neu co permission/role, an disable action khong co quyen thay vi chi de API tra 403.
- [ ] Chua can RBAC phuc tap neu phase auth chi can admin toi thieu.

Acceptance:

- Truy cap dashboard khi chua login se ve login.
- Login xong quay lai URL ban dau.
- 401/403 co UX ro rang.

## Phase 7 - User/admin management baseline

Muc tieu: dap ung dashboard toi thieu va QA auth/user ve sau ma khong rebuild Identity.

Checklist:

- [ ] Xac dinh co dung admin/user management legacy service nao duoc khong.
- [ ] Neu can UI ControlPlane user:
  - list users
  - create/assign user
  - enable/disable user
  - assign/remove permission
- [ ] Neu legacy admin user service phu hop, tao ControlPlane adapter endpoint thay vi copy logic.
- [ ] Neu permission hien chua co model ro, dung role `Admin` truoc va document extension point.

Acceptance:

- Co it nhat 1 admin seeded dang nhap duoc.
- User disabled khong dang nhap duoc.
- Permission missing co 403/UX phu hop khi feature duoc bat.

## Phase 8 - QA va regression gates

Muc tieu: moi thay doi auth deu co checklist ro trong `QA-ControlPlane.todo.md`.

Checklist bo sung:

- [ ] Auth login dung credential hop le.
- [ ] Auth login sai password.
- [ ] Auth login sai nhieu lan, xac nhan lockout/rate limit neu co cau hinh.
- [ ] Refresh token sau reload.
- [ ] Logout xoa token/cookie state.
- [ ] Truy cap protected route khi anonymous.
- [ ] Truy cap protected route khi token het han.
- [ ] Audit co log `auth.login`, `auth.refresh`, `auth.logout`.
- [ ] API khong con phu thuoc `localhost:5432` trong local ControlPlane dev.

Automated checks nen co:

- `dotnet build` cho ControlPlane API/Web va shared project lien quan.
- Integration test cho `ControlPlaneAuthController` neu test harness san sang.
- Browser QA login/logout bang Playwright/qa skill.

## Phase 9 - Hardening sau MVP

Muc tieu: dua auth tu dev-ready len production-ready.

Checklist:

- [ ] Bat HTTPS dev/prod de dung Secure HttpOnly cookie dung cach.
- [ ] Xem lai SameSite cookie neu API/Web khac origin.
- [ ] Them rate limit rieng cho login/refresh.
- [ ] Them lockout policy neu chua bat.
- [ ] Them audit metadata cho failure reason o muc an toan, khong leak password/user enumeration.
- [ ] Them health check cho `AuthConnection` va `ControlPlaneConnection` rieng.
- [ ] Them config validation khi ControlPlane auth van fallback ve `DefaultConnection` `5432` trong dev.

## Khong lam trong plan nay

- Khong rewrite Identity.
- Khong viet auth provider moi tu dau neu provider cu co the extract.
- Khong tron node API key voi admin login.
- Khong sua legacy Web route/global constants theo ControlPlane.
- Khong remove `BlazorShop.Presentation` trong phase auth.

## Definition of done

- ControlPlane Web co login/logout that su chay.
- Protected pages khong truy cap duoc khi anonymous.
- Auth dung `AuthConnection` PostgreSQL port `5433` trong local dev.
- ControlPlane operational data van dung `ControlPlaneConnection` port `5433`.
- Legacy `BlazorShop.Web` van build va van dung endpoint auth cu.
- Reuse logic nam o shared/adapters, khong copy paste class auth legacy sang V2 roi tach drift.
- QA checklist auth trong `QA-ControlPlane.todo.md` duoc cap nhat khi implement.
