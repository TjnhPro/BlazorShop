namespace BlazorShop.CommerceNode.API.Configuration
{
    public sealed class CommerceNodeOptions
    {
        public const string SectionName = "CommerceNode";

        public string NodeKey { get; set; } = string.Empty;

        public string NodeSecret { get; set; } = string.Empty;

        public string[] AllowedControlPlaneIps { get; set; } = [];
    }
}
