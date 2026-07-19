namespace BlazorShop.Application.Services.Payment
{
    using System.Net.Mail;
    using System.Text.Json;

    using AutoMapper;

    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Application.Services.Contracts;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Contracts.Authentication;
    using BlazorShop.Domain.Contracts.Payment;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;
    using Microsoft.Extensions.Options;

    public class CartService : ICartService
    {
        private const int MaxSelectedAttributes = 5;
        private const int MaxSelectedAttributeNameLength = 100;
        private const int MaxSelectedAttributeValueLength = 200;

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ICart _cart;
        private readonly IMapper _mapper;
        private readonly IProductReadRepository _productReadRepository;
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IPaymentService _paymentService; // Stripe/Card
        private readonly IPayPalPaymentService _payPalPaymentService; // PayPal
        private readonly IAppUserManager _userManager;
        private readonly IOrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        private readonly BankTransferSettings _btSettings;
        private readonly ICommerceStoreContext? _storeContext;
        private readonly IGenericRepository<Product>? _productRepository;
        private readonly IGenericRepository<ProductVariant>? _variantRepository;
        private readonly IApplicationTransactionManager? _transactionManager;
        private readonly IPaymentHandlerResolver? _paymentHandlerResolver;
        private readonly IAppRoleManager? _roleManager;

        public CartService(ICart cart,
                           IMapper mapper,
                           IProductReadRepository productReadRepository,
                           IPaymentMethodService paymentMethodService,
                           IPaymentService paymentService,
                           IPayPalPaymentService payPalPaymentService,
                           IAppUserManager userManager,
                           IOrderRepository orderRepository,
                           IEmailService emailService,
                           IOptions<BankTransferSettings> bankTransferOptions,
                           ICommerceStoreContext? storeContext = null,
                           IGenericRepository<Product>? productRepository = null,
                           IGenericRepository<ProductVariant>? variantRepository = null,
                           IApplicationTransactionManager? transactionManager = null,
                           IPaymentHandlerResolver? paymentHandlerResolver = null,
                           IAppRoleManager? roleManager = null)
        {
            _cart = cart;
            _mapper = mapper;
            _productReadRepository = productReadRepository;
            _paymentMethodService = paymentMethodService;
            _paymentService = paymentService;
            _payPalPaymentService = payPalPaymentService;
            _userManager = userManager;
            _orderRepository = orderRepository;
            _emailService = emailService;
            _btSettings = bankTransferOptions.Value;
            _storeContext = storeContext;
            _productRepository = productRepository;
            _variantRepository = variantRepository;
            _transactionManager = transactionManager;
            _paymentHandlerResolver = paymentHandlerResolver;
            _roleManager = roleManager;
        }

        public async Task<ServiceResponse> SaveCheckoutHistoryAsync(string userId, IEnumerable<CreateOrderItem> orderItems)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ServiceResponse(false, "A signed-in user is required to save checkout history.");
            }

            var currentStoreId = await ResolveCurrentStoreIdAsync();
            var sanitizedOrderItems = orderItems
                .Where(orderItem => orderItem.ProductId != Guid.Empty && orderItem.Quantity > 0)
                .Select(orderItem => new CreateOrderItem
                {
                    ProductId = orderItem.ProductId,
                    Quantity = orderItem.Quantity,
                })
                .ToArray();

            if (sanitizedOrderItems.Length == 0)
            {
                return new ServiceResponse(false, "No valid checkout items were provided.");
            }

            var mappedData = _mapper.Map<IEnumerable<OrderItem>>(sanitizedOrderItems).ToArray();
            foreach (var item in mappedData)
            {
                item.StoreId = currentStoreId;
                item.UserId = userId;
            }

            var result = await _cart.SaveCheckoutHistory(mappedData);

            return result > 0 ? new ServiceResponse(true, "Checkout history saved successfully") : new ServiceResponse(false, "Failed to save checkout history");
        }

        public async Task<ServiceResponse> ConfirmOrderAsync(IEnumerable<ProcessCart> carts, string userId, string? status = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new ServiceResponse(false, "A signed-in user is required to confirm the order.");
            }

            return await CreateOrderAsync(carts, userId, status ?? OrderStatuses.Processing, "ORD");
        }

        public async Task<ServiceResponse> CheckoutAsync(Checkout checkout)
        {
            return await CheckoutAsync(checkout, null);
        }

        public async Task<ServiceResponse> CheckoutAsync(Checkout checkout, string? userId)
        {
            if (_paymentHandlerResolver is not null)
            {
                var paymentMethods = (await _paymentMethodService.GetPaymentMethodsAsync()).ToArray();
                var method = paymentMethods.FirstOrDefault(candidate => candidate.Id == checkout.PaymentMethodId);
                if (method is null)
                {
                    return new ServiceResponse(false, "Invalid payment method.");
                }

                return await CheckoutAsync(new StorefrontCheckoutRequest
                {
                    CustomerEmail = string.Empty,
                    CustomerName = string.Empty,
                    PaymentMethodKey = method.Key,
                    Carts = checkout.Carts,
                }, userId);
            }

            var (products, totalAmount) = await this.GetCartTotalAmount(checkout.Carts);
            var methods = (await _paymentMethodService.GetPaymentMethodsAsync()).ToList();
            if (!methods.Any()) return new ServiceResponse(false, "No payment methods available");

            var creditCardId = methods.FirstOrDefault(m => m.Name == "Credit Card")?.Id;
            var payPalId = methods.FirstOrDefault(m => m.Name == "PayPal")?.Id;
            var codId = methods.FirstOrDefault(m => m.Name == "Cash on Delivery")?.Id;
            var bankId = methods.FirstOrDefault(m => m.Name == "Bank Transfer")?.Id;

            if (creditCardId.HasValue && checkout.PaymentMethodId == creditCardId.Value)
            {
                return await _paymentService.Pay(totalAmount, products, checkout.Carts);
            }
            if (payPalId.HasValue && checkout.PaymentMethodId == payPalId.Value)
            {
                return await _payPalPaymentService.Pay(totalAmount, products, checkout.Carts);
            }
            if (codId.HasValue && checkout.PaymentMethodId == codId.Value)
            {
                return new ServiceResponse(true, "Order placed with Cash on Delivery. You will pay upon delivery.");
            }
            if (bankId.HasValue && checkout.PaymentMethodId == bankId.Value)
            {
                var reference = $"BT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                var orderResult = await CreateOrderAsync(checkout.Carts, userId, "Pending", "BT", reference);

                if (!orderResult.Success)
                {
                    return orderResult;
                }

                try
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var user = await _userManager.GetUserByIdAsync(userId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            var iban = string.IsNullOrWhiteSpace(_btSettings.Iban) ? "BG00UNCR70001512345678" : _btSettings.Iban;
                            var html = $@"<p>Thank you for your order.</p>
<p>Please make a bank transfer to the following account:</p>
<ul>
<li>Bank: <b>{_btSettings.BankName}</b></li>
<li>Beneficiary: <b>{_btSettings.Beneficiary}</b></li>
<li>IBAN: <b>{iban}</b></li>
<li>Amount: <b>{totalAmount:F2} EUR</b></li>
<li>Reference: <b>{reference}</b></li>
</ul>
<p>{_btSettings.AdditionalInfo}</p>
<p>Your order will be processed once we receive the payment.</p>";
                            await _emailService.SendEmailAsync(user.Email, "Bank Transfer Instructions", html);
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                var info = new BankTransferInfo
                {
                    Iban = _btSettings.Iban,
                    Beneficiary = _btSettings.Beneficiary,
                    BankName = _btSettings.BankName,
                    Reference = reference,
                    Amount = totalAmount,
                    AdditionalInfo = _btSettings.AdditionalInfo
                };

                return new ServiceResponse(true, "Bank Transfer selected. Please check your email for payment instructions.")
                {
                    Payload = info
                };
            }

            return new ServiceResponse(false, "Invalid payment method");
        }

        public async Task<ServiceResponse> CheckoutAsync(StorefrontCheckoutRequest checkout, string? userId)
        {
            ArgumentNullException.ThrowIfNull(checkout);

            if (_paymentHandlerResolver is null)
            {
                return new ServiceResponse(false, "Storefront checkout is not available in this runtime.");
            }

            var validationMessage = ValidateCheckout(checkout);
            if (validationMessage is not null)
            {
                return new ServiceResponse(false, validationMessage);
            }

            var storeId = await ResolveCurrentStoreIdAsync();
            if (!storeId.HasValue)
            {
                return new ServiceResponse(false, "Current store could not be resolved.");
            }

            var customerResult = await ResolveOrCreateCheckoutCustomerAsync(checkout, userId);
            if (!customerResult.Success)
            {
                return customerResult;
            }

            var methods = (await _paymentMethodService.GetPaymentMethodsAsync()).ToArray();
            var paymentMethodKey = NormalizeKey(checkout.PaymentMethodKey);
            var method = methods.FirstOrDefault(candidate =>
                string.Equals(candidate.Key, paymentMethodKey, StringComparison.OrdinalIgnoreCase));
            if (method is null)
            {
                return new ServiceResponse(false, "Payment method is not enabled for this store.");
            }

            var (products, totalAmount) = await this.GetCartTotalAmount(checkout.Carts);
            if (!products.Any() || totalAmount <= 0)
            {
                return new ServiceResponse(false, "We couldn't resolve the cart items for this order.");
            }

            var currencyCode = await ResolveCurrentCurrencyCodeAsync() ?? "USD";
            PaymentHandlerResult paymentResult;
            try
            {
                var handler = _paymentHandlerResolver.Resolve(paymentMethodKey);
                paymentResult = await handler.ProcessAsync(new PaymentHandlerContext(
                    storeId.Value,
                    Guid.Empty,
                    paymentMethodKey,
                    totalAmount,
                    currencyCode,
                    JsonSerializer.Serialize(new
                    {
                        handler = paymentMethodKey,
                        mode = "test",
                        processedAt = DateTime.UtcNow,
                    })));
            }
            catch (InvalidOperationException ex)
            {
                return new ServiceResponse(false, ex.Message);
            }

            if (!paymentResult.Success)
            {
                return new ServiceResponse(false, paymentResult.Message);
            }

            return await CreateOrderAsync(
                checkout.Carts,
                (string?)customerResult.Payload,
                OrderStatuses.Processing,
                "ORD",
                paymentMethodKey: paymentMethodKey,
                paymentStatus: paymentResult.PaymentStatus,
                paymentAt: paymentResult.PaymentAt,
                paymentMetadataJson: paymentResult.MetadataJson,
                checkout: checkout);
        }

        private async Task<ServiceResponse> CreateOrderAsync(
            IEnumerable<ProcessCart> carts,
            string? userId,
            string status,
            string referencePrefix,
            string? reference = null,
            string? paymentMethodKey = null,
            string? paymentStatus = null,
            DateTime? paymentAt = null,
            string? paymentMetadataJson = null,
            StorefrontCheckoutRequest? checkout = null)
        {
            var cartList = carts
                .Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
                .ToList();

            if (cartList.Count == 0)
            {
                return new ServiceResponse(false, "Your cart is empty.");
            }

            var lineResult = await ResolveCartLinesAsync(cartList);
            if (!lineResult.Success)
            {
                return new ServiceResponse(false, lineResult.ErrorMessage ?? "We couldn't resolve the cart items for this order.");
            }

            var lines = lineResult.Lines;
            var totalAmount = lines.Sum(line => line.Quantity * line.UnitPrice);

            if (lines.Count == 0 || totalAmount <= 0)
            {
                return new ServiceResponse(false, "We couldn't resolve the cart items for this order.");
            }

            async Task<ServiceResponse> CreateOrderAndDeductStockAsync()
            {
                var user = !string.IsNullOrWhiteSpace(userId)
                    ? await _userManager.GetUserByIdAsync(userId)
                    : null;
                var order = new Order
                {
                    UserId = userId ?? string.Empty,
                    OrderStatus = status,
                    PaymentStatus = paymentStatus ?? PaymentStatuses.Pending,
                    PaymentMethodKey = paymentMethodKey ?? PaymentMethodKeys.Cod,
                    PaymentAt = paymentAt,
                    PaymentMetadataJson = paymentMetadataJson,
                    Reference = reference ?? $"{referencePrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                    StoreId = await ResolveCurrentStoreIdAsync(),
                    CurrencyCode = await ResolveCurrentCurrencyCodeAsync(),
                    TotalAmount = totalAmount,
                    CustomerName = NormalizeNullable(checkout?.CustomerName) ?? user?.FullName,
                    CustomerEmail = NormalizeNullable(checkout?.CustomerEmail) ?? user?.Email,
                    ShippingFullName = NormalizeNullable(checkout?.ShippingAddress.FullName) ?? NormalizeNullable(checkout?.CustomerName) ?? user?.FullName ?? string.Empty,
                    ShippingEmail = NormalizeNullable(checkout?.ShippingAddress.Email) ?? NormalizeNullable(checkout?.CustomerEmail) ?? user?.Email ?? string.Empty,
                    ShippingPhone = NormalizeNullable(checkout?.ShippingAddress.Phone),
                    ShippingAddress1 = NormalizeNullable(checkout?.ShippingAddress.Address1) ?? string.Empty,
                    ShippingAddress2 = NormalizeNullable(checkout?.ShippingAddress.Address2),
                    ShippingCity = NormalizeNullable(checkout?.ShippingAddress.City) ?? string.Empty,
                    ShippingState = NormalizeNullable(checkout?.ShippingAddress.State),
                    ShippingPostalCode = NormalizeNullable(checkout?.ShippingAddress.PostalCode) ?? string.Empty,
                    ShippingCountryCode = NormalizeNullable(checkout?.ShippingAddress.CountryCode)?.ToUpperInvariant() ?? string.Empty,
                    ShippingStatus = ShippingStatuses.NotYetShipped,
                    UpdatedAt = DateTime.UtcNow,
                    Lines = lines
                        .Select(item => new OrderLine
                        {
                            ProductId = item.Product.Id,
                            ProductName = item.Product.Name,
                            Sku = item.Variant?.Sku ?? item.Product.Sku,
                            Image = item.Product.Image,
                            ProductVariantId = item.Variant?.Id,
                            VariantAttributesJson = item.VariantAttributesJson ?? item.Variant?.AttributesJson,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                        })
                        .ToList(),
                };

                if (order.Lines.Count == 0)
                {
                    return new ServiceResponse(false, "We couldn't resolve the cart items for this order.");
                }

                await _orderRepository.CreateAsync(order);

                var stockResult = await DeductStockAsync(lines);
                if (!stockResult.Success)
                {
                    return stockResult;
                }

                return new ServiceResponse(true, "Order saved successfully")
                {
                    Id = order.Id,
                    Payload = new StorefrontCheckoutResult(
                        order.Id,
                        order.Reference,
                        order.OrderStatus,
                        order.PaymentStatus,
                        order.PaymentMethodKey,
                        order.CreatedOn)
                };
            }

            if (_transactionManager is not null)
            {
                return await _transactionManager.ExecuteInTransactionAsync(CreateOrderAndDeductStockAsync);
            }

            return await CreateOrderAndDeductStockAsync();
        }

        private async Task<ServiceResponse> ResolveOrCreateCheckoutCustomerAsync(
            StorefrontCheckoutRequest checkout,
            string? userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var authenticatedUser = await _userManager.GetUserByIdAsync(userId);
                return authenticatedUser is null
                    ? new ServiceResponse(false, "Authenticated customer was not found.")
                    : new ServiceResponse(true, "Customer resolved.")
                    {
                        Payload = authenticatedUser.Id,
                    };
            }

            var email = NormalizeNullable(checkout.CustomerEmail);
            if (email is null)
            {
                return new ServiceResponse(false, "Customer email is required.");
            }

            var existing = await _userManager.GetUserByEmailAsync(email);
            if (existing is not null)
            {
                return new ServiceResponse(true, "Customer resolved.")
                {
                    Payload = existing.Id,
                };
            }

            var password = $"Checkout!{Guid.NewGuid():N}aA1";
            var user = new AppUser
            {
                FullName = NormalizeNullable(checkout.CustomerName) ?? email,
                Email = email,
                UserName = email,
                PasswordHash = password,
                EmailConfirmed = true,
            };

            var created = await _userManager.CreateUserAsync(user);
            if (!created)
            {
                return new ServiceResponse(false, "Customer could not be created.");
            }

            var currentUser = await _userManager.GetUserByEmailAsync(email);
            if (currentUser is null)
            {
                return new ServiceResponse(false, "Customer could not be loaded after creation.");
            }

            if (_roleManager is not null)
            {
                await _roleManager.AddUserToRoleAsync(currentUser, "User");
            }

            return new ServiceResponse(true, "Customer created.")
            {
                Payload = currentUser.Id,
            };
        }

        public async Task<IEnumerable<GetOrderItem>> GetOrderItemsAsync()
        {
            var history = (await _cart.GetAllCheckoutHistory())?.ToList();

            if (history == null)
            {
                return [];
            }

            var groupByCustomerId = history.GroupBy(x => x.UserId).ToList();
            var products = await _productReadRepository.GetProductsByIdsAsync(history.Select(item => item.ProductId));
            var orderItems = new List<GetOrderItem>();

            foreach (var customerId in groupByCustomerId)
            {
                if (string.IsNullOrWhiteSpace(customerId.Key))
                {
                    continue;
                }

                var customerDetails = await _userManager.GetUserByIdAsync(customerId.Key);

                foreach (var item in customerId)
                {
                    products.TryGetValue(item.ProductId, out var product);

                    orderItems.Add(new GetOrderItem
                    {
                        CustomerName = customerDetails?.UserName,
                        CustomerEmail = customerDetails?.Email,
                        ProductName = product?.Name,
                        AmountPayed = item.Quantity * (product?.Price ?? 0),
                        QuantityOrdered = item.Quantity,
                        DatePurchased = item.CreatedOn,
                        TrackingNumber = null,
                        TrackingUrl = null,
                        ShippingStatus = "PendingShipment"
                    });
                }
            }

            return orderItems;
        }

        private async Task<(IEnumerable<Product>, decimal)> GetCartTotalAmount(IEnumerable<ProcessCart> carts)
        {
            var result = await ResolveCartLinesAsync(carts);
            return result.Success
                ? (result.Lines.Select(line => line.Product).DistinctBy(product => product.Id).ToArray(), result.Lines.Sum(line => line.Quantity * line.UnitPrice))
                : ([], 0);
        }

        private async Task<Guid?> ResolveCurrentStoreIdAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreIdAsync();
            return result.Success ? result.Value : null;
        }

        private async Task<string?> ResolveCurrentCurrencyCodeAsync()
        {
            if (_storeContext is null)
            {
                return null;
            }

            var result = await _storeContext.GetCurrentStoreAsync();
            return result.Success ? result.Value?.DefaultCurrencyCode : null;
        }

        private async Task<CartLineResolution> ResolveCartLinesAsync(IEnumerable<ProcessCart> carts)
        {
            var cartList = carts
                .Where(item => item.ProductId != Guid.Empty && item.Quantity > 0)
                .ToList();

            if (cartList.Count == 0)
            {
                return CartLineResolution.Failure("Your cart is empty.");
            }

            var currentStoreId = await ResolveCurrentStoreIdAsync();
            var productLookup = await _productReadRepository.GetProductsByIdsAsync(cartList.Select(item => item.ProductId));
            var lines = new List<CartLineContext>();

            foreach (var item in cartList)
            {
                if (!productLookup.TryGetValue(item.ProductId, out var product)
                    || product.ArchivedAt != null
                    || (currentStoreId.HasValue && product.StoreId != currentStoreId.Value))
                {
                    return CartLineResolution.Failure("A cart product could not be found for this store.");
                }

                var selectedAttributes = NormalizeSelectedAttributes(product, item.SelectedAttributes);
                if (!selectedAttributes.Success)
                {
                    return CartLineResolution.Failure(selectedAttributes.ErrorMessage);
                }

                var variant = ResolveVariant(product, item.ProductVariantId);
                if (variant.Failed)
                {
                    return CartLineResolution.Failure(variant.ErrorMessage);
                }

                var selectedVariant = variant.Value;
                var availableStock = selectedVariant?.Stock ?? product.Quantity;
                if (availableStock < item.Quantity)
                {
                    return CartLineResolution.Failure("One or more cart items are out of stock.");
                }

                lines.Add(new CartLineContext(
                    product,
                    selectedVariant,
                    selectedAttributes.AttributesJson,
                    item.Quantity,
                    selectedVariant?.Price ?? product.Price));
            }

            return CartLineResolution.Ok(lines);
        }

        private static VariantResolution ResolveVariant(Product product, Guid? productVariantId)
        {
            if (string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                return VariantResolution.Success(null);
            }

            var allVariants = product.Variants.ToArray();
            var variants = allVariants.Where(item => item.IsActive).ToArray();
            if (productVariantId.HasValue)
            {
                var variant = allVariants.FirstOrDefault(item => item.Id == productVariantId.Value);
                if (variant is null)
                {
                    return VariantResolution.Failure("Selected product variant was not found.");
                }

                return variant.IsActive
                    ? VariantResolution.Success(variant)
                    : VariantResolution.Failure("Selected product variant is not available.");
            }

            if (allVariants.Length == 0)
            {
                return VariantResolution.Success(null);
            }

            if (variants.Length == 0)
            {
                return VariantResolution.Failure("Product variants are not available.");
            }

            var defaultVariants = variants.Where(item => item.IsDefault).ToArray();
            if (defaultVariants.Length == 1)
            {
                return VariantResolution.Success(defaultVariants[0]);
            }

            return VariantResolution.Failure("Please select a product variant before checkout.");
        }

        private async Task<ServiceResponse> DeductStockAsync(IReadOnlyList<CartLineContext> lines)
        {
            if (_productRepository is null || _variantRepository is null)
            {
                return new ServiceResponse(true, "Stock deduction skipped because writable catalog repositories are not available.");
            }

            foreach (var line in lines)
            {
                if (line.Variant is not null)
                {
                    var variant = await _variantRepository.GetByIdAsync(line.Variant.Id);
                    if (variant is null || !variant.IsActive || variant.Stock < line.Quantity)
                    {
                        return new ServiceResponse(false, "One or more variants are out of stock.");
                    }

                    variant.Stock -= line.Quantity;
                    await _variantRepository.UpdateAsync(variant);
                    continue;
                }

                var product = await _productRepository.GetByIdAsync(line.Product.Id);
                if (product is null || product.Quantity < line.Quantity)
                {
                    return new ServiceResponse(false, "One or more products are out of stock.");
                }

                product.Quantity -= line.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
                await _productRepository.UpdateAsync(product);
            }

            return new ServiceResponse(true, "Stock deducted successfully.");
        }

        public async Task<IEnumerable<GetOrderItem>> GetCheckoutHistoryByUserId(string userId)
        {
            var history = (await _cart.GetCheckoutHistoryByUserId(userId))?.ToList();

            if (history == null || !history.Any())
            {
                return new List<GetOrderItem>();
            }

            var products = await _productReadRepository.GetProductsByIdsAsync(history.Select(item => item.ProductId));

            var orderItems = new List<GetOrderItem>();

            var customerDetails = await _userManager.GetUserByIdAsync(userId);

            foreach (var item in history)
            {
                products.TryGetValue(item.ProductId, out var product);

                orderItems.Add(new GetOrderItem
                                   {
                                       CustomerName = customerDetails?.UserName,
                                       CustomerEmail = customerDetails?.Email,
                                       ProductName = product?.Name,
                                       AmountPayed = item.Quantity * (product?.Price ?? 0),
                                       QuantityOrdered = item.Quantity,
                                       DatePurchased = item.CreatedOn,
                                       TrackingNumber = null,
                                       TrackingUrl = null,
                                       ShippingStatus = "PendingShipment"
                                   });
            }

            return orderItems;
        }

        private static SelectedAttributesResolution NormalizeSelectedAttributes(
            Product product,
            IReadOnlyList<SelectedAttributeDto>? selectedAttributes)
        {
            if (!string.Equals(product.ProductType, ProductTypes.CustomVariations, StringComparison.OrdinalIgnoreCase))
            {
                return SelectedAttributesResolution.Ok(null);
            }

            var attributes = (selectedAttributes ?? [])
                .Select(attribute => new SelectedAttributeDto(
                    attribute.Name?.Trim() ?? string.Empty,
                    attribute.Value?.Trim() ?? string.Empty))
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute.Name)
                    || !string.IsNullOrWhiteSpace(attribute.Value))
                .ToArray();

            if (attributes.Length > MaxSelectedAttributes)
            {
                return SelectedAttributesResolution.Failure("At most 5 selected attributes are allowed.");
            }

            foreach (var attribute in attributes)
            {
                if (string.IsNullOrWhiteSpace(attribute.Name))
                {
                    return SelectedAttributesResolution.Failure("Selected attribute name is required.");
                }

                if (string.IsNullOrWhiteSpace(attribute.Value))
                {
                    return SelectedAttributesResolution.Failure("Selected attribute value is required.");
                }

                if (attribute.Name.Length > MaxSelectedAttributeNameLength)
                {
                    return SelectedAttributesResolution.Failure("Selected attribute name must be 100 characters or fewer.");
                }

                if (attribute.Value.Length > MaxSelectedAttributeValueLength)
                {
                    return SelectedAttributesResolution.Failure("Selected attribute value must be 200 characters or fewer.");
                }
            }

            var normalized = attributes
                .GroupBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            return SelectedAttributesResolution.Ok(normalized.Length == 0
                ? null
                : JsonSerializer.Serialize(normalized, SerializerOptions));
        }

        private static string? ValidateCheckout(StorefrontCheckoutRequest checkout)
        {
            if (string.IsNullOrWhiteSpace(checkout.CustomerEmail) || !IsEmail(checkout.CustomerEmail))
            {
                return "A valid customer email is required.";
            }

            if (string.IsNullOrWhiteSpace(checkout.CustomerName) || checkout.CustomerName.Trim().Length > 256)
            {
                return "Customer name is required and must be 256 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(checkout.PaymentMethodKey))
            {
                return "Payment method is required.";
            }

            if (checkout.Carts is null || !checkout.Carts.Any())
            {
                return "Your cart is empty.";
            }

            var shipping = checkout.ShippingAddress;
            if (shipping is null)
            {
                return "Shipping address is required.";
            }

            if (string.IsNullOrWhiteSpace(shipping.FullName) || shipping.FullName.Trim().Length > 256)
            {
                return "Shipping full name is required and must be 256 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(shipping.Email) || !IsEmail(shipping.Email))
            {
                return "A valid shipping email is required.";
            }

            if (string.IsNullOrWhiteSpace(shipping.Address1) || shipping.Address1.Trim().Length > 400)
            {
                return "Shipping address line 1 is required and must be 400 characters or fewer.";
            }

            if (!string.IsNullOrWhiteSpace(shipping.Address2) && shipping.Address2.Trim().Length > 400)
            {
                return "Shipping address line 2 must be 400 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(shipping.City) || shipping.City.Trim().Length > 160)
            {
                return "Shipping city is required and must be 160 characters or fewer.";
            }

            if (!string.IsNullOrWhiteSpace(shipping.State) && shipping.State.Trim().Length > 160)
            {
                return "Shipping state must be 160 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(shipping.PostalCode) || shipping.PostalCode.Trim().Length > 64)
            {
                return "Shipping postal code is required and must be 64 characters or fewer.";
            }

            if (string.IsNullOrWhiteSpace(shipping.CountryCode) || shipping.CountryCode.Trim().Length != 2)
            {
                return "Shipping country code must be a two-letter code.";
            }

            if (!string.IsNullOrWhiteSpace(shipping.Phone) && shipping.Phone.Trim().Length > 64)
            {
                return "Shipping phone must be 64 characters or fewer.";
            }

            return null;
        }

        private static bool IsEmail(string email)
        {
            try
            {
                _ = new MailAddress(email.Trim());
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string NormalizeKey(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private sealed record CartLineContext(Product Product, ProductVariant? Variant, string? VariantAttributesJson, int Quantity, decimal UnitPrice);

        private sealed record CartLineResolution(bool Success, IReadOnlyList<CartLineContext> Lines, string? ErrorMessage)
        {
            public static CartLineResolution Ok(IReadOnlyList<CartLineContext> lines) => new(true, lines, null);

            public static CartLineResolution Failure(string? errorMessage) => new(false, [], errorMessage);
        }

        private sealed record VariantResolution(bool Failed, ProductVariant? Value, string? ErrorMessage)
        {
            public static VariantResolution Success(ProductVariant? variant) => new(false, variant, null);

            public static VariantResolution Failure(string? errorMessage) => new(true, null, errorMessage);
        }

        private sealed record SelectedAttributesResolution(bool Success, string? AttributesJson, string? ErrorMessage)
        {
            public static SelectedAttributesResolution Ok(string? attributesJson) => new(true, attributesJson, null);

            public static SelectedAttributesResolution Failure(string? errorMessage) => new(false, null, errorMessage);
        }
    }
}
