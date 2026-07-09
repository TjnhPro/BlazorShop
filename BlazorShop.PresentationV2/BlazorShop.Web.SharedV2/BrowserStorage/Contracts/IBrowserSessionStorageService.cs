namespace BlazorShop.Web.SharedV2V2.BrowserStorage.Contracts
{
    public interface IBrowserSessionStorageService
    {
        Task SetAsync(string key, string value);

        Task<string?> GetAsync(string key);

        Task RemoveAsync(string key);
    }
}