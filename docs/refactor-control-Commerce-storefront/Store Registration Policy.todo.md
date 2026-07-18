# Store Registration Policy

Status: in progress
Date: 2026-07-18
Purpose: cho phép Control Plane khoa/mo dang ky tai khoan customer theo tung store, va dam bao storefront khong cho submit khi register disabled.

## Codebase Baseline

- Commerce Node Storefront API da co endpoint `POST api/storefront/stores/{storeKey}/auth/register` va `GET api/storefront/stores/{storeKey}/auth/registration-policy`.
- `StorefrontScopedAuthController` hien chan register server-side bang `Runtime:Security:RegistrationMode`; neu disabled thi tra `403` voi code `auth.registration_disabled`.
- `registration-policy` hien tra `mode` va `registrationAllowed`, nhung nguon policy dang la runtime option cua `BlazorShop.CommerceNode.API`, khong phai setting theo store.
- Commerce Node da co `StoreSecurityPrivacySettings`, `IStoreSecurityPrivacySettingsService`, `CommerceSecurityPrivacyController`, audit, cache invalidation va Control Plane API gateway cho `security-privacy`.
- Control Plane API da co route `api/controlplane/commerce/stores/{storePublicId}/security-privacy` GET/PUT va policy `CommerceSecurityPrivacyRead/Write`.
- Control Plane Web chua co page/menu de admin cau hinh Security/Privacy settings.
- Storefront V2 register page hien render form SSR tai `/register`, submit qua local POST `/register`, sau do server goi `IStorefrontAuthClient.RegisterAsync`.
- Storefront V2 `StorefrontAuthClient` chua co method doc `registration-policy`, nen UI chua biet disable form truoc khi submit.
- `Storefront Playwright E2E Release.todo.md` dang account/defer `AUTH-002 P0` vi fixture/config registration-disabled chua duoc chuan hoa.

## Autoplan Decisions

| Decision | Chon | Ly do |
| --- | --- | --- |
| Policy owner | `StoreSecurityPrivacySettings` trong Commerce Node DB | Day la setting theo store, da co Control Plane gateway, permission, audit va public config invalidation. |
| Runtime option fallback | Giu `Runtime:Security:RegistrationMode` chi lam default/fallback | Khong pha deployment hien co, nhung Control Plane setting phai la nguon chinh khi row store da ton tai. |
| Scope MVP | `standard` va `disabled` | Dung voi nhu cau khoa register; khong nhoi admin-approval/invite-only/waitlist trong phase nay. |
| Enforcement | Chan o Commerce Node API va UI Storefront | UI giup UX dung, API la boundary bao mat that. |
| Admin UI | Them Control Plane Web page `commerce-admin/security-privacy` | Pattern giong currencies/payment/pages: chon store, load setting, edit, save qua Control Plane API. |

## Target Behavior

- Admin Control Plane co the chon store va set registration mode:
  - `standard`: storefront hien form va cho submit.
  - `disabled`: storefront hien thong bao dang ky tam dong, khong hien/khong enable submit tao account.
- Neu browser hoac script goi truc tiep `POST /auth/register` khi disabled, Commerce Node tra typed error `403 auth.registration_disabled` va khong tao user.
- Policy la store-scoped: Store A disabled khong anh huong Store B.
- Default cua store hien tai van la `standard` de khong lam hong luong dang ky dang co.

## Phase 0 - Confirm Contract And Data Shape

- [x] Ra soat lai `StorefrontScopedAuthController`, `StorefrontApiContracts`, `StorefrontAuthClient`, `RegisterPage.razor`, `Program.cs` local POST `/register`. 2026-07-18: controller currently reads runtime option directly; Storefront Auth client has no policy method; register page/local POST cannot gate before submit.
- [x] Ra soat `StoreSecurityPrivacySettingsService` de xac dinh noi them field, mapping, validation, audit metadata va cache invalidation. 2026-07-18: add field to DTO/request/entity/EF mapping/service apply/validate/audit/default runtime projection; service already invalidates public config cache.
- [x] Ra soat Control Plane Web client pattern trong `ControlPlaneCatalogClient` va cac page `CommerceCurrencies.razor`, `CommercePaymentMethods.razor`. 2026-07-18: Web still uses `IControlPlaneCatalogClient`; security/privacy gateway methods already exist in Control Plane API/service but no Web page/nav exists.
- [x] Xac nhan OpenAPI snapshot hien co cua `StorefrontAuth_GetRegistrationPolicy` va `StorefrontAuth_Register`. 2026-07-18: both operation IDs exist in `storefront-openapi.snapshot.json` and metadata is defined in `CommerceNodeSwaggerExtensions.cs`.

