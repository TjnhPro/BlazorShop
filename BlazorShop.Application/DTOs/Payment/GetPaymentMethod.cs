namespace BlazorShop.Application.DTOs.Payment
{
    public class GetPaymentMethod
    {
        public required Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public required string Name { get; set; }

        public string? Description { get; set; }
    }
}
