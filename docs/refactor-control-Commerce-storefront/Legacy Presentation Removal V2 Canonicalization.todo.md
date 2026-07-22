# Legacy Presentation Removal V2 Canonicalization.todo.md

Status: proposed  
Source: investigate review of legacy Presentation removal blockers  
Purpose: remove `BlazorShop.Presentation` and make V2 the canonical repository lifecycle target without breaking active V2 runtime, CI, Docker, deployment, tests, or docs.

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
- [x] `BlazorShop.V2.slnf` gom core, ServiceDefaults, PresentationV2 va `BlazorShop.Tests.V2`; khong gom legacy Presentation/AppHost.
- [x] `BlazorShop.sln` van gom `BlazorShop.Presentation` projects, `BlazorShop.AppHost`, va `BlazorShop.Tests`.
- [x] `BlazorShop.Tests.csproj` van reference AppHost va cac legacy projects.
- [x] `BlazorShop.Tests.V2.csproj` van link source tu `BlazorShop.Tests/Architecture/**` va `BlazorShop.Tests/PresentationV2/**`.
- [x] `BlazorShop.AppHost` reference legacy API/Web/Storefront va Program dung `DefaultConnection`.
- [x] CI hien restore/build `BlazorShop.sln`, test `BlazorShop.Tests`, build Dockerfile legacy.
- [x] `compose.production.yml` hien la topology legacy voi `DefaultConnection`.
- [x] Hien chi co Dockerfile V2 cho `BlazorShop.Storefront.V2`; chua co Dockerfile V2 cho ControlPlane API, ControlPlane Web, CommerceNode API.
- [x] Storefront V2 Dockerfile chua copy `Storefront.Components.csproj` va `Storefront.WASM.csproj` truoc restore du project co reference hai project nay.
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

- [ ] Tao script `scripts/verify-no-active-legacy-reference.ps1`.
- [ ] Script co 2 mode:
  - [ ] `-Mode ActiveStrict`.
  - [ ] `-Mode Inventory`.
- [ ] `ActiveStrict` scan cac duong active:
  - [ ] `BlazorShop.PresentationV2/**`
  - [ ] `BlazorShop.V2.slnf`
  - [ ] `.github/workflows/**`
  - [ ] `compose.production.yml` sau khi duoc migrate sang V2
  - [ ] `compose.v2.production.yml` neu ton tai trong transition
  - [ ] `scripts/run-v2-local.ps1`
  - [ ] `scripts/stop-v2-local.ps1`
  - [ ] `docs/architecture/**` sau khi phase cleanup docs bat dau
- [ ] `ActiveStrict` fail khi active runtime/CI/deploy chua duoc allowlist ma co:
  - [ ] `BlazorShop.Presentation`
  - [ ] `BlazorShop.AppHost`
  - [ ] `ConnectionStrings__DefaultConnection`
  - [ ] `DefaultConnection`
  - [ ] `AppDbContext`
  - [ ] `AddInfrastructure(`
  - [ ] `AddSharedAuthenticationInfrastructure(`
- [ ] `Inventory` scan toan repo va xuat danh sach grouped theo source/test/docs/legacy/runtime.
- [ ] Tao allowlist file ro rang, vi du `docs/refactor-control-Commerce-storefront/legacy-removal-allowlist.json`.
- [ ] Them architecture tests:
  - [ ] V2 projects khong ProjectReference legacy.
  - [ ] V2 Programs khong goi `AddInfrastructure`.
  - [ ] V2 runtime config khong yeu cau `DefaultConnection`.
  - [ ] Storefront V2 khong call legacy route groups.
  - [ ] Main solution con legacy thi duoc record baseline, sau Phase 2 doi thanh forbidden.
- [ ] Cap nhat `V2ProductionReadinessTests` hien co hoac tao test moi trong `BlazorShop.Tests/Architecture`.
- [ ] Ghi ro current blockers vao plan file sau khi script inventory chay lan dau.

### Verification