Acceptance:

- [x] Co danh sach file can sua, khong dung legacy `BlazorShop.Presentation/*`. 2026-07-18: active files are Application/Domain/Infrastructure CommerceNode, CommerceNode API, ControlPlane Web/API, Storefront V2, and focused tests/docs only.
- [x] Khong them route `api/internal/*`, `api/admin/*`, `api/public/*`. 2026-07-18: planned changes stay on `api/storefront/stores/{storeKey}/*`, `api/commerce/admin/security-privacy`, and existing Control Plane gateway routes.

## Phase 1 - Commerce Node Store-Scoped Registration Setting

- [x] Them model setting vao `BlazorShop.Application.CommerceNode.SecurityPrivacy`:
  - `StoreRegistrationAdminSettingsDto`
  - `Registration` property trong `StoreSecurityPrivacySettingsDto`
  - `Registration` property trong `UpdateStoreSecurityPrivacySettingsRequest`
  - runtime projection co `RegistrationMode` hoac `RegistrationAllowed`.
  2026-07-18 Phase 1: added `StoreRegistrationAdminSettingsDto` and `StoreRegistrationRuntimeSettings`.
- [x] Them property vao domain entity `StoreSecurityPrivacySettings`:
  - `RegistrationMode` string, default `"standard"`, max length hop ly.
  2026-07-18 Phase 1: added `RegistrationMode = "standard"`.
- [x] Update `CommerceNodeDbContext` mapping:
  - column `registration_mode`
  - default `"standard"`
  - max length, required.
  2026-07-18 Phase 1: mapping uses max length 32, required, default `standard`.
- [x] Tao EF migration cho `CommerceNodeDbContext`. 2026-07-18 Phase 1: migration `20260718123244_CommerceNodeStoreRegistrationMode` only adds/drops `registration_mode` on `store_security_privacy_settings`.
- [x] Update `StoreSecurityPrivacySettingsService`:
  - default entity lay registration mode tu `Runtime:Security:RegistrationMode` neu chua co row.
  - map DTO admin.
  - validate chi chap nhan `"standard"` hoac `"disabled"`.
  - apply update.
  - audit metadata co `RegistrationMode`.
  - invalidate public config cache nhu setting hien tai.
  2026-07-18 Phase 1: service maps/applies/validates registration mode and includes it in audit metadata; `SecurityPrivacyOptions.DefaultRegistrationMode` is wired from `Runtime:Security:RegistrationMode`.
- [x] Update development seeder de seed `RegistrationMode = "standard"` cho test store. 2026-07-18 Phase 1: development QA seed sets `standard`.

Acceptance:

- [x] Store moi khong bi khoa dang ky ngoai y muon. 2026-07-18 Phase 1: entity/migration/default service mode is `standard`.
- [x] Update setting validation tra loi ro rang khi mode invalid. 2026-07-18 Phase 1: invalid mode returns `Registration mode must be either standard or disabled.`
- [x] Migration khong dung `AppDbContext` hoac `ControlPlaneDbContext`. 2026-07-18 Phase 1: migration generated for `CommerceNodeDbContext`; CommerceNode API build passed.

## Phase 2 - Storefront API Enforcement From Store Setting

