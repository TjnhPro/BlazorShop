namespace BlazorShop.Web.SharedV2V2.Helper.Contracts
{
    public interface IHttpClientHelper
    {
        HttpClient GetPublicClient();

        Task<HttpClient> GetPrivateClientAsync();
    }
}
