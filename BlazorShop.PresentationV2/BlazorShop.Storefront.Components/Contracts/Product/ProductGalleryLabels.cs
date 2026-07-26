namespace BlazorShop.Storefront.Components.Contracts.Product;

public sealed record ProductGalleryLabels(
    string ImageUnavailableText,
    string ImageUnavailableAltFormat,
    string PreviousImage,
    string NextImage,
    string ImagesRegion,
    string ImageButtonFormat)
{
    public static ProductGalleryLabels Empty { get; } = new(
        string.Empty,
        "{0}",
        string.Empty,
        string.Empty,
        string.Empty,
        "{0}");

    public string FormatImageUnavailableAlt(string productName)
    {
        return string.Format(ImageUnavailableAltFormat, productName);
    }

    public string FormatImageButton(int imageNumber)
    {
        return string.Format(ImageButtonFormat, imageNumber);
    }
}
