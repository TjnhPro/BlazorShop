# Legacy Presentation Removal V2 Canonicalization.todo.md

Status: phase 5 legacy Infrastructure/AppDbContext purge complete; phase 6 physical Presentation removal next
Source: investigate review of legacy Presentation removal blockers  
Purpose: remove `BlazorShop.Presentation` and make V2 the canonical repository lifecycle target without breaking active V2 runtime, CI, Docker, deployment, tests, or docs.

## Current investigation status - 2026-07-22

Root cause hypothesis: this plan started as a proposed legacy-removal/canonicalization plan, but several Phase 1 packaging and CI tasks were already completed by `V2 Production Readiness Hardening.todo.md`. Phases 0-5 have now established guardrails, made `BlazorShop.sln` V2 canonical, moved V2 tests to owned sources, removed legacy `BlazorShop.AppHost`, and purged `AppDbContext`/`DefaultConnection` from active source. The remaining blocker is physical legacy `BlazorShop.Presentation` source and final docs/QA cleanup.

Evidence checked:

- Working tree was clean before this investigation edit.
- `scripts/verify-no-active-legacy-reference.ps1` does not exist.
- `docs/refactor-control-Commerce-storefront/legacy-removal-allowlist.json` does not exist.
- `BlazorShop.Presentation` still exists as legacy reference source.
- `BlazorShop.sln` is V2 canonical and excludes legacy `BlazorShop.Presentation/*`, `BlazorShop.AppHost`, and old `BlazorShop.Tests`.
- `BlazorShop.sln` is now the V2 canonical solution; the temporary `BlazorShop.V2.slnf` transition file has been removed.
- `BlazorShop.Tests.V2.csproj` still links source and snapshots from `..\BlazorShop.Tests\...`; V2 test source ownership is not independent yet.
- `.github/workflows/ci.yml` has blocking `ci-v2`, validates `compose.v2.production.yml`, builds four V2 images, and no longer carries the old `legacy-compatibility` job.
- `compose.production.yml` is now V2 canonical; `compose.v2.production.yml` remains as a transition alias while CI/downstream scripts are updated.
- V2 Dockerfiles exist for ControlPlane API, CommerceNode API, ControlPlane Web, and Storefront V2. Storefront V2 Dockerfile now copies Components/WASM/Web.SharedV2 projects before restore and source before publish.
- `BlazorShop.AppHost` is being removed in Phase 4; it previously referenced legacy API/Web/Storefront and used Aspire database name `DefaultConnection`.
- `BlazorShop.Infrastructure` no longer contains `AppDbContext`, legacy root migrations, `AddInfrastructure`, `AddSharedAuthenticationInfrastructure`, `UseInfrastructure`, or AppDbContext-bound legacy repository/service implementations.
- Focused verification passed on 2026-07-22: `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~V2ProductionReadiness|FullyQualifiedName~V2ArchitectureBoundary" --verbosity minimal` returned `Passed: 30, Failed: 0`.
- Existing warnings remain non-blocking for this investigation: MessagePack NU1902/NU1903 advisories and Browserslist stale notice.

Phase status summary:

| Phase | Status | Evidence |
|---|---|---|
| 0 Baseline inventory and guardrail modes | Done | `scripts/verify-no-active-legacy-reference.ps1`, allowlist file, and architecture tests exist |
| 1A Dockerfile V2 | Done | All four V2 Dockerfiles exist; Storefront V2 Dockerfile includes Components/WASM project/source copy |
| 1B V2 production compose | Done | `compose.v2.production.yml` uses V2 topology, has CommerceNode media volume, and now persists CommerceNode Data Protection keys at `/app/runtime/data-protection-keys`; `compose.production.yml` canonical swap is intentionally deferred to Phase 2 |
| 1C CI V2 blocking | Done | `ci-v2` restores/builds/tests V2, validates compose config, builds four V2 images, and no longer carries the old `legacy-compatibility` job |
| 2 Main solution becomes V2 canonical | Done | `BlazorShop.sln` now includes shared core, ServiceDefaults, active PresentationV2 projects, and `BlazorShop.Tests.V2`; legacy Presentation, AppHost, and old mixed tests are removed from the main solution |
| 3 V2 test source ownership | Done | `BlazorShop.Tests.V2` now owns Architecture, PresentationV2, CommerceNode, ControlPlane, shared Application/Domain, and non-legacy Infrastructure tests directly; old mixed `BlazorShop.Tests` project was retired |
| 4 Remove legacy AppHost and operational entrypoints | Done | `BlazorShop.AppHost` tracked files and ignored build artifacts removed; `run-v2-local.ps1 -StopExisting -NoOpenBrowser` and four endpoint smoke checks passed |
| 5 Purge dead legacy Infrastructure/AppDbContext | Done | Active source grep is clean for `AppDbContext`, `DefaultConnection`, and legacy DI methods; build/full V2 tests/migration model tests passed |
| 6 Physically remove BlazorShop.Presentation | Not started | `BlazorShop.Presentation` folder exists and is referenced by solution, AppHost, old tests, legacy CI job, and legacy compose |
| 7 Docs, QA, clean verification, release gate | Not started | Docs still describe legacy as present/reference; final canonical V2 verification has not run |

