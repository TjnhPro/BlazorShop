namespace BlazorShop.ControlPlane.Web.Pages
{
    using BlazorShop.Application.DTOs.Product;

    public sealed class CommerceProductBasicForm
    {
        public string? Name { get; set; }

        public string? ShortDescription { get; set; }

        public string? FullDescription { get; set; }

        public decimal Price { get; set; }

        public decimal? ComparePrice { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime? AvailableStartUtc { get; set; }

        public DateTime? AvailableEndUtc { get; set; }

        public int MinOrderQuantity { get; set; } = 1;

        public int? MaxOrderQuantity { get; set; }

        public int QuantityStep { get; set; } = 1;

        public bool PurchasingDisabled { get; set; }

        public string? PurchasingDisabledReason { get; set; }

        public bool ManageStock { get; set; } = true;

        public bool HideWhenOutOfStock { get; set; }

        public bool ShippingRequired { get; set; } = true;

        public bool FreeShipping { get; set; }

        public decimal? ShippingSurcharge { get; set; }

        public string? DeliveryEstimateText { get; set; }

        public string? Gtin { get; set; }

        public string? Barcode { get; set; }

        public string? ManufacturerPartNumber { get; set; }

        public string? Condition { get; set; }

        public decimal? Weight { get; set; }

        public decimal? Length { get; set; }

        public decimal? Width { get; set; }

        public decimal? Height { get; set; }

        public static CommerceProductBasicForm FromProduct(GetProduct product)
        {
            return new CommerceProductBasicForm
            {
                Name = product.Name,
                ShortDescription = product.ShortDescription,
                FullDescription = product.FullDescription,
                Price = product.Price,
                ComparePrice = product.ComparePrice,
                DisplayOrder = product.DisplayOrder,
                AvailableStartUtc = product.AvailableStartUtc,
                AvailableEndUtc = product.AvailableEndUtc,
                MinOrderQuantity = product.MinOrderQuantity,
                MaxOrderQuantity = product.MaxOrderQuantity,
                QuantityStep = product.QuantityStep,
                PurchasingDisabled = product.PurchasingDisabled,
                PurchasingDisabledReason = product.PurchasingDisabledReason,
                ManageStock = product.ManageStock,
                HideWhenOutOfStock = product.HideWhenOutOfStock,
                ShippingRequired = product.ShippingRequired,
                FreeShipping = product.FreeShipping,
                ShippingSurcharge = product.ShippingSurcharge,
                DeliveryEstimateText = product.DeliveryEstimateText,
                Gtin = product.Gtin,
                Barcode = product.Barcode,
                ManufacturerPartNumber = product.ManufacturerPartNumber,
                Condition = product.Condition,
                Weight = product.Weight,
                Length = product.Length,
                Width = product.Width,
                Height = product.Height,
            };
        }
    }
}
