namespace BlazorShop.Application.Services.Contracts.Authentication
{
    public interface IAccountEmailDispatcher
    {
        Task<AccountEmailDispatchResult> SendActivationAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default);

        Task<AccountEmailDispatchResult> SendPasswordRecoveryAsync(
            AccountEmailDispatchRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed record AccountEmailDispatchRequest(
        string Email,
        string? FullName,
        string ActionUrl,
        string? UserId = null);

    public sealed record AccountEmailDispatchResult(
        bool Success,
        string? ErrorCode = null,
        string? Message = null)
    {
        public static AccountEmailDispatchResult Succeeded()
        {
            return new AccountEmailDispatchResult(true);
        }

        public static AccountEmailDispatchResult Failed(string errorCode, string message)
        {
            return new AccountEmailDispatchResult(false, errorCode, message);
        }
    }
}
