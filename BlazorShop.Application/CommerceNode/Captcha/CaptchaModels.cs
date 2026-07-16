namespace BlazorShop.Application.CommerceNode.Captcha
{
    public sealed record CaptchaVerificationRequest(
        string Target,
        string? Token,
        string? RemoteIpAddress,
        string? UserAgent);

    public sealed record CaptchaVerificationResult(
        bool Success,
        string? FailureCode = null)
    {
        public static CaptchaVerificationResult Passed() => new(true);

        public static CaptchaVerificationResult Failed(string failureCode) => new(false, failureCode);
    }
}
