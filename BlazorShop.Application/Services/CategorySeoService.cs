namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Exceptions;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Application.Validations;
    using BlazorShop.Domain.Contracts.CategoryPersistence;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities;

    using FluentValidation;

    public class CategorySeoService : ICategorySeoService
    {
        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly ICategoryRepository _categoryReadRepository;
        private readonly IMapper _mapper;
        private readonly ISlugService _slugService;
        private readonly IApplicationTransactionManager _transactionManager;
        private readonly ISeoRedirectAutomationService _seoRedirectAutomationService;
        private readonly IValidationService _validationService;
        private readonly IValidator<UpdateCategorySeoDto> _validator;
        private readonly IAdminAuditService? _auditService;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly IStoreSeoSlugPolicyService? _slugPolicyService;
        private readonly IStoreSeoSlugHistoryService? _slugHistoryService;

        public CategorySeoService(
            IGenericRepository<Category> categoryRepository,
            ICategoryRepository categoryReadRepository,
            IMapper mapper,
            ISlugService slugService,
            IApplicationTransactionManager transactionManager,
            ISeoRedirectAutomationService seoRedirectAutomationService,
            IValidationService validationService,
            IValidator<UpdateCategorySeoDto> validator,
            IAdminAuditService? auditService = null,
            ICommerceStoreContext? storeContext = null,
            IStoreSeoSlugPolicyService? slugPolicyService = null,
            IStoreSeoSlugHistoryService? slugHistoryService = null)
        {
            _categoryRepository = categoryRepository;
            _categoryReadRepository = categoryReadRepository;
            _mapper = mapper;
            _slugService = slugService;
            _transactionManager = transactionManager;
            _seoRedirectAutomationService = seoRedirectAutomationService;
            _validationService = validationService;
            _validator = validator;
            _auditService = auditService;
            _storeContext = storeContext;
            _slugPolicyService = slugPolicyService;
            _slugHistoryService = slugHistoryService;
        }

        public async Task<ServiceResponse<CategorySeoDto>> GetByCategoryIdAsync(Guid categoryId)
        {
            if (categoryId == Guid.Empty)
            {
                return ValidationError("Category id is required.");
            }

            var category = await GetReadableCategoryAsync(categoryId);

            if (category is null)
            {
                return NotFound("Category not found.");
            }

            return Success(_mapper.Map<CategorySeoDto>(category), category.Id, "Category SEO retrieved successfully.");
        }

        public async Task<ServiceResponse<CategorySeoDto>> UpdateAsync(Guid categoryId, UpdateCategorySeoDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (categoryId == Guid.Empty)
            {
                return ValidationError("Category id is required.");
            }

            var normalizedRequest = CopyRequest(categoryId, request);
            var slugValidationMessage = NormalizeSlug(normalizedRequest);

            if (slugValidationMessage is not null)
            {
                return ValidationError(slugValidationMessage);
            }

            var validationResult = await _validationService.ValidateAsync(normalizedRequest, _validator);

            if (!validationResult.Success)
            {
                return ValidationError(validationResult.Message ?? "Invalid SEO payload.");
            }

            var categorySnapshot = await GetReadableCategoryAsync(categoryId);
            if (categorySnapshot is null)
            {
                return NotFound("Category not found.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedRequest.Slug))
            {
                var slugPolicyResponse = await ValidateCategorySlugAsync(normalizedRequest.Slug, categorySnapshot.StoreId, categoryId);
                if (slugPolicyResponse is not null)
                {
                    return slugPolicyResponse;
                }
            }

            var category = await _categoryRepository.GetByIdAsync(categoryId);
            if (category is null || !await CategoryBelongsToCurrentStoreAsync(category))
            {
                return NotFound("Category not found.");
            }

            var oldPublicPath = BuildCategoryPublicPath(category.Slug, category.IsPublished);
            var newPublicPath = BuildCategoryPublicPath(normalizedRequest.Slug, normalizedRequest.IsPublished);

            try
            {
                return await _transactionManager.ExecuteInTransactionAsync(async () =>
                {
                    await EnsureRedirectAsync(oldPublicPath, newPublicPath);
                    await EnsureSlugHistoryAsync(category.StoreId, category.Id, categorySnapshot?.Slug, normalizedRequest.Slug);

                    _mapper.Map(normalizedRequest, category);
                    var rowsAffected = await _categoryRepository.UpdateAsync(category);

                    if (rowsAffected <= 0)
                    {
                        throw new ServiceResponseException("Category SEO update failed.", ServiceResponseType.Failure);
                    }

                    await LogAsync(category.Id, "Category SEO updated.", normalizedRequest);
                    return Success(_mapper.Map<CategorySeoDto>(category), category.Id, "Category SEO updated successfully.");
                });
            }
            catch (ServiceResponseException exception)
            {
                return FromServiceException(exception);
            }
        }

        private string? NormalizeSlug(UpdateCategorySeoDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Slug))
            {
                request.Slug = null;
                return null;
            }

            var normalizedSlug = _slugService.NormalizeSlug(request.Slug);

            if (string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return "Slug is invalid after normalization.";
            }

            request.Slug = normalizedSlug;
            return null;
        }

        private Task<Category?> GetReadableCategoryAsync(Guid categoryId)
        {
            return _storeContext is not null
                ? _categoryReadRepository.GetCategoryByIdForCurrentStoreAsync(categoryId)
                : _categoryRepository.GetByIdAsync(categoryId);
        }

        private Task<bool> CategorySlugExistsAsync(string slug, Guid? storeId, Guid categoryId)
        {
            return _storeContext is not null
                ? _categoryReadRepository.CategorySlugExistsInStoreAsync(slug, storeId, categoryId)
                : _categoryReadRepository.CategorySlugExistsAsync(slug, categoryId);
        }

        private async Task<ServiceResponse<CategorySeoDto>?> ValidateCategorySlugAsync(string slug, Guid? storeId, Guid categoryId)
        {
            if (_slugPolicyService is not null && _storeContext is not null && storeId.HasValue)
            {
                var result = await _slugPolicyService.ValidateSlugAsync(SeoSlugEntityTypes.Category, slug, storeId.Value, excludedEntityId: categoryId);
                if (result.Success)
                {
                    return null;
                }

                return string.Equals(result.Message, "Slug is already in use.", StringComparison.Ordinal)
                    ? Conflict("Category slug is already in use.")
                    : ValidationError(result.Message ?? "Category slug is invalid.");
            }

            return await CategorySlugExistsAsync(slug, storeId, categoryId)
                ? Conflict("Category slug is already in use.")
                : null;
        }

        private async Task<bool> CategoryBelongsToCurrentStoreAsync(Category category)
        {
            if (_storeContext is null)
            {
                return true;
            }

            var storeResult = await _storeContext.GetCurrentStoreIdAsync();
            return storeResult.Success && category.StoreId == storeResult.Value;
        }

        private static UpdateCategorySeoDto CopyRequest(Guid categoryId, UpdateCategorySeoDto request)
        {
            return new UpdateCategorySeoDto
            {
                CategoryId = categoryId,
                Slug = request.Slug,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                CanonicalUrl = request.CanonicalUrl,
                OgTitle = request.OgTitle,
                OgDescription = request.OgDescription,
                OgImage = request.OgImage,
                RobotsIndex = request.RobotsIndex,
                RobotsFollow = request.RobotsFollow,
                SeoContent = request.SeoContent,
                IsPublished = request.IsPublished,
            };
        }

        private async Task LogAsync(Guid categoryId, string summary, UpdateCategorySeoDto request)
        {
            if (_auditService is null)
            {
                return;
            }

            await _auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = "CategorySeo.Updated",
                EntityType = "Category",
                EntityId = categoryId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(new { request.Slug, request.MetaTitle, request.IsPublished }),
            });
        }

        private async Task EnsureRedirectAsync(string? oldPublicPath, string? newPublicPath)
        {
            if (string.IsNullOrWhiteSpace(oldPublicPath)
                || string.IsNullOrWhiteSpace(newPublicPath)
                || SeoRedirectPathUtility.PathsEqual(oldPublicPath, newPublicPath))
            {
                return;
            }

            var redirectResult = await _seoRedirectAutomationService.EnsurePermanentRedirectAsync(oldPublicPath, newPublicPath);
            if (!redirectResult.Success)
            {
                throw new ServiceResponseException(
                    redirectResult.Message ?? "Automatic redirect could not be created.",
                    redirectResult.ResponseType);
            }
        }

        private async Task EnsureSlugHistoryAsync(Guid? storeId, Guid categoryId, string? oldSlug, string? newSlug)
        {
            if (_slugHistoryService is null || !storeId.HasValue || string.IsNullOrWhiteSpace(newSlug))
            {
                return;
            }

            var active = await _slugHistoryService.GetActiveSlugAsync(SeoSlugEntityTypes.Category, categoryId, storeId.Value);
            if (active is null && !string.IsNullOrWhiteSpace(oldSlug))
            {
                await EnsureSlugHistoryResultAsync(
                    _slugHistoryService.RecordInitialActiveSlugAsync(SeoSlugEntityTypes.Category, categoryId, storeId.Value, oldSlug),
                    "Category SEO slug history could not be recorded.");
            }

            await EnsureSlugHistoryResultAsync(
                _slugHistoryService.ReplaceActiveSlugAsync(SeoSlugEntityTypes.Category, categoryId, storeId.Value, newSlug),
                "Category SEO slug history could not be updated.");
        }

        private static async Task EnsureSlugHistoryResultAsync(
            Task<ServiceResponse<StoreSeoSlugHistoryDto>> operation,
            string fallbackMessage)
        {
            var result = await operation;
            if (!result.Success)
            {
                throw new ServiceResponseException(
                    result.Message ?? fallbackMessage,
                    result.ResponseType);
            }
        }

        private static string? BuildCategoryPublicPath(string? slug, bool isPublished)
        {
            return isPublished && !string.IsNullOrWhiteSpace(slug)
                ? $"/category/{slug}"
                : null;
        }

        private static ServiceResponse<CategorySeoDto> FromServiceException(ServiceResponseException exception)
        {
            return new ServiceResponse<CategorySeoDto>(false, exception.Message)
            {
                ResponseType = exception.ResponseType,
            };
        }

        private static ServiceResponse<CategorySeoDto> Success(CategorySeoDto payload, Guid id, string message)
        {
            return new ServiceResponse<CategorySeoDto>(true, message, id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<CategorySeoDto> ValidationError(string message)
        {
            return new ServiceResponse<CategorySeoDto>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }

        private static ServiceResponse<CategorySeoDto> NotFound(string message)
        {
            return new ServiceResponse<CategorySeoDto>(false, message)
            {
                ResponseType = ServiceResponseType.NotFound,
            };
        }

        private static ServiceResponse<CategorySeoDto> Conflict(string message)
        {
            return new ServiceResponse<CategorySeoDto>(false, message)
            {
                ResponseType = ServiceResponseType.Conflict,
            };
        }

        private static ServiceResponse<CategorySeoDto> Failure(string message)
        {
            return new ServiceResponse<CategorySeoDto>(false, message)
            {
                ResponseType = ServiceResponseType.Failure,
            };
        }
    }
}
