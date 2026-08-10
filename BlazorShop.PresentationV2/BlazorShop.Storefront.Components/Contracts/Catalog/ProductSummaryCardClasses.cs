namespace BlazorShop.Storefront.Components.Contracts.Catalog;

public sealed record ProductSummaryCardClasses(
    string? Root = null,
    string? Body = null,
    string? Header = null,
    string? Category = null,
    string? Title = null,
    string? BadgeGroup = null,
    string? Badge = null,
    string? Price = null,
    string? ComparePrice = null,
    string? ImageLink = null,
    string? ImageFrame = null,
    string? Image = null,
    string? ImageFallback = null,
    string? Description = null,
    string? Footer = null,
    string? ActionGroup = null,
    string? PrimaryAction = null,
    string? SecondaryAction = null,
    string? Status = null);
