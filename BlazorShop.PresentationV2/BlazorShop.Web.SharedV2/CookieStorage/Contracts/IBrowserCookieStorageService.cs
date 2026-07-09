namespace BlazorShop.Web.SharedV2V2.CookieStorage.Contracts
{
    public interface IBrowserCookieStorageService
    {
        Task SetAsync(string name, string value, int days, string path = "/");

        Task<string?> GetAsync(string name);

        Task RemoveAsync(string name);
    }
}