## Goal

Xoa hoan toan legacy Presentation khoi active repository lifecycle:

- Main solution tro thanh V2 canonical.
- CI build/test/deploy artifacts dung V2.
- Test ownership cua V2 doc lap, khong link nguoc sang mixed legacy test tree.
- `BlazorShop.AppHost` legacy bi xoa hoac loai khoi active graph.
- Production packaging V2 day du truoc khi thay compose production.
- `AppDbContext`, `DefaultConnection`, `AddInfrastructure` va legacy services bi purge sau khi het consumer.
- Cuoi phase co the xoa vat ly `BlazorShop.Presentation`.

## Non-goals

- [ ] Khong rewrite Control Plane, Commerce Node, Storefront V2, cart, checkout, order, payment, media.
- [ ] Khong tao Aspire AppHost V2 trong phase nay.
- [ ] Khong thay doi V2 API route shape neu khong can.
- [ ] Khong giu source legacy trong active branch chi de tham khao; neu can tham khao thi dung git history/tag/archive docs.
- [ ] Khong xoa class/DTO/service chi vi ten cu; chi xoa khi actual consumer graph chung minh dead.
- [ ] Khong xoa test source cu truoc khi `BlazorShop.Tests.V2` so huu source doc lap.

## Verified current evidence

- [x] V2 project graph khong reference `BlazorShop.Presentation`.
- [x] `BlazorShop.sln` gom core, ServiceDefaults, PresentationV2 va `BlazorShop.Tests.V2`; khong gom legacy Presentation/AppHost.
- [x] `BlazorShop.sln` van gom `BlazorShop.Presentation` projects, `BlazorShop.AppHost`, va `BlazorShop.Tests`.
- [x] `BlazorShop.Tests.csproj` van reference AppHost va cac legacy projects.
- [x] `BlazorShop.Tests.V2.csproj` van link source tu `BlazorShop.Tests/Architecture/**` va `BlazorShop.Tests/PresentationV2/**`.
- [x] `BlazorShop.AppHost` reference legacy API/Web/Storefront va Program dung `DefaultConnection`.
- [x] CI hien co blocking `ci-v2` restore/build/test V2 va build Dockerfile V2; old `legacy-compatibility` job da duoc xoa khi old mixed test project retire.
- [x] `compose.production.yml` hien la topology legacy voi `DefaultConnection`.
- [x] V2 Dockerfile da co cho `BlazorShop.Storefront.V2`, ControlPlane API, ControlPlane Web, va CommerceNode API.
- [x] Storefront V2 Dockerfile da copy `Storefront.Components.csproj`, `Storefront.WASM.csproj`, va source cua cac project nay truoc publish.
- [x] `BlazorShop.Infrastructure/DependencyInjection.cs` van co `AddInfrastructure`, `AddSharedAuthenticationInfrastructure`, `AppDbContext`, `DefaultConnection`.
- [x] `BlazorShop.Infrastructure/Data/AppDbContext.cs` va legacy migrations van ton tai.
- [x] Co `BlazorShop.Tests/Architecture/V2ProductionReadinessTests.cs` dang la baseline/inventory test nhung file nay van nam trong mixed old test tree.

## Strategy

Dung hai lop guardrail:

- **Active strict guard**: fail voi legacy reference trong V2 projects, active CI, production compose, active scripts, active runtime config.
- **Repository inventory guard**: cho phep legacy source/test/docs co allowlist trong qua trinh migrate, nhung ghi ro con lai gi va phase nao xoa.

Ly do: neu fail toan repo ngay tu dau voi `BlazorShop.Presentation`, `AppDbContext`, `DefaultConnection`, thi guardrail se fail truoc khi migration co the bat dau.

## Phase dependency map

