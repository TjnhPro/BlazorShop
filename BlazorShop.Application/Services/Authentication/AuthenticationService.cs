namespace BlazorShop.Application.Services.Authentication
{
    using AutoMapper;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.UserIdentity;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Application.Services.Contracts.Logging;
    using BlazorShop.Application.Validations;
    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Entities.Identity;

    using FluentValidation;

    using Microsoft.Extensions.Options;

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAppTokenManager _tokenManager;
        private readonly IAppUserManager _userManager;
        private readonly IAppRoleManager _roleManager;
        private readonly IAppLogger<AuthenticationService> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUser> _createUserValidator;
        private readonly IValidator<LoginUser> _loginUserValidator;
        private readonly IValidator<ChangePassword> _changePasswordValidator;
        private readonly IValidator<ResetPassword> _resetPasswordValidator;
        private readonly IValidationService _validationService;
        private readonly IAccountEmailDispatcher _accountEmailDispatcher;
        private readonly ClientAppOptions _clientAppOptions;
        private readonly IdentityConfirmationOptions _identityConfirmationOptions;

        public AuthenticationService(
            IAppTokenManager tokenManager,
            IAppUserManager userManager,
            IAppRoleManager roleManager,
            IAppLogger<AuthenticationService> logger,
            IMapper mapper,
            IValidator<CreateUser> createUserValidator,
            IValidator<LoginUser> loginUserValidator,
            IValidationService validationService,
            IValidator<ChangePassword> changePasswordValidator,
            IValidator<ResetPassword> resetPasswordValidator,
            IAccountEmailDispatcher accountEmailDispatcher,
            IOptions<ClientAppOptions> clientAppOptions,
            IOptions<IdentityConfirmationOptions> identityConfirmationOptions)
        {
            _tokenManager = tokenManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _mapper = mapper;
            _createUserValidator = createUserValidator;
            _loginUserValidator = loginUserValidator;
            _validationService = validationService;
            _changePasswordValidator = changePasswordValidator;
            _resetPasswordValidator = resetPasswordValidator;
            _accountEmailDispatcher = accountEmailDispatcher;
            _clientAppOptions = clientAppOptions.Value;
            _identityConfirmationOptions = identityConfirmationOptions.Value;
        }

        public async Task<ServiceResponse> CreateUser(CreateUser user)
        {
            var validationResult = await _validationService.ValidateAsync(user, _createUserValidator);

            if (!validationResult.Success)
            {
                return validationResult;
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return new ServiceResponse { Message = "Email is required." };
            }

            var mappedUser = _mapper.Map<AppUser>(user);
            mappedUser.UserName = user.Email;
            mappedUser.PasswordHash = user.Password;

            var result = await _userManager.CreateUserAsync(mappedUser);

            if (!result)
            {
                return new ServiceResponse { Message = "User already exists." };
            }

            var currentUser = await _userManager.GetUserByEmailAsync(user.Email);
            if (currentUser == null)
            {
                await RollbackCreatedUserAsync(user.Email, "Unable to remove user after create because the newly created user could not be loaded.");
                return new ServiceResponse { Message = "Unable to load the newly created user." };
            }

            var users = await _userManager.GetAllUsersAsync();

            bool assignedResult = await _roleManager.AddUserToRoleAsync(currentUser, users.Count() > 1 ? "User" : "Admin");

            if (!assignedResult)
            {
                await RollbackCreatedUserAsync(currentUser.Email,
                    $"User with Email {currentUser.Email} failed to be removed after failed role assignment.");
                return new ServiceResponse { Message = "Error occurred in creating account." };
            }

            if (RequiresConfirmedEmail())
            {
                try
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(currentUser);
                    var confirmationLink = this.BuildClientUrl($"confirm-email?userId={currentUser.Id}&token={Uri.EscapeDataString(token)}");

                    await this.SendConfirmationEmail(
                        user.Email,
                        confirmationLink,
                        currentUser.FullName,
                        currentUser.Id);
                }
                catch (Exception ex)
                {
                _logger.LogError(ex, $"Failed to enqueue email confirmation link to {user.Email}");
                    await RollbackCreatedUserAsync(currentUser.Email,
                        $"User with Email {currentUser.Email} failed to be removed after confirmation email dispatch failed.");
                    return new ServiceResponse { Message = "Failed to send confirmation email." };
                }
            }
            else
            {
                await TryConfirmUserForNonStrictRegistrationAsync(currentUser);
            }

            return new ServiceResponse { Success = true, Message = "User created successfully." };
        }

        public async Task<LoginResponse> LoginUser(LoginUser user, string? ipAddress = null, string? userAgent = null)
        {
            var validationResult = await _validationService.ValidateAsync(user, _loginUserValidator);

            if (!validationResult.Success)
            {
                return new LoginResponse { Message = validationResult.Message ?? "Validation failed." };
            }

            var mappedUser = _mapper.Map<AppUser>(user);
            mappedUser.PasswordHash = user.Password;

            var currentUser = await _userManager.GetUserByEmailAsync(user.Email);
            if (currentUser == null)
            {
                return new LoginResponse { Message = "Invalid credentials." };
            }

            var loginResult = await _userManager.LoginUserAsync(mappedUser);

            if (!loginResult.Success)
            {
                if (loginResult.IsLockedOut)
                {
                    return new LoginResponse { Message = "Account is temporarily locked. Please try again later." };
                }

                if (loginResult.IsNotAllowed && RequiresConfirmedSignIn() && !currentUser.EmailConfirmed)
                {
                    return await CreateConfirmationRequiredResponseAsync(currentUser);
                }

                return new LoginResponse { Message = "Invalid credentials." };
            }

            if (RequiresConfirmedSignIn() && !currentUser.EmailConfirmed)
            {
                return await CreateConfirmationRequiredResponseAsync(currentUser);
            }


            var confirmedEmail = currentUser.Email;
            if (string.IsNullOrWhiteSpace(confirmedEmail))
            {
                return new LoginResponse { Message = "Email not available for user." };
            }

            var claims = await _userManager.GetUserClaimsAsync(confirmedEmail);

            var accessToken = _tokenManager.GenerateAccessToken(claims);
            var refreshToken = _tokenManager.GetReFreshToken();

            var refreshTokenEntry = _tokenManager.CreateRefreshToken(currentUser.Id, refreshToken, ipAddress, userAgent);
            var saveTokenResult = await _tokenManager.AddRefreshTokenAsync(refreshTokenEntry);

            return saveTokenResult <= 0
                ? new LoginResponse { Message = "Error occurred in login." }
                : new LoginResponse { Success = true, Message = "Login successful.", Token = accessToken, RefreshToken = refreshToken };

        }

        public async Task<LoginResponse> ReviveToken(string refreshToken, string? ipAddress = null, string? userAgent = null)
        {
            var storedRefreshToken = await _tokenManager.GetRefreshTokenAsync(refreshToken);

            if (storedRefreshToken is null)
            {
                return new LoginResponse { Message = "Invalid token." };
            }

            if (!_tokenManager.IsRefreshTokenActive(storedRefreshToken))
            {
                if (storedRefreshToken.RevokedAtUtc is null && storedRefreshToken.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    await _tokenManager.RevokeRefreshTokenAsync(refreshToken, ipAddress);
                }

                if (storedRefreshToken.RevokedAtUtc is not null && !string.IsNullOrWhiteSpace(storedRefreshToken.ReplacedByTokenHash))
                {
                    await _tokenManager.RevokeRefreshTokenFamilyAsync(refreshToken, ipAddress);
                }

                return new LoginResponse { Message = "Invalid token." };
            }

            var currentUser = await _userManager.GetUserByIdAsync(storedRefreshToken.UserId);
            if (currentUser == null || string.IsNullOrWhiteSpace(currentUser.Email))
            {
                return new LoginResponse { Message = "Invalid token." };
            }

            var claims = await _userManager.GetUserClaimsAsync(currentUser.Email);
            var newAccessToken = _tokenManager.GenerateAccessToken(claims);
            var newRefreshToken = _tokenManager.GetReFreshToken();

            var newRefreshTokenEntry = _tokenManager.CreateRefreshToken(currentUser.Id, newRefreshToken, ipAddress, userAgent);
            var saveTokenResult = await _tokenManager.AddRefreshTokenAsync(newRefreshTokenEntry);

            if (saveTokenResult <= 0)
            {
                return new LoginResponse { Message = "Error occurred in login." };
            }

            var revokeTokenResult = await _tokenManager.RevokeRefreshTokenAsync(refreshToken, ipAddress, newRefreshToken);

            if (revokeTokenResult <= 0)
            {
                await _tokenManager.RevokeRefreshTokenAsync(newRefreshToken, ipAddress);
                return new LoginResponse { Message = "Error occurred in login." };
            }

            return new LoginResponse { Success = true, Message = "Token revived successfully.", Token = newAccessToken, RefreshToken = newRefreshToken };
        }

        public async Task<ServiceResponse> Logout(string refreshToken, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new ServiceResponse(true, "Logged out successfully.");
            }

            await _tokenManager.RevokeRefreshTokenAsync(refreshToken, ipAddress);
            return new ServiceResponse(true, "Logged out successfully.");
        }

        public async Task<ServiceResponse> ChangePassword(ChangePassword changePasswordDto, string userId)
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResponse { Message = "User not found." };
            }

            var validationResult = await this._validationService.ValidateAsync(
                                       changePasswordDto,
                                       _changePasswordValidator);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result)
            {
                return new ServiceResponse { Message = "Error occurred while changing password." };
            }

            _logger.LogInformation($"Password changed successfully for user with ID: {userId}");

            return new ServiceResponse { Success = true, Message = "Password changed successfully." };
        }

        public async Task<bool> CheckPasswordAsync(AppUser user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<ServiceResponse> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ServiceResponse { Message = "Invalid user ID." };
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result)
            {
                return new ServiceResponse { Message = "Email confirmation failed." };
            }

            return new ServiceResponse { Success = true, Message = "Email confirmed successfully." };
        }

        public async Task<ServiceResponse> ForgotPassword(string email)
        {
            var genericResponse = new ServiceResponse(
                true,
                "If an account exists for this email, password reset instructions have been sent.");

            if (string.IsNullOrWhiteSpace(email))
            {
                return genericResponse;
            }

            var user = await _userManager.GetUserByEmailAsync(email.Trim());
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                return genericResponse;
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = this.BuildClientUrl(
                    $"reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(token)}");
                var dispatchResult = await _accountEmailDispatcher.SendPasswordRecoveryAsync(
                    new AccountEmailDispatchRequest(
                        user.Email,
                        user.FullName,
                        resetLink,
                        user.Id));

                if (!dispatchResult.Success)
                {
                    _logger.LogError(
                        new InvalidOperationException(dispatchResult.ErrorCode ?? "account_email.password_recovery_failed"),
                        $"Failed to enqueue password reset email to {user.Email}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enqueue password reset email to {email}");
            }

            return genericResponse;
        }

        public async Task<ServiceResponse> ResetPassword(ResetPassword resetPassword)
        {
            var validationResult = await _validationService.ValidateAsync(resetPassword, _resetPasswordValidator);
            if (!validationResult.Success)
            {
                return validationResult;
            }

            var user = await _userManager.GetUserByEmailAsync(resetPassword.Email.Trim());
            if (user is null)
            {
                return new ServiceResponse(false, "Password reset could not be completed.");
            }

            var result = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
            return result
                ? new ServiceResponse(true, "Password reset successfully.")
                : new ServiceResponse(false, "Password reset could not be completed.");
        }

        public Task SendConfirmationEmail(string email, string confirmationLink)
        {
            return this.SendConfirmationEmail(email, confirmationLink, null, null);
        }

        private async Task SendConfirmationEmail(
            string email,
            string confirmationLink,
            string? fullName,
            string? userId)
        {
            var result = await _accountEmailDispatcher.SendActivationAsync(
                new AccountEmailDispatchRequest(email, fullName, confirmationLink, userId));
            if (!result.Success)
            {
                throw new InvalidOperationException(result.ErrorCode ?? "account_email.activation_failed");
            }
        }

        public async Task<ServiceResponse> UpdateProfile(string userId, UpdateProfile dto)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ServiceResponse(false, "Invalid user id.");
            }

            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            {
                return new ServiceResponse(false, "Full name and email are required.");
            }

            var ok = await _userManager.UpdateUserAsync(userId, dto.FullName, dto.Email, dto.PhoneNumber);
            return ok
                ? new ServiceResponse(true, "Profile updated successfully.")
                : new ServiceResponse(false, "Failed to update profile.");
        }

        private string BuildClientUrl(string pathAndQuery)
        {
            return $"{_clientAppOptions.BaseUrl.TrimEnd('/')}/{pathAndQuery.TrimStart('/')}";
        }

        private bool RequiresConfirmedSignIn()
        {
            return _identityConfirmationOptions.RequireConfirmedAccount || _identityConfirmationOptions.RequireConfirmedEmail;
        }

        private bool RequiresConfirmedEmail()
        {
            return _identityConfirmationOptions.RequireConfirmedEmail;
        }

        private async Task<LoginResponse> CreateConfirmationRequiredResponseAsync(AppUser currentUser)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(currentUser);
            var confirmationLink = this.BuildClientUrl($"confirm-email?userId={currentUser.Id}&token={Uri.EscapeDataString(token)}");

            var pendingEmail = currentUser.Email;
            if (!string.IsNullOrWhiteSpace(pendingEmail))
            {
                try
                {
                    await this.SendConfirmationEmail(
                        pendingEmail,
                        confirmationLink,
                        currentUser.FullName,
                        currentUser.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        $"Failed to enqueue email confirmation link for unconfirmed login. Email: {currentUser.Email}, UserId: {currentUser.Id}");

                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Email not confirmed. A new confirmation link could not be sent at this time."
                    };
                }
            }

            _logger.LogWarning($"User with unconfirmed email tried to log in. Email: {currentUser.Email}, UserId: {currentUser.Id}");

            return new LoginResponse
                       {
                           Success = false,
                           Message = "Email not confirmed. A new confirmation link has been sent to your email."
                       };
        }

        private async Task TryConfirmUserForNonStrictRegistrationAsync(AppUser currentUser)
        {
            if (currentUser.EmailConfirmed)
            {
                return;
            }

            try
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(currentUser);
                var confirmationResult = await _userManager.ConfirmEmailAsync(currentUser, token);
                if (!confirmationResult)
                {
                    _logger.LogWarning($"User was created without confirmation requirements, but local email confirmation could not be completed. Email: {currentUser.Email}, UserId: {currentUser.Id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to locally confirm email for user created without confirmation requirements. Email: {currentUser.Email}, UserId: {currentUser.Id}");
            }
        }

        private async Task RollbackCreatedUserAsync(string? email, string rollbackFailureMessage)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError(
                    new InvalidOperationException("Cannot remove user because email is missing."),
                    "Cannot remove user because email is missing.");
                return;
            }

            var removeUserResult = await _userManager.RemoveUserByEmail(email);
            if (removeUserResult <= 0)
            {
                _logger.LogError(new Exception(rollbackFailureMessage), rollbackFailureMessage);
            }
        }
    }
}
