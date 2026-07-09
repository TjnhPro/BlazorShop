namespace BlazorShop.Web.SharedV2V2.Helper.Contracts
{
    public interface ITokenService
    {
        Task<string> GetJwtTokenAsync(string key);

        Task StoreJwtTokenAsync(string key, string value);

        Task RemoveJwtTokenAsync(string key);
    }
}