```text
Phase 0: Baseline inventory and guardrail modes
  -> Phase 1: V2 production packaging and CI blocking gate
      -> Phase 2: Main solution becomes V2 canonical
          -> Phase 3: V2 test source ownership
              -> Phase 4: Remove legacy AppHost and operational entrypoints
                  -> Phase 5: Purge dead legacy Infrastructure/AppDbContext
                      -> Phase 6: Physically remove BlazorShop.Presentation
                          -> Phase 7: Docs, QA, clean verification, release gate
```

## Phase 0 - Baseline inventory and guardrail modes

Goal: tao safety net de khong them dependency moi vao legacy trong khi chua xoa duoc legacy cu.

### Tasks

- [x] Tao script `scripts/verify-no-active-legacy-reference.ps1`.
- [x] Script co 2 mode:
  - [x] `-Mode ActiveStrict`.
  - [x] `-Mode Inventory`.
- [x] `ActiveStrict` scan cac duong active:
  - [x] `BlazorShop.PresentationV2/**`
  - [x] `BlazorShop.sln`
  - [x] `.github/workflows/**`
  - [x] `compose.production.yml` sau khi duoc migrate sang V2
  - [x] `compose.v2.production.yml` neu ton tai trong transition
  - [x] `scripts/run-v2-local.ps1`
  - [x] `scripts/stop-v2-local.ps1`
  - [x] `docs/architecture/**` sau khi phase cleanup docs bat dau
- [x] `ActiveStrict` fail khi active runtime/CI/deploy chua duoc allowlist ma co:
  - [x] `BlazorShop.Presentation`
  - [x] `BlazorShop.AppHost`
  - [x] `ConnectionStrings__DefaultConnection`
  - [x] `DefaultConnection`
  - [x] `AppDbContext`
  - [x] `AddInfrastructure(`
  - [x] `AddSharedAuthenticationInfrastructure(`
- [x] `Inventory` scan toan repo va xuat danh sach grouped theo source/test/docs/legacy/runtime.
- [x] Tao allowlist file ro rang, vi du `docs/refactor-control-Commerce-storefront/legacy-removal-allowlist.json`.
- [x] Them architecture tests:
  - [x] V2 projects khong ProjectReference legacy.
  - [x] V2 Programs khong goi `AddInfrastructure`.
  - [x] V2 runtime config khong yeu cau `DefaultConnection`.
  - [x] Storefront V2 khong call legacy route groups.
  - [x] Main solution con legacy thi duoc record baseline, sau Phase 2 doi thanh forbidden.
- [x] Cap nhat `V2ProductionReadinessTests` hien co hoac tao test moi trong `BlazorShop.Tests/Architecture`.
- [x] Ghi ro current blockers vao plan file sau khi script inventory chay lan dau.

### Verification

- [x] `powershell -ExecutionPolicy Bypass -File scripts/verify-no-active-legacy-reference.ps1 -Mode Inventory`
- [x] `powershell -ExecutionPolicy Bypass -File scripts/verify-no-active-legacy-reference.ps1 -Mode ActiveStrict`
- [x] `dotnet test BlazorShop.Tests/BlazorShop.Tests.csproj --no-restore --filter "FullyQualifiedName~LegacyRemovalGuardrailTests|FullyQualifiedName~V2ProductionReadinessTests.Phase0_RateLimitIdentityBaseline_RecordsCurrentUserAndRemoteIpPartitioning"`

### Done when

- [x] Guardrail khong chan migration bat dau.
- [x] ActiveStrict co allowlist nho va co chu dich.
- [x] Inventory cho thay day du legacy blockers con lai.

## Phase 1 - V2 production packaging and CI blocking gate

Goal: truoc khi xoa legacy, V2 phai co du artifact build/deploy that.

### Phase 1A - Dockerfile V2

- [x] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`.
- [x] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
- [x] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`.
- [x] Sua Dockerfile `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile`:
  - [x] Copy `BlazorShop.Storefront.Components.csproj` truoc `dotnet restore`.
  - [x] Copy `BlazorShop.Storefront.WASM.csproj` truoc `dotnet restore`.
  - [x] Copy source cua `Storefront.Components` va `Storefront.WASM` truoc publish.
  - [x] Dung SDK/runtime phu hop `global.json` va project target.
- [x] ControlPlane Web Dockerfile:
  - [x] Publish Blazor WASM static assets.
  - [x] Serve bang Nginx/static server.
  - [x] Co SPA fallback.
  - [x] Khong bake API secret/node secret vao client image.
- [x] CommerceNode API image:
  - [x] Khong can legacy `DefaultConnection`.
  - [x] Dung `CommerceNodeConnection`.
  - [x] Co persistent media/data-protection volume points documented.
