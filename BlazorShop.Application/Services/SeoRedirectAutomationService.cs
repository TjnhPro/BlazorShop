namespace BlazorShop.Application.Services
{
    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Validations;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Seo;
    using BlazorShop.Domain.Entities;

    using FluentValidation;

    public class SeoRedirectAutomationService : ISeoRedirectAutomationService
    {
        private const string ExistingRedirectConflictMessage = "Automatic redirect could not be created because the old path is already managed by an existing redirect.";
        private const string TargetPathConflictMessage = "Automatic redirect could not be created because the target path is already claimed by an active redirect.";

        private readonly IGenericRepository<SeoRedirect> _genericRepository;
        private readonly ISeoRedirectRepository _seoRedirectRepository;
        private readonly IMapper _mapper;
        private readonly IValidationService _validationService;
        private readonly IValidator<SeoRedirectDto> _validator;
        private readonly ICommerceStoreContext? _storeContext;

        public SeoRedirectAutomationService(
            IGenericRepository<SeoRedirect> genericRepository,
            ISeoRedirectRepository seoRedirectRepository,
            IMapper mapper,
            IValidationService validationService,
            IValidator<SeoRedirectDto> validator,
            ICommerceStoreContext? storeContext = null)
        {
            _genericRepository = genericRepository;
            _seoRedirectRepository = seoRedirectRepository;
            _mapper = mapper;
            _validationService = validationService;
            _validator = validator;
            _storeContext = storeContext;
        }

        public async Task<ServiceResponse<SeoRedirectDto>> EnsurePermanentRedirectAsync(string oldPath, string newPath)
        {
            var normalizedOldPath = SeoRedirectPathUtility.NormalizePath(oldPath);
            var normalizedNewPath = SeoRedirectPathUtility.NormalizePath(newPath);

            var redirectDto = new SeoRedirectDto
            {
                OldPath = normalizedOldPath,
                NewPath = normalizedNewPath,
                StatusCode = SeoConstraints.PermanentRedirectStatusCode,
                IsActive = true,
            };

            var validationResult = await _validationService.ValidateAsync(redirectDto, _validator);
            if (!validationResult.Success)
            {
                return ValidationError(validationResult.Message ?? "Invalid redirect payload.");
            }

            var storeId = await ResolveCurrentStoreIdAsync();
            if (_storeContext is not null && !storeId.HasValue)
            {
                return ValidationError("Current store is required.");
            }

            var targetPathRedirect = await GetActiveByOldPathAsync(normalizedNewPath!, storeId);
            if (targetPathRedirect is not null)
            {
                return Conflict(TargetPathConflictMessage);
            }

            var existingRedirect = await GetByOldPathAsync(normalizedOldPath!, storeId);
            if (existingRedirect is not null)
            {
                if (existingRedirect.IsActive && SeoRedirectPathUtility.PathsEqual(existingRedirect.NewPath, normalizedNewPath))
                {
                    return Success(_mapper.Map<SeoRedirectDto>(existingRedirect), existingRedirect.Id, "Existing SEO redirect reused.");
                }

                return Conflict(ExistingRedirectConflictMessage);
            }

            var redirect = new SeoRedirect
            {
                OldPath = normalizedOldPath,
                NewPath = normalizedNewPath,
                StatusCode = SeoConstraints.PermanentRedirectStatusCode,
                IsActive = true,
                StoreId = storeId,
            };

            var rowsAffected = await _genericRepository.AddAsync(redirect);
            if (rowsAffected <= 0)
            {
                return Failure("Automatic redirect could not be created.");
            }

            return Success(_mapper.Map<SeoRedirectDto>(redirect), redirect.Id, "Automatic SEO redirect created successfully.");
        }

        private async Task<SeoRedirect?> GetActiveByOldPathAsync(string oldPath, Guid? storeId)
        {
            if (_storeContext is null)
            {
                return await _seoRedirectRepository.GetActiveByOldPathAsync(oldPath);
            }

            return storeId.HasValue
                ? await _seoRedirectRepository.GetActiveByOldPathInStoreAsync(storeId.Value, oldPath)
                : null;
        }

        private async Task<SeoRedirect?> GetByOldPathAsync(string oldPath, Guid? storeId)
        {
            if (_storeContext is null)
            {
                return await _seoRedirectRepository.GetByOldPathAsync(oldPath);
            }

            return storeId.HasValue
                ? await _seoRedirectRepository.GetByOldPathInStoreAsync(storeId.Value, oldPath)
                : null;
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
