namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class CategoryMediaService : ICategoryMediaService
    {
        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ICommerceMediaUrlBuilder mediaUrlBuilder;
        private readonly ICatalogQueryCache catalogQueryCache;

        public CategoryMediaService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ICommerceMediaUrlBuilder mediaUrlBuilder,
            ICatalogQueryCache catalogQueryCache)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.mediaUrlBuilder = mediaUrlBuilder;
            this.catalogQueryCache = catalogQueryCache;
        }

        public async Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> GetPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveCategoryScopeAsync(categoryId, asTracking: false, cancellationToken);
            if (!scope.Success)
            {
                return Failed(scope.Failure!.Value, scope.Message);
            }

            var assignment = await this.GetPrimaryAssignmentQuery(scope.StoreId, categoryId)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            return Succeeded("Category media retrieved.", this.ToDto(categoryId, assignment));
        }

        public async Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> SetPrimaryAsync(
            Guid categoryId,
            SetCategoryPrimaryMediaRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null || request.MediaAssetPublicId == Guid.Empty)
            {
                return Failed(CategoryMediaOperationFailure.Validation, "Media asset public id is required.");
            }

            var scope = await this.ResolveCategoryScopeAsync(categoryId, asTracking: true, cancellationToken);
            if (!scope.Success)
            {
                return Failed(scope.Failure!.Value, scope.Message);
            }

            var asset = await this.context.CommerceMediaAssets
                .FirstOrDefaultAsync(
                    media => media.StoreId == scope.StoreId && media.PublicId == request.MediaAssetPublicId,
                    cancellationToken);
            if (asset is null)
            {
                return Failed(CategoryMediaOperationFailure.NotFound, "Media asset was not found for the current store.");
            }

            var now = DateTimeOffset.UtcNow;
            var assignment = await this.context.CategoryMediaAssignments
                .FirstOrDefaultAsync(
                    item => item.StoreId == scope.StoreId && item.CategoryId == categoryId && item.IsPrimary,
                    cancellationToken);

            if (assignment is null)
            {
                assignment = new CategoryMediaAssignment
                {
                    Id = Guid.NewGuid(),
                    StoreId = scope.StoreId,
                    CategoryId = categoryId,
                    MediaAssetId = asset.Id,
                    SortOrder = 0,
                    IsPrimary = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                this.context.CategoryMediaAssignments.Add(assignment);
            }
            else
            {
                assignment.MediaAssetId = asset.Id;
                assignment.UpdatedAt = now;
            }

            assignment.AltText = NormalizeAltText(request.AltText) ?? asset.AltText;
            scope.Category!.Image = this.BuildCategoryImageUrl(asset);
            scope.Category.UpdatedAt = now.UtcDateTime;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.catalogQueryCache.InvalidateStoreCatalogAsync(scope.StoreId, cancellationToken);

            assignment.MediaAsset = asset;
            return Succeeded("Category primary media updated.", this.ToDto(categoryId, assignment));
        }

        public async Task<CategoryMediaOperationResult<CategoryMediaAssignmentDto>> ClearPrimaryAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default)
        {
            var scope = await this.ResolveCategoryScopeAsync(categoryId, asTracking: true, cancellationToken);
            if (!scope.Success)
            {
                return Failed(scope.Failure!.Value, scope.Message);
            }

            var assignment = await this.context.CategoryMediaAssignments
                .FirstOrDefaultAsync(
                    item => item.StoreId == scope.StoreId && item.CategoryId == categoryId && item.IsPrimary,
                    cancellationToken);

            if (assignment is not null)
            {
                this.context.CategoryMediaAssignments.Remove(assignment);
            }

            scope.Category!.Image = null;
            scope.Category.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.catalogQueryCache.InvalidateStoreCatalogAsync(scope.StoreId, cancellationToken);

            return Succeeded("Category primary media cleared.", EmptyDto(categoryId));
        }

        private IQueryable<CategoryMediaAssignment> GetPrimaryAssignmentQuery(Guid storeId, Guid categoryId)
        {
            return this.context.CategoryMediaAssignments
                .Include(assignment => assignment.MediaAsset)
                .Where(assignment => assignment.StoreId == storeId && assignment.CategoryId == categoryId && assignment.IsPrimary);
        }

        private async Task<CategoryScopeResult> ResolveCategoryScopeAsync(
            Guid categoryId,
            bool asTracking,
            CancellationToken cancellationToken)
        {
            var storeResult = await this.storeContext.GetCurrentStoreIdAsync(cancellationToken);
            if (!storeResult.Success || storeResult.Payload == Guid.Empty)
            {
                return CategoryScopeResult.Failed(CategoryMediaOperationFailure.Validation, storeResult.Message ?? "Current store could not be resolved.");
            }

            var storeId = storeResult.Payload;
            var categories = asTracking ? this.context.Categories : this.context.Categories.AsNoTracking();
            var category = await categories.FirstOrDefaultAsync(
                entity => entity.Id == categoryId && entity.StoreId == storeId && entity.ArchivedAt == null,
                cancellationToken);

            return category is null
                ? CategoryScopeResult.Failed(CategoryMediaOperationFailure.NotFound, "Category was not found for the current store.")
                : CategoryScopeResult.Succeeded(storeId, category);
        }

        private CategoryMediaAssignmentDto ToDto(Guid categoryId, CategoryMediaAssignment? assignment)
        {
            if (assignment?.MediaAsset is null)
            {
                return EmptyDto(categoryId);
            }

            var asset = assignment.MediaAsset;
            return new CategoryMediaAssignmentDto(
                categoryId,
                asset.PublicId,
                this.BuildCategoryImageUrl(asset),
                assignment.AltText,
                assignment.SortOrder,
                assignment.IsPrimary,
                assignment.UpdatedAt);
        }

        private string BuildCategoryImageUrl(CommerceMediaAsset asset)
        {
            return this.mediaUrlBuilder.BuildAssetUrl(
                asset.PublicId,
                asset.CanonicalFileName,
                asset.UpdatedAt.ToUnixTimeSeconds(),
                MediaUrlPresetNames.CategoryCard);
        }

        private static CategoryMediaAssignmentDto EmptyDto(Guid categoryId)
        {
            return new CategoryMediaAssignmentDto(categoryId, null, null, null, 0, false, null);
        }

        private static string? NormalizeAltText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static CategoryMediaOperationResult<CategoryMediaAssignmentDto> Succeeded(
            string message,
            CategoryMediaAssignmentDto payload)
        {
            return new CategoryMediaOperationResult<CategoryMediaAssignmentDto>(true, message, payload);
        }

        private static CategoryMediaOperationResult<CategoryMediaAssignmentDto> Failed(
            CategoryMediaOperationFailure failure,
            string? message)
        {
            return new CategoryMediaOperationResult<CategoryMediaAssignmentDto>(
                false,
                string.IsNullOrWhiteSpace(message) ? "Category media request could not be completed." : message,
                Failure: failure);
        }

        private sealed record CategoryScopeResult(
            bool Success,
            Guid StoreId,
            Category? Category,
            CategoryMediaOperationFailure? Failure = null,
            string? Message = null)
        {
            public static CategoryScopeResult Succeeded(Guid storeId, Category category)
            {
                return new CategoryScopeResult(true, storeId, category);
            }

            public static CategoryScopeResult Failed(CategoryMediaOperationFailure failure, string message)
            {
                return new CategoryScopeResult(false, Guid.Empty, null, failure, message);
            }
        }
    }
}