- [x] ControlPlane API image:
  - [x] Dung `ControlPlaneConnection`.
  - [x] Khong co Commerce Node secret trong Web image.

### Phase 1B - V2 production compose

- [x] Tao `compose.v2.production.yml` trong transition.
- [x] Services toi thieu:
  - [x] `controlplane-postgres`.
  - [x] `commercenode-postgres`.
  - [x] `controlplane-api`.
  - [x] `controlplane-web`.
  - [x] `commercenode-api`.
  - [x] `commercenode-nginx`.
  - [x] `commercenode-imgproxy`.
  - [x] `storefront-v2` hoac document ro store deployment task se tao container theo store.
- [x] Config bat buoc:
  - [x] `ConnectionStrings__ControlPlaneConnection`.
  - [x] `ConnectionStrings__CommerceNodeConnection`.
  - [x] Khong co `ConnectionStrings__DefaultConnection`.
  - [x] `ControlPlane__Database__MigrateOnStartup`.
  - [x] `CommerceNode__Database__MigrateOnStartup`.
  - [x] Data Protection key ring persistent cho CommerceNode.
  - [x] Media storage persistent cho CommerceNode.
  - [x] Storefront env chi co `Api__BaseUrl`, `STORE_KEY`/`StoreKey`, public URL config can thiet.
- [x] Khi `compose.v2.production.yml` pass, quyet dinh doi `compose.production.yml` thanh V2 canonical trong phase nay hoac Phase 2. Decision: defer canonical `compose.production.yml` swap to Phase 2 after `BlazorShop.sln` becomes V2 canonical.

### Phase 1C - CI V2 blocking

