namespace BlazorShop.Application.CommerceNode.Captcha
{
    public interface ICaptchaVerifier
    {
        Task<CaptchaVerificationResult> VerifyAsync(
            CaptchaVerificationRequest request,
            CancellationToken cancellationToken = default);
    }
}
