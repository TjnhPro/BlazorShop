namespace BlazorShop.CommerceNode.API.Configuration
{
    using Microsoft.Extensions.Options;

    public sealed class CommerceNodeOptionsValidator : IValidateOptions<CommerceNodeOptions>
    {
        private readonly IWebHostEnvironment environment;

        public CommerceNodeOptionsValidator(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public ValidateOptionsResult Validate(string? name, CommerceNodeOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.NodeKey))
            {
                failures.Add("CommerceNode:NodeKey is required.");
            }

            if (string.IsNullOrWhiteSpace(options.NodeSecret))
            {
                failures.Add("CommerceNode:NodeSecret is required.");
            }

            if (!this.environment.IsDevelopment() && options.AllowedControlPlaneIps.Length == 0)
            {
                failures.Add("CommerceNode:AllowedControlPlaneIps must contain at least one Control Plane IP outside Development.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}
