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

    public partial class CommerceCategories
    {
        private readonly List<StoreSummary> stores = [];
        private readonly List<GetCategory> categories = [];
        private readonly List<GetCategoryTreeNode> categoryTree = [];
        private readonly List<StoreSeoSlugHistoryDto> categorySlugHistory = [];
        private Guid? selectedStorePublicId;
        private GetCategory? selectedCategory;
        private CreateCategory categoryForm = new();
        private CategorySeoDto categorySeoForm = new();
        private StoreSeoSlugPolicyResult? categorySlugResult;
        private string categoryParentId = string.Empty;
        private bool isLoading;
        private bool isSaving;
        private bool isSlugActionBusy;
        private bool isDrawerOpen;
        private string? errorMessage;
        private string? successMessage;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private string DrawerTitle => selectedCategory is null ? "Create category" : selectedCategory.Name ?? "Category";

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
                var categoriesResult = await CategoryClient.ListCategoriesAsync(selectedStorePublicId!.Value);
                if (!categoriesResult.Success)
                {
                    errorMessage = categoriesResult.Message;
                    return;
                }

                categories.Clear();
                categories.AddRange(categoriesResult.Data?.Items ?? []);

                var treeResult = await CategoryClient.GetCategoryTreeAsync(selectedStorePublicId.Value);
                if (treeResult.Success)
                {
                    categoryTree.Clear();
                    categoryTree.AddRange(treeResult.Data ?? []);
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        private void NewCategory()
        {
            selectedCategory = null;
            categoryForm = new CreateCategory();
            categorySeoForm = new CategorySeoDto();
            categorySlugResult = null;
            categorySlugHistory.Clear();
            categoryParentId = string.Empty;
            isDrawerOpen = true;
        }

        private async Task EditCategory(GetCategory category)
        {
            selectedCategory = category;
            categoryForm = new CreateCategory
            {
                Name = category.Name,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                Image = category.Image,
                DisplayOrder = category.DisplayOrder,
            };
            categoryParentId = category.ParentCategoryId?.ToString("D") ?? string.Empty;
            categorySeoForm = new CategorySeoDto
            {
                CategoryId = category.Id,
                Slug = category.Slug,
                MetaTitle = category.MetaTitle,
                MetaDescription = category.MetaDescription,
                CanonicalUrl = category.CanonicalUrl,
                OgTitle = category.OgTitle,
                OgDescription = category.OgDescription,
                OgImage = category.OgImage,
                SeoContent = category.SeoContent,
                RobotsIndex = category.RobotsIndex,
                RobotsFollow = category.RobotsFollow,
                IsPublished = true,
            };
            await LoadCategorySeoAsync();
            await LoadCategorySlugLifecycleAsync();
            isDrawerOpen = true;
        }

        private async Task LoadCategorySeoAsync()
        {
            if (!HasStore || selectedCategory is null)
            {
                return;
            }

            var result = await CategoryClient.GetCategorySeoAsync(selectedStorePublicId!.Value, selectedCategory.Id);
            if (result.Success && result.Data is not null)
            {
                categorySeoForm = result.Data;
            }
        }

        private async Task LoadCategorySlugLifecycleAsync()
        {
            categorySlugResult = null;
            categorySlugHistory.Clear();
            if (!HasStore || selectedCategory is null)
            {
                return;
            }

            var validation = await SeoClient.ValidateSeoSlugAsync(
                selectedStorePublicId!.Value,
                new StoreSeoSlugValidateRequest
                {
                    EntityType = "category",
                    Slug = categorySeoForm.Slug,
                    ExcludedEntityId = selectedCategory.Id,
                });
            if (validation.Success)
            {
                categorySlugResult = validation.Data;
            }

            var history = await SeoClient.ListSeoSlugHistoryAsync(
                selectedStorePublicId.Value,
                new StoreSeoSlugHistoryQuery
                {
                    EntityType = "category",
                    EntityId = selectedCategory.Id,
                });
            if (history.Success)
            {
                categorySlugHistory.AddRange(history.Data ?? []);
            }
        }

        private async Task GenerateCategorySlugAsync()
        {
            if (!HasStore || selectedCategory is null)
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
                        EntityType = "category",
                        SourceName = categoryForm.Name ?? selectedCategory.Name,
                        ExcludedEntityId = selectedCategory.Id,
                    });
                categorySlugResult = result.Data;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                if (result.Data?.Success == true)
                {
                    categorySeoForm.Slug = result.Data.Slug;
                }
            }
            finally
            {
                isSlugActionBusy = false;
            }
        }

        private async Task ValidateCategorySlugAsync()
        {
            if (!HasStore || selectedCategory is null)
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
                        EntityType = "category",
                        Slug = categorySeoForm.Slug,
                        ExcludedEntityId = selectedCategory.Id,
                    });
                categorySlugResult = result.Data;
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

        private async Task SaveCategoryAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                categoryForm.ParentCategoryId = Guid.TryParse(categoryParentId, out var parentId) ? parentId : null;
                var result = selectedCategory is null
                    ? await CategoryClient.CreateCategoryAsync(selectedStorePublicId!.Value, categoryForm)
                    : await CategoryClient.UpdateCategoryAsync(
                        selectedStorePublicId!.Value,
                        selectedCategory.Id,
                        new UpdateCategory
                        {
                            Id = selectedCategory.Id,
                            Name = categoryForm.Name,
                            Description = categoryForm.Description,
                            ParentCategoryId = categoryForm.ParentCategoryId,
                            Image = categoryForm.Image,
                            DisplayOrder = categoryForm.DisplayOrder,
                        });

                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                successMessage = result.Message;
                isDrawerOpen = false;
                await LoadAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task SaveCategorySeoAsync()
        {
            if (!HasStore || selectedCategory is null)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CategoryClient.UpdateCategorySeoAsync(
                    selectedStorePublicId!.Value,
                    selectedCategory.Id,
                    new UpdateCategorySeoDto
                    {
                        CategoryId = selectedCategory.Id,
                        Slug = categorySeoForm.Slug,
                        MetaTitle = categorySeoForm.MetaTitle,
                        MetaDescription = categorySeoForm.MetaDescription,
                        CanonicalUrl = categorySeoForm.CanonicalUrl,
                        OgTitle = categorySeoForm.OgTitle,
                        OgDescription = categorySeoForm.OgDescription,
                        OgImage = categorySeoForm.OgImage,
                        SeoContent = categorySeoForm.SeoContent,
                        RobotsIndex = categorySeoForm.RobotsIndex,
                        RobotsFollow = categorySeoForm.RobotsFollow,
                        IsPublished = categorySeoForm.IsPublished,
                    });

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                categorySeoForm = result.Data;
                successMessage = result.Message;
                await LoadCategorySlugLifecycleAsync();
                await LoadAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task ArchiveCategoryAsync()
        {
            if (!HasStore || selectedCategory is null)
            {
                return;
            }

            isSaving = true;
            errorMessage = null;
            successMessage = null;
            try
            {
                var result = await CategoryClient.ArchiveCategoryAsync(selectedStorePublicId!.Value, selectedCategory.Id);
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                successMessage = result.Message;
                isDrawerOpen = false;
                selectedCategory = null;
                await LoadAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private string GetParentName(Guid? parentCategoryId)
        {
            if (!parentCategoryId.HasValue)
            {
                return "-";
            }

            return categories.FirstOrDefault(category => category.Id == parentCategoryId.Value)?.Name ?? parentCategoryId.Value.ToString("D");
        }

        private Task OnDrawerChanged(bool value)
        {
            isDrawerOpen = value;
            return Task.CompletedTask;
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

        private RenderFragment RenderCategoryNode(GetCategoryTreeNode category, int depth) => builder =>
        {
            var sequence = 0;
            builder.OpenElement(sequence++, "button");
            builder.AddAttribute(sequence++, "class", "flex w-full items-center justify-between rounded-md border border-ink-200 bg-white px-3 py-2 text-left text-sm hover:bg-ink-50");
            builder.AddAttribute(sequence++, "style", $"margin-left:{Math.Min(depth, 4) * 12}px");
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create(this, () => EditCategoryById(category.Id)));
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "font-medium text-ink-800");
            builder.AddContent(sequence++, category.Name);
            builder.CloseElement();
            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", "font-mono text-xs text-ink-500");
            builder.AddContent(sequence++, category.Slug);
            builder.CloseElement();
            builder.CloseElement();

            foreach (var child in category.Children)
            {
                builder.AddContent(sequence++, RenderCategoryNode(child, depth + 1));
            }
        };

        private async Task EditCategoryById(Guid categoryId)
        {
            var category = categories.FirstOrDefault(item => item.Id == categoryId);
            if (category is not null)
            {
                await EditCategory(category);
            }
        }
    }
}