- [ ] `powershell -ExecutionPolicy Bypass -File scripts/verify-no-active-legacy-reference.ps1 -Mode Inventory`
- [ ] `powershell -ExecutionPolicy Bypass -File scripts/verify-no-active-legacy-reference.ps1 -Mode ActiveStrict`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --no-restore --filter "FullyQualifiedName~V2ProductionReadiness|FullyQualifiedName~V2ArchitectureBoundary"`

### Done when

- [ ] Guardrail khong chan migration bat dau.
- [ ] ActiveStrict co allowlist nho va co chu dich.
- [ ] Inventory cho thay day du legacy blockers con lai.

## Phase 1 - V2 production packaging and CI blocking gate

Goal: truoc khi xoa legacy, V2 phai co du artifact build/deploy that.

### Phase 1A - Dockerfile V2

- [ ] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.ControlPlane.API`.
- [ ] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API`.
- [ ] Tao Dockerfile cho `BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web`.
- [ ] Sua Dockerfile `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Dockerfile`:
  - [ ] Copy `BlazorShop.Storefront.Components.csproj` truoc `dotnet restore`.
  - [ ] Copy `BlazorShop.Storefront.WASM.csproj` truoc `dotnet restore`.
  - [ ] Copy source cua `Storefront.Components` va `Storefront.WASM` truoc publish.
  - [ ] Dung SDK/runtime phu hop `global.json` va project target.
- [ ] ControlPlane Web Dockerfile:
  - [ ] Publish Blazor WASM static assets.
  - [ ] Serve bang Nginx/static server.
  - [ ] Co SPA fallback.
  - [ ] Khong bake API secret/node secret vao client image.
- [ ] CommerceNode API image:
  - [ ] Khong can legacy `DefaultConnection`.
  - [ ] Dung `CommerceNodeConnection`.
  - [ ] Co persistent media/data-protection volume points documented.
- [ ] ControlPlane API image:
  - [ ] Dung `ControlPlaneConnection`.
  - [ ] Khong co Commerce Node secret trong Web image.

### Phase 1B - V2 production compose

- [ ] Tao `compose.v2.production.yml` trong transition.
- [ ] Services toi thieu:
  - [ ] `controlplane-postgres`.
  - [ ] `commercenode-postgres`.
  - [ ] `controlplane-api`.
  - [ ] `controlplane-web`.
  - [ ] `commercenode-api`.
  - [ ] `commercenode-nginx`.
  - [ ] `commercenode-imgproxy`.
  - [ ] `storefront-v2` hoac document ro store deployment task se tao container theo store.
- [ ] Config bat buoc:
  - [ ] `ConnectionStrings__ControlPlaneConnection`.
  - [ ] `ConnectionStrings__CommerceNodeConnection`.
  - [ ] Khong co `ConnectionStrings__DefaultConnection`.
  - [ ] `ControlPlane__Database__MigrateOnStartup`.
  - [ ] `CommerceNode__Database__MigrateOnStartup`.
  - [ ] Data Protection key ring persistent cho CommerceNode.
  - [ ] Media storage persistent cho CommerceNode.
  - [ ] Storefront env chi co `Api__BaseUrl`, `STORE_KEY`/`StoreKey`, public URL config can thiet.
- [ ] Khi `compose.v2.production.yml` pass, quyet dinh doi `compose.production.yml` thanh V2 canonical trong phase nay hoac Phase 2.

### Phase 1C - CI V2 blocking

- [ ] Tao job `ci-v2`.
- [ ] `ci-v2` run:
  - [ ] `dotnet restore BlazorShop.V2.slnf`.
  - [ ] `dotnet build BlazorShop.V2.slnf --configuration Release --no-restore`.
  - [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj --configuration Release --no-build --verbosity normal`.
- [ ] Build 4 Docker images V2 trong CI.
- [ ] Run `docker compose -f compose.v2.production.yml config`.
- [ ] Optional boot compose smoke neu runtime cost chap nhan:
  - [ ] ControlPlane API health.
  - [ ] CommerceNode API health.
  - [ ] ControlPlane Web HTTP 200.
  - [ ] Storefront V2 HTTP 200/maintenance expected.
- [ ] Chuyen legacy job thanh `legacy-compatibility` tam thoi, khong la release blocker.
- [ ] Sua npm cache path khong tro vao `BlazorShop.Presentation/BlazorShop.Web/package-lock.json`.

### Done when

- [ ] V2 image build tu clean checkout.
- [ ] V2 compose config pass.
- [ ] CI V2 la required release signal.
- [ ] Legacy CI khong con tao cam giac production dang deploy V2.

## Phase 2 - Main solution becomes V2 canonical

Goal: `BlazorShop.sln` tro thanh main solution active, khong can solution filter de ne legacy.

### Tasks

- [ ] Remove legacy projects khoi `BlazorShop.sln`:
  - [ ] `BlazorShop.Presentation/BlazorShop.API`.
  - [ ] `BlazorShop.Presentation/BlazorShop.Web`.
  - [ ] `BlazorShop.Presentation/BlazorShop.Web.Shared`.
  - [ ] `BlazorShop.Presentation/BlazorShop.Storefront`.
  - [ ] `BlazorShop.AppHost`.
  - [ ] `BlazorShop.Tests` old mixed project sau khi Phase 3 san sang; neu Phase 3 chua xong, defer remove old tests khoi solution den Phase 3.
- [ ] Giu trong solution:
  - [ ] `BlazorShop.Domain`.
  - [ ] `BlazorShop.Application`.
  - [ ] `BlazorShop.Infrastructure`.
  - [ ] `BlazorShop.ServiceDefaults`.
  - [ ] Tat ca `BlazorShop.PresentationV2/*`.
  - [ ] `BlazorShop.Tests.V2`.
