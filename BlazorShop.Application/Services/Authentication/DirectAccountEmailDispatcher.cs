namespace BlazorShop.Application.Services.Authentication
{
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Domain.Contracts;

    public sealed class DirectAccountEmailDispatcher : IAccountEmailDispatcher
    {
        private readonly IEmailService emailService;

        public DirectAccountEmailDispatcher(IEmailService emailService)
        {
            this.emailService = emailService;
        }

        public async Task<AccountEmailDispatchResult> SendActivationAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await this.emailService.SendEmailAsync(
                    request.Email,
                    "Confirm your email",
                    $"Please confirm your email by clicking <a href=\"{request.ActionUrl}\">here</a>.");

                return AccountEmailDispatchResult.Succeeded();
            }
            catch (Exception ex)
            {
                return AccountEmailDispatchResult.Failed("account_email.activation_failed", ex.Message);
            }
        }

        public async Task<AccountEmailDispatchResult> SendPasswordRecoveryAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await this.emailService.SendEmailAsync(
                    request.Email,
                    "Reset your password",
                    $"Reset your password by clicking <a href=\"{request.ActionUrl}\">here</a>.");

                return AccountEmailDispatchResult.Succeeded();
            }
            catch (Exception ex)
            {
                return AccountEmailDispatchResult.Failed("account_email.password_recovery_failed", ex.Message);
            }
        }
    }
}
