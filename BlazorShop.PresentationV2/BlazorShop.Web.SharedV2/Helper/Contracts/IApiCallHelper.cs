namespace BlazorShop.Web.SharedV2V2.Helper.Contracts
{
    using System.Net.Http;

    using BlazorShop.Web.SharedV2V2.Models;

    public interface IApiCallHelper
    {
        Task<HttpResponseMessage> ApiCallTypeCall<TModel>(ApiCall apiCall);

        Task<TResponse> GetServiceResponse<TResponse>(HttpResponseMessage responseMessage);

        Task<ServiceResponse<TPayload>> GetMutationResponse<TPayload>(HttpResponseMessage responseMessage, string defaultErrorMessage);

        Task<QueryResult<TResponse>> GetQueryResult<TResponse>(HttpResponseMessage responseMessage, string defaultErrorMessage);

        ServiceResponse ConnectionError();
    }
}
