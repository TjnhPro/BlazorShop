namespace BlazorShop.Web.SharedV2V2.Models
{
    public record LoginResponse(bool Success = false, string Message = null!, string Token = null!);
}
