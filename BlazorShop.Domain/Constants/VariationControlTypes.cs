namespace BlazorShop.Domain.Constants
{
    public static class VariationControlTypes
    {
        public const string Dropdown = "dropdown";
        public const string Radio = "radio";
        public const string Color = "color";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(
            [Dropdown, Radio, Color],
            StringComparer.OrdinalIgnoreCase);
    }
}
