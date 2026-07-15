namespace BlazorShop.Application.DTOs.Seo
{
    public sealed record StoreSeoSlugPolicyResult(
        bool Success,
        string? Slug,
        string? Message = null)
    {
        public static StoreSeoSlugPolicyResult Succeeded(string slug)
        {
            return new StoreSeoSlugPolicyResult(true, slug);
        }

        public static StoreSeoSlugPolicyResult Failed(string message)
        {
            return new StoreSeoSlugPolicyResult(false, null, message);
        }
    }
}
