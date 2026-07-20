# Product Image Gallery 1x1.todo.md

## Goal

Them gallery anh cho trang chi tiet product trong Storefront V2, voi quy uoc hien thi tat ca anh product theo ty le 1:1.

Muc tieu cua phase nay la nang product detail tu 1 anh dai dien sang danh sach anh co thu tu, co anh chinh, co thumbnail, khong rewrite media core va khong pha contract hien co dang dung `Image`.

## Codebase facts

- `ProductMedia` da ton tai trong `BlazorShop.Domain/Entities/CommerceNode/ProductMedia.cs` va da co du thong tin can cho gallery: `ProductId`, `StoreId`, `PublicId`, `SortOrder`, `IsPrimary`, `AltText`, `Status`, `Version`, kich thuoc file va timestamp.
- `ProductMediaService` da co flow admin quan ly media: list, set primary, update order, delete, retry. Khi set primary service da dong bo `Product.Image` ve public URL cua anh chinh.
- Storefront product detail hien tai chi doc va render 1 field `_product.Image` trong `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor`.
- `GetProduct` trong `BlazorShop.Application/DTOs/Product/GetProduct.cs` chua co gallery.
- `ProductBase` van can giu `Image` de khong pha listing/search/cart/order projection dang phu thuoc anh dai dien.
- `CommerceNodeProductReadRepository` hien dang load product detail, variants va variation template, nhung chua load danh sach `ProductMedia`.
- Smartstore reference cho thay huong dung la media gallery model gom main image + thumbnails + fallback, sort theo display order, khong gan gallery vao product list mac dinh.

## Scope

- Them gallery cho public product detail.
- Anh product hien thi theo khung vuong 1:1 o main image va thumbnail.
- Giu product listing, search result, cart line, checkout/order snapshot dung primary image hien co.
- Giu `Product.Image` nhu compatibility/primary-image fallback.
- Khong them storage provider moi, khong rewrite media import, khong doi Control Plane media management trong phase nay.

## Non-goals

- Khong lam variant image mapping trong phase nay.
- Khong lam zoom/lightbox nang cao neu chua can cho release.
- Khong them setting quan tri so luong anh gallery toi da.
- Khong thay doi media storage abstraction, imgproxy, S3/local storage policy.
- Khong xoa `Product.Image` cho den khi tat ca consumer da co du read model rieng.

## Decisions

- Gallery data source la `ProductMedia` da `Stored`, chua bi soft-delete, dung `StoreId` cua product/store context.
- Thu tu hien thi: primary image dau tien, sau do `SortOrder`, sau do `CreatedAt`/`Id` de on dinh.
- Neu gallery rong, Storefront fallback ve `_product.Image`.
- Neu `_product.Image` cung rong, Storefront hien placeholder square 1:1.
- UI gallery nen la Blazor/component state don gian hoac HTML/CSS + JS nho tuy theo render mode hien co cua `ProductPage`; khong bat buoc chuyen product page sang WASM trong phase nay.

## Phase 0 - Characterization and contract guard

- [x] Xac nhan route product detail Storefront V2 hien dung `GetPublishedProductBySlugAsync` va response cu van co `Image`.
- [x] Xac nhan Commerce Node Storefront API contract hien tai co `StorefrontProductResponse.Image` va chua co gallery.
- [x] Xac nhan danh sach consumer cua `GetProduct.Image`, `StorefrontProductResponse.Image`, `GetCatalogProduct.Image`.
- [x] Them/Cap nhat characterization test cho product detail API de khoa behavior hien co: product published tra ve `Image`, variants, variation template nhu truoc.
- [x] Khong doi endpoint URL, route name, auth policy, store scope.

## Phase 1 - Public gallery read model

- [x] Tao DTO nho cho gallery, vi du `ProductGalleryImageDto`.
- [ ] Field de xuat:
  - [x] `Guid PublicId`.
  - [x] `string ImageUrl`.
  - [x] `string? ThumbnailUrl`.
  - [x] `string? FullSizeUrl`.
  - [x] `string? AltText`.
  - [x] `int SortOrder`.
  - [x] `bool IsPrimary`.
  - [x] `int? Width`.
  - [x] `int? Height`.
  - [x] `int Version`.
