namespace BlazorShop.Application.Common.Results
{
    public sealed record ApplicationMediaContent(
        byte[] Content,
        string ContentType,
        string? FileName = null,
        IReadOnlyDictionary<string, string>? Metadata = null);
}
