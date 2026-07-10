namespace BlazorShop.Infrastructure.Data.CommerceNode.Services
{
    using System.Text;

    using BlazorShop.Application.CommerceNode.ProductImports;

    public sealed class ProductImportCsvParser : IProductImportCsvParser
    {
        private static readonly string[] RequiredHeaders =
        [
            "sku",
            "name",
            "slug",
            "category_slug",
            "product_type",
            "variation_template_slug",
            "price",
            "compare_price",
            "quantity",
            "is_published",
            "short_description",
            "description",
            "image_urls",
        ];

        public async Task<ProductImportParsedFile> ParseAsync(
            Stream content,
            int maxRows,
            CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return new ProductImportParsedFile([], [new ProductImportError("file", "CSV header row is required.")]);
            }

            var headers = ParseLine(headerLine)
                .Select(header => (header ?? string.Empty).Trim().ToLowerInvariant())
                .ToArray();

            var errors = RequiredHeaders
                .Where(required => !headers.Contains(required, StringComparer.OrdinalIgnoreCase))
                .Select(required => new ProductImportError(required, $"Required column '{required}' is missing."))
                .ToList();

            if (errors.Count > 0)
            {
                return new ProductImportParsedFile([], errors);
            }

            var rows = new List<ProductImportParsedRow>();
            var rowNumber = 1;
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowNumber++;
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (rows.Count >= maxRows)
                {
                    errors.Add(new ProductImportError("file", $"CSV file exceeds the maximum of {maxRows} rows."));
                    break;
                }

                var cells = ParseLine(line);
                var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < headers.Length; index++)
                {
                    values[headers[index]] = index < cells.Count ? cells[index]?.Trim() : null;
                }

                rows.Add(new ProductImportParsedRow(rowNumber, values));
            }

            return new ProductImportParsedFile(rows, errors);
        }

        private static IReadOnlyList<string?> ParseLine(string line)
        {
            var result = new List<string?>();
            var builder = new StringBuilder();
            var inQuotes = false;

            for (var index = 0; index < line.Length; index++)
            {
                var current = line[index];
                if (current == '"')
                {
                    if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        builder.Append('"');
                        index++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (current == ',' && !inQuotes)
                {
                    result.Add(builder.ToString());
                    builder.Clear();
                    continue;
                }

                builder.Append(current);
            }

            result.Add(builder.ToString());
            return result;
        }
    }
}
