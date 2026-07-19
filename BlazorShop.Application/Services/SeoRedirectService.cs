namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Application.Validations;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Domain.Entities;

    using FluentValidation;

    public class SeoRedirectService : ISeoRedirectService
    {
        private readonly IGenericRepository<SeoRedirect> _genericRepository;
        private readonly ISeoRedirectRepository _seoRedirectRepository;
        private readonly IMapper _mapper;
        private readonly IValidationService _validationService;
        private readonly IValidator<SeoRedirectDto> _validator;
        private readonly IAdminAuditService? _auditService;
        private readonly ICommerceStoreContext? _storeContext;

        public SeoRedirectService(
            IGenericRepository<SeoRedirect> genericRepository,
            ISeoRedirectRepository seoRedirectRepository,
            IMapper mapper,
            IValidationService validationService,
            IValidator<SeoRedirectDto> validator,
            IAdminAuditService? auditService = null,
            ICommerceStoreContext? storeContext = null)
        {
            _genericRepository = genericRepository;
            _seoRedirectRepository = seoRedirectRepository;
            _mapper = mapper;
            _validationService = validationService;
            _validator = validator;
            _auditService = auditService;
            _storeContext = storeContext;
        }

        public async Task<IReadOnlyList<SeoRedirectDto>> GetAllAsync()
        {
            var storeId = await ResolveCurrentStoreIdAsync();
            var redirects = _storeContext is not null
                ? storeId.HasValue
                    ? await _seoRedirectRepository.ListForStoreAsync(storeId.Value)
                    : []
                : await _genericRepository.GetAllAsync();

            return _mapper.Map<List<SeoRedirectDto>>(redirects.OrderByDescending(redirect => redirect.CreatedOn).ThenBy(redirect => redirect.OldPath));
        }

        public async Task<ServiceResponse<SeoRedirectDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return ValidationError("Redirect id is required.");
            }

            var redirect = await GetReadableRedirectAsync(id);

            if (redirect is null)
            {
                return NotFound("Redirect not found.");
            }

            return Success(_mapper.Map<SeoRedirectDto>(redirect), redirect.Id, "SEO redirect retrieved successfully.");
        }

        public async Task<ServiceResponse<SeoRedirectDto>> CreateAsync(UpsertSeoRedirectDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            NormalizeRequest(request);

            var validationResult = await ValidateAsync(request);

            if (!validationResult.Success)
            {
                return validationResult;
            }

            var storeId = await ResolveCurrentStoreIdAsync();
            if (_storeContext is not null && !storeId.HasValue)
            {
                return ValidationError("Current store is required.");
            }

            if (await OldPathExistsAsync(request.OldPath!, storeId))
            {
                return Conflict("Redirect old path is already in use.");
            }

            var redirect = _mapper.Map<SeoRedirect>(request);
            redirect.StoreId = storeId;
            var rowsAffected = await _genericRepository.AddAsync(redirect);

            if (rowsAffected <= 0)
            {
                return Failure("SEO redirect could not be created.");
            }

            await LogAsync("SeoRedirect.Created", redirect.Id, $"Redirect {redirect.OldPath} created.", redirect);
            return Success(_mapper.Map<SeoRedirectDto>(redirect), redirect.Id, "SEO redirect created successfully.");
        }

        public async Task<ServiceResponse<SeoRedirectDto>> UpdateAsync(Guid id, UpsertSeoRedirectDto request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (id == Guid.Empty)
            {
                return ValidationError("Redirect id is required.");
            }

            NormalizeRequest(request);

            var validationResult = await ValidateAsync(request);

            if (!validationResult.Success)
            {
                return validationResult;
            }

            var storeId = await ResolveCurrentStoreIdAsync();
            if (_storeContext is not null && !storeId.HasValue)
            {
                return NotFound("Redirect not found.");
            }

            var redirect = await GetReadableRedirectAsync(id, storeId);

            if (redirect is null)
            {
                return NotFound("Redirect not found.");
            }

            if (await OldPathExistsAsync(request.OldPath!, storeId, id))
            {
                return Conflict("Redirect old path is already in use.");
            }

            var existingStoreId = redirect.StoreId;
            _mapper.Map(request, redirect);
            redirect.StoreId = existingStoreId ?? storeId;
            var rowsAffected = await _genericRepository.UpdateAsync(redirect);

            if (rowsAffected <= 0)
            {
                return Failure("SEO redirect could not be updated.");
            }

            await LogAsync("SeoRedirect.Updated", redirect.Id, $"Redirect {redirect.OldPath} updated.", redirect);
            return Success(_mapper.Map<SeoRedirectDto>(redirect), redirect.Id, "SEO redirect updated successfully.");
        }

        public async Task<ServiceResponse<SeoRedirectDto>> DeactivateAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return ValidationError("Redirect id is required.");
            }

            var redirect = await GetReadableRedirectAsync(id);

            if (redirect is null)
            {
                return NotFound("Redirect not found.");
            }

            redirect.IsActive = false;
            var rowsAffected = await _genericRepository.UpdateAsync(redirect);

            if (rowsAffected <= 0)
            {
                return Failure("SEO redirect could not be deactivated.");
            }

            await LogAsync("SeoRedirect.Deactivated", redirect.Id, $"Redirect {redirect.OldPath} deactivated.", redirect);
            return Success(_mapper.Map<SeoRedirectDto>(redirect), redirect.Id, "SEO redirect deactivated successfully.");
        }

        public async Task<ServiceResponse<SeoRedirectDto>> DeleteAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return ValidationError("Redirect id is required.");
            }

            var redirect = await GetReadableRedirectAsync(id);

            if (redirect is null)
            {
                return NotFound("Redirect not found.");
            }

            var payload = _mapper.Map<SeoRedirectDto>(redirect);
            var rowsAffected = await _genericRepository.DeleteAsync(id);

            if (rowsAffected <= 0)
            {
                return Failure("SEO redirect could not be deleted.");
            }

            await LogAsync("SeoRedirect.Deleted", id, $"Redirect {redirect.OldPath} deleted.", redirect);
            return Success(payload, id, "SEO redirect deleted successfully.");
        }

        private async Task LogAsync(string action, Guid entityId, string summary, SeoRedirect redirect)
        {
            if (_auditService is null)
            {
                return;
            }

            await _auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "SeoRedirect",
                EntityId = entityId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(new { redirect.StoreId, redirect.OldPath, redirect.NewPath, redirect.StatusCode, redirect.IsActive }),
            });
        }

        private async Task<SeoRedirect?> GetReadableRedirectAsync(Guid id, Guid? storeId = null)
        {
            storeId ??= await ResolveCurrentStoreIdAsync();
            if (_storeContext is null)
            {
                return await _genericRepository.GetByIdAsync(id);
            }

            return storeId.HasValue
                ? await _seoRedirectRepository.GetByIdForStoreAsync(storeId.Value, id)
                : null;
        }

        private async Task<bool> OldPathExistsAsync(string oldPath, Guid? storeId, Guid? excludedRedirectId = null)
        {
            if (_storeContext is null)
            {
                return await _seoRedirectRepository.OldPathExistsAsync(oldPath, excludedRedirectId);
            }

            return storeId.HasValue
                && await _seoRedirectRepository.OldPathExistsInStoreAsync(storeId.Value, oldPath, excludedRedirectId);
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Value : null;
        }

        private async Task<ServiceResponse<SeoRedirectDto>> ValidateAsync(UpsertSeoRedirectDto request)
        {
            var validationDto = _mapper.Map<SeoRedirectDto>(request);
            var validationResult = await _validationService.ValidateAsync(validationDto, _validator);

            if (!validationResult.Success)
            {
                return ValidationError(validationResult.Message ?? "Invalid redirect payload.");
            }

            return new ServiceResponse<SeoRedirectDto>(true)
            {
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static void NormalizeRequest(UpsertSeoRedirectDto request)
        {
            request.OldPath = SeoRedirectPathUtility.NormalizePath(request.OldPath);
            request.NewPath = SeoRedirectPathUtility.NormalizePath(request.NewPath);
        }

        private static ServiceResponse<SeoRedirectDto> Success(SeoRedirectDto payload, Guid id, string message)
        {
            return new ServiceResponse<SeoRedirectDto>(true, message, id)
            {
                Payload = payload,
                ResponseType = ServiceResponseType.Success,
            };
        }

        private static ServiceResponse<SeoRedirectDto> ValidationError(string message)
        {
            return new ServiceResponse<SeoRedirectDto>(false, message)
            {
                ResponseType = ServiceResponseType.ValidationError,
            };
        }

        private static ServiceResponse<SeoRedirectDto> NotFound(string message)
        {
            return new ServiceResponse<SeoRedirectDto>(false, message)
            {
                ResponseType = ServiceResponseType.NotFound,
            };
        }

        private static ServiceResponse<SeoRedirectDto> Conflict(string message)
        {
            return new ServiceResponse<SeoRedirectDto>(false, message)
            {
                ResponseType = ServiceResponseType.Conflict,
            };
        }

        private static ServiceResponse<SeoRedirectDto> Failure(string message)
        {
            return new ServiceResponse<SeoRedirectDto>(false, message)
            {
                ResponseType = ServiceResponseType.Failure,
            };
        }
    }
}
