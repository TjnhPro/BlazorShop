namespace BlazorShop.CommerceNode.API.Validation;

using BlazorShop.Application.CommerceNode.Media;

public static class ImageFileSignatureValidator
{
    public static async Task<bool> IsValidAsync(Stream stream, string contentType, CancellationToken cancellationToken = default)
    {
        return await MediaFilePolicy.IsValidImageSignatureAsync(stream, contentType, cancellationToken);
    }
}
