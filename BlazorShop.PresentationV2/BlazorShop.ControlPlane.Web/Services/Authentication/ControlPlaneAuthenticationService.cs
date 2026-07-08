namespace BlazorShop.ControlPlane.Web.Services.Authentication
{
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Web.Shared.Helper.Contracts;
    using BlazorShop.Web.Shared.Models;
    using BlazorShop.Web.Shared.Models.Authentication;
    using BlazorShop.Web.Shared.Services.Contracts;

    public sealed class ControlPlaneAuthenticationService : IAuthenticationService
    {
        private const string LoginRoute = "api/control-plane/auth/login";
        private const string RefreshRoute = "api/control-plane/auth/refresh-token";
        private const string LogoutRoute = "api/control-plane/auth/logout";

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientHelper httpClientHelper;

        public ControlPlaneAuthenticationService(IHttpClientHelper httpClientHelper)
        {
            this.httpClientHelper = httpClientHelper;
        }

        public Task<ServiceResponse> CreateUser(CreateUser user)
        {
            return Task.FromResult(new ServiceResponse(Message: "Control Plane user creation is not available from this sign-in client."));
        }

        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            var client = this.httpClientHelper.GetPublicClient();

            try
            {
                using var response = await client.PostAsJsonAsync(LoginRoute, user, SerializerOptions);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>(SerializerOptions)
                           ?? new LoginResponse(Message: "Invalid login response.");
                }

                return await ReadLoginFailureAsync(response, "Unable to sign in with those credentials.");
            }
            catch (HttpRequestException)
            {
                return new LoginResponse(Message: "Unable to reach the Control Plane API.");
            }
            catch (OperationCanceledException)
            {
                return new LoginResponse(Message: "The sign-in request timed out.");
            }
        }

        public async Task<QueryResult<LoginResponse>> ReviveToken()
        {
            var client = this.httpClientHelper.GetPublicClient();

            try
            {
                using var response = await client.PostAsync(RefreshRoute, content: null);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(SerializerOptions);
                    return loginResponse is null
                        ? QueryResult<LoginResponse>.Failed("Invalid refresh response.")
                        : QueryResult<LoginResponse>.Succeeded(loginResponse);
                }

                var failure = await ReadLoginFailureAsync(response, "Session refresh failed.");
                return QueryResult<LoginResponse>.Failed(failure.Message, response.StatusCode);
            }
            catch (HttpRequestException)
            {
                return QueryResult<LoginResponse>.Failed("Unable to reach the Control Plane API.");
            }
            catch (OperationCanceledException)
            {
                return QueryResult<LoginResponse>.Failed("The session refresh request timed out.");
            }
        }

        public async Task<ServiceResponse> Logout()
        {
            var client = await this.httpClientHelper.GetPrivateClientAsync();

            try
            {
                using var response = await client.PostAsync(LogoutRoute, content: null);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ServiceResponse>(SerializerOptions)
                           ?? new ServiceResponse(Success: true, Message: "Signed out.");
                }

                return new ServiceResponse(Message: "Server logout failed.");
            }
            catch (HttpRequestException)
            {
                return new ServiceResponse(Message: "Unable to reach the Control Plane API.");
            }
            catch (OperationCanceledException)
            {
                return new ServiceResponse(Message: "The logout request timed out.");
            }
        }

        public Task<ServiceResponse> ChangePassword(PasswordChangeModel changePasswordDto)
        {
            return Task.FromResult(new ServiceResponse(Message: "Control Plane password changes are not available from this client."));
        }

        public Task<ServiceResponse> ConfirmEmail(string userId, string token)
        {
            return Task.FromResult(new ServiceResponse(Message: "Control Plane email confirmation is not available from this client."));
        }

        public Task<ServiceResponse> UpdateProfile(UpdateProfileModel model)
        {
            return Task.FromResult(new ServiceResponse(Message: "Control Plane profile updates are not available from this client."));
        }

        private static async Task<LoginResponse> ReadLoginFailureAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.Content is null)
            {
                return new LoginResponse(Message: fallbackMessage);
            }

            try
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(SerializerOptions);
                if (!string.IsNullOrWhiteSpace(loginResponse?.Message))
                {
                    return loginResponse;
                }
            }
            catch (JsonException)
            {
            }

            return new LoginResponse(Message: fallbackMessage);
        }
    }
}