- [x] Doi `StorefrontScopedAuthController` de resolve registration mode tu `IStoreSecurityPrivacySettingsService.ResolveCurrentAsync()`. 2026-07-18 Phase 2: controller registration policy now comes from security/privacy runtime settings.
- [x] Giu runtime option `Runtime:Security:RegistrationMode` lam fallback trong service/defaults, khong doc truc tiep trong controller cho policy chinh. 2026-07-18 Phase 2: controller no longer reads `RegistrationMode`; Infrastructure wires the runtime option into `SecurityPrivacyOptions.DefaultRegistrationMode`.
- [x] Doi `Register` thanh async policy check:
  - resolve current settings.
  - neu disabled, return `403 auth.registration_disabled` truoc captcha va truoc `CreateUser`.
  - neu standard, tiep tuc captcha va `CreateUser` nhu hien tai.
  2026-07-18 Phase 2: `Register` checks policy before captcha and registration service call.
- [x] Doi `GetRegistrationPolicy` thanh async va tra policy theo store setting. 2026-07-18 Phase 2: endpoint returns async store-scoped policy.
- [x] Neu can, mo rong `StorefrontRegistrationPolicyResponse` them `message` an toan cho storefront UI; giu `mode` va `registrationAllowed` de khong pha client hien co. 2026-07-18 Phase 2: response now includes safe `message`.
- [x] Cap nhat Swagger metadata/snapshot neu response schema thay doi. 2026-07-18 Phase 2: Storefront OpenAPI snapshot and contract assertion include `message`.

Acceptance:

- [x] Direct API register khi disabled tra `403 auth.registration_disabled`. 2026-07-18 Phase 2: `CommerceNodeStorefrontAuthContractTests` passed.
- [x] Direct API register khi enabled van theo luong hien tai. 2026-07-18 Phase 2: enabled path still proceeds after policy check; CommerceNode API build passed.
- [x] Policy endpoint khong can auth, store-scoped theo route `{storeKey}`. 2026-07-18 Phase 2: endpoint remains anonymous under scoped Storefront auth route.
- [x] Khong leak admin/internal setting trong public response. 2026-07-18 Phase 2: response exposes only `mode`, `registrationAllowed`, and safe `message`.

## Phase 3 - Control Plane API/Web Admin Surface

- [x] Mo rong `IControlPlaneCatalogClient` va `ControlPlaneCatalogClient` trong Web de co:
  - `GetSecurityPrivacySettingsAsync`
  - `UpdateSecurityPrivacySettingsAsync`
  2026-07-18 Phase 3: methods call existing Control Plane gateway route `security-privacy`.
- [x] Them page Control Plane Web `commerce-admin/security-privacy`. 2026-07-18 Phase 3: added `CommerceSecurityPrivacy.razor`.
- [x] Page co pattern:
  - load stores active.
  - select store.
  - load `security-privacy`.
  - hien section `Registration`.
  - toggle/select registration mode `standard` / `disabled`.
  - giu cac section captcha/consent/privacy toi thieu theo DTO neu can update full request.
  2026-07-18 Phase 3: page loads active stores, selected store settings, registration, consent, captcha, and privacy retention fields, then sends a full update request.
- [x] Them nav item `Security/Privacy` duoi Commerce Admin. 2026-07-18 Phase 3: nav item added under Commerce Admin.
- [x] Save qua Control Plane API, khong goi Commerce Node truc tiep tu Web. 2026-07-18 Phase 3: page uses `IControlPlaneCatalogClient` and `IControlPlaneApiClient`; static boundary tests passed.
- [x] Hien warning ro khi disabled: customer moi khong the tao account, customer hien co van login duoc. 2026-07-18 Phase 3: page warning states new account creation is blocked while existing sign-in remains.

Acceptance:

- [x] User co permission `commerce.security_privacy.write` moi save duoc. 2026-07-18 Phase 3: save goes through existing Control Plane API endpoint protected by `CommerceSecurityPrivacyWrite`.
- [x] Control Plane Web khong chua Commerce Node base URL/secret va khong call `api/commerce/*` truc tiep. 2026-07-18 Phase 3: `SecurityPrivacyPhase6AdminManagementTests` and `EmailSmtpControlPlaneGatewayTests` passed.
- [x] Save thanh cong update dung store dang chon. 2026-07-18 Phase 3: route uses selected `storePublicId`; ControlPlane Web build passed.

## Phase 4 - Storefront V2 Register UX

- [ ] Them DTO/client trong `StorefrontAuthClient`:
  - `GetRegistrationPolicyAsync`.
