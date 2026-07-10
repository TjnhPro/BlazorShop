namespace BlazorShop.Application.CommerceNode.VariationTemplates
{
    using BlazorShop.Application.DTOs;

    public sealed record VariationTemplateListResponse(IReadOnlyList<VariationTemplateSummaryDto> Items);

    public sealed record VariationTemplateSummaryDto(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        string Name,
        string Slug,
        bool IsActive,
        int OptionCount,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record VariationTemplateDetailDto(
        Guid Id,
        Guid PublicId,
        Guid StoreId,
        string Name,
        string Slug,
        bool IsActive,
        IReadOnlyList<VariationTemplateOptionDto> Options,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record VariationTemplateOptionDto(
        Guid Id,
        Guid PublicId,
        string Name,
        int SortOrder,
        bool IsActive,
        IReadOnlyList<VariationTemplateValueDto> Values,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record VariationTemplateValueDto(
        Guid Id,
        Guid PublicId,
        string Value,
        int SortOrder,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public sealed record StorefrontVariationTemplateDto(
        string Name,
        string Slug,
        IReadOnlyList<StorefrontVariationOptionDto> Options);

    public sealed record StorefrontVariationOptionDto(
        string Name,
        IReadOnlyList<StorefrontVariationValueDto> Values);

    public sealed record StorefrontVariationValueDto(string Value);

    public sealed record SelectedAttributeDto(string Name, string Value);

    public sealed record CreateVariationTemplateRequest(
        string? Name,
        string? Slug,
        bool IsActive = true);

    public sealed record UpdateVariationTemplateRequest(
        string? Name,
        string? Slug,
        bool IsActive = true);

    public sealed record CreateVariationTemplateOptionRequest(
        string? Name,
        int SortOrder = 0,
        bool IsActive = true);

    public sealed record UpdateVariationTemplateOptionRequest(
        string? Name,
        int SortOrder = 0,
        bool IsActive = true);

    public sealed record CreateVariationTemplateValueRequest(
        string? Value,
        int SortOrder = 0,
        bool IsActive = true);

    public sealed record UpdateVariationTemplateValueRequest(
        string? Value,
        int SortOrder = 0,
        bool IsActive = true);

    public interface IVariationTemplateService
    {
        Task<ServiceResponse<VariationTemplateListResponse>> ListAsync(CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> CreateAsync(CreateVariationTemplateRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> UpdateAsync(Guid id, UpdateVariationTemplateRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> CreateOptionAsync(Guid templateId, CreateVariationTemplateOptionRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> UpdateOptionAsync(Guid templateId, Guid optionId, UpdateVariationTemplateOptionRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> CreateValueAsync(Guid templateId, Guid optionId, CreateVariationTemplateValueRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResponse<VariationTemplateDetailDto>> UpdateValueAsync(Guid templateId, Guid optionId, Guid valueId, UpdateVariationTemplateValueRequest request, CancellationToken cancellationToken = default);
    }
}
