namespace BlazorShop.Application.CommerceNode.Captcha
{
    public sealed class CaptchaOptions
    {
        public bool Enabled { get; set; }

        public string ProviderSystemName { get; set; } = "none";

        public string? PublicSiteKey { get; set; }

        public string? SecretReference { get; set; }

        public double MinimumScore { get; set; } = 0.5;

        public CaptchaTargetOptions Targets { get; set; } = new();
    }

    public sealed class CaptchaTargetOptions
    {
        public bool Login { get; set; }

        public bool Registration { get; set; }

        public bool Newsletter { get; set; }

        public bool PasswordRecovery { get; set; }

        public bool Contact { get; set; }

        public bool Review { get; set; }
    }
}
