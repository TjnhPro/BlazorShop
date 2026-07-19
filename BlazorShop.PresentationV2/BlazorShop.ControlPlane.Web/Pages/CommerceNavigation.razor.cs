namespace BlazorShop.ControlPlane.Web.Pages
{
    using System.Globalization;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.ControlPlane.Web.Services.Nodes;
    using BlazorShop.ControlPlane.Web.Services.Stores;
    using BlazorShop.ControlPlane.Web.Services.Users;
    using BlazorShop.Domain.Contracts;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    public partial class CommerceNavigation
    {
        private static readonly string[] MenuSystemNames =
        [
            StoreNavigationMenuNames.Main,
            StoreNavigationMenuNames.FooterCompany,
            StoreNavigationMenuNames.FooterSupport,
            StoreNavigationMenuNames.FooterLegal,
            StoreNavigationMenuNames.Utility,
            StoreNavigationMenuNames.Mobile,
        ];

        private static readonly string[] TargetTypes =
        [
            StoreNavigationTargetTypes.System,
            StoreNavigationTargetTypes.Category,
            StoreNavigationTargetTypes.Page,
            StoreNavigationTargetTypes.Product,
            StoreNavigationTargetTypes.ExternalUrl,
            StoreNavigationTargetTypes.Group,
            StoreNavigationTargetTypes.InternalRoute,
        ];

        private readonly List<StoreSummary> stores = [];
        private readonly List<StoreNavigationMenuSummaryDto> menus = [];
        private readonly List<StoreNavigationTargetOptionDto> systemTargets = [];
        private Guid? selectedStorePublicId;
        private StoreNavigationMenuDetailDto? selectedMenu;
        private MenuFormState menuForm = new();
        private ItemFormState itemForm = new();
        private bool isLoading;
        private bool isSavingMenu;
        private bool isSavingItem;
        private bool isItemDrawerOpen;
        private string? errorMessage;
        private string? successMessage;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private bool IsEditingMenu => selectedMenu is not null;

        private string MenuFormTitle => IsEditingMenu ? "Menu settings" : "Create menu";

        private string SelectedMenuTitle => selectedMenu?.DisplayName ?? "Menu items";

        private string SelectedMenuSubtitle => selectedMenu is null
            ? "No menu selected."
            : $"{selectedMenu.SystemName} Â· {(selectedMenu.IsEnabled ? "enabled" : "disabled")}";

        private string ItemFormTitle => itemForm.PublicId.HasValue ? "Edit item" : "Create item";

        private IReadOnlyList<ItemRow> FlattenedItems => selectedMenu is null ? [] : Flatten(selectedMenu.Items, 0).ToArray();

        protected override async Task OnInitializedAsync()
        {
            var response = await StoreClient.ListAsync(status: "active", pageSize: 100);
            stores.AddRange(response.Items);
            selectedStorePublicId = stores.FirstOrDefault()?.PublicId;
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isLoading = true;
            errorMessage = null;
            try
            {
                var menuResult = await NavigationClient.ListNavigationMenusAsync(selectedStorePublicId!.Value);
                if (!menuResult.Success)
                {
                    errorMessage = menuResult.Message;
                    return;
                }

                menus.Clear();
                menus.AddRange(menuResult.Data ?? []);

                var targetResult = await NavigationClient.ListNavigationSystemTargetsAsync(selectedStorePublicId.Value);
                if (targetResult.Success)
                {
                    systemTargets.Clear();
                    systemTargets.AddRange(targetResult.Data ?? []);
                }

                if (selectedMenu is not null && menus.Any(menu => menu.PublicId == selectedMenu.PublicId))
                {
                    await SelectMenuAsync(selectedMenu.PublicId);
                }
                else
                {
                    selectedMenu = null;
                    menuForm = new MenuFormState();
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SelectMenuAsync(Guid menuPublicId)
        {
            if (!HasStore)
            {
                return;
            }

            var result = await NavigationClient.GetNavigationMenuAsync(selectedStorePublicId!.Value, menuPublicId);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedMenu = result.Data;
            menuForm = MenuFormState.From(result.Data);
        }

        private void NewMenu()
        {
            selectedMenu = null;
            menuForm = new MenuFormState
            {
                SystemName = MenuSystemNames.First(),
                DisplayName = "Main",
                IsEnabled = true,
            };
        }

        private async Task SaveMenuAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isSavingMenu = true;
            errorMessage = null;
            try
            {
                var result = selectedMenu is null
                    ? await NavigationClient.CreateNavigationMenuAsync(selectedStorePublicId!.Value, menuForm.ToCreateRequest())
                    : await NavigationClient.UpdateNavigationMenuAsync(selectedStorePublicId!.Value, selectedMenu.PublicId, menuForm.ToUpdateRequest());
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedMenu = result.Data;
                menuForm = MenuFormState.From(result.Data);
                successMessage = "Navigation menu saved.";
                await LoadAsync();
            }
            finally
            {
                isSavingMenu = false;
            }
        }

        private void NewItem()
        {
            itemForm = new ItemFormState
            {
                TargetType = StoreNavigationTargetTypes.System,
                TargetKey = systemTargets.FirstOrDefault()?.Key ?? StoreNavigationSystemTargets.Home,
                IsEnabled = true,
            };
            isItemDrawerOpen = true;
        }

        private void EditItem(StoreNavigationMenuItemAdminDto item)
        {
            itemForm = ItemFormState.From(item);
            isItemDrawerOpen = true;
        }

        private async Task SaveItemAsync()
        {
            if (!HasStore || selectedMenu is null)
            {
                return;
            }

            isSavingItem = true;
            errorMessage = null;
            try
            {
                var result = itemForm.PublicId.HasValue
                    ? await NavigationClient.UpdateNavigationItemAsync(selectedStorePublicId!.Value, itemForm.PublicId.Value, itemForm.ToUpdateRequest())
                    : await NavigationClient.CreateNavigationItemAsync(selectedStorePublicId!.Value, selectedMenu.PublicId, itemForm.ToCreateRequest());
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedMenu = result.Data;
                successMessage = "Navigation item saved.";
                isItemDrawerOpen = false;
                await LoadAsync();
            }
            finally
            {
                isSavingItem = false;
            }
        }

        private async Task ArchiveItemAsync(Guid itemPublicId)
        {
            if (!HasStore || selectedMenu is null)
            {
                return;
            }

            var result = await NavigationClient.ArchiveNavigationItemAsync(selectedStorePublicId!.Value, itemPublicId);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedMenu = result.Data;
            successMessage = "Navigation item archived.";
            await LoadAsync();
        }

        private Task OnItemDrawerChanged(bool value)
        {
            isItemDrawerOpen = value;
            return Task.CompletedTask;
        }

        private static IEnumerable<ItemRow> Flatten(IEnumerable<StoreNavigationMenuItemAdminDto> items, int depth)
        {
            foreach (var item in items)
            {
                yield return new ItemRow(item, depth);
                foreach (var child in Flatten(item.Children, depth + 1))
                {
                    yield return child;
                }
            }
        }

        private string MenuButtonClass(StoreNavigationMenuSummaryDto menu)
        {
            var active = selectedMenu?.PublicId == menu.PublicId ? " border-control-500 bg-control-50" : " border-ink-200 bg-white hover:bg-ink-50";
            return "flex min-h-12 items-center justify-between gap-3 rounded-md border px-3 py-2 text-sm text-ink-700" + active;
        }

        private static string IndentStyle(int depth)
        {
            return $"padding-left:{Math.Min(depth * 1.25, 5):0.##}rem";
        }

        private static string TargetTone(string status)
        {
            return status switch
            {
                StoreNavigationTargetStatuses.Ok => "success",
                StoreNavigationTargetStatuses.Broken => "warning",
                StoreNavigationTargetStatuses.Invalid => "danger",
                _ => "neutral",
            };
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "n/a";
        }

        private sealed record ItemRow(StoreNavigationMenuItemAdminDto Item, int Depth);

        private sealed class MenuFormState
        {
            public string? SystemName { get; set; } = StoreNavigationMenuNames.Main;

            public string? DisplayName { get; set; }

            public bool IsEnabled { get; set; } = true;

            public static MenuFormState From(StoreNavigationMenuDetailDto detail)
            {
                return new MenuFormState
                {
                    SystemName = detail.SystemName,
                    DisplayName = detail.DisplayName,
                    IsEnabled = detail.IsEnabled,
                };
            }

            public CreateStoreNavigationMenuRequest ToCreateRequest()
            {
                return new CreateStoreNavigationMenuRequest(this.SystemName, this.DisplayName, this.IsEnabled);
            }

            public UpdateStoreNavigationMenuRequest ToUpdateRequest()
            {
                return new UpdateStoreNavigationMenuRequest(this.DisplayName, this.IsEnabled);
            }
        }

        private sealed class ItemFormState
        {
            public Guid? PublicId { get; set; }

            public Guid? ParentItemPublicId { get; set; }

            public string? Label { get; set; }

            public string TargetType { get; set; } = StoreNavigationTargetTypes.System;

            public string? TargetKey { get; set; }

            public string? TargetEntityPublicIdText { get; set; }

            public string? Url { get; set; }

            public bool IsEnabled { get; set; } = true;

            public int DisplayOrder { get; set; }

            public bool OpensInNewTab { get; set; }

            public static ItemFormState From(StoreNavigationMenuItemAdminDto item)
            {
                return new ItemFormState
                {
                    PublicId = item.PublicId,
                    ParentItemPublicId = item.ParentItemPublicId,
                    Label = item.Label,
                    TargetType = item.TargetType,
                    TargetKey = item.TargetKey,
                    TargetEntityPublicIdText = item.TargetEntityPublicId?.ToString("D"),
                    Url = item.Url,
                    IsEnabled = item.IsEnabled,
                    DisplayOrder = item.DisplayOrder,
                    OpensInNewTab = item.OpensInNewTab,
                };
            }

            public CreateStoreNavigationMenuItemRequest ToCreateRequest()
            {
                return new CreateStoreNavigationMenuItemRequest(
                    this.ParentItemPublicId,
                    this.Label,
                    this.TargetType,
                    this.TargetKey,
                    this.TargetEntityPublicId,
                    this.Url,
                    this.IsEnabled,
                    this.DisplayOrder,
                    this.OpensInNewTab);
            }

            public UpdateStoreNavigationMenuItemRequest ToUpdateRequest()
            {
                return new UpdateStoreNavigationMenuItemRequest(
                    this.ParentItemPublicId,
                    this.Label,
                    this.TargetType,
                    this.TargetKey,
                    this.TargetEntityPublicId,
                    this.Url,
                    this.IsEnabled,
                    this.DisplayOrder,
                    this.OpensInNewTab);
            }

            private Guid? TargetEntityPublicId => Guid.TryParse(this.TargetEntityPublicIdText, out var value) ? value : null;
        }
    }
}
