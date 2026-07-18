namespace BlazorShop.CommerceNode.API.Contracts.Storefront
{
    using System.ComponentModel.DataAnnotations;

    using BlazorShop.Application.CommerceNode.Catalog;
    using BlazorShop.Application.CommerceNode.VariationTemplates;

    using Microsoft.AspNetCore.Mvc;

    public static class StorefrontContractValidation
    {
        public const int DefaultPageSize = 24;
        public const int MaxPageSize = 100;
        public const int EmailMaxLength = 254;
        public const int PasswordMinLength = 8;

        public const string SortByPattern =
            "^(newest|oldest|priceLowToHigh|priceHighToLow|nameAscending|nameDescending|displayOrder|updated)$";
    }

    public sealed record StorefrontPagedResponse<TItem>(
        IReadOnlyList<TItem> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
