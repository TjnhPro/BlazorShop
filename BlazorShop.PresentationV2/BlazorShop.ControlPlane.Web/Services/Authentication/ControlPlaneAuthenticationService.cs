namespace BlazorShop.ControlPlane.Web.Services.Authentication
{
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Web.SharedV2.Models.Authentication;
    using BlazorShop.Web.SharedV2.Services.Contracts;

    public sealed class ControlPlaneAuthenticationService : IAuthenticationService
    {
        private const string LoginRoute = "api/control-plane/auth/login";
        private const string RefreshRoute = "api/control-plane/auth/refresh-token";
        private const string LogoutRoute = "api/control-plane/auth/logout";

        private readonly IControlPlaneApiClient apiClient;

        public ControlPlaneAuthenticationService(IControlPlaneApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        public Task<ServiceResponse> CreateUser(CreateUser user)
        {
            return Task.FromResult(new ServiceResponse(Message: "Control Plane user creation is not available from this sign-in client."));
        }

        public async Task<LoginResponse> LoginUser(LoginUser user)
        {
            var result = await this.apiClient.PostPublicAsync<LoginUser, LoginResponse>(
                LoginRoute,
                user,
                "Unable to sign in with those credentials.");

            return result.Success && result.Data is not null
                ? result.Data
                : new LoginResponse(Message: result.Message);
        }

        public async Task<QueryResult<LoginResponse>> ReviveToken()
        {
            var result = await this.apiClient.PostPublicAsync<LoginResponse>(
                RefreshRoute,
                "Session refresh failed.");

            return result.Success && result.Data is not null
                ? QueryResult<LoginResponse>.Succeeded(result.Data)
                : QueryResult<LoginResponse>.Failed(result.Message, result.StatusCode);
        }

        public async Task<ServiceResponse> Logout()
        {
            var result = await this.apiClient.PostPrivateAsync<ServiceResponse>(
                LogoutRoute,
                "Server logout failed.");

            return result.Success
                ? result.Data ?? new ServiceResponse(Success: true, Message: result.Message)
                : new ServiceResponse(Message: result.Message);
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

    }
}
