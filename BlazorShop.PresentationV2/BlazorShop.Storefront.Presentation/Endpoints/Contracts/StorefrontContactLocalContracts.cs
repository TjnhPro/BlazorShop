namespace BlazorShop.Storefront.Presentation.Endpoints;

public sealed class StorefrontLocalContactRequest
{
    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }
}
