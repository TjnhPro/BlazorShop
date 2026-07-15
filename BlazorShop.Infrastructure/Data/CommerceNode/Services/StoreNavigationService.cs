namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class StoreNavigationService : IStoreNavigationService
    {
        private const int MaxLabelLength = 120;
        private const int MaxDisplayNameLength = 120;
        private const int MaxUrlLength = 500;

        private static readonly IReadOnlyDictionary<string, string> StaticRouteMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [StoreNavigationSystemTargets.Home] = "/",
                [StoreNavigationSystemTargets.Search] = "/search",
                [StoreNavigationSystemTargets.Cart] = "/my-cart",
                [StoreNavigationSystemTargets.Checkout] = "/checkout",
                [StoreNavigationSystemTargets.Account] = "/signin",
                [StoreNavigationSystemTargets.Login] = "/signin",
                [StoreNavigationSystemTargets.Register] = "/register",
                [StoreNavigationSystemTargets.NewReleases] = "/new-releases",
                [StoreNavigationSystemTargets.TodaysDeals] = "/todays-deals",
            };

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly IStorefrontNavigationCache cache;
        private readonly IAdminAuditService auditService;

        public StoreNavigationService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            IStorefrontNavigationCache cache,
            IAdminAuditService auditService)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.cache = cache;
            this.auditService = auditService;
        }

        public IReadOnlyList<StoreNavigationTargetOptionDto> ListSystemTargets()
        {
            return StoreNavigationSystemTargets.All
                .OrderBy(target => target)
                .Select(target => new StoreNavigationTargetOptionDto(
                    StoreNavigationTargetTypes.System,
                    target,
                    ToDisplayLabel(target),
                    StaticRouteMap.TryGetValue(target, out var href) ? href : null))
                .ToArray();
        }

        public async Task<ServiceResponse<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListMenusAsync(
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<IReadOnlyList<StoreNavigationMenuSummaryDto>>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var menus = await this.context.StoreNavigationMenus
                .AsNoTracking()
                .Where(menu => menu.StoreId == storeId && menu.ArchivedAt == null)
                .OrderBy(menu => menu.SystemName)
                .Select(menu => new StoreNavigationMenuSummaryDto(
                    menu.PublicId,
                    menu.SystemName,
                    menu.DisplayName,
                    menu.IsEnabled,
                    menu.UpdatedAt,
                    menu.Items.Count(item => item.ArchivedAt == null)))
                .ToListAsync(cancellationToken);

            return Success<IReadOnlyList<StoreNavigationMenuSummaryDto>>(menus, "Navigation menus retrieved.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> GetMenuAsync(
            Guid menuPublicId,
            CancellationToken cancellationToken = default)
        {
            var menu = await this.LoadMenuAsync(menuPublicId, asTracking: false, cancellationToken);
            return menu is null
                ? Failure<StoreNavigationMenuDetailDto>("Navigation menu was not found.", ServiceResponseType.NotFound)
                : Success(await this.MapAdminDetailAsync(menu, cancellationToken), "Navigation menu retrieved.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> CreateMenuAsync(
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<StoreNavigationMenuDetailDto>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var normalized = NormalizeMenu(request.SystemName, request.DisplayName);
            if (!normalized.Success)
            {
                return Failure<StoreNavigationMenuDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var duplicate = await this.context.StoreNavigationMenus.AnyAsync(
                menu => menu.StoreId == storeId && menu.SystemName == normalized.SystemName && menu.ArchivedAt == null,
                cancellationToken);
            if (duplicate)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu system name already exists for this store.", ServiceResponseType.Conflict);
            }

            var now = DateTimeOffset.UtcNow;
            var menu = new StoreNavigationMenu
            {
                StoreId = storeId.Value,
                SystemName = normalized.SystemName!,
                DisplayName = normalized.DisplayName!,
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.StoreNavigationMenus.Add(menu);
            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(storeId.Value);
            await this.LogAsync("StoreNavigation.MenuCreated", menu.Id, "Navigation menu created.", new { menu.SystemName, menu.DisplayName }, cancellationToken);

            return Success(await this.MapAdminDetailAsync(menu, cancellationToken), "Navigation menu created.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateMenuAsync(
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var menu = await this.LoadMenuAsync(menuPublicId, asTracking: true, cancellationToken);
            if (menu is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu was not found.", ServiceResponseType.NotFound);
            }

            var displayName = NormalizeRequired(request.DisplayName);
            if (displayName is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu display name is required.", ServiceResponseType.ValidationError);
            }

            if (displayName.Length > MaxDisplayNameLength)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu display name must be 120 characters or fewer.", ServiceResponseType.ValidationError);
            }

            menu.DisplayName = displayName;
            menu.IsEnabled = request.IsEnabled;
            menu.UpdatedAt = DateTimeOffset.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(menu.StoreId);
            await this.LogAsync("StoreNavigation.MenuUpdated", menu.Id, "Navigation menu updated.", new { menu.SystemName, menu.DisplayName, menu.IsEnabled }, cancellationToken);

            return Success(await this.MapAdminDetailAsync(menu, cancellationToken), "Navigation menu updated.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> CreateItemAsync(
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var menu = await this.LoadMenuAsync(menuPublicId, asTracking: true, cancellationToken);
            if (menu is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu was not found.", ServiceResponseType.NotFound);
            }

            var normalized = NormalizeItem(
                request.ParentItemPublicId,
                request.Label,
                request.TargetType,
                request.TargetKey,
                request.TargetEntityPublicId,
                request.Url,
                request.DisplayOrder);
            if (!normalized.Success)
            {
                return Failure<StoreNavigationMenuDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var parentId = await this.ResolveParentIdAsync(menu.StoreId, menu.Id, request.ParentItemPublicId, null, cancellationToken);
            if (parentId.Invalid)
            {
                return Failure<StoreNavigationMenuDetailDto>(parentId.Message!, ServiceResponseType.ValidationError);
            }

            var now = DateTimeOffset.UtcNow;
            var item = new StoreNavigationMenuItem
            {
                StoreId = menu.StoreId,
                MenuId = menu.Id,
                ParentItemId = parentId.Value,
                Label = normalized.Label!,
                TargetType = normalized.TargetType!,
                TargetKey = normalized.TargetKey,
                TargetEntityPublicId = normalized.TargetEntityPublicId,
                Url = normalized.Url,
                IsEnabled = request.IsEnabled,
                DisplayOrder = normalized.DisplayOrder,
                OpensInNewTab = request.OpensInNewTab,
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.StoreNavigationMenuItems.Add(item);
            menu.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(menu.StoreId);
            await this.LogAsync("StoreNavigation.ItemCreated", item.Id, "Navigation menu item created.", new { menu.SystemName, item.Label, item.TargetType }, cancellationToken);

            return Success(await this.ReloadAndMapAdminDetailAsync(menu.Id, cancellationToken), "Navigation menu item created.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateItemAsync(
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var item = await this.LoadItemAsync(itemPublicId, cancellationToken);
            if (item is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu item was not found.", ServiceResponseType.NotFound);
            }

            var normalized = NormalizeItem(
                request.ParentItemPublicId,
                request.Label,
                request.TargetType,
                request.TargetKey,
                request.TargetEntityPublicId,
                request.Url,
                request.DisplayOrder);
            if (!normalized.Success)
            {
                return Failure<StoreNavigationMenuDetailDto>(normalized.Message!, ServiceResponseType.ValidationError);
            }

            var parentId = await this.ResolveParentIdAsync(item.StoreId, item.MenuId, request.ParentItemPublicId, item.Id, cancellationToken);
            if (parentId.Invalid)
            {
                return Failure<StoreNavigationMenuDetailDto>(parentId.Message!, ServiceResponseType.ValidationError);
            }

            item.ParentItemId = parentId.Value;
            item.Label = normalized.Label!;
            item.TargetType = normalized.TargetType!;
            item.TargetKey = normalized.TargetKey;
            item.TargetEntityPublicId = normalized.TargetEntityPublicId;
            item.Url = normalized.Url;
            item.IsEnabled = request.IsEnabled;
            item.DisplayOrder = normalized.DisplayOrder;
            item.OpensInNewTab = request.OpensInNewTab;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.Menu!.UpdatedAt = item.UpdatedAt;

            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(item.StoreId);
            await this.LogAsync("StoreNavigation.ItemUpdated", item.Id, "Navigation menu item updated.", new { item.Label, item.TargetType, item.IsEnabled }, cancellationToken);

            return Success(await this.ReloadAndMapAdminDetailAsync(item.MenuId, cancellationToken), "Navigation menu item updated.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> ArchiveItemAsync(
            Guid itemPublicId,
            CancellationToken cancellationToken = default)
        {
            var item = await this.LoadItemAsync(itemPublicId, cancellationToken);
            if (item is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu item was not found.", ServiceResponseType.NotFound);
            }

            var now = DateTimeOffset.UtcNow;
            var descendants = await this.context.StoreNavigationMenuItems
                .Where(candidate => candidate.StoreId == item.StoreId && candidate.MenuId == item.MenuId && candidate.ArchivedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var descendant in FindDescendants(descendants, item.Id).Append(item))
            {
                descendant.IsEnabled = false;
                descendant.ArchivedAt = now;
                descendant.UpdatedAt = now;
            }

            item.Menu!.UpdatedAt = now;
            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(item.StoreId);
            await this.LogAsync("StoreNavigation.ItemArchived", item.Id, "Navigation menu item archived.", new { item.Label }, cancellationToken);

            return Success(await this.ReloadAndMapAdminDetailAsync(item.MenuId, cancellationToken), "Navigation menu item archived.");
        }

        public async Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateItemOrderAsync(
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var menu = await this.LoadMenuAsync(menuPublicId, asTracking: true, cancellationToken);
            if (menu is null)
            {
                return Failure<StoreNavigationMenuDetailDto>("Navigation menu was not found.", ServiceResponseType.NotFound);
            }

            var items = await this.context.StoreNavigationMenuItems
                .Where(item => item.StoreId == menu.StoreId && item.MenuId == menu.Id && item.ArchivedAt == null)
                .ToListAsync(cancellationToken);
            var byPublicId = items.ToDictionary(item => item.PublicId);

            foreach (var order in request.Items)
            {
                if (!byPublicId.TryGetValue(order.PublicId, out var item))
                {
                    return Failure<StoreNavigationMenuDetailDto>("Navigation menu item order contains an unknown item.", ServiceResponseType.ValidationError);
                }

                if (order.DisplayOrder < 0)
                {
                    return Failure<StoreNavigationMenuDetailDto>("Navigation menu item display order must be greater than or equal to zero.", ServiceResponseType.ValidationError);
                }

                var parent = order.ParentItemPublicId.HasValue
                    ? items.FirstOrDefault(candidate => candidate.PublicId == order.ParentItemPublicId.Value)
                    : null;
                if (order.ParentItemPublicId.HasValue && parent is null)
                {
                    return Failure<StoreNavigationMenuDetailDto>("Navigation menu item parent was not found in this menu.", ServiceResponseType.ValidationError);
                }

                if (parent is not null && CreatesCycle(items, item.Id, parent.Id))
                {
                    return Failure<StoreNavigationMenuDetailDto>("Navigation menu item parent would create a cycle.", ServiceResponseType.ValidationError);
                }

                item.ParentItemId = parent?.Id;
                item.DisplayOrder = order.DisplayOrder;
                item.UpdatedAt = DateTimeOffset.UtcNow;
            }

            menu.UpdatedAt = DateTimeOffset.UtcNow;
            await this.context.SaveChangesAsync(cancellationToken);
            this.cache.Invalidate(menu.StoreId);
            await this.LogAsync("StoreNavigation.ItemsReordered", menu.Id, "Navigation menu items reordered.", new { menu.SystemName, Count = request.Items.Count }, cancellationToken);

            return Success(await this.ReloadAndMapAdminDetailAsync(menu.Id, cancellationToken), "Navigation menu items reordered.");
        }

        public async Task<ServiceResponse<StoreNavigationPublicMenuDto>> GetPublicMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            var normalizedSystemName = NormalizeSystemName(systemName);
            if (!storeId.HasValue || normalizedSystemName is null)
            {
                return Failure<StoreNavigationPublicMenuDto>("Navigation menu was not found.", ServiceResponseType.NotFound);
            }

            if (this.cache.TryGet(storeId.Value, normalizedSystemName, out var cached) && cached is not null)
            {
                return Success(cached, "Navigation menu retrieved.");
            }

            var menu = await this.context.StoreNavigationMenus
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.StoreId == storeId &&
                        item.SystemName == normalizedSystemName &&
                        item.IsEnabled &&
                        item.ArchivedAt == null,
                    cancellationToken);
            if (menu is null)
            {
                return Failure<StoreNavigationPublicMenuDto>("Navigation menu was not found.", ServiceResponseType.NotFound);
            }

            var items = await this.context.StoreNavigationMenuItems
                .AsNoTracking()
                .Where(item => item.StoreId == storeId && item.MenuId == menu.Id && item.IsEnabled && item.ArchivedAt == null)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Label)
                .ToListAsync(cancellationToken);

            var projected = await this.BuildPublicItemsAsync(storeId.Value, items, null, cancellationToken);
            var response = new StoreNavigationPublicMenuDto(menu.SystemName, DateTimeOffset.UtcNow, projected);
            this.cache.Set(storeId.Value, normalizedSystemName, response);
            return Success(response, "Navigation menu retrieved.");
        }

        private async Task<StoreNavigationMenuDetailDto> ReloadAndMapAdminDetailAsync(Guid menuId, CancellationToken cancellationToken)
        {
            var menu = await this.context.StoreNavigationMenus
                .AsNoTracking()
                .FirstAsync(item => item.Id == menuId, cancellationToken);

            return await this.MapAdminDetailAsync(menu, cancellationToken);
        }

        private async Task<StoreNavigationMenuDetailDto> MapAdminDetailAsync(
            StoreNavigationMenu menu,
            CancellationToken cancellationToken)
        {
            var items = await this.context.StoreNavigationMenuItems
                .AsNoTracking()
                .Where(item => item.StoreId == menu.StoreId && item.MenuId == menu.Id && item.ArchivedAt == null)
                .OrderBy(item => item.DisplayOrder)
                .ThenBy(item => item.Label)
                .ToListAsync(cancellationToken);

            return new StoreNavigationMenuDetailDto(
                menu.PublicId,
                menu.SystemName,
                menu.DisplayName,
                menu.IsEnabled,
                menu.UpdatedAt,
                await this.BuildAdminItemsAsync(menu.StoreId, items, null, cancellationToken));
        }

        private async Task<IReadOnlyList<StoreNavigationMenuItemAdminDto>> BuildAdminItemsAsync(
            Guid storeId,
            IReadOnlyList<StoreNavigationMenuItem> items,
            Guid? parentId,
            CancellationToken cancellationToken)
        {
            var result = new List<StoreNavigationMenuItemAdminDto>();
            foreach (var item in items.Where(candidate => candidate.ParentItemId == parentId))
            {
                var resolution = await this.ResolveTargetAsync(storeId, item, requirePublished: false, cancellationToken);
                result.Add(new StoreNavigationMenuItemAdminDto(
                    item.PublicId,
                    items.FirstOrDefault(candidate => candidate.Id == item.ParentItemId)?.PublicId,
                    item.Label,
                    item.TargetType,
                    item.TargetKey,
                    item.TargetEntityPublicId,
                    item.Url,
                    item.IsEnabled,
                    item.DisplayOrder,
                    item.OpensInNewTab,
                    resolution.Status,
                    resolution.Href,
                    await this.BuildAdminItemsAsync(storeId, items, item.Id, cancellationToken)));
            }

            return result;
        }

        private async Task<IReadOnlyList<StoreNavigationPublicItemDto>> BuildPublicItemsAsync(
            Guid storeId,
            IReadOnlyList<StoreNavigationMenuItem> items,
            Guid? parentId,
            CancellationToken cancellationToken)
        {
            var result = new List<StoreNavigationPublicItemDto>();
            foreach (var item in items.Where(candidate => candidate.ParentItemId == parentId))
            {
                var children = await this.BuildPublicItemsAsync(storeId, items, item.Id, cancellationToken);
                var resolution = await this.ResolveTargetAsync(storeId, item, requirePublished: true, cancellationToken);
                if (item.TargetType == StoreNavigationTargetTypes.Group)
                {
                    if (children.Count > 0)
                    {
                        result.Add(new StoreNavigationPublicItemDto(
                            item.Label,
                            null,
                            item.TargetType,
                            item.TargetKey,
                            item.OpensInNewTab,
                            children));
                    }

                    continue;
                }

                if (resolution.Status != StoreNavigationTargetStatuses.Ok || string.IsNullOrWhiteSpace(resolution.Href))
                {
                    continue;
                }

                result.Add(new StoreNavigationPublicItemDto(
                    item.Label,
                    resolution.Href,
                    item.TargetType,
                    item.TargetKey,
                    item.OpensInNewTab,
                    children));
            }

            return result;
        }

        private async Task<TargetResolution> ResolveTargetAsync(
            Guid storeId,
            StoreNavigationMenuItem item,
            bool requirePublished,
            CancellationToken cancellationToken)
        {
            if (item.TargetType is StoreNavigationTargetTypes.System or StoreNavigationTargetTypes.InternalRoute)
            {
                return !string.IsNullOrWhiteSpace(item.TargetKey) && StaticRouteMap.TryGetValue(item.TargetKey, out var href)
                    ? TargetResolution.Ok(href)
                    : TargetResolution.Invalid();
            }

            if (item.TargetType == StoreNavigationTargetTypes.ExternalUrl)
            {
                return IsHttpsUrl(item.Url) ? TargetResolution.Ok(item.Url!) : TargetResolution.Invalid();
            }

            if (item.TargetType == StoreNavigationTargetTypes.Group)
            {
                return TargetResolution.Ok(null);
            }

            if (!item.TargetEntityPublicId.HasValue)
            {
                return TargetResolution.Invalid();
            }

            if (item.TargetType == StoreNavigationTargetTypes.Page)
            {
                var page = await this.context.StorefrontPages
                    .AsNoTracking()
                    .Where(page =>
                        page.StoreId == storeId &&
                        page.PublicId == item.TargetEntityPublicId &&
                        page.ArchivedAt == null &&
                        (!requirePublished || page.IsPublished))
                    .Select(page => new { page.Slug })
                    .FirstOrDefaultAsync(cancellationToken);
                return page is null ? TargetResolution.Broken() : TargetResolution.Ok($"/pages/{Uri.EscapeDataString(page.Slug)}");
            }

            if (item.TargetType == StoreNavigationTargetTypes.Category)
            {
                var category = await this.context.Categories
                    .AsNoTracking()
                    .Where(category =>
                        category.StoreId == storeId &&
                        category.Id == item.TargetEntityPublicId &&
                        category.ArchivedAt == null &&
                        !string.IsNullOrWhiteSpace(category.Slug) &&
                        (!requirePublished || category.IsPublished))
                    .Select(category => new { category.Slug })
                    .FirstOrDefaultAsync(cancellationToken);
                return category is null ? TargetResolution.Broken() : TargetResolution.Ok($"/category/{Uri.EscapeDataString(category.Slug!)}");
            }

            if (item.TargetType == StoreNavigationTargetTypes.Product)
            {
                var product = await this.context.Products
                    .AsNoTracking()
                    .Where(product =>
                        product.StoreId == storeId &&
                        product.Id == item.TargetEntityPublicId &&
                        product.ArchivedAt == null &&
                        !string.IsNullOrWhiteSpace(product.Slug) &&
                        (!requirePublished || (product.IsPublished && product.PublishedOn != null)))
                    .Select(product => new { product.Slug })
                    .FirstOrDefaultAsync(cancellationToken);
                return product is null ? TargetResolution.Broken() : TargetResolution.Ok($"/product/{Uri.EscapeDataString(product.Slug!)}");
            }

            return TargetResolution.Invalid();
        }

        private async Task<StoreNavigationMenu?> LoadMenuAsync(
            Guid publicId,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue || publicId == Guid.Empty)
            {
                return null;
            }

            var menus = asTracking ? this.context.StoreNavigationMenus : this.context.StoreNavigationMenus.AsNoTracking();
            return await menus.FirstOrDefaultAsync(
                menu => menu.StoreId == storeId && (menu.PublicId == publicId || menu.Id == publicId) && menu.ArchivedAt == null,
                cancellationToken);
        }

        private async Task<StoreNavigationMenuItem?> LoadItemAsync(Guid publicId, CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue || publicId == Guid.Empty)
            {
                return null;
            }

            return await this.context.StoreNavigationMenuItems
                .Include(item => item.Menu)
                .FirstOrDefaultAsync(
                    item => item.StoreId == storeId && (item.PublicId == publicId || item.Id == publicId) && item.ArchivedAt == null,
                    cancellationToken);
        }

        private async Task<ParentResolution> ResolveParentIdAsync(
            Guid storeId,
            Guid menuId,
            Guid? parentPublicId,
            Guid? currentItemId,
            CancellationToken cancellationToken)
        {
            if (!parentPublicId.HasValue)
            {
                return ParentResolution.Valid(null);
            }

            var items = await this.context.StoreNavigationMenuItems
                .Where(item => item.StoreId == storeId && item.MenuId == menuId && item.ArchivedAt == null)
                .ToListAsync(cancellationToken);
            var parent = items.FirstOrDefault(item => item.PublicId == parentPublicId.Value || item.Id == parentPublicId.Value);
            if (parent is null)
            {
                return ParentResolution.InvalidValue("Navigation menu item parent was not found in this menu.");
            }

            if (currentItemId.HasValue && CreatesCycle(items, currentItemId.Value, parent.Id))
            {
                return ParentResolution.InvalidValue("Navigation menu item parent would create a cycle.");
            }

            return ParentResolution.Valid(parent.Id);
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }

        private static bool CreatesCycle(IReadOnlyList<StoreNavigationMenuItem> items, Guid currentItemId, Guid parentItemId)
        {
            var currentParentId = parentItemId;
            while (currentParentId != Guid.Empty)
            {
                if (currentParentId == currentItemId)
                {
                    return true;
                }

                var parent = items.FirstOrDefault(item => item.Id == currentParentId);
                if (parent?.ParentItemId is null)
                {
                    return false;
                }

                currentParentId = parent.ParentItemId.Value;
            }

            return false;
        }

        private static IEnumerable<StoreNavigationMenuItem> FindDescendants(
            IReadOnlyList<StoreNavigationMenuItem> items,
            Guid parentId)
        {
            foreach (var child in items.Where(item => item.ParentItemId == parentId))
            {
                yield return child;
                foreach (var descendant in FindDescendants(items, child.Id))
                {
                    yield return descendant;
                }
            }
        }

        private static NormalizedMenu NormalizeMenu(string? systemName, string? displayName)
        {
            var normalizedSystemName = NormalizeSystemName(systemName);
            if (normalizedSystemName is null)
            {
                return NormalizedMenu.Failed("Navigation menu system name is not supported.");
            }

            var normalizedDisplayName = NormalizeRequired(displayName);
            if (normalizedDisplayName is null)
            {
                return NormalizedMenu.Failed("Navigation menu display name is required.");
            }

            if (normalizedDisplayName.Length > MaxDisplayNameLength)
            {
                return NormalizedMenu.Failed("Navigation menu display name must be 120 characters or fewer.");
            }

            return NormalizedMenu.Succeeded(normalizedSystemName, normalizedDisplayName);
        }

        private static NormalizedItem NormalizeItem(
            Guid? parentItemPublicId,
            string? label,
            string? targetType,
            string? targetKey,
            Guid? targetEntityPublicId,
            string? url,
            int displayOrder)
        {
            var normalizedLabel = NormalizeRequired(label);
            if (normalizedLabel is null)
            {
                return NormalizedItem.Failed("Navigation menu item label is required.");
            }

            if (normalizedLabel.Length > MaxLabelLength)
            {
                return NormalizedItem.Failed("Navigation menu item label must be 120 characters or fewer.");
            }

            var normalizedTargetType = NormalizeTargetType(targetType);
            if (normalizedTargetType is null)
            {
                return NormalizedItem.Failed("Navigation menu item target type is not supported.");
            }

            if (displayOrder < 0)
            {
                return NormalizedItem.Failed("Navigation menu item display order must be greater than or equal to zero.");
            }

            var normalizedTargetKey = NormalizeOptional(targetKey)?.ToLowerInvariant();
            var normalizedUrl = NormalizeOptional(url);

            if (normalizedTargetType == StoreNavigationTargetTypes.System)
            {
                if (normalizedTargetKey is null || !StoreNavigationSystemTargets.IsKnown(normalizedTargetKey))
                {
                    return NormalizedItem.Failed("Navigation menu item system target is not supported.");
                }

                return NormalizedItem.Succeeded(parentItemPublicId, normalizedLabel, normalizedTargetType, normalizedTargetKey, null, null, displayOrder);
            }

            if (normalizedTargetType == StoreNavigationTargetTypes.InternalRoute)
            {
                if (normalizedTargetKey is null || !StoreNavigationInternalRoutes.IsKnown(normalizedTargetKey))
                {
                    return NormalizedItem.Failed("Navigation menu item internal route is not supported.");
                }

                return NormalizedItem.Succeeded(parentItemPublicId, normalizedLabel, normalizedTargetType, normalizedTargetKey, null, null, displayOrder);
            }

            if (normalizedTargetType is StoreNavigationTargetTypes.Category or StoreNavigationTargetTypes.Page or StoreNavigationTargetTypes.Product)
            {
                return !targetEntityPublicId.HasValue || targetEntityPublicId.Value == Guid.Empty
                    ? NormalizedItem.Failed("Navigation menu item target entity is required.")
                    : NormalizedItem.Succeeded(parentItemPublicId, normalizedLabel, normalizedTargetType, null, targetEntityPublicId, null, displayOrder);
            }

            if (normalizedTargetType == StoreNavigationTargetTypes.ExternalUrl)
            {
                if (!IsHttpsUrl(normalizedUrl))
                {
                    return NormalizedItem.Failed("Navigation menu item external URL must be an absolute HTTPS URL.");
                }

                if (normalizedUrl!.Length > MaxUrlLength)
                {
                    return NormalizedItem.Failed("Navigation menu item external URL must be 500 characters or fewer.");
                }

                return NormalizedItem.Succeeded(parentItemPublicId, normalizedLabel, normalizedTargetType, null, null, normalizedUrl, displayOrder);
            }

            return normalizedTargetType == StoreNavigationTargetTypes.Group
                ? NormalizedItem.Succeeded(parentItemPublicId, normalizedLabel, normalizedTargetType, null, null, null, displayOrder)
                : NormalizedItem.Failed("Navigation menu item target type is not supported.");
        }

        private async Task LogAsync(string action, Guid entityId, string summary, object metadata, CancellationToken cancellationToken)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "StoreNavigation",
                EntityId = entityId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
        }

        private static string? NormalizeSystemName(string? value)
        {
            var normalized = NormalizeOptional(value)?.ToLowerInvariant();
            return normalized is not null && StoreNavigationMenuNames.IsKnown(normalized) ? normalized : null;
        }

        private static string? NormalizeTargetType(string? value)
        {
            var normalized = NormalizeOptional(value)?.ToLowerInvariant();
            return normalized is not null && StoreNavigationTargetTypes.IsKnown(normalized) ? normalized : null;
        }

        private static string? NormalizeRequired(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool IsHttpsUrl(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
        }

        private static string ToDisplayLabel(string value)
        {
            return string.Join(' ', value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }

        private static ServiceResponse<TPayload> Success<TPayload>(TPayload payload, string message)
        {
            return new ServiceResponse<TPayload>(true, message)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<TPayload> Failure<TPayload>(string message, ServiceResponseType responseType)
        {
            return new ServiceResponse<TPayload>(false, message)
            {
                ResponseType = responseType,
            };
        }

        private sealed record TargetResolution(string Status, string? Href)
        {
            public static TargetResolution Ok(string? href)
            {
                return new TargetResolution(StoreNavigationTargetStatuses.Ok, href);
            }

            public static TargetResolution Broken()
            {
                return new TargetResolution(StoreNavigationTargetStatuses.Broken, null);
            }

            public static TargetResolution Invalid()
            {
                return new TargetResolution(StoreNavigationTargetStatuses.Invalid, null);
            }
        }

        private sealed record NormalizedMenu(bool Success, string? SystemName, string? DisplayName, string? Message = null)
        {
            public static NormalizedMenu Succeeded(string systemName, string displayName)
            {
                return new NormalizedMenu(true, systemName, displayName);
            }

            public static NormalizedMenu Failed(string message)
            {
                return new NormalizedMenu(false, null, null, message);
            }
        }

        private sealed record NormalizedItem(
            bool Success,
            Guid? ParentItemPublicId,
            string? Label,
            string? TargetType,
            string? TargetKey,
            Guid? TargetEntityPublicId,
            string? Url,
            int DisplayOrder,
            string? Message = null)
        {
            public static NormalizedItem Succeeded(
                Guid? parentItemPublicId,
                string label,
                string targetType,
                string? targetKey,
                Guid? targetEntityPublicId,
                string? url,
                int displayOrder)
            {
                return new NormalizedItem(true, parentItemPublicId, label, targetType, targetKey, targetEntityPublicId, url, displayOrder);
            }

            public static NormalizedItem Failed(string message)
            {
                return new NormalizedItem(false, null, null, null, null, null, null, 0, message);
            }
        }

        private sealed record ParentResolution(bool Invalid, Guid? Value, string? Message)
        {
            public static ParentResolution Valid(Guid? value)
            {
                return new ParentResolution(false, value, null);
            }

            public static ParentResolution InvalidValue(string message)
            {
                return new ParentResolution(true, null, message);
            }
        }
    }
}