- [ ] Sau khi `BlazorShop.sln` da sach:
  - [ ] Cap nhat CI dung `BlazorShop.sln` thay cho `BlazorShop.V2.slnf`.
  - [ ] Cap nhat README/docs active build commands.
  - [ ] Xoa `BlazorShop.V2.slnf` chi khi no khong con can cho transition.
- [ ] Update guardrail: main solution khong duoc co `BlazorShop.Presentation` hoac `BlazorShop.AppHost`.

### Verification

- [ ] `dotnet restore BlazorShop.sln`
- [ ] `dotnet build BlazorShop.sln -c Release --no-restore`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build`
- [ ] `rg "BlazorShop\\.Presentation|BlazorShop\\.AppHost" BlazorShop.sln` returns no active project entries.

### Done when

- [ ] Main solution build duoc khong can legacy project folders.
- [ ] `BlazorShop.V2.slnf` hoac da xoa, hoac chi con duoc ghi ro la temporary until Phase 3.

## Phase 3 - V2 test source ownership

Goal: `BlazorShop.Tests.V2` so huu source cua minh, khong link nguoc sang `BlazorShop.Tests`.

### Phase 3A - Move active V2 tests physically

- [ ] Di chuyen `BlazorShop.Tests/Architecture/**` vao `BlazorShop.Tests.V2/Architecture/**`.
- [ ] Di chuyen `BlazorShop.Tests/PresentationV2/**` vao `BlazorShop.Tests.V2/PresentationV2/**`.
- [ ] Di chuyen V2 snapshots vao `BlazorShop.Tests.V2/PresentationV2/**/Snapshots`.
- [ ] Remove `<Compile Include="..\BlazorShop.Tests\Architecture\**\*.cs"...>`.
- [ ] Remove `<Compile Include="..\BlazorShop.Tests\PresentationV2\**\*.cs"...>`.
- [ ] Remove `<None Include="..\BlazorShop.Tests\PresentationV2\CommerceNode\Snapshots\*"...>`.
- [ ] Ensure namespaces van hop ly hoac update namespace neu can.

### Phase 3B - Migrate V2 core tests

- [ ] Phan loai `BlazorShop.Tests/Application/**`:
  - [ ] V2 commerce/application tests -> move to `BlazorShop.Tests.V2/Application/**`.
  - [ ] Legacy-only authentication/payment tests -> delete or rewrite after review.
  - [ ] Shared business tests that still protect V2 -> move and adapt to V2 services.
- [ ] Phan loai `BlazorShop.Tests/Infrastructure/**`:
  - [ ] `Infrastructure/CommerceNode/**` -> move to V2 tests.
  - [ ] `Infrastructure/ControlPlane/**` if any -> move to V2 tests.
  - [ ] `AppDbContext`/legacy repository/admin service tests -> hold for Phase 5 purge or rewrite only if behavior needed by V2.
- [ ] Uu tien move:
  - [ ] Cart/session/sellability.
  - [ ] Checkout/pricing/payment attempt/order placement.
  - [ ] Currency/rounding.
  - [ ] Navigation/pages/SEO.
  - [ ] CommerceNodeDbContext model/migration.
  - [ ] Seeder idempotency.
  - [ ] Store SMTP/message queue.
  - [ ] Payment callback/webhook safety.
- [ ] Them guardrail dem expected V2 test files/namespaces de test khong bi rot khoi project khi move.

### Phase 3C - Retire old mixed test project

- [ ] Sau khi V2 tests du source doc lap, remove `BlazorShop.Tests` project reference khoi solution.
- [ ] Xoa `BlazorShop.Tests.csproj`.
- [ ] Xoa old test files legacy-only neu khong con gia tri.
- [ ] Neu muon giu comparison docs, archive note trong `docs/archive/legacy/`, khong giu runnable old mixed test project.
- [ ] Optional cleanup PR sau: rename `BlazorShop.Tests.V2` thanh `BlazorShop.Tests`; khong bat buoc cho legacy deletion.

### Verification

- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release`
- [ ] `rg "\\.\\.\\\\BlazorShop.Tests|\\.\\./BlazorShop.Tests" BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj` returns none.
- [ ] `rg "BlazorShop.AppHost|BlazorShop.Presentation" BlazorShop.Tests.V2` returns none except allowed documentation comments if any.

### Done when

- [ ] V2 test project doc lap ve source va snapshots.
- [ ] Xoa folder/test project cu khong lam mat V2 tests.

## Phase 4 - Remove legacy AppHost and operational entrypoints

Goal: xoa AppHost legacy va moi script/entrypoint active dang chay legacy runtime.

### Tasks

- [ ] Xoa `BlazorShop.AppHost` project folder.
- [ ] Remove AppHost project entry khoi solution neu chua xong Phase 2.
- [ ] Remove AppHost reference khoi tests/CI/docs.
- [ ] Xoa AppHost user-secrets references neu co.
- [ ] Rg scripts va docs:
  - [ ] `dotnet run --project BlazorShop.Presentation/BlazorShop.API`.
  - [ ] `dotnet run --project BlazorShop.Presentation/BlazorShop.Web`.
  - [ ] `dotnet run --project BlazorShop.Presentation/BlazorShop.Storefront`.
  - [ ] `BlazorShop.AppHost`.
- [ ] Giu `scripts/run-v2-local.ps1` la local entrypoint chinh.
- [ ] Khong tao AppHost V2 trong phase nay; neu can future local orchestration, tao plan rieng.

### Verification

- [ ] `rg "BlazorShop\\.AppHost|Projects\\.BlazorShop_API|DefaultConnection" BlazorShop.AppHost BlazorShop.sln scripts docs .github` khong co active hit sau cleanup.
- [ ] `.\scripts\run-v2-local.ps1 -StopExisting -NoOpenBrowser` starts V2 runtimes.
- [ ] ControlPlane API/Web, CommerceNode API, Storefront V2 health/smoke pass.

### Done when

- [ ] AppHost khong con trong repository hoac khong con active legacy dependency.
- [ ] Khong co operational command active nao chay API/Web/Storefront cu.

## Phase 5 - Purge dead legacy Infrastructure and AppDbContext

Goal: xoa persistence/runtime code chi con phuc vu legacy sau khi legacy consumers da bi loai khoi graph.

### Preconditions

- [ ] `BlazorShop.Presentation` khong con trong solution/CI active.
- [ ] `BlazorShop.AppHost` da xoa.
- [ ] `BlazorShop.Tests.V2` khong link source tu old tests.
- [ ] Inventory script cho thay remaining `AppDbContext` consumers chi nam trong legacy/dead areas.

### Tasks

- [ ] Build consumer graph cho:
  - [ ] `AppDbContext`.
  - [ ] `AddInfrastructure`.
  - [ ] `AddSharedAuthenticationInfrastructure`.
  - [ ] `UseInfrastructure`.
  - [ ] `DefaultConnection`.
  - [ ] Legacy repositories under `BlazorShop.Infrastructure/Repositories`.
  - [ ] Legacy admin services under `BlazorShop.Infrastructure/Services/Admin`.
  - [ ] Legacy payment/cart/order repositories that bind to `AppDbContext`.
- [ ] Delete `BlazorShop.Infrastructure/Data/AppDbContext.cs`.
- [ ] Delete `BlazorShop.Infrastructure/Data/AppDbContextFactory.cs`.
- [ ] Delete `BlazorShop.Infrastructure/Migrations/**` for AppDbContext.
- [ ] Delete `AddInfrastructure`, `AddSharedAuthenticationInfrastructure`, `UseInfrastructure` if no consumer.
- [ ] Delete health checks, seeders, repositories, services chi con dung `AppDbContext`.
- [ ] Remove `DefaultConnection` from active appsettings, examples, scripts, compose.
- [ ] Keep shared Application/Domain contracts only if active V2 still consumes them through ControlPlane/CommerceNode paths.
- [ ] If a V2 service still depends on legacy repository contract:
  - [ ] Replace with CommerceNode/ControlPlane implementation first.
  - [ ] Add focused test before deleting old implementation.
- [ ] Update architecture docs: `AppDbContext` no longer exists in active repo.

### Verification

- [ ] `rg "AppDbContext|DefaultConnection|AddInfrastructure\\(|AddSharedAuthenticationInfrastructure\\(|UseInfrastructure\\(" BlazorShop.Application BlazorShop.Infrastructure BlazorShop.PresentationV2 scripts .github compose*.yml`
- [ ] `dotnet build BlazorShop.sln -c Release --no-restore`
- [ ] `dotnet test BlazorShop.Tests.V2/BlazorShop.Tests.V2.csproj -c Release --no-build`
- [ ] EF migration/model tests for `ControlPlaneDbContext` and `CommerceNodeDbContext` pass.

### Done when

- [ ] Active source khong con AppDbContext legacy.
- [ ] V2 startup khong bao gio can `DefaultConnection`.
- [ ] Purge dua tren consumer graph, khong phai ten file.

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

- [ ] Phase 0 complete and committed.
- [ ] Phase 1A Dockerfiles complete and committed.
- [ ] Phase 1B compose V2 complete and committed.
- [ ] Phase 1C CI V2 blocking complete and committed.
- [ ] Phase 2 main solution V2 canonical complete and committed.
- [ ] Phase 3A V2 test source move complete and committed.
- [ ] Phase 3B V2 core test migration complete and committed.
- [ ] Phase 3C old mixed test retirement complete and committed.
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

