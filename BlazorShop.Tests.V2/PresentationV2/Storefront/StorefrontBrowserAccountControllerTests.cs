namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Text;
    using System.Text.Json;

    using BlazorShop.Storefront.Browser;
    using BlazorShop.Storefront.Browser.Account;
    using BlazorShop.Storefront.Components.Browser;
    using BlazorShop.Storefront.Components.Headless.Account;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public sealed class StorefrontBrowserAccountControllerTests
    {
        [Fact]
        public async Task HydrateProfileAsync_LoadsProfileAndCopiesForm()
        {
            var profile = CreateProfile("Taylor Store");
            var handler = new QueueingHandler(profile);
            var controller = CreateController(handler);
            controller.InitializeProfile(null, null, null, StorefrontFeatureDataMode.BrowserFetch, ProfileActions);

            var changed = await controller.HydrateProfileAsync();

            Assert.True(changed);
            Assert.Equal(profile.CustomerPublicId, controller.State.Profile?.CustomerPublicId);
            Assert.Equal("Taylor Store", controller.State.ProfileForm.FullName);
            Assert.Equal("taylor@example.test", controller.State.ProfileForm.Email);
            Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/account/profile", handler.Requests.Single().RequestUri?.ToString());
        }

        [Fact]
        public async Task SaveProfileAsync_SendsUpdateRequestAndAppliesSuccess()
        {
            var handler = new QueueingHandler(CreateProfile("Updated Buyer"));
            var controller = CreateController(handler);
            controller.InitializeProfile(CreateProfile("Original Buyer"), null, null, StorefrontFeatureDataMode.InitialSnapshot, ProfileActions);
            controller.State.ProfileForm.FullName = "Updated Buyer";
            controller.State.ProfileForm.Email = "updated@example.test";
            controller.State.ProfileForm.PhoneNumber = "+15550101";

            var changed = await controller.SaveProfileAsync();

            Assert.True(changed);
            Assert.False(controller.State.ProfileSaving);
            Assert.Equal("Profile updated.", controller.State.ProfileSuccess);
            Assert.Null(controller.State.ProfileError);
            Assert.Equal(HttpMethod.Put, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/account/profile", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains("\"fullName\":\"Updated Buyer\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Contains("\"email\":\"updated@example.test\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.True(handler.Requests.Single().Headers.Contains("X-CSRF-TOKEN"));
        }

        [Fact]
        public async Task SaveProfileAsync_MapsApiErrorToProfileError()
        {
            var error = JsonSerializer.Serialize(
                new StorefrontLocalApiErrorResponse("Profile email is invalid.", "account.profile", null, null, false, 422),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new QueueingHandler(new StringContent(error, Encoding.UTF8, "application/json"), HttpStatusCode.UnprocessableEntity);
            var controller = CreateController(handler);
            controller.InitializeProfile(CreateProfile("Original Buyer"), null, null, StorefrontFeatureDataMode.InitialSnapshot, ProfileActions);

            var changed = await controller.SaveProfileAsync();

            Assert.True(changed);
            Assert.False(controller.State.ProfileSaving);
            Assert.Equal("Profile email is invalid.", controller.State.ProfileError);
            Assert.Null(controller.State.ProfileSuccess);
        }

        [Fact]
        public async Task HydrateAddressesAsync_LoadsAddressesAndBuildsForms()
        {
            var addressId = Guid.NewGuid();
            var handler = new QueueingHandler(new List<StorefrontBrowserCustomerAddress> { CreateAddress(addressId, "Taylor Store") });
            var controller = CreateController(handler);
            controller.InitializeAddresses([], null, null, StorefrontFeatureDataMode.BrowserFetch, AddressActions);

            var changed = await controller.HydrateAddressesAsync();

            Assert.True(changed);
            Assert.Single(controller.State.Addresses);
            Assert.True(controller.State.AddressForms.ContainsKey(addressId));
            Assert.Equal("Taylor Store", controller.State.AddressForms[addressId].FullName);
            Assert.Equal(HttpMethod.Get, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/account/addresses", handler.Requests.Single().RequestUri?.ToString());
        }

        [Fact]
        public async Task CreateAddressAsync_SendsRequestRefreshesAddressesAndClearsForm()
        {
            var addressId = Guid.NewGuid();
            var handler = new QueueingHandler(
                CreateAddress(addressId, "New Address"),
                new List<StorefrontBrowserCustomerAddress> { CreateAddress(addressId, "New Address") });
            var controller = CreateController(handler);
            controller.InitializeAddresses([], null, null, StorefrontFeatureDataMode.InitialSnapshot, AddressActions);
            controller.State.NewAddress.FullName = "New Address";
            controller.State.NewAddress.Address1 = "100 Market St";
            controller.State.NewAddress.City = "San Francisco";
            controller.State.NewAddress.PostalCode = "94105";
            controller.State.NewAddress.CountryCode = "US";
            controller.State.NewAddress.IsDefaultShipping = true;

            var changed = await controller.CreateAddressAsync();

            Assert.True(changed);
            Assert.Equal("Address book updated.", controller.State.AddressSuccess);
            Assert.Null(controller.State.AddressError);
            Assert.Equal(string.Empty, controller.State.NewAddress.FullName);
            Assert.Equal(2, handler.Requests.Count);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.Equal("https://storefront.example/api/account/addresses", handler.Requests[0].RequestUri?.ToString());
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
            Assert.Contains("\"fullName\":\"New Address\"", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.Contains("\"isDefaultShipping\":true", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.Single(controller.State.Addresses);
        }

        [Fact]
        public async Task UpdateAddressAsync_SendsAddressRequestAndRefreshesAddresses()
        {
            var addressId = Guid.NewGuid();
            var handler = new QueueingHandler(
                CreateAddress(addressId, "Updated Address"),
                new List<StorefrontBrowserCustomerAddress> { CreateAddress(addressId, "Updated Address") });
            var controller = CreateController(handler);
            controller.InitializeAddresses([CreateAddress(addressId, "Old Address")], null, null, StorefrontFeatureDataMode.InitialSnapshot, AddressActions);
            controller.State.AddressForms[addressId].City = "Seattle";

            var changed = await controller.UpdateAddressAsync(addressId);

            Assert.True(changed);
            Assert.Equal(HttpMethod.Put, handler.Requests[0].Method);
            Assert.Equal($"https://storefront.example/api/account/addresses/{addressId:D}", handler.Requests[0].RequestUri?.ToString());
            Assert.Contains("\"city\":\"Seattle\"", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        }

        [Fact]
        public async Task DeleteAddressAsync_UsesDeleteRouteAndRefreshesAddresses()
        {
            var addressId = Guid.NewGuid();
            var handler = new QueueingHandler(
                new StorefrontBrowserAccountCommandResult(true, "Deleted."),
                Array.Empty<StorefrontBrowserCustomerAddress>());
            var controller = CreateController(handler);
            controller.InitializeAddresses([CreateAddress(addressId, "Old Address")], null, null, StorefrontFeatureDataMode.InitialSnapshot, AddressActions);

            var changed = await controller.DeleteAddressAsync(addressId);

            Assert.True(changed);
            Assert.Equal(HttpMethod.Delete, handler.Requests[0].Method);
            Assert.Equal($"https://storefront.example/api/account/addresses/{addressId:D}", handler.Requests[0].RequestUri?.ToString());
            Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
            Assert.Empty(controller.State.Addresses);
            Assert.Empty(controller.State.AddressForms);
        }

        [Fact]
        public async Task SetDefaultAddressAsync_UsesDefaultRouteAndRefreshesAddresses()
        {
            var addressId = Guid.NewGuid();
            var handler = new QueueingHandler(
                CreateAddress(addressId, "Default Address"),
                new List<StorefrontBrowserCustomerAddress> { CreateAddress(addressId, "Default Address", isDefaultShipping: true) });
            var controller = CreateController(handler);
            controller.InitializeAddresses([CreateAddress(addressId, "Default Address")], null, null, StorefrontFeatureDataMode.InitialSnapshot, AddressActions);

            var changed = await controller.SetDefaultAddressAsync(addressId, shipping: true);

            Assert.True(changed);
            Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
            Assert.Equal($"https://storefront.example/api/account/addresses/{addressId:D}/default-shipping", handler.Requests[0].RequestUri?.ToString());
            Assert.Contains("{}", handler.RequestBodies[0], StringComparison.Ordinal);
            Assert.True(controller.State.Addresses.Single().IsDefaultShipping);
        }

        [Fact]
        public async Task HydrateOrdersAsync_LoadsPagedOrders()
        {
            var orders = new StorefrontBrowserAccountOrderList(
                [new StorefrontBrowserAccountOrderListItem("ORDER-100", "Today", "Pending", "Pending", "Pending", "$25.00", 2)],
                PageNumber: 2,
                PageSize: 10,
                TotalCount: 11,
                TotalPages: 2);
            var handler = new QueueingHandler(orders);
            var controller = CreateController(handler);
            controller.InitializeOrders(new StorefrontBrowserAccountOrderList([], 1, 10, 0, 0), null, StorefrontFeatureDataMode.BrowserFetch, OrderActions, pageNumber: 2);

            var changed = await controller.HydrateOrdersAsync();

            Assert.True(changed);
            Assert.Equal("ORDER-100", controller.State.Orders.Items.Single().Reference);
            Assert.Equal("https://storefront.example/api/account/orders?page=2", handler.Requests.Single().RequestUri?.ToString());
        }

        [Fact]
        public async Task HydrateOrderDetailAsync_LoadsDetailAndReceiptRoutes()
        {
            var detail = CreateOrderDetail("ORDER 100", receiptMode: false);
            var receipt = CreateOrderDetail("ORDER 100", receiptMode: true);
            var detailHandler = new QueueingHandler(detail);
            var detailController = CreateController(detailHandler);
            detailController.InitializeOrderDetail(null, null, StorefrontFeatureDataMode.BrowserFetch, OrderActions, "ORDER 100", receiptMode: false);

            var detailChanged = await detailController.HydrateOrderDetailAsync();

            Assert.True(detailChanged);
            Assert.False(detailController.State.OrderDetail?.ReceiptMode);
            Assert.Equal("https://storefront.example/api/account/orders/ORDER 100", detailHandler.Requests.Single().RequestUri?.ToString());

            var receiptHandler = new QueueingHandler(receipt);
            var receiptController = CreateController(receiptHandler);
            receiptController.InitializeOrderDetail(null, null, StorefrontFeatureDataMode.BrowserFetch, OrderActions, "ORDER 100", receiptMode: true);

            var receiptChanged = await receiptController.HydrateOrderDetailAsync();

            Assert.True(receiptChanged);
            Assert.True(receiptController.State.OrderDetail?.ReceiptMode);
            Assert.Equal("https://storefront.example/api/account/orders/ORDER 100/receipt", receiptHandler.Requests.Single().RequestUri?.ToString());
        }

        [Fact]
        public async Task ChangePasswordAsync_RejectsMismatchWithoutApiCall()
        {
            var handler = new QueueingHandler(new StorefrontBrowserAccountCommandResult(true, "Changed."));
            var controller = CreateController(handler);
            controller.InitializePassword(null, null, PasswordActions);
            controller.State.PasswordForm.CurrentPassword = "old-password";
            controller.State.PasswordForm.NewPassword = "new-password";
            controller.State.PasswordForm.ConfirmPassword = "different-password";

            var changed = await controller.ChangePasswordAsync();

            Assert.True(changed);
            Assert.Empty(handler.Requests);
            Assert.Equal("Passwords do not match.", controller.State.PasswordError);
            Assert.False(controller.State.PasswordSaving);
        }

        [Fact]
        public async Task ChangePasswordAsync_SendsRequestClearsFormAndAppliesSuccess()
        {
            var handler = new QueueingHandler(new StorefrontBrowserAccountCommandResult(true, "Password changed."));
            var controller = CreateController(handler);
            controller.InitializePassword(null, null, PasswordActions);
            controller.State.PasswordForm.CurrentPassword = "old-password";
            controller.State.PasswordForm.NewPassword = "new-password";
            controller.State.PasswordForm.ConfirmPassword = "new-password";

            var changed = await controller.ChangePasswordAsync();

            Assert.True(changed);
            Assert.Equal(HttpMethod.Post, handler.Requests.Single().Method);
            Assert.Equal("https://storefront.example/api/account/change-password", handler.Requests.Single().RequestUri?.ToString());
            Assert.Contains("\"currentPassword\":\"old-password\"", handler.RequestBodies.Single(), StringComparison.Ordinal);
            Assert.Null(controller.State.PasswordForm.CurrentPassword);
            Assert.Null(controller.State.PasswordForm.NewPassword);
            Assert.Null(controller.State.PasswordForm.ConfirmPassword);
            Assert.Equal("Password changed.", controller.State.PasswordSuccess);
            Assert.Null(controller.State.PasswordError);
        }

        private static readonly StorefrontAccountProfileActionDescriptor ProfileActions = new(
            "/account/profile",
            "/api/account/profile",
            "/api/account/profile");

        private static readonly StorefrontAccountPasswordActionDescriptor PasswordActions = new(
            "/account/change-password",
            "/api/account/change-password");

        private static readonly StorefrontAccountAddressActionDescriptor AddressActions = new(
            "/account/addresses",
            "/api/account/addresses",
            "/api/account/addresses",
            "/api/account/addresses/{addressId}",
            "/api/account/addresses/{addressId}",
            "/api/account/addresses/{addressId}/default-shipping",
            "/api/account/addresses/{addressId}/default-billing");

        private static readonly StorefrontAccountOrderActionDescriptor OrderActions = new(
            "/api/account/orders?page={pageNumber}",
            "/api/account/orders/{orderReference}",
            "/api/account/orders/{orderReference}/receipt",
            "/account/orders/{orderReference}");

        private static StorefrontBrowserAccountController CreateController(QueueingHandler handler)
        {
            var services = new ServiceCollection();
            services.AddSingleton(_ => new HttpClient(handler)
            {
                BaseAddress = new Uri("https://storefront.example/"),
            });
            services.AddSingleton<IStorefrontAntiforgeryTokenReader>(new StaticTokenReader());
            services.AddSingleton<StorefrontLocalApiClient>();
            var provider = services.BuildServiceProvider();
            return new StorefrontBrowserAccountController(provider);
        }

        private static StorefrontBrowserCustomerProfile CreateProfile(string fullName)
        {
            return new StorefrontBrowserCustomerProfile(
                Guid.NewGuid(),
                "taylor@example.test",
                fullName,
                "Taylor",
                "Store",
                "BlazorShop",
                "+15550000",
                "en-US",
                "USD",
                "Today",
                null);
        }

        private static StorefrontBrowserCustomerAddress CreateAddress(
            Guid publicId,
            string fullName,
            bool isDefaultShipping = false)
        {
            return new StorefrontBrowserCustomerAddress(
                publicId,
                fullName,
                "BlazorShop",
                "taylor@example.test",
                "+15550000",
                "100 Market St",
                null,
                "San Francisco",
                "94105",
                "US",
                "CA",
                "California",
                isDefaultShipping,
                IsDefaultBilling: false);
        }

        private static StorefrontBrowserAccountOrderDetail CreateOrderDetail(string reference, bool receiptMode)
        {
            return new StorefrontBrowserAccountOrderDetail(
                reference,
                receiptMode,
                "Today",
                "Pending",
                "Pending",
                "Pending",
                "$25.00",
                new StorefrontBrowserOrderAddress("Taylor Store", "taylor@example.test", "+15550000", "100 Market St", null, "San Francisco", "CA", "94105", "US"),
                null,
                [new StorefrontBrowserAccountOrderLine("Canvas Tote", "TOTE", 1, "$25.00")],
                new StorefrontBrowserOrderTotals("$20.00", "$5.00", "$0.00", "$0.00", "$25.00"));
        }

        private sealed class StaticTokenReader : IStorefrontAntiforgeryTokenReader
        {
            public ValueTask<StorefrontAntiforgeryToken?> ReadAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<StorefrontAntiforgeryToken?>(new StorefrontAntiforgeryToken("X-CSRF-TOKEN", "csrf-token"));
            }
        }

        private sealed class QueueingHandler : HttpMessageHandler
        {
            private readonly Queue<(HttpContent Content, HttpStatusCode StatusCode)> _responses = new();

            public QueueingHandler(params object[] responses)
            {
                foreach (var response in responses)
                {
                    _responses.Enqueue((JsonContent(response), HttpStatusCode.OK));
                }
            }

            public QueueingHandler(HttpContent content, HttpStatusCode statusCode)
            {
                _responses.Enqueue((content, statusCode));
            }

            public List<HttpRequestMessage> Requests { get; } = [];

            public List<string> RequestBodies { get; } = [];

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                RequestBodies.Add(request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken));
                var response = _responses.Dequeue();
                return new HttpResponseMessage(response.StatusCode)
                {
                    Content = response.Content,
                    RequestMessage = request,
                };
            }

            private static StringContent JsonContent(object value)
            {
                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return new StringContent(json, Encoding.UTF8, "application/json");
            }
        }
    }
}