- [x] Them `IReadOnlyList<ProductGalleryImageDto> MediaGallery` vao `GetProduct` voi default empty list de backward-compatible.
- [x] Load gallery tu `CommerceNodeDbContext.ProductMedia` bang query rieng trong Application/Infrastructure read path, tranh thay doi lon EF include graph cua product detail.
- [ ] Filter bat buoc:
  - [x] `ProductId == product.Id`.
  - [x] `StoreId == current store id`.
  - [x] `DeletedAt == null`.
  - [x] `Status == Stored`.
- [x] Map URL bang media URL contract hien co, uu tien public URL dang duoc `ProductMediaService` su dung.
- [x] Dam bao primary media trong gallery khop voi `Product.Image` khi co du lieu; neu lech thi khong tu sua data trong read path.

## Phase 2 - Storefront API contract extension

- [x] Them gallery vao `StorefrontProductResponse` neu endpoint Storefront API dang map qua contract nay.
- [x] Cap nhat `CatalogMappings` de map gallery tu `GetProduct`.
- [x] Giu `Image` trong response de client cu khong vo.
- [x] Neu `StorefrontApiClient` Storefront V2 van deserialize truc tiep ve `GetProduct`, dam bao JSON property moi khong lam hong client hien co.
- [x] Cap nhat OpenAPI/contract test cho product detail response schema co gallery, nhung `MediaGallery` khong bat buoc phai co item.
- [x] Khong de public response lo storage path noi bo, file system path, provider bucket/key rieng, error message internal.

## Phase 3 - Storefront product detail UI

- [x] Tach logic hien anh trong `ProductPage.razor` thanh mot block/component gallery nho neu giup file de doc hon.
- [ ] Main image:
  - [x] Khung co `aspect-ratio: 1 / 1`.
  - [x] `object-fit: cover` hoac `contain` can duoc chon ro; uu tien `cover` neu product media da duoc crop/thumbnail 1:1, uu tien `contain` neu can tranh cat mat san pham.
  - [x] Khong lam layout shift khi anh load cham.
- [ ] Thumbnail:
  - [x] Moi thumbnail la khung 1:1.
  - [x] Desktop hien grid/rail ngan ben duoi hoac ben trai main image.
  - [x] Mobile hien horizontal scroll thumbnail strip.
  - [x] Thumbnail dang chon co active state ro rang nhung khong lam thay doi kich thuoc layout.
- [ ] Interaction:
  - [x] Click/tap thumbnail doi main image.
  - [x] Keyboard focus duoc tren thumbnail button.
  - [x] `alt` lay tu `AltText`, fallback product name.
- [ ] Fallback:
  - [x] Co gallery thi dung gallery.
  - [x] Khong co gallery nhung co `_product.Image` thi tao 1 item fallback.
  - [x] Khong co anh thi hien placeholder 1:1 nhu hien tai nhung khong pha layout.
- [x] Khong thay doi add-to-cart, variant selection, cart endpoint, checkout flow trong phase nay.

## Phase 4 - Square image policy

- [ ] Chuan hoa CSS cua product detail de main image va thumbnail luon 1:1 bat ke source image doc/ngang.
- [ ] Kiem tra desktop, tablet, mobile khong bi overflow, overlap hoac crop qua muc.
- [ ] Neu can crop server-side sau nay, ghi thanh future task cho media processing; phase nay chi bat buoc UI frame 1:1.
- [ ] Khong ep admin upload anh 1:1 trong phase nay vi se tao friction va co the pha media hien co.

## Phase 5 - QA and Playwright browser coverage