- [ ] Update `IStorefrontAuthClient`.
- [ ] Update `RegisterPage.razor`:
  - load policy trong `OnInitializedAsync` sau auth redirect check.
  - neu `registrationAllowed = false`, hien disabled state thay vi form submit.
  - giu noindex/nofollow.
  - link ve sign-in van con cho customer hien co.
- [ ] Update local POST `/register` trong `Program.cs`:
  - check policy truoc validate form/captcha/register.
  - neu disabled, redirect ve `/register?error=...` hoac tra disabled state message nhat quan.
- [ ] Dam bao message khong noi ve internal config/env.

Acceptance:

- [ ] Browser khong co nut submit active khi disabled.
- [ ] Tamper POST local `/register` khong tao account va hien message dung.
- [ ] Enabled mode van render form va register flow khong doi.

## Phase 5 - Tests And Contract Gates

- [ ] Update `CommerceNodeStorefrontAuthContractTests`:
  - disabled policy lay tu DB setting theo store, khong chi runtime option.
  - enabled store van allow.
  - invalid/missing store behavior khong fallback.
- [ ] Update/add SecurityPrivacy admin tests:
  - DTO co registration setting.
  - validation mode.
  - audit metadata.
  - Control Plane gateway route va permission khong doi boundary.
- [ ] Add Control Plane Web static markup tests:
  - page/nav co `Security/Privacy`.
  - page dung `StoreClient`/`CatalogClient`, khong call Commerce Node direct.
- [ ] Add Storefront V2 tests:
  - `RegisterPage` render disabled message/form gate.
  - `StorefrontAuthClient` doc policy endpoint.
  - local POST `/register` handles disabled policy.
- [ ] Update OpenAPI snapshots neu contract thay doi.

Acceptance:

- [ ] Focused `dotnet test` pass cho CommerceNode auth/security privacy, ControlPlane Web, Storefront V2 auth client/page tests.
- [ ] Snapshot OpenAPI hop le va operation id giu on dinh.

## Phase 6 - Playwright Release QA

- [ ] Update fixture setup de co store/test account state:
  - store registration enabled cho luong register binh thuong.
  - store registration disabled cho `AUTH-002`.
  - neu dung cung store, test phai set disabled qua Control Plane UI/API roi restore enabled sau test.
- [ ] Update `Storefront Playwright E2E Release.todo.md`:
  - chuyen `AUTH-002 P0` tu accounted/deferred sang testcase executable.
- [ ] Browser testcase:
  - admin login Control Plane.
  - mo `commerce-admin/security-privacy`.
  - chon store test.
  - set registration disabled va save.
  - mo storefront `/register`.
  - verify disabled message visible.
  - verify khong co active submit `Create account`.
  - try tamper submit/direct request va expect no account created.
  - restore registration standard.
  - verify `/register` hien form lai.
- [ ] Browser network assertions:
  - Storefront browser khong call `api/commerce/*`, `api/controlplane/*`, `api/internal/*` truc tiep.
  - unexpected 5xx = 0.
- [ ] Evidence artifacts:
  - screenshots disabled/enabled.
  - network json.
  - response evidence for forbidden direct register.

Acceptance:

- [ ] `AUTH-002 P0` pass bang headed Chromium Playwright release run.
- [ ] Store state duoc restore sau test.
- [ ] QA note khong con ghi registration-disabled policy la deferred.

## Not In Scope

- [ ] Admin approval registration.
- [ ] Invite-only registration.
- [ ] Waitlist/pre-registration.
- [ ] Email activation policy changes.
- [ ] Login disable policy.
- [ ] Customer role based registration rules.
- [ ] Domain/country allowlist cho registration.
- [ ] Tax core.

## Release Gate

- [ ] Control Plane admin co the disable register theo tung store.
- [ ] Storefront UI khong cho submit khi disabled.
- [ ] Commerce Node API van chan server-side khi bi bypass UI.
- [ ] Enabled mode khong bi regression.
- [ ] Playwright `AUTH-002 P0` co evidence that.
- [ ] QA todo files duoc update sau implementation.
