namespace BlazorShop.Web.SharedV2.Authentication
{
    using System.Net;
    using System.Net.Http.Headers;

    public class RefreshTokenHandler : DelegatingHandler
    {
        private static readonly HttpRequestOptionsKey<bool> RetriedKey = new("X-Refresh-Retried");

        private readonly IAuthenticationSessionRefresher sessionRefresher;

        public RefreshTokenHandler(IAuthenticationSessionRefresher sessionRefresher)
        {
            this.sessionRefresher = sessionRefresher;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            if (!request.Options.TryGetValue(RetriedKey, out var retried) || !retried)
            {
                var loginResponse = await this.sessionRefresher.TryRefreshAsync();
                if (loginResponse is not null)
                {
                    if (request.Method == HttpMethod.Get || request.Content is null)
                    {
                        request.Options.Set(RetriedKey, true);
                        request.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationConstants.BearerScheme, loginResponse.Token);
                        response.Dispose();
                        return await base.SendAsync(request, cancellationToken);
                    }
                }
            }

            return response;
        }
    }
}
