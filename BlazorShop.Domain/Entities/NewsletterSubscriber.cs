namespace BlazorShop.Domain.Entities
{
    public class NewsletterSubscriber
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Email { get; set; } = string.Empty;

        public Guid? StoreId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
