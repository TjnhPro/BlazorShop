namespace BlazorShop.Domain.Constants
{
    public static class ProductTypes
    {
        public const string Simple = "Simple";

        public const string VariantInventory = "VariantInventory";

        public const string CustomVariations = "CustomVariations";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Simple,
            VariantInventory,
            CustomVariations,
        };
    }
}
