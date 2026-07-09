namespace BlazorShop.Web.SharedV2.Models
{
    public record LoginResponse(bool Success = false, string Message = null!, string Token = null!);
}
