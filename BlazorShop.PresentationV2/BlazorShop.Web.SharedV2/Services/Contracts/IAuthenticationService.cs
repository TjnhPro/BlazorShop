namespace BlazorShop.Web.SharedV2V2.Services.Contracts
{
    using BlazorShop.Web.SharedV2V2.Models;
    using BlazorShop.Web.SharedV2V2.Models.Authentication;

    public interface IAuthenticationService
    {
        Task<ServiceResponse> CreateUser(CreateUser user);

        Task<LoginResponse> LoginUser(LoginUser user);

        Task<QueryResult<LoginResponse>> ReviveToken();

        Task<ServiceResponse> Logout();

        Task<ServiceResponse> ChangePassword(PasswordChangeModel changePasswordDto);

        Task<ServiceResponse> ConfirmEmail(string userId, string token);

        Task<ServiceResponse> UpdateProfile(UpdateProfileModel model);
    }
}
