namespace BlazorShop.Application.Services
{
    using System.Text.Json;

    using BlazorShop.Application.DTOs.Product.ProductVariant;

    public static class ProductVariantAttributeNormalizer
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public static ProductVariantAttributeNormalization Normalize(
            IEnumerable<ProductVariantAttributeDto>? attributes,
            string? fallbackColor = null,
            string? fallbackSize = null)
        {
            var normalizedAttributes = BuildAttributes(attributes, fallbackColor, fallbackSize);

            if (normalizedAttributes.Count == 0)
            {
                return new ProductVariantAttributeNormalization([], null, null, null);
            }

            var signature = string.Join(
                "|",
                normalizedAttributes.Select(attribute =>
                    $"{NormalizeSignaturePart(attribute.Name)}={NormalizeSignaturePart(attribute.Value)}"));

            var displayName = string.Join(" / ", normalizedAttributes.Select(attribute => attribute.Value));
            var attributesJson = JsonSerializer.Serialize(normalizedAttributes, SerializerOptions);

            return new ProductVariantAttributeNormalization(normalizedAttributes, attributesJson, signature, displayName);
        }

        public static IReadOnlyList<ProductVariantAttributeDto> Deserialize(string? attributesJson)
        {
            if (string.IsNullOrWhiteSpace(attributesJson))
            {
                return [];
            }

            try
            {
                var attributes = JsonSerializer.Deserialize<IReadOnlyList<ProductVariantAttributeDto>>(attributesJson, SerializerOptions);
                return BuildAttributes(attributes, null, null);
            }
            catch (JsonException)
            {
                return [];
            }
        }

        private static List<ProductVariantAttributeDto> BuildAttributes(
            IEnumerable<ProductVariantAttributeDto>? attributes,
            string? fallbackColor,
            string? fallbackSize)
        {
            var normalized = (attributes ?? [])
                .Select(attribute => new ProductVariantAttributeDto
                {
                    Name = attribute.Name.Trim(),
                    Value = attribute.Value.Trim(),
                })
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name)
                    && !string.IsNullOrWhiteSpace(attribute.Value))
                .GroupBy(attribute => NormalizeSignaturePart(attribute.Name))
                .Select(group => group.First())
                .ToList();

            if (normalized.Count == 0)
            {
                AddFallbackAttribute(normalized, "Color", fallbackColor);
                AddFallbackAttribute(normalized, "Size", fallbackSize);
            }

            return normalized
                .OrderBy(attribute => NormalizeSignaturePart(attribute.Name), StringComparer.Ordinal)
                .ToList();
        }

        private static void AddFallbackAttribute(
            ICollection<ProductVariantAttributeDto> attributes,
            string name,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            attributes.Add(new ProductVariantAttributeDto
            {
                Name = name,
                Value = value.Trim(),
            });
        }

        private static string NormalizeSignaturePart(string value)
            => value.Trim().ToLowerInvariant();
    }

    public sealed record ProductVariantAttributeNormalization(
        IReadOnlyList<ProductVariantAttributeDto> Attributes,
        string? AttributesJson,
        string? AttributeSignature,
        string? DisplayName);
}
