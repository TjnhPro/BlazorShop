namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.ComponentModel.DataAnnotations;
    using System.Net;

    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.DTOs;

    public sealed class StoreEmailTestSendService : IStoreEmailTestSendService
    {
        private readonly IStoreEmailTransportResolver transportResolver;
        private readonly IStoreEmailTransportSender transportSender;

        public StoreEmailTestSendService(
            IStoreEmailTransportResolver transportResolver,
            IStoreEmailTransportSender transportSender)
        {
            this.transportResolver = transportResolver;
            this.transportSender = transportSender;
        }

        public async Task<ServiceResponse<SendStoreEmailTestResponse>> SendAsync(
            Guid storeId,
            SendStoreEmailTestRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(
                    request,
                    new ValidationContext(request),
                    validationResults,
                    validateAllProperties: true))
            {
                return new ServiceResponse<SendStoreEmailTestResponse>(
                    false,
                    string.Join(" ", validationResults.Select(result => result.ErrorMessage)));
            }

            var transportResult = await this.transportResolver.ResolveTransportAsync(
                storeId,
                cancellationToken);
            if (!transportResult.Success || transportResult.Transport is null)
            {
                return new ServiceResponse<SendStoreEmailTestResponse>(
                    false,
                    transportResult.Message ?? "Store SMTP transport is not configured.");
            }

            var subject = string.IsNullOrWhiteSpace(request.Subject)
                ? "BlazorShop SMTP test"
                : request.Subject.Trim();
            var body = "<p>This is a BlazorShop store SMTP test message.</p>"
                + $"<p>Store: {WebUtility.HtmlEncode(storeId.ToString("D"))}</p>";
            await this.transportSender.SendAsync(
                transportResult.Transport,
                request.ToEmail.Trim(),
                subject,
                body,
                cancellationToken);

            return new ServiceResponse<SendStoreEmailTestResponse>(true, "Store SMTP test email sent.")
            {
                Payload = new SendStoreEmailTestResponse(request.ToEmail.Trim(), subject, DateTimeOffset.UtcNow),
            };
        }
    }
}
