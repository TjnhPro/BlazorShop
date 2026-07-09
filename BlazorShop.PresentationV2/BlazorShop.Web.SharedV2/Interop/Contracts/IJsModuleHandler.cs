namespace BlazorShop.Web.SharedV2.Interop.Contracts
{
    using System;
    using System.Threading.Tasks;

    public interface IJsModuleHandler : IAsyncDisposable
    {
        Task InvokeVoidAsync(string method, params object[] args);

        Task<T> InvokeAsync<T>(string method, params object[] args);
    }
}