- [ ] Cap nhat `QA-StorefrontV2.todo.md` voi case product gallery.
- [ ] Cap nhat `Storefront Playwright E2E Release.todo.md` voi case release browser that.
- [ ] Tao/seed fixture product co it nhat 3 anh stored, 1 anh primary, co `SortOrder` khac nhau.
- [ ] Playwright case: product detail hien main image 1:1 va thumbnail list.
- [ ] Playwright case: click thumbnail thu 2 thi main image doi dung URL/alt.
- [ ] Playwright case: mobile viewport thumbnail strip khong overlap thong tin gia/add-to-cart.
- [ ] Playwright case: product chi co `Image` legacy nhung khong co `ProductMedia` van hien 1 anh.
- [ ] Playwright case: product khong co anh hien placeholder square.
- [ ] Playwright case: media URL tra 200 hoac fallback hop le, khong co broken image visible.

## Phase 6 - Cleanup and future backlog

- [ ] Ghi chu future task cho variant-specific image khi `ProductVariantAttribute`/combination selection da on dinh.
- [ ] Ghi chu future task cho lightbox/zoom neu can merchandising cao hon.
- [ ] Ghi chu future task cho structured data `image` array trong SEO phase neu can.
- [ ] Ghi chu future task cho admin validation/crop hint 1:1, khong chen vao phase nay.
- [ ] Sau khi tat ca public consumer dung gallery/read model moi, moi xem xet giam phu thuoc vao `Product.Image`.

## Acceptance criteria

- [ ] Product detail Storefront V2 co the hien nhieu anh product theo thu tu quan tri.
- [ ] Anh main va thumbnail luon nam trong khung 1:1 tren desktop/mobile.
- [ ] Product cu chi co `Image` khong bi mat anh.
- [ ] Product listing/search/cart/checkout/order projection khong doi behavior.
- [ ] API khong tra storage path, internal error, private media metadata.
- [ ] Store scope cua media gallery duoc filter theo store hien tai.
- [ ] Playwright browser test pass cho gallery, fallback va mobile layout.

## Expected files to touch

- [ ] `BlazorShop.Application/DTOs/Product/GetProduct.cs`
- [ ] `BlazorShop.Application/DTOs/Product/ProductGalleryImageDto.cs` hoac vi tri DTO tuong duong.
- [ ] `BlazorShop.Application/Services/PublicCatalogService.cs`
- [ ] `BlazorShop.Application/Interfaces/IProductReadRepository.cs` neu can them contract read gallery.
- [ ] `BlazorShop.Infrastructure/Data/CommerceNode/Repositories/CommerceNodeProductReadRepository.cs`
- [ ] `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CatalogContracts.cs`
- [ ] `BlazorShop.PresentationV2/BlazorShop.CommerceNode.API/Contracts/Storefront/CatalogMappings.cs`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/ProductPage.razor`
- [ ] `BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot` CSS/JS file neu can.
- [ ] `docs/refactor-control-Commerce-storefront/QA-StorefrontV2.todo.md`
- [ ] `docs/refactor-control-Commerce-storefront/Storefront Playwright E2E Release.todo.md`

## Risk controls

- [ ] Them field moi vao response thay vi doi/xoa field cu.
- [ ] Khong thay doi schema database neu `ProductMedia` hien tai da dap ung.
- [ ] Khong doi media URL public contract.
- [ ] Khong thay doi product search/listing query de tranh anh huong performance catalog list.
- [ ] Neu gallery query fail, endpoint phai fail ro rang trong test/dev thay vi im lang tra sai store media.

## Autoplan review

### CEO review

Pham vi nay hop ly cho MVP len production vi anh san pham nhieu goc la nhu cau that cua ecommerce. Khong nen mo rong sang zoom, variant gallery, crop pipeline hay storage provider trong cung phase.

### Design review

Quy uoc 1:1 giup grid, thumbnail va product detail on dinh hon. UI can uu tien main image lon, thumbnail ro, mobile scroll gon, khong them decoration lam phan tan khoi gia va add-to-cart.

### Engineering review

Huong an toan nhat la mo rong read model tu `ProductMedia` hien co va giu `Product.Image` lam fallback. Khong nen query gallery cho product list. Khong nen gan gallery vao checkout/order snapshot vi order chi can primary image/display snapshot.

### DX and QA review

Can co fixture co nhieu anh that va Playwright click thumbnail tren browser that. Snapshot/contract test can dam bao field moi backward-compatible va khong leak internal media path.
