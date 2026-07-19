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

    public partial class CommercePages
    {
        private readonly List<StoreSummary> stores = [];
        private readonly List<StorefrontPageSummaryDto> pages = [];
        private readonly List<StorefrontPageTemplateStatusDto> templateStatuses = [];
        private readonly List<StoreSeoSlugHistoryDto> pageSlugHistory = [];
        private Guid? selectedStorePublicId;
        private StorefrontPageDetailDto? selectedPage;
        private PageFormState form = new();
        private StoreSeoSlugPolicyResult? pageSlugResult;
        private string? search;
        private string status = StorefrontPageStatuses.All;
        private int pageNumber = 1;
        private int pageSize = 25;
        private int totalCount;
        private int totalPages;
        private bool isLoading;
        private bool isTemplateLoading;
        private bool isTemplateActionBusy;
        private bool isSlugActionBusy;
        private bool isSaving;
        private bool isDrawerOpen;
        private string? errorMessage;
        private string? successMessage;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private string DrawerTitle => selectedPage is null ? "Create page" : selectedPage.Title;

        protected override async Task OnInitializedAsync()
        {
            await LoadStoresAsync();
        }

        private async Task LoadStoresAsync()
        {
            var response = await StoreClient.ListAsync(status: "active", pageSize: 100);
            stores.Clear();
            stores.AddRange(response.Items);
            selectedStorePublicId ??= stores.FirstOrDefault()?.PublicId;
            await LoadAsync();
        }

        private async Task SearchAsync()
        {
            pageNumber = 1;
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
                var result = await ContentClient.ListStorefrontPagesAsync(
                    selectedStorePublicId!.Value,
                    new StorefrontPageListQuery(pageNumber, pageSize, search, status));
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                var payload = result.Data ?? new StorefrontPageListResponse([], 0, pageNumber, pageSize, 0);
                pages.Clear();
                pages.AddRange(payload.Items);
                totalCount = payload.TotalCount;
                totalPages = payload.TotalPages;
                pageNumber = payload.PageNumber;
                pageSize = payload.PageSize;
                await LoadTemplateStatusAsync();
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task PreviousPageAsync()
        {
            pageNumber = Math.Max(1, pageNumber - 1);
            await LoadAsync();
        }

        private async Task NextPageAsync()
        {
            pageNumber++;
            await LoadAsync();
        }

        private void NewPage()
        {
            selectedPage = null;
            form = new PageFormState();
            pageSlugResult = null;
            pageSlugHistory.Clear();
            isDrawerOpen = true;
        }

        private async Task OpenPageAsync(Guid pagePublicId)
        {
            if (!HasStore)
            {
                return;
            }

            var result = await ContentClient.GetStorefrontPageAsync(selectedStorePublicId!.Value, pagePublicId);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedPage = result.Data;
            form = PageFormState.From(result.Data);
            await LoadPageSlugLifecycleAsync();
            isDrawerOpen = true;
        }

        private async Task LoadPageSlugLifecycleAsync()
        {
            pageSlugResult = null;
            pageSlugHistory.Clear();
            if (!HasStore)
            {
                return;
            }

            var validation = await SeoClient.ValidateSeoSlugAsync(
                selectedStorePublicId!.Value,
                new StoreSeoSlugValidateRequest
                {
                    EntityType = "page",
                    Slug = form.Slug,
                    ExcludedEntityId = selectedPage?.Id,
                });
            if (validation.Success)
            {
                pageSlugResult = validation.Data;
            }

            if (selectedPage is null)
            {
                return;
            }

            var history = await SeoClient.ListSeoSlugHistoryAsync(
                selectedStorePublicId.Value,
                new StoreSeoSlugHistoryQuery
                {
                    EntityType = "page",
                    EntityId = selectedPage.Id,
                });
            if (history.Success)
            {
                pageSlugHistory.AddRange(history.Data ?? []);
            }
        }

        private async Task GeneratePageSlugAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isSlugActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await SeoClient.GenerateSeoSlugAsync(
                    selectedStorePublicId!.Value,
                    new StoreSeoSlugGenerateRequest
                    {
                        EntityType = "page",
                        SourceName = form.Title,
                        ExcludedEntityId = selectedPage?.Id,
                    });
                pageSlugResult = result.Data;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                if (result.Data?.Success == true)
                {
                    form.Slug = result.Data.Slug;
                }
            }
            finally
            {
                isSlugActionBusy = false;
            }
        }

        private async Task ValidatePageSlugAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isSlugActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await SeoClient.ValidateSeoSlugAsync(
                    selectedStorePublicId!.Value,
                    new StoreSeoSlugValidateRequest
                    {
                        EntityType = "page",
                        Slug = form.Slug,
                        ExcludedEntityId = selectedPage?.Id,
                    });
                pageSlugResult = result.Data;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                }
            }
            finally
            {
                isSlugActionBusy = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!HasStore)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(form.Title) || string.IsNullOrWhiteSpace(form.Slug) || string.IsNullOrWhiteSpace(form.BodyHtml))
            {
                errorMessage = "Title, slug, and body HTML are required.";
                return;
            }

            if (form.IncludeInNavigation && string.IsNullOrWhiteSpace(form.NavigationLocation))
            {
                errorMessage = "Navigation location is required when the page is included in content navigation.";
                return;
            }

            isSaving = true;
            errorMessage = null;
            try
            {
                var result = selectedPage is null
                    ? await ContentClient.CreateStorefrontPageAsync(selectedStorePublicId!.Value, form.ToCreateRequest())
                    : await ContentClient.UpdateStorefrontPageAsync(selectedStorePublicId!.Value, selectedPage.PublicId, form.ToUpdateRequest());

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedPage = result.Data;
                form = PageFormState.From(result.Data);
                await LoadPageSlugLifecycleAsync();
                successMessage = "Storefront page saved.";
                await LoadAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task ArchiveAsync()
        {
            if (!HasStore || selectedPage is null)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            try
            {
                var result = await ContentClient.ArchiveStorefrontPageAsync(selectedStorePublicId!.Value, selectedPage.PublicId);
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                successMessage = "Storefront page archived.";
                isDrawerOpen = false;
                selectedPage = null;
                await LoadAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private Task OnDrawerChanged(bool value)
        {
            isDrawerOpen = value;
            return Task.CompletedTask;
        }

        private async Task LoadTemplateStatusAsync()
        {
            if (!HasStore)
            {
                templateStatuses.Clear();
                return;
            }

            isTemplateLoading = true;
            errorMessage = null;
            try
            {
                var result = await ContentClient.GetStorefrontPageTemplateStatusAsync(selectedStorePublicId!.Value);
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                templateStatuses.Clear();
                templateStatuses.AddRange(result.Data ?? []);
            }
            finally
            {
                isTemplateLoading = false;
            }
        }

        private async Task CreateDraftFromTemplateAsync(string pageKey)
        {
            if (!HasStore || string.IsNullOrWhiteSpace(pageKey))
            {
                return;
            }

            isTemplateActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await ContentClient.CreateStorefrontPageDraftFromTemplateAsync(
                    selectedStorePublicId!.Value,
                    pageKey,
                    new CreatePageFromTemplateRequest());
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedPage = result.Data;
                form = PageFormState.From(result.Data);
                isDrawerOpen = true;
                successMessage = "Draft page created from template.";
                await LoadAsync();
            }
            finally
            {
                isTemplateActionBusy = false;
            }
        }

        private async Task MapSuggestedPageAsync(StorefrontPageTemplateStatusDto status)
        {
            if (!HasStore || status.SuggestedExistingPages.Count == 0)
            {
                return;
            }

            isTemplateActionBusy = true;
            errorMessage = null;
            try
            {
                var suggested = status.SuggestedExistingPages[0];
                var result = await ContentClient.MapStorefrontPageTemplateAsync(
                    selectedStorePublicId!.Value,
                    suggested.PublicId,
                    new MapPageTemplateRequest(status.PageKey));
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedPage = result.Data;
                form = PageFormState.From(result.Data);
                isDrawerOpen = true;
                successMessage = "Suggested page mapped to content slot.";
                await LoadAsync();
            }
            finally
            {
                isTemplateActionBusy = false;
            }
        }

        private async Task ClearTemplateAsync(Guid pagePublicId)
        {
            if (!HasStore || pagePublicId == Guid.Empty)
            {
                return;
            }

            isTemplateActionBusy = true;
            errorMessage = null;
            try
            {
                var result = await ContentClient.ClearStorefrontPageTemplateAsync(selectedStorePublicId!.Value, pagePublicId);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedPage = result.Data;
                form = PageFormState.From(result.Data);
                successMessage = "Content slot cleared from page.";
                await LoadAsync();
            }
            finally
            {
                isTemplateActionBusy = false;
            }
        }

        private static string TemplateStatusLabel(string? status)
        {
            return status switch
            {
                StorefrontPageTemplateStatuses.MappedPublished => "Published",
                StorefrontPageTemplateStatuses.MappedDraft => "Draft mapped",
                StorefrontPageTemplateStatuses.Missing => "Missing",
                _ => "Unknown",
            };
        }

        private static string TemplateStatusTone(string? status)
        {
            return status switch
            {
                StorefrontPageTemplateStatuses.MappedPublished => "success",
                StorefrontPageTemplateStatuses.MappedDraft => "warning",
                StorefrontPageTemplateStatuses.Missing => "danger",
                _ => "neutral",
            };
        }

        private static string SlugPreviewText(string? slug)
        {
            return string.IsNullOrWhiteSpace(slug) ? "not generated" : slug;
        }

        private static string SlugPreviewMessage(StoreSeoSlugPolicyResult result)
        {
            return result.Success
                ? "Slug is available."
                : string.IsNullOrWhiteSpace(result.Message) ? "Slug is not available." : result.Message;
        }

        private static string BuildSeoPublicPath(string entityType, string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return "not available";
            }

            var trimmedSlug = slug.Trim();
            return entityType switch
            {
                "product" => $"/product/{trimmedSlug}",
                "category" => $"/category/{trimmedSlug}",
                "page" => $"/pages/{trimmedSlug}",
                _ => $"/{trimmedSlug}",
            };
        }

        private sealed class PageFormState
        {
            public string? Slug { get; set; }

            public string? Title { get; set; }

            public string? Intro { get; set; }

            public string? BodyHtml { get; set; }

            public bool IsPublished { get; set; }

            public bool IncludeInSitemap { get; set; }

            public string? PageKey { get; set; }

            public int DisplayOrder { get; set; }

            public bool IncludeInNavigation { get; set; }

            public string? NavigationLocation { get; set; }

            public string? MetaTitle { get; set; }

            public string? MetaDescription { get; set; }

            public string? CanonicalUrl { get; set; }

            public string? OgTitle { get; set; }

            public string? OgDescription { get; set; }

            public string? OgImage { get; set; }

            public bool RobotsIndex { get; set; } = true;

            public bool RobotsFollow { get; set; } = true;

            public static PageFormState From(StorefrontPageDetailDto detail)
            {
                return new PageFormState
                {
                    Slug = detail.Slug,
                    Title = detail.Title,
                    Intro = detail.Intro,
                    BodyHtml = detail.BodyHtml,
                    IsPublished = detail.IsPublished,
                    IncludeInSitemap = detail.IncludeInSitemap,
                    PageKey = detail.PageKey,
                    DisplayOrder = detail.DisplayOrder,
                    IncludeInNavigation = detail.IncludeInNavigation,
                    NavigationLocation = detail.NavigationLocation,
                    MetaTitle = detail.Seo.MetaTitle,
                    MetaDescription = detail.Seo.MetaDescription,
                    CanonicalUrl = detail.Seo.CanonicalUrl,
                    OgTitle = detail.Seo.OgTitle,
                    OgDescription = detail.Seo.OgDescription,
                    OgImage = detail.Seo.OgImage,
                    RobotsIndex = detail.Seo.RobotsIndex,
                    RobotsFollow = detail.Seo.RobotsFollow,
                };
            }

            public CreateStorefrontPageRequest ToCreateRequest()
            {
                return new CreateStorefrontPageRequest(
                    Slug: this.Slug,
                    Title: this.Title,
                    Intro: this.Intro,
                    BodyHtml: this.BodyHtml,
                    IsPublished: this.IsPublished,
                    IncludeInSitemap: this.IncludeInSitemap,
                    Seo: this.BuildSeo(),
                    PageKey: this.PageKey,
                    DisplayOrder: this.DisplayOrder,
                    IncludeInNavigation: this.IncludeInNavigation,
                    NavigationLocation: this.NavigationLocation);
            }

            public UpdateStorefrontPageRequest ToUpdateRequest()
            {
                return new UpdateStorefrontPageRequest(
                    Slug: this.Slug,
                    Title: this.Title,
                    Intro: this.Intro,
                    BodyHtml: this.BodyHtml,
                    IsPublished: this.IsPublished,
                    IncludeInSitemap: this.IncludeInSitemap,
                    Seo: this.BuildSeo(),
                    PageKey: this.PageKey,
                    DisplayOrder: this.DisplayOrder,
                    IncludeInNavigation: this.IncludeInNavigation,
                    NavigationLocation: this.NavigationLocation);
            }

            private StorefrontPageSeoDto BuildSeo()
            {
                return new StorefrontPageSeoDto(
                    this.MetaTitle,
                    this.MetaDescription,
                    this.CanonicalUrl,
                    this.OgTitle,
                    this.OgDescription,
                    this.OgImage,
                    this.RobotsIndex,
                    this.RobotsFollow);
            }
        }
    }
}
