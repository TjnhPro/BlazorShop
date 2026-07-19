namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Globalization;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;

    using Microsoft.EntityFrameworkCore;

    public sealed class VariationTemplateService : IVariationTemplateService
    {
        private static readonly Regex HexColorPattern = new("^#?[0-9a-fA-F]{6}$", RegexOptions.Compiled);

        private readonly CommerceNodeDbContext context;
        private readonly ICommerceStoreContext storeContext;
        private readonly ISlugService slugService;
        private readonly IAdminAuditService auditService;
        private readonly ICatalogQueryCache catalogQueryCache;

        public VariationTemplateService(
            CommerceNodeDbContext context,
            ICommerceStoreContext storeContext,
            ISlugService slugService,
            IAdminAuditService auditService,
            ICatalogQueryCache catalogQueryCache)
        {
            this.context = context;
            this.storeContext = storeContext;
            this.slugService = slugService;
            this.auditService = auditService;
            this.catalogQueryCache = catalogQueryCache;
        }

        public async Task<ServiceResponse<VariationTemplateListResponse>> ListAsync(
            VariationTemplateListQuery query,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<VariationTemplateListResponse>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize <= 0 ? 25 : query.PageSize, 1, 100);
            var templates = this.context.VariationTemplates
                .AsNoTracking()
                .Where(template => template.StoreId == storeId);
            var totalCount = await templates.CountAsync(cancellationToken);
            var items = await templates
                .OrderBy(template => template.Name)
                .ThenBy(template => template.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(template => new VariationTemplateSummaryDto(
                    template.Id,
                    template.PublicId,
                    template.StoreId,
                    template.Name,
                    template.Slug,
                    template.IsActive,
                    template.Options.Count,
                    template.CreatedAt,
                    template.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Success(
                new VariationTemplateListResponse(
                    items,
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)),
                "Variation templates retrieved.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var template = await this.LoadTemplateAsync(id, asTracking: false, cancellationToken);
            return template is null
                ? Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound)
                : Success(MapDetail(template), "Variation template retrieved.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> CreateAsync(
            CreateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue)
            {
                return Failure<VariationTemplateDetailDto>("Store context was not resolved.", ServiceResponseType.NotFound);
            }

            var name = NormalizeRequired(request.Name);
            if (name is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template name is required.", ServiceResponseType.ValidationError);
            }

            if (name.Length > 160)
            {
                return Failure<VariationTemplateDetailDto>("Variation template name must be 160 characters or fewer.", ServiceResponseType.ValidationError);
            }

            var slug = this.NormalizeSlug(request.Slug, name);
            if (slug is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template slug is required.", ServiceResponseType.ValidationError);
            }

            var duplicate = await this.context.VariationTemplates.AnyAsync(
                template => template.StoreId == storeId && template.Slug == slug,
                cancellationToken);
            if (duplicate)
            {
                return Failure<VariationTemplateDetailDto>("Variation template slug already exists for this store.", ServiceResponseType.Conflict);
            }

            var now = DateTime.UtcNow;
            var template = new VariationTemplate
            {
                StoreId = storeId.Value,
                Name = name,
                Slug = slug,
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };

            this.context.VariationTemplates.Add(template);
            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("VariationTemplate.Created", template.Id, "Variation template created.", new { template.Name, template.Slug }, cancellationToken);

            return Success(MapDetail(template), "Variation template created.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> UpdateAsync(
            Guid id,
            UpdateVariationTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var template = await this.LoadTemplateAsync(id, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var name = NormalizeRequired(request.Name);
            if (name is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template name is required.", ServiceResponseType.ValidationError);
            }

            if (name.Length > 160)
            {
                return Failure<VariationTemplateDetailDto>("Variation template name must be 160 characters or fewer.", ServiceResponseType.ValidationError);
            }

            var slug = this.NormalizeSlug(request.Slug, name);
            if (slug is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template slug is required.", ServiceResponseType.ValidationError);
            }

            var duplicate = await this.context.VariationTemplates.AnyAsync(
                item => item.StoreId == template.StoreId && item.Slug == slug && item.Id != template.Id,
                cancellationToken);
            if (duplicate)
            {
                return Failure<VariationTemplateDetailDto>("Variation template slug already exists for this store.", ServiceResponseType.Conflict);
            }

            template.Name = name;
            template.Slug = slug;
            template.IsActive = request.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.InvalidateCatalogAsync(template.StoreId);
            await this.LogAsync("VariationTemplate.Updated", template.Id, "Variation template updated.", new { template.Name, template.Slug, template.IsActive }, cancellationToken);

            return Success(MapDetail(template), "Variation template updated.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var template = await this.LoadTemplateAsync(id, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var referenced = await this.context.Products.AnyAsync(product => product.VariationTemplateId == template.Id, cancellationToken);
            if (referenced)
            {
                return Failure<VariationTemplateDetailDto>("Variation template cannot be deleted because products reference it.", ServiceResponseType.Conflict);
            }

            this.context.VariationTemplates.Remove(template);
            await this.context.SaveChangesAsync(cancellationToken);
            await this.LogAsync("VariationTemplate.Deleted", template.Id, "Variation template deleted.", new { template.Name, template.Slug }, cancellationToken);

            return Success(MapDetail(template), "Variation template deleted.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> CreateOptionAsync(
            Guid templateId,
            CreateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var template = await this.LoadTemplateAsync(templateId, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var name = NormalizeRequired(request.Name);
            var validation = ValidateOptionName(name);
            if (validation is not null)
            {
                return Failure<VariationTemplateDetailDto>(validation, ServiceResponseType.ValidationError);
            }

            if (template.Options.Any(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure<VariationTemplateDetailDto>("Variation option name already exists for this template.", ServiceResponseType.Conflict);
            }

            var controlType = NormalizeControlType(request.ControlType);
            var controlTypeValidation = ValidateControlType(controlType);
            if (controlTypeValidation is not null)
            {
                return Failure<VariationTemplateDetailDto>(controlTypeValidation, ServiceResponseType.ValidationError);
            }

            var now = DateTime.UtcNow;
            var option = new VariationTemplateOption
            {
                Name = name!,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                ControlType = controlType,
                IsRequired = request.IsRequired,
                CreatedAt = now,
                UpdatedAt = now,
            };
            template.Options.Add(option);
            this.context.Entry(option).State = EntityState.Added;
            template.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.InvalidateCatalogAsync(template.StoreId);
            await this.LogAsync("VariationTemplate.OptionUpdated", template.Id, "Variation template option created.", new { OptionName = name }, cancellationToken);

            return Success(MapDetail(template), "Variation template option created.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> UpdateOptionAsync(
            Guid templateId,
            Guid optionId,
            UpdateVariationTemplateOptionRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var template = await this.LoadTemplateAsync(templateId, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var option = template.Options.FirstOrDefault(item => item.Id == optionId || item.PublicId == optionId);
            if (option is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation option was not found.", ServiceResponseType.NotFound);
            }

            var name = NormalizeRequired(request.Name);
            var validation = ValidateOptionName(name);
            if (validation is not null)
            {
                return Failure<VariationTemplateDetailDto>(validation, ServiceResponseType.ValidationError);
            }

            if (template.Options.Any(item => item.Id != option.Id && string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure<VariationTemplateDetailDto>("Variation option name already exists for this template.", ServiceResponseType.Conflict);
            }

            var controlType = NormalizeControlType(request.ControlType);
            var controlTypeValidation = ValidateControlType(controlType);
            if (controlTypeValidation is not null)
            {
                return Failure<VariationTemplateDetailDto>(controlTypeValidation, ServiceResponseType.ValidationError);
            }

            var now = DateTime.UtcNow;
            option.Name = name!;
            option.SortOrder = request.SortOrder;
            option.IsActive = request.IsActive;
            option.ControlType = controlType;
            option.IsRequired = request.IsRequired;
            if (!string.Equals(controlType, VariationControlTypes.Color, StringComparison.Ordinal))
            {
                foreach (var value in option.Values)
                {
                    value.ColorHex = null;
                }
            }

            option.UpdatedAt = now;
            template.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.InvalidateCatalogAsync(template.StoreId);
            await this.LogAsync("VariationTemplate.OptionUpdated", template.Id, "Variation template option updated.", new { option.Id, option.Name, option.IsActive }, cancellationToken);

            return Success(MapDetail(template), "Variation template option updated.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> CreateValueAsync(
            Guid templateId,
            Guid optionId,
            CreateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var template = await this.LoadTemplateAsync(templateId, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var option = template.Options.FirstOrDefault(item => item.Id == optionId || item.PublicId == optionId);
            if (option is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation option was not found.", ServiceResponseType.NotFound);
            }

            var value = NormalizeRequired(request.Value);
            var validation = ValidateValue(value);
            if (validation is not null)
            {
                return Failure<VariationTemplateDetailDto>(validation, ServiceResponseType.ValidationError);
            }

            if (option.Values.Any(item => string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure<VariationTemplateDetailDto>("Variation value already exists for this option.", ServiceResponseType.Conflict);
            }

            var colorHexResult = NormalizeColorHex(request.ColorHex, option.ControlType);
            if (!colorHexResult.Success)
            {
                return Failure<VariationTemplateDetailDto>(colorHexResult.ErrorMessage, ServiceResponseType.ValidationError);
            }

            var now = DateTime.UtcNow;
            var valueRow = new VariationTemplateValue
            {
                Value = value!,
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
                ColorHex = colorHexResult.Value,
                CreatedAt = now,
                UpdatedAt = now,
            };
            option.Values.Add(valueRow);
            this.context.Entry(valueRow).State = EntityState.Added;
            option.UpdatedAt = now;
            template.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.InvalidateCatalogAsync(template.StoreId);
            await this.LogAsync("VariationTemplate.ValueUpdated", template.Id, "Variation template value created.", new { option.Id, Value = value }, cancellationToken);

            return Success(MapDetail(template), "Variation template value created.");
        }

        public async Task<ServiceResponse<VariationTemplateDetailDto>> UpdateValueAsync(
            Guid templateId,
            Guid optionId,
            Guid valueId,
            UpdateVariationTemplateValueRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var template = await this.LoadTemplateAsync(templateId, asTracking: true, cancellationToken);
            if (template is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation template was not found.", ServiceResponseType.NotFound);
            }

            var option = template.Options.FirstOrDefault(item => item.Id == optionId || item.PublicId == optionId);
            if (option is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation option was not found.", ServiceResponseType.NotFound);
            }

            var value = option.Values.FirstOrDefault(item => item.Id == valueId || item.PublicId == valueId);
            if (value is null)
            {
                return Failure<VariationTemplateDetailDto>("Variation value was not found.", ServiceResponseType.NotFound);
            }

            var normalizedValue = NormalizeRequired(request.Value);
            var validation = ValidateValue(normalizedValue);
            if (validation is not null)
            {
                return Failure<VariationTemplateDetailDto>(validation, ServiceResponseType.ValidationError);
            }

            if (option.Values.Any(item => item.Id != value.Id && string.Equals(item.Value, normalizedValue, StringComparison.OrdinalIgnoreCase)))
            {
                return Failure<VariationTemplateDetailDto>("Variation value already exists for this option.", ServiceResponseType.Conflict);
            }

            var colorHexResult = NormalizeColorHex(request.ColorHex, option.ControlType);
            if (!colorHexResult.Success)
            {
                return Failure<VariationTemplateDetailDto>(colorHexResult.ErrorMessage, ServiceResponseType.ValidationError);
            }

            var now = DateTime.UtcNow;
            value.Value = normalizedValue!;
            value.SortOrder = request.SortOrder;
            value.IsActive = request.IsActive;
            value.ColorHex = colorHexResult.Value;
            value.UpdatedAt = now;
            option.UpdatedAt = now;
            template.UpdatedAt = now;

            await this.context.SaveChangesAsync(cancellationToken);
            await this.InvalidateCatalogAsync(template.StoreId);
            await this.LogAsync("VariationTemplate.ValueUpdated", template.Id, "Variation template value updated.", new { value.Id, value.Value, value.IsActive }, cancellationToken);

            return Success(MapDetail(template), "Variation template value updated.");
        }

        private async Task<VariationTemplate?> LoadTemplateAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
        {
            var storeId = await this.ResolveStoreIdAsync(cancellationToken);
            if (!storeId.HasValue || id == Guid.Empty)
            {
                return null;
            }

            var query = this.context.VariationTemplates
                .Include(template => template.Options.OrderBy(option => option.SortOrder).ThenBy(option => option.Name))
                .ThenInclude(option => option.Values.OrderBy(value => value.SortOrder).ThenBy(value => value.Value))
                .Where(template => template.StoreId == storeId && (template.Id == id || template.PublicId == id));

            if (!asTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<Guid?> ResolveStoreIdAsync(CancellationToken cancellationToken)
        {
            var result = await this.storeContext.GetCurrentStoreIdAsync();
            return result.Success && result.Payload != Guid.Empty ? result.Payload : null;
        }

        private string? NormalizeSlug(string? slug, string fallbackName)
        {
            var source = string.IsNullOrWhiteSpace(slug) ? fallbackName : slug;
            var normalized = this.slugService.NormalizeSlug(source);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private async Task InvalidateCatalogAsync(Guid storeId)
        {
            await this.catalogQueryCache.InvalidateStoreCatalogAsync(storeId);
        }

        private async Task LogAsync(string action, Guid entityId, string summary, object metadata, CancellationToken cancellationToken)
        {
            await this.auditService.LogAsync(new CreateAdminAuditLogDto
            {
                Action = action,
                EntityType = "VariationTemplate",
                EntityId = entityId.ToString(),
                Summary = summary,
                MetadataJson = JsonSerializer.Serialize(metadata),
            });
        }

        private static VariationTemplateDetailDto MapDetail(VariationTemplate template)
        {
            return new VariationTemplateDetailDto(
                template.Id,
                template.PublicId,
                template.StoreId,
                template.Name,
                template.Slug,
                template.IsActive,
                template.Options
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Name)
                    .Select(option => new VariationTemplateOptionDto(
                        option.Id,
                        option.PublicId,
                        option.Name,
                        option.SortOrder,
                        option.IsActive,
                        NormalizeControlType(option.ControlType),
                        option.IsRequired,
                        option.Values
                            .OrderBy(value => value.SortOrder)
                            .ThenBy(value => value.Value)
                            .Select(value => new VariationTemplateValueDto(
                                value.Id,
                                value.PublicId,
                                value.Value,
                                value.SortOrder,
                                value.IsActive,
                                value.ColorHex,
                                value.CreatedAt,
                                value.UpdatedAt))
                            .ToArray(),
                        option.CreatedAt,
                        option.UpdatedAt))
                    .ToArray(),
                template.CreatedAt,
                template.UpdatedAt);
        }

        private static string? NormalizeRequired(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? ValidateOptionName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Variation option name is required.";
            }

            return name.Length > 100 ? "Variation option name must be 100 characters or fewer." : null;
        }

        private static string NormalizeControlType(string? controlType)
        {
            return string.IsNullOrWhiteSpace(controlType)
                ? VariationControlTypes.Dropdown
                : controlType.Trim().ToLowerInvariant();
        }

        private static string? ValidateControlType(string controlType)
        {
            return VariationControlTypes.All.Contains(controlType)
                ? null
                : "Variation option control type is invalid.";
        }

        private static string? ValidateValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Variation value is required.";
            }

            return value.Length > 200 ? "Variation value must be 200 characters or fewer." : null;
        }

        private static ColorHexNormalization NormalizeColorHex(string? colorHex, string controlType)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
            {
                return ColorHexNormalization.Ok(null);
            }

            if (!string.Equals(NormalizeControlType(controlType), VariationControlTypes.Color, StringComparison.Ordinal))
            {
                return ColorHexNormalization.Failed("Color hex is only allowed for color variation options.");
            }

            var normalized = colorHex.Trim();
            if (!HexColorPattern.IsMatch(normalized))
            {
                return ColorHexNormalization.Failed("Color hex must be a 6-digit hex color.");
            }

            normalized = normalized.TrimStart('#').ToUpper(CultureInfo.InvariantCulture);
            return ColorHexNormalization.Ok($"#{normalized}");
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

        private sealed record ColorHexNormalization(bool Success, string? Value, string ErrorMessage)
        {
            public static ColorHexNormalization Ok(string? value)
            {
                return new ColorHexNormalization(true, value, string.Empty);
            }

            public static ColorHexNormalization Failed(string message)
            {
                return new ColorHexNormalization(false, null, message);
            }
        }
    }
}
