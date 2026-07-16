namespace BlazorShop.Application.CommerceNode.Carts
{
    public sealed class StorefrontCartOptions
    {
        public const int DefaultMaxLines = 100;
        public const int DefaultMaxQuantityPerLine = 999;
        public const int DefaultMaxPersonalizationHashLength = 128;
        public const int DefaultMaxPersonalizationJsonLength = 8192;
        public const int DefaultMaxFulfillmentProviderKeyLength = 64;

        public int MaxLines { get; set; } = DefaultMaxLines;

        public int MaxQuantityPerLine { get; set; } = DefaultMaxQuantityPerLine;

        public int MaxPersonalizationHashLength { get; set; } = DefaultMaxPersonalizationHashLength;

        public int MaxPersonalizationJsonLength { get; set; } = DefaultMaxPersonalizationJsonLength;

        public int MaxFulfillmentProviderKeyLength { get; set; } = DefaultMaxFulfillmentProviderKeyLength;

        public int EffectiveMaxLines => MaxLines > 0 ? MaxLines : DefaultMaxLines;

        public int EffectiveMaxQuantityPerLine => MaxQuantityPerLine > 0 ? MaxQuantityPerLine : DefaultMaxQuantityPerLine;

        public int EffectiveMaxPersonalizationHashLength =>
            MaxPersonalizationHashLength > 0 ? MaxPersonalizationHashLength : DefaultMaxPersonalizationHashLength;

        public int EffectiveMaxPersonalizationJsonLength =>
            MaxPersonalizationJsonLength > 0 ? MaxPersonalizationJsonLength : DefaultMaxPersonalizationJsonLength;

        public int EffectiveMaxFulfillmentProviderKeyLength =>
            MaxFulfillmentProviderKeyLength > 0 ? MaxFulfillmentProviderKeyLength : DefaultMaxFulfillmentProviderKeyLength;
    }
}