- [x] Tao job `ci-v2`.
- [x] `ci-v2` run:
  - [x] `dotnet restore BlazorShop.sln`.
  - [x] `dotnet build BlazorShop.sln --configuration Release --no-restore`.
  - [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --configuration Release --no-build --verbosity normal`.
- [x] Build 4 Docker images V2 trong CI.
- [x] Run `docker compose -f compose.v2.production.yml config`.
- [x] Optional boot compose smoke neu runtime cost chap nhan. Decision: defer full boot/runtime smoke to Phase 7 release gate.
  - [x] ControlPlane API health covered by Phase 7 release smoke script.
  - [x] CommerceNode API health covered by Phase 7 release smoke script.
  - [x] ControlPlane Web HTTP 200 covered by Phase 7 release smoke script.
  - [x] Storefront V2 HTTP 200/maintenance expected covered by Phase 7 release smoke script.
- [x] Chuyen legacy job thanh `legacy-compatibility` tam thoi, khong la release blocker. Superseded in Phase 3: old job removed after old mixed test project retirement.
- [x] Sua npm cache path khong tro vao `BlazorShop.Presentation/BlazorShop.Web/package-lock.json`.

### Done when

- [x] V2 image build tu clean checkout.
- [x] V2 compose config pass.
- [x] CI V2 la required release signal.
- [x] Legacy CI khong con tao cam giac production dang deploy V2.

## Phase 2 - Main solution becomes V2 canonical

Goal: `BlazorShop.sln` tro thanh main solution active, khong can solution filter de ne legacy.

### Tasks

- [x] Remove legacy projects khoi `BlazorShop.sln`:
  - [x] `BlazorShop.Presentation/BlazorShop.API`.
  - [x] `BlazorShop.Presentation/BlazorShop.Web`.
  - [x] `BlazorShop.Presentation/BlazorShop.Web.Shared`.
  - [x] `BlazorShop.Presentation/BlazorShop.Storefront`.
  - [x] `BlazorShop.AppHost`.
  - [x] `BlazorShop.Tests` old mixed project removed from solution now; source/project files stay on disk for Phase 3 source migration and legacy compatibility until later cleanup.
- [x] Giu trong solution:
  - [x] `BlazorShop.Domain`.
  - [x] `BlazorShop.Application`.
  - [x] `BlazorShop.Infrastructure`.
  - [x] `BlazorShop.ServiceDefaults`.
  - [x] Tat ca `BlazorShop.PresentationV2/*`.
  - [x] `BlazorShop.Tests.V2`.
- [x] Sau khi `BlazorShop.sln` da sach:
  - [x] Cap nhat CI dung `BlazorShop.sln` thay cho `BlazorShop.V2.slnf`.
  - [x] Cap nhat README/docs active build commands.
  - [x] Xoa `BlazorShop.V2.slnf` chi khi no khong con can cho transition. Done in Phase 3 after `BlazorShop.Tests.V2` source links were removed.
- [x] Update guardrail: main solution khong duoc co `BlazorShop.Presentation` hoac `BlazorShop.AppHost`.

### Verification

- [x] `dotnet restore BlazorShop.sln`
- [x] `dotnet build BlazorShop.sln -c Release --no-restore`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build`
- [x] `rg "BlazorShop\\.Presentation|BlazorShop\\.AppHost" BlazorShop.sln` returns no legacy project entries. `BlazorShop.PresentationV2` remains expected.

### Done when

- [x] Main solution build duoc khong can legacy project folders.
- [x] `BlazorShop.V2.slnf` hoac da xoa, hoac chi con duoc ghi ro la temporary until Phase 3. Done: file removed in Phase 3.

## Phase 3 - V2 test source ownership

Goal: `BlazorShop.Tests.V2` so huu source cua minh, khong link nguoc sang `BlazorShop.Tests`.

### Phase 3A - Move active V2 tests physically

- [x] Di chuyen `BlazorShop.Tests/Architecture/**` vao `BlazorShop.Tests.V2/Architecture/**`.
- [x] Di chuyen `BlazorShop.Tests/PresentationV2/**` vao `BlazorShop.Tests.V2/PresentationV2/**`.
- [x] Di chuyen V2 snapshots vao `BlazorShop.Tests.V2/PresentationV2/**/Snapshots`.
- [x] Remove `<Compile Include="..\BlazorShop.Tests\Architecture\**\*.cs"...>`.
- [x] Remove `<Compile Include="..\BlazorShop.Tests\PresentationV2\**\*.cs"...>`.
- [x] Remove `<None Include="..\BlazorShop.Tests\PresentationV2\CommerceNode\Snapshots\*"...>`.
- [x] Ensure namespaces van hop ly hoac update namespace neu can.

### Phase 3B - Migrate V2 core tests

- [x] Phan loai `BlazorShop.Tests/Application/**`:
  - [x] V2 commerce/application tests -> move to `BlazorShop.Tests.V2/Application/**`.
  - [x] Legacy-only authentication/payment tests -> delete or rewrite after review.
  - [x] Shared business tests that still protect V2 -> move and adapt to V2 services.
- [x] Phan loai `BlazorShop.Tests/Infrastructure/**`:
  - [x] `Infrastructure/CommerceNode/**` -> move to V2 tests.
  - [x] `Infrastructure/ControlPlane/**` if any -> move to V2 tests.
  - [x] `AppDbContext`/legacy repository/admin service tests retired with old mixed test project; active V2 behavior is covered by CommerceNode/ControlPlane tests.
- [x] Uu tien move:
  - [x] Cart/session/sellability.
  - [x] Checkout/pricing/payment attempt/order placement.
  - [x] Currency/rounding.
  - [x] Navigation/pages/SEO.
  - [x] CommerceNodeDbContext model/migration.
  - [x] Seeder idempotency.
  - [x] Store SMTP/message queue.
  - [x] Payment callback/webhook safety.
- [x] Them guardrail dem expected V2 test files/namespaces de test khong bi rot khoi project khi move.

### Phase 3C - Retire old mixed test project

- [x] Sau khi V2 tests du source doc lap, remove `BlazorShop.Tests` project reference khoi solution.
- [x] Xoa `BlazorShop.Tests.csproj`.
- [x] Xoa old test files legacy-only neu khong con gia tri.
- [x] Neu muon giu comparison docs, archive note trong `docs/archive/legacy/`, khong giu runnable old mixed test project.
- [x] Optional cleanup PR sau: rename `BlazorShop.Tests.V2` thanh `BlazorShop.Tests`; khong bat buoc cho legacy deletion. Decision: keep `BlazorShop.Tests.V2` name through this removal phase.

### Verification

- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release`
- [x] `rg "\\.\\.\\\\BlazorShop.Tests|\\.\\./BlazorShop.Tests" BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj` returns none.
- [x] `rg "BlazorShop.AppHost|BlazorShop.Presentation" BlazorShop.Tests.V2` returns no runtime/test dependency hits; guardrail assertions that mention legacy strings are expected until Phase 6.

### Done when

- [x] V2 test project doc lap ve source va snapshots.
- [x] Xoa folder/test project cu khong lam mat V2 tests.

## Phase 4 - Remove legacy AppHost and operational entrypoints

Goal: xoa AppHost legacy va moi script/entrypoint active dang chay legacy runtime.

### Tasks

- [x] Xoa `BlazorShop.AppHost` project folder.
- [x] Remove AppHost project entry khoi solution neu chua xong Phase 2.
- [x] Remove AppHost reference khoi tests/CI/docs.
- [x] Xoa AppHost user-secrets references neu co.
- [x] Rg scripts va docs:
  - [x] `dotnet run --project BlazorShop.Presentation/BlazorShop.API`.
  - [x] `dotnet run --project BlazorShop.Presentation/BlazorShop.Web`.
  - [x] `dotnet run --project BlazorShop.Presentation/BlazorShop.Storefront`.
  - [x] `BlazorShop.AppHost`.
- [x] Giu `scripts/run-v2-local.ps1` la local entrypoint chinh.
- [x] Khong tao AppHost V2 trong phase nay; neu can future local orchestration, tao plan rieng.

### Verification

- [x] `rg "BlazorShop\\.AppHost|Projects\\.BlazorShop_API|Projects\\.BlazorShop_Web|Projects\\.BlazorShop_Storefront" BlazorShop.sln scripts .github` khong co active hit sau cleanup.
- [x] `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser` starts V2 runtimes.
- [x] ControlPlane API/Web, CommerceNode API, Storefront V2 health/smoke pass.

### Done when

- [x] AppHost khong con trong repository hoac khong con active legacy dependency.
- [x] Khong co operational command active nao chay API/Web/Storefront cu.

## Phase 5 - Purge dead legacy Infrastructure and AppDbContext

Goal: xoa persistence/runtime code chi con phuc vu legacy sau khi legacy consumers da bi loai khoi graph.

### Preconditions

- [x] `BlazorShop.Presentation` khong con trong solution/CI active.
- [x] `BlazorShop.AppHost` da xoa.
- [x] `BlazorShop.Tests.V2` khong link source tu old tests.
- [x] Inventory script cho thay remaining `AppDbContext` consumers chi nam trong legacy/dead areas.

### Tasks

- [x] Build consumer graph cho:
  - [x] `AppDbContext`.
  - [x] `AddInfrastructure`.
  - [x] `AddSharedAuthenticationInfrastructure`.
  - [x] `UseInfrastructure`.
  - [x] `DefaultConnection`.
  - [x] Legacy repositories under `BlazorShop.Infrastructure/Repositories`.
  - [x] Legacy admin services under `BlazorShop.Infrastructure/Services/Admin`.
  - [x] Legacy payment/cart/order repositories that bind to `AppDbContext`.
- [x] Delete `BlazorShop.Infrastructure/Data/AppDbContext.cs`.
- [x] Delete `BlazorShop.Infrastructure/Data/AppDbContextFactory.cs`.
- [x] Delete `BlazorShop.Infrastructure/Migrations/**` for AppDbContext.
- [x] Delete `AddInfrastructure`, `AddSharedAuthenticationInfrastructure`, `UseInfrastructure` if no consumer.
- [x] Delete health checks, seeders, repositories, services chi con dung `AppDbContext`.
- [x] Remove `DefaultConnection` from active appsettings, examples, scripts, compose.
- [x] Keep shared Application/Domain contracts only if active V2 still consumes them through ControlPlane/CommerceNode paths.
- [x] If a V2 service still depends on legacy repository contract:
  - [x] Replace with CommerceNode/ControlPlane implementation first.
  - [x] Add focused test before deleting old implementation.
- [x] Update architecture docs: `AppDbContext` no longer exists in active repo.

### Verification

- [x] `rg "AppDbContext|DefaultConnection|AddInfrastructure\\(|AddSharedAuthenticationInfrastructure\\(|UseInfrastructure\\(" BlazorShop.Application BlazorShop.Infrastructure BlazorShop.PresentationV2 scripts .github compose*.yml`
- [x] `dotnet build BlazorShop.sln -c Release --no-restore`
- [x] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build`
- [x] EF migration/model tests for `ControlPlaneDbContext` and `CommerceNodeDbContext` pass.

### Done when

- [x] Active source khong con AppDbContext legacy.
- [x] V2 startup khong bao gio can `DefaultConnection`.
- [x] Purge dua tren consumer graph, khong phai ten file.

## Phase 6 - Physically remove BlazorShop.Presentation

Goal: xoa legacy source folder khoi active branch.

### Tasks

- [ ] Tao tag truoc khi xoa neu can history reference: `legacy-presentation-final`.
- [ ] `git rm -r BlazorShop.Presentation`.
- [ ] Remove remaining legacy project entries neu con.
- [ ] Remove legacy Dockerfile references.
- [ ] Remove legacy static asset references.
- [ ] Remove legacy appsettings/run profile references.
- [ ] Remove or archive docs huong dan legacy runtime.
- [ ] Update guardrail allowlist: `BlazorShop.Presentation` khong con la allowed source hit.

### Verification

- [ ] `Test-Path BlazorShop.Presentation` returns false.
- [ ] `rg "BlazorShop\\.Presentation" --glob '!docs/archive/**'` returns no active source/CI/script hits.
- [ ] `dotnet restore BlazorShop.sln`.
- [ ] `dotnet build BlazorShop.sln -c Release --no-restore`.
- [ ] `dotnet test BlazorShop.sln -c Release --no-build`.
- [ ] 4 V2 Docker images build.
- [ ] Production compose config pass.

### Done when

- [ ] Folder legacy da xoa vat ly.
- [ ] Main solution/test/CI/container khong can folder legacy.

## Phase 7 - Docs, QA, clean verification, release gate

Goal: dong phase bang bang chung build/test/container/browser production-ready.

### Docs updates

- [ ] `README.md`: V2 la canonical, khong con "legacy remains for reference".
- [ ] `AGENTS.md`: xoa rule legacy-reference neu source da bi xoa; giu note git history/tag neu can.
- [ ] `docs/architecture/01-system-map.md`: xoa hoac archive legacy runtime rows.
- [ ] `docs/architecture/02-layered-architecture.md`: remove legacy project family section hoac chuyen archive.
- [ ] `docs/architecture/03-runtime-boundaries.md`: remove "legacy remains in solution".
- [ ] `docs/architecture/04-data-ownership.md`: remove `AppDbContext` active table row.
- [ ] `docs/architecture/05-project-and-folder-guide.md`: remove `BlazorShop.Presentation` active guide.
- [ ] `docs/architecture/07-deployment-and-local-run.md`: commands dung `BlazorShop.sln`, production compose V2.
- [ ] `docs/production-runbook.md`: replace legacy sections bang V2 topology.
- [ ] Archive historical legacy deployment notes under `docs/archive/legacy/` neu can.

### QA updates

- [ ] `QA-ControlPlane.todo.md`:
  - [ ] ControlPlane API/Web build/container.
  - [ ] No direct CommerceNode calls from Web.
  - [ ] No AppDbContext.
- [ ] `QA-CommerceNode.todo.md`:
  - [ ] CommerceNode API build/container.
  - [ ] CommerceNodeConnection only.
  - [ ] Swagger docs.
  - [ ] Storefront scoped APIs.
- [ ] `QA-StorefrontV2.todo.md`:
  - [ ] Storefront V2 build/container.
  - [ ] Storefront startup with `STORE_KEY`.
  - [ ] Store resolution, maintenance, catalog, media.
- [ ] `Storefront Playwright E2E Release.todo.md`:
  - [ ] Home/category/product.
  - [ ] Account login/register/recovery.
  - [ ] Cart.
  - [ ] Checkout COD place order in test store.
  - [ ] Customer order history.
  - [ ] Browser network rejects direct `api/commerce/*`, `api/control-plane/*`, `api/internal/*`.

### Final verification

- [ ] Clean checkout restore:
  - [ ] `dotnet restore BlazorShop.sln`.
- [ ] Build:
  - [ ] `dotnet build BlazorShop.sln -c Release --no-restore`.
- [ ] Test:
  - [ ] `dotnet test BlazorShop.sln -c Release --no-build`.
- [ ] Static guard:
  - [ ] `powershell -ExecutionPolicy Bypass -File scripts/verify-no-active-legacy-reference.ps1 -Mode ActiveStrict`.
  - [ ] Inventory mode has no source/CI/script/compose hits except `docs/archive/legacy`.
- [ ] Runtime:
  - [ ] `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser`.
  - [ ] ControlPlane API health.
  - [ ] CommerceNode API health.
  - [ ] Storefront V2 current store page.
  - [ ] Swagger CommerceAdmin and Storefront docs.
- [ ] Containers:
  - [ ] Build ControlPlane API image.
  - [ ] Build CommerceNode API image.
  - [ ] Build ControlPlane Web image.
  - [ ] Build Storefront V2 image.
  - [ ] `docker compose -f compose.production.yml config`.
- [ ] Browser:
  - [ ] Visible Playwright release checklist P0 pass.

### Done when

- [ ] Repository, build, test, CI, Docker, compose, docs va QA deu V2 canonical.
- [ ] `BlazorShop.Presentation` va `BlazorShop.AppHost` khong con active.
- [ ] Production publish co bang chung container + browser e2e pass.

## Risk register

| Risk | Phase | Impact | Mitigation |
|---|---:|---|---|
| Guardrail fail qua som vi legacy source con ton tai | 0 | Khong the migrate tung buoc | Tach ActiveStrict va Inventory mode |
| Doi production compose truoc khi Dockerfile V2 build duoc | 1 | Deployment gia, CI fail | Tao compose V2 transition va build images truoc |
| Xoa old test source khi `Tests.V2` van link nguoc | 3 | Mat test hoac compile fail | Move physical source/snapshots truoc, remove links sau |
| Xoa `AppDbContext` trong khi old test/legacy service con consumer | 5 | Compile fail lon | Consumer graph + delete theo group |
| Xoa class dung chung bi nham la legacy | 5 | Regression V2 | Verify references, build/test after each group |
| Docs van noi legacy la active | 7 | Agent/operator di sai duong | Update architecture docs/runbook cung phase |
| Playwright browser e2e khong cover enough | 7 | Public production miss real bug | Use release checklist with real COD order in test store |

## Implementation order checklist

- [x] Phase 0 complete and committed.
- [x] Phase 1A Dockerfiles complete and committed through V2 production-readiness work.
- [x] Phase 1B compose V2 complete and committed. Data Protection key-ring wiring is complete; `compose.production.yml` canonical swap is deferred to Phase 2.
- [x] Phase 1C CI V2 blocking complete and committed through V2 production-readiness work.
- [x] Phase 2 main solution V2 canonical complete and committed.
- [x] Phase 3A V2 test source move complete and committed.
- [x] Phase 3B V2 core test migration complete and committed.
- [x] Phase 3C old mixed test retirement complete and committed.
- [ ] Phase 4 AppHost removal complete and committed.
- [ ] Phase 5 legacy Infrastructure purge complete by consumer group and committed.
- [ ] Phase 6 physical `BlazorShop.Presentation` removal complete and committed.
- [ ] Phase 7 docs/QA/final release verification complete and committed.

## GSTACK REVIEW REPORT

### CEO review

The plan is worth doing because legacy is no longer just "reference"; it is still in CI, production compose, test ownership, solution membership, and AppHost. That creates release ambiguity. The plan makes V2 the only production truth while preserving the ability to move safely in small PRs.

### Design review

No product UI change is intended. Browser QA is still required at the end because deleting legacy paths and changing packaging can break public Storefront behavior without touching UI files.

### Engineering review

The critical adjustment is not deleting the folder first. The safe path is guardrails, V2 packaging/CI, main solution cleanup, test ownership, AppHost removal, Infrastructure purge, then physical deletion. `AppDbContext` deletion must be consumer-graph-driven because Infrastructure still contains shared contracts and services that may be active through CommerceNode.

### DX review

After this phase, developer commands should be simpler: `dotnet restore/build/test BlazorShop.sln`, one V2 production compose, one V2 local runner, and no solution filter needed. Error output should point to active V2 boundaries instead of mixed legacy/V2 projects.

## Decision Audit Trail

| # | Phase | Decision | Classification | Principle | Rationale | Rejected |
|---|---|---|---|---|---|---|
| 1 | 0 | Split guardrail into ActiveStrict and Inventory modes | Auto-decided | Incremental safety | Full repo fail would block migration while legacy source intentionally still exists | One strict grep across entire repo from PR 1 |
| 2 | 1 | Complete V2 Docker/compose before physical legacy deletion | Auto-decided | Production truth | Removing legacy before V2 artifacts build would leave no reliable deployment target | Delete `BlazorShop.Presentation` first and fix build fallout |
| 3 | 2 | Make `BlazorShop.sln` V2 canonical before deleting V2 solution filter | Auto-decided | One canonical command | The solution filter is a transition tool; final repo should not need it | Keep `BlazorShop.V2.slnf` forever |
| 4 | 3 | Move V2 test source physically before deleting old test project | Auto-decided | Preserve verification | `BlazorShop.Tests.V2` currently links source from old test tree | Delete old tests immediately |
| 5 | 4 | Delete legacy AppHost instead of converting it to V2 now | Auto-decided | Avoid new dependency | Current V2 local runner already exists; a V2 AppHost is optional future DX work | Rewrite AppHost before removal |
| 6 | 5 | Purge `AppDbContext` only after consumer graph is clean | Auto-decided | Do not break shared core | Some old-named contracts/services may still be active through V2 infrastructure | Delete by folder/name alone |
| 7 | 7 | Require container and visible Playwright release evidence before close | Auto-decided | Real production confidence | Build-only checks do not prove browser commerce flows still work | Treat compile/test as sufficient |
