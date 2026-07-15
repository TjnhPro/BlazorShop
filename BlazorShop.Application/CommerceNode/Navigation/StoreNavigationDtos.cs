namespace BlazorShop.Application.CommerceNode.Navigation
{
    using BlazorShop.Application.DTOs;

    public sealed record StoreNavigationMenuSummaryDto(
        Guid PublicId,
        string SystemName,
        string DisplayName,
        bool IsEnabled,
        DateTimeOffset UpdatedAt,
        int ItemCount);

    public sealed record StoreNavigationMenuDetailDto(
        Guid PublicId,
        string SystemName,
        string DisplayName,
        bool IsEnabled,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<StoreNavigationMenuItemAdminDto> Items);

    public sealed record StoreNavigationMenuItemAdminDto(
        Guid PublicId,
        Guid? ParentItemPublicId,
        string Label,
        string TargetType,
        string? TargetKey,
        Guid? TargetEntityPublicId,
        string? Url,
        bool IsEnabled,
        int DisplayOrder,
        bool OpensInNewTab,
        string TargetStatus,
        string? ResolvedHref,
        IReadOnlyList<StoreNavigationMenuItemAdminDto> Children);

    public sealed record StoreNavigationPublicMenuDto(
        string SystemName,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<StoreNavigationPublicItemDto> Items);

    public sealed record StoreNavigationPublicItemDto(
        string Label,
        string? Href,
        string TargetType,
        string? TargetKey,
        bool OpensInNewTab,
        IReadOnlyList<StoreNavigationPublicItemDto> Children);

    public sealed record CreateStoreNavigationMenuRequest(
        string? SystemName,
        string? DisplayName,
        bool IsEnabled = true);

    public sealed record UpdateStoreNavigationMenuRequest(
        string? DisplayName,
        bool IsEnabled = true);

    public sealed record CreateStoreNavigationMenuItemRequest(
        Guid? ParentItemPublicId,
        string? Label,
        string? TargetType,
        string? TargetKey,
        Guid? TargetEntityPublicId,
        string? Url,
        bool IsEnabled = true,
        int DisplayOrder = 0,
        bool OpensInNewTab = false);

    public sealed record UpdateStoreNavigationMenuItemRequest(
        Guid? ParentItemPublicId,
        string? Label,
        string? TargetType,
        string? TargetKey,
        Guid? TargetEntityPublicId,
        string? Url,
        bool IsEnabled = true,
        int DisplayOrder = 0,
        bool OpensInNewTab = false);

    public sealed record UpdateStoreNavigationMenuItemOrderRequest(
        IReadOnlyList<StoreNavigationMenuItemOrderDto> Items);

    public sealed record StoreNavigationMenuItemOrderDto(
        Guid PublicId,
        Guid? ParentItemPublicId,
        int DisplayOrder);

    public sealed record StoreNavigationTargetOptionDto(
        string TargetType,
        string Key,
        string Label,
        string? Href = null);

    public static class StoreNavigationTargetStatuses
    {
        public const string Ok = "ok";
        public const string Broken = "broken";
        public const string Invalid = "invalid";
    }

    public interface IStoreNavigationService
    {
        IReadOnlyList<StoreNavigationTargetOptionDto> ListSystemTargets();

        Task<ServiceResponse<IReadOnlyList<StoreNavigationMenuSummaryDto>>> ListMenusAsync(
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> GetMenuAsync(
            Guid menuPublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> CreateMenuAsync(
            CreateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateMenuAsync(
            Guid menuPublicId,
            UpdateStoreNavigationMenuRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> CreateItemAsync(
            Guid menuPublicId,
            CreateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateItemAsync(
            Guid itemPublicId,
            UpdateStoreNavigationMenuItemRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> ArchiveItemAsync(
            Guid itemPublicId,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationMenuDetailDto>> UpdateItemOrderAsync(
            Guid menuPublicId,
            UpdateStoreNavigationMenuItemOrderRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResponse<StoreNavigationPublicMenuDto>> GetPublicMenuAsync(
            string systemName,
            CancellationToken cancellationToken = default);
    }

    public interface IStorefrontNavigationCache
    {
        bool TryGet(Guid storeId, string systemName, out StoreNavigationPublicMenuDto? value);

        void Set(Guid storeId, string systemName, StoreNavigationPublicMenuDto value);

        void Invalidate(Guid storeId);
    }
}
