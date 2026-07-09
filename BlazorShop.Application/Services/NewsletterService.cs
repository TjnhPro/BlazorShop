namespace BlazorShop.Application.Services
{
    using System;
    using System.Linq;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;

    public class NewsletterService : INewsletterService
    {
        private readonly IGenericRepository<Domain.Entities.NewsletterSubscriber> _repo;
        private readonly IEmailService _emailService;
        private readonly ICommerceStoreContext? _storeContext;

        public NewsletterService(
            IGenericRepository<Domain.Entities.NewsletterSubscriber> repo,
            IEmailService emailService,
            ICommerceStoreContext? storeContext = null)
        {
            _repo = repo;
            _emailService = emailService;
            _storeContext = storeContext;
        }

        public async Task<ServiceResponse> SubscribeAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new ServiceResponse(false, "Email is required.");
            }

            email = email.Trim();

            var storeId = await ResolveCurrentStoreIdAsync();
            var exists = (await _repo.GetAllAsync()).Any(x => x.Email == email && x.StoreId == storeId);
            if (exists)
                return new ServiceResponse(true, "Already subscribed.");

            try
            {
                var added = await _repo.AddAsync(new Domain.Entities.NewsletterSubscriber { Email = email, StoreId = storeId, CreatedOn = DateTime.UtcNow });
                if (added <= 0)
                    return new ServiceResponse(false, "Failed to subscribe.");
            }
            catch
            {
                return new ServiceResponse(false, "Subscription failed.");
            }

            _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            email,
                            "Welcome to BlazorShop Newsletter",
                            "<p>Thank you for subscribing!</p>");
                    }
                    catch
                    {
                        /* ignore */
                    }
                });

            return new ServiceResponse(true, "Subscribed successfully.");
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Payload : null;
        }
    }
}
