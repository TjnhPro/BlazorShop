namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using BlazorShop.Application.CommerceNode.Captcha;

    public sealed class NoopCaptchaVerifier : ICaptchaVerifier
    {
        public Task<CaptchaVerificationResult> VerifyAsync(
            CaptchaVerificationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CaptchaVerificationResult.Passed());
        }
    }
}
