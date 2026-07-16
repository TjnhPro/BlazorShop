extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.UserIdentity;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Services;
    using StorefrontV2::BlazorShop.Storefront.Services.Contracts;

    using StorefrontV2Program = StorefrontV2::Program;

    public sealed class StorefrontV2HostSmokeTests : IClassFixture<WebApplicationFactory<StorefrontV2Program>>
    {
        private readonly WebApplicationFactory<StorefrontV2Program> _factory;

        public StorefrontV2HostSmokeTests(WebApplicationFactory<StorefrontV2Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Checkout_WhenCartIsEmpty_ShowsEmptyCartState()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                },
                allowAutoRedirect: false);

            using var response = await client.GetAsync(StorefrontRoutes.Checkout);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Your cart is empty", content, StringComparison.Ordinal);
            Assert.Contains("Shop New Releases", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task SignIn_ReturnsStorefrontLoginPage()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
            });

            using var response = await client.GetAsync(StorefrontRoutes.SignIn);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Customer account", content, StringComparison.Ordinal);
            Assert.Contains("method=\"post\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SignIn_PostSuccess_SetsRefreshCookieAndRedirectsToSafeReturnUrl()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                        StorefrontAuthResult<StorefrontTokenResponse>.Succeeded(
                            new StorefrontTokenResponse("jwt-token", DateTime.UtcNow.AddHours(2)),
                            "Signed in.",
                            ["__Host-blazorshop-refresh=abc; Path=/; Secure; HttpOnly"])));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateSignInPost(token, cookieHeader, "/my-cart");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/my-cart", response.Headers.Location?.ToString());
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("__Host-blazorshop-refresh=abc", StringComparison.Ordinal));
        }

        [Fact]
        public async Task SignIn_PostFailure_RedirectsWithApiMessage()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                        StorefrontAuthResult<StorefrontTokenResponse>.Failed("Invalid credentials.")));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateSignInPost(token, cookieHeader, "/checkout");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/signin?returnUrl=%2Fcheckout&error=Invalid%20credentials.", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task SignIn_PostSuccess_RejectsUnsafeReturnUrl()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                        StorefrontAuthResult<StorefrontTokenResponse>.Succeeded(
                            new StorefrontTokenResponse("jwt-token", DateTime.UtcNow.AddHours(2)),
                            "Signed in.",
                            [])));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateSignInPost(token, cookieHeader, "https://evil.example/");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location?.ToString());
        }

        [Theory]
        [InlineData("/signin")]
        [InlineData("/register")]
        [InlineData("/logout")]
        [InlineData("/currency")]
        [InlineData("/checkout")]
        public async Task StorefrontFormPost_WithoutAntiforgeryToken_ReturnsBadRequest(string path)
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontCurrentStoreProvider>();
                    services.AddScoped<IStorefrontCurrentStoreProvider>(_ => new StubCurrentStoreProvider(
                        StorefrontCurrentStoreResolution.Succeeded(CreateActiveCurrentStore())));
                },
                allowAutoRedirect: false);

            using var response = await client.PostAsync(
                path,
                new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("ReturnUrl", StorefrontRoutes.Home),
                ]));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsStorefrontRegisterPage()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Register);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Create account", content, StringComparison.Ordinal);
            Assert.Contains("method=\"post\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Register_PostPasswordMismatch_RedirectsWithMessageWithoutCallingApi()
        {
            var authClient = new StubStorefrontAuthClient(registerResult: StorefrontAuthResult<object>.Succeeded(new object(), "Created.", []));
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Register);
            using var request = CreateRegisterPost(token, cookieHeader, "/checkout", confirmPassword: "Different123!");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/register?returnUrl=%2Fcheckout&error=Passwords%20do%20not%20match.", response.Headers.Location?.ToString());
            Assert.Equal(0, authClient.RegisterCalls);
        }

        [Fact]
        public async Task Register_PostFailure_RedirectsWithApiMessage()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                        registerResult: StorefrontAuthResult<object>.Failed("Email already exists.")));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Register);
            using var request = CreateRegisterPost(token, cookieHeader, "/checkout");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/register?returnUrl=%2Fcheckout&error=Email%20already%20exists.", response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Register_PostSuccess_RedirectsToSignInWithRegisteredState()
        {
            var authClient = new StubStorefrontAuthClient(registerResult: StorefrontAuthResult<object>.Succeeded(new object(), "Created.", []));
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Register);
            using var request = CreateRegisterPost(token, cookieHeader, "/checkout");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/signin?returnUrl=%2Fcheckout&registered=1", response.Headers.Location?.ToString());
            Assert.Equal(1, authClient.RegisterCalls);
            Assert.Equal("Customer One", authClient.LastRegisteredUser?.FullName);
            Assert.Equal("customer@example.test", authClient.LastRegisteredUser?.Email);
        }

        [Fact]
        public async Task AccountMenu_WhenAnonymous_UsesLocalAuthLinks()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new ServiceUnavailableHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Checkout);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("href=\"/signin\"", content, StringComparison.Ordinal);
            Assert.Contains("href=\"/register\"", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AccountMenu_WhenAuthenticated_ShowsLocalLogoutWithoutLegacyLinks()
        {
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test");
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new ServiceUnavailableHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Checkout);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Customer One", content, StringComparison.Ordinal);
            Assert.Contains("action=\"/logout\"", content, StringComparison.Ordinal);
            Assert.Contains("Sign out", content, StringComparison.Ordinal);
            Assert.DoesNotContain("My Account", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Admin Panel", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CurrencyPreference_PostSuccess_SetsPreferenceCookieAndRedirectsToSafeReturnUrl()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(new CurrencyPreferenceHandler())
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateCurrencyPreferencePost(token, cookieHeader, "EUR", StorefrontRoutes.NewReleases);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(StorefrontRoutes.NewReleases, response.Headers.Location?.ToString());
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("bs-currency=EUR", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Logout_PostCallsCommerceNodeAndCopiesExpiredCookie()
        {
            var authClient = new StubStorefrontAuthClient(
                logoutResult: StorefrontAuthResult<object>.Succeeded(
                    null,
                    "Signed out.",
                    ["__Host-blazorshop-refresh=; expires=Thu, 01 Jan 1970 00:00:00 GMT; Path=/; Secure; HttpOnly"]));
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test");

            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(new ServiceUnavailableHandler())
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Terms);
            using var request = CreateLogoutPost(token, AppendCookie(cookieHeader, "__Host-blazorshop-refresh=abc"));
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location?.ToString());
            Assert.Equal(1, authClient.LogoutCalls);
            Assert.Equal("__Host-blazorshop-refresh=abc", authClient.LastLogoutCookieHeader);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("__Host-blazorshop-refresh=;", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Robots_ReturnsTextDocument()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontRobotsService>();
                services.AddScoped<IStorefrontRobotsService>(_ => new StubRobotsService("User-agent: *\nAllow: /\n"));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Robots);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("User-agent: *", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Sitemap_ReturnsXmlDocument()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSitemapService>();
                services.AddScoped<IStorefrontSitemapService>(_ => new StubSitemapService(StorefrontSitemapGenerationResult.Success("<urlset />")));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Sitemap);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<urlset />", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Maintenance_WhenCurrentStoreRecovered_RedirectsHome()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontCurrentStoreProvider>();
                    services.AddScoped<IStorefrontCurrentStoreProvider>(_ => new StubCurrentStoreProvider(
                        StorefrontCurrentStoreResolution.Succeeded(CreateActiveCurrentStore())));
                },
                allowAutoRedirect: false);

            using var response = await client.GetAsync($"{StorefrontRoutes.Maintenance}?reason=maintenance");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(StorefrontRoutes.Home, response.Headers.Location?.ToString());
        }

        [Fact]
        public async Task Maintenance_WhenCurrentStoreStillInMaintenance_RendersAutoRefresh()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontCurrentStoreProvider>();
                    services.AddScoped<IStorefrontCurrentStoreProvider>(_ => new StubCurrentStoreProvider(
                        StorefrontCurrentStoreResolution.Maintenance(CreateActiveCurrentStore(
                            maintenanceModeEnabled: true,
                            maintenanceMessage: "Scheduled maintenance."))));
                },
                allowAutoRedirect: false);

            using var response = await client.GetAsync($"{StorefrontRoutes.Maintenance}?reason=maintenance");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Contains("Scheduled maintenance.", content, StringComparison.Ordinal);
            Assert.Contains("http-equiv=\"refresh\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("content=\"10\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cart_RendersEmptyCartWithoutCommerceNode()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new ServiceUnavailableHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Cart);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("cart", content, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("POST", "/api/cart/lines")]
        [InlineData("PUT", "/api/cart/lines/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
        [InlineData("DELETE", "/api/cart/lines/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")]
        [InlineData("DELETE", "/api/cart")]
        public async Task CartApi_MutationWithoutAntiforgeryToken_ReturnsBadRequest(string method, string path)
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(new ServiceUnavailableHandler())
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (!string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                request.Content = JsonContent(new { ProductId = Guid.NewGuid(), Quantity = 1 });
            }

            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Security validation failed", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CartApi_PostLine_SetsHttpOnlyCartToken_AndDoesNotSendUnitPrice()
        {
            var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var handler = new CartApiHandler(productId);
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateJsonRequest(
                HttpMethod.Post,
                "/api/cart/lines",
                new
                {
                    ProductId = productId,
                    Quantity = 1,
                    UnitPrice = 99.95m,
                },
                token,
                cookieHeader);
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"count\":1", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
                value.Contains("bs-cart-token=server-token", StringComparison.Ordinal)
                && value.Contains("httponly", StringComparison.OrdinalIgnoreCase)
                && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase)
                && value.Contains("path=/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("unitPrice", handler.LastAddLineBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CartApi_Get_ImportsLegacyCookieAndDeletesReadableCartPayload()
        {
            var productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var handler = new CartApiHandler(productId);
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/cart");
            request.Headers.Add("Cookie", CreateLegacyCartCookie(productId, 2, 129.95m));
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"count\":2", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
                value.Contains("bs-cart-token=server-token", StringComparison.Ordinal)
                && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value =>
                value.Contains("my-cart=", StringComparison.Ordinal)
                && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("unitPrice", handler.LastAddLineBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Checkout_PostRedirectsToProviderNextAction()
        {
            var handler = new CheckoutPaymentRedirectHandler();
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Checkout, "bs-cart-token=server-token");
            using var request = CreateCheckoutPost(token, AppendCookie(cookieHeader, "bs-cart-token=server-token"));
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://checkout.stripe.test/session", response.Headers.Location?.ToString());
            Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.Contains("bs-cart-token=", StringComparison.Ordinal));
            Assert.Equal(1, handler.PlaceOrderCalls);
        }

        [Fact]
        public async Task PaymentSuccess_WhenCapturedAttempt_ShowsConfirmedState()
        {
            var handler = new PaymentAttemptStatusHandler("captured");
            using var client = CreateClientWithPaymentAttemptHandler(handler);

            using var response = await client.GetAsync($"{StorefrontRoutes.PaymentSuccess}?paymentAttemptId={PaymentAttemptStatusHandler.AttemptId:D}&provider=stripe");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Payment confirmed", content, StringComparison.Ordinal);
            Assert.Contains("Thank you", content, StringComparison.Ordinal);
            Assert.Contains("captured", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task PaymentSuccess_WhenAttemptPending_ShowsPollingState()
        {
            var handler = new PaymentAttemptStatusHandler("requires_action");
            using var client = CreateClientWithPaymentAttemptHandler(handler);

            using var response = await client.GetAsync($"{StorefrontRoutes.PaymentSuccess}?paymentAttemptId={PaymentAttemptStatusHandler.AttemptId:D}&provider=stripe");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Payment pending", content, StringComparison.Ordinal);
            Assert.Contains("Payment is being confirmed", content, StringComparison.Ordinal);
            Assert.Contains("http-equiv=\"refresh\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PaymentCancel_WhenFailedAttempt_ShowsRetryState()
        {
            var handler = new PaymentAttemptStatusHandler("failed", "Payment provider declined this payment.");
            using var client = CreateClientWithPaymentAttemptHandler(handler);

            using var response = await client.GetAsync($"{StorefrontRoutes.PaymentCancel}?paymentAttemptId={PaymentAttemptStatusHandler.AttemptId:D}&provider=stripe");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Payment not completed", content, StringComparison.Ordinal);
            Assert.Contains("Try another payment method", content, StringComparison.Ordinal);
            Assert.Contains("Payment provider declined this payment.", content, StringComparison.Ordinal);
            Assert.Contains($"href=\"{StorefrontRoutes.Checkout}\"", content, StringComparison.Ordinal);
        }

        private HttpClient CreateClient(Action<IServiceCollection> configureServices, bool allowAutoRedirect = true)
        {
            var configuredFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IStorefrontCurrentStoreProvider>();
                    services.AddScoped<IStorefrontCurrentStoreProvider>(_ => new StubCurrentStoreProvider(
                        StorefrontCurrentStoreResolution.Succeeded(CreateActiveCurrentStore())));
                    configureServices(services);
                });
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect,
            });
        }

        private HttpClient CreateClientWithPaymentAttemptHandler(PaymentAttemptStatusHandler handler)
        {
            return CreateClient(services =>
            {
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(handler)
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });
        }

        private static async Task<(string Token, string CookieHeader)> ReadAntiforgeryAsync(
            HttpClient client,
            string path,
            string? requestCookieHeader = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            if (!string.IsNullOrWhiteSpace(requestCookieHeader))
            {
                request.Headers.Add("Cookie", requestCookieHeader);
            }

            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            var tokenMatch = Regex.Match(
                content,
                "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"|<input[^>]*value=\"(?<token>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"",
                RegexOptions.IgnoreCase);

            Assert.True(tokenMatch.Success, "The sign-in page should render an antiforgery token.");

            var cookieHeader = string.Join(
                "; ",
                response.Headers.GetValues("Set-Cookie")
                    .Select(value => value.Split(';', 2)[0]));

            return (WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value), cookieHeader);
        }

        private static HttpRequestMessage CreateSignInPost(string antiforgeryToken, string cookieHeader, string returnUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.SignIn)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("Email", "customer@example.test"),
                    new KeyValuePair<string, string>("Password", "Password123!"),
                    new KeyValuePair<string, string>("ReturnUrl", returnUrl),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateRegisterPost(
            string antiforgeryToken,
            string cookieHeader,
            string returnUrl,
            string confirmPassword = "Password123!")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.Register)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("FullName", "Customer One"),
                    new KeyValuePair<string, string>("Email", "customer@example.test"),
                    new KeyValuePair<string, string>("Password", "Password123!"),
                    new KeyValuePair<string, string>("ConfirmPassword", confirmPassword),
                    new KeyValuePair<string, string>("ReturnUrl", returnUrl),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateCheckoutPost(string antiforgeryToken, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.Checkout)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("CartVersion", "2"),
                    new KeyValuePair<string, string>("IdempotencyKey", "checkout-online-key"),
                    new KeyValuePair<string, string>("CustomerEmail", "customer@example.test"),
                    new KeyValuePair<string, string>("CustomerName", "Customer One"),
                    new KeyValuePair<string, string>("PaymentMethodKey", "stripe"),
                    new KeyValuePair<string, string>("ShippingFullName", "Customer One"),
                    new KeyValuePair<string, string>("ShippingEmail", "customer@example.test"),
                    new KeyValuePair<string, string>("ShippingPhone", "5550100"),
                    new KeyValuePair<string, string>("ShippingAddress1", "1 Test Street"),
                    new KeyValuePair<string, string>("ShippingCity", "Test City"),
                    new KeyValuePair<string, string>("ShippingPostalCode", "10000"),
                    new KeyValuePair<string, string>("ShippingCountryCode", "US"),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateLogoutPost(string antiforgeryToken, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.Logout)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("ReturnUrl", StorefrontRoutes.Home),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateCurrencyPreferencePost(
            string antiforgeryToken,
            string cookieHeader,
            string currencyCode,
            string returnUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.CurrencyPreference)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("CurrencyCode", currencyCode),
                    new KeyValuePair<string, string>("ReturnUrl", returnUrl),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateJsonRequest(
            HttpMethod method,
            string path,
            object value,
            string antiforgeryToken,
            string cookieHeader)
        {
            var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent(value),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
            return request;
        }

        private static string AppendCookie(string cookieHeader, string cookie)
        {
            return string.IsNullOrWhiteSpace(cookieHeader)
                ? cookie
                : $"{cookieHeader}; {cookie}";
        }

        private static StringContent JsonContent(object value)
        {
            return new StringContent(
                JsonSerializer.Serialize(value),
                Encoding.UTF8,
                "application/json");
        }

        private static string CreateLegacyCartCookie(Guid productId, int quantity, decimal unitPrice)
        {
            var cartJson = JsonSerializer.Serialize(
                new[]
                {
                    new
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                    },
                });

            return $"my-cart={Uri.EscapeDataString(cartJson)}";
        }

        private static StorefrontCurrentStore CreateActiveCurrentStore(
            bool maintenanceModeEnabled = false,
            string? maintenanceMessage = null)
        {
            return new StorefrontCurrentStore(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "default",
                "Default Store",
                "active",
                "https://shop.example.test",
                "shop.example.test",
                true,
                null,
                null,
                "Default Company",
                "company@example.test",
                "5550100",
                "1 Test Street",
                null,
                null,
                null,
                null,
                null,
                "USD",
                "en-US",
                "support@example.test",
                "5550101",
                maintenanceModeEnabled,
                maintenanceMessage,
                null);
        }

        private sealed class StubCurrentStoreProvider : IStorefrontCurrentStoreProvider
        {
            private readonly StorefrontCurrentStoreResolution _resolution;

            public StubCurrentStoreProvider(StorefrontCurrentStoreResolution resolution)
            {
                _resolution = resolution;
            }

            public Task<StorefrontCurrentStoreResolution> ResolveAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_resolution);
            }
        }

        private sealed class StubStorefrontSessionResolver : IStorefrontSessionResolver
        {
            private readonly StorefrontSessionInfo _sessionInfo;

            public StubStorefrontSessionResolver(StorefrontSessionInfo sessionInfo)
            {
                _sessionInfo = sessionInfo;
            }

            public Task<StorefrontSessionInfo> GetCurrentUserAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_sessionInfo);
            }
        }

        private sealed class StubStorefrontClientAppUrlResolver : IStorefrontClientAppUrlResolver
        {
            private readonly string _baseUrl;

            public StubStorefrontClientAppUrlResolver(string baseUrl)
            {
                _baseUrl = baseUrl;
            }

            public string ResolveBaseUrl()
            {
                return _baseUrl;
            }

            public string ResolveUrl(string? path)
            {
                return $"{_baseUrl.TrimEnd('/')}/{(path ?? string.Empty).TrimStart('/')}";
            }
        }

        private sealed class StubStorefrontAuthClient : IStorefrontAuthClient
        {
            private readonly StorefrontAuthResult<StorefrontTokenResponse> loginResult;
            private readonly StorefrontAuthResult<object> registerResult;
            private readonly StorefrontAuthResult<object> logoutResult;

            public StubStorefrontAuthClient(
                StorefrontAuthResult<StorefrontTokenResponse>? loginResult = null,
                StorefrontAuthResult<object>? registerResult = null,
                StorefrontAuthResult<object>? logoutResult = null)
            {
                this.loginResult = loginResult ?? StorefrontAuthResult<StorefrontTokenResponse>.Failed("Login is not used by this test.");
                this.registerResult = registerResult ?? StorefrontAuthResult<object>.Failed("Register is not used by this test.");
                this.logoutResult = logoutResult ?? StorefrontAuthResult<object>.Failed("Logout is not used by this test.");
            }

            public int RegisterCalls { get; private set; }

            public int LogoutCalls { get; private set; }

            public CreateUser? LastRegisteredUser { get; private set; }

            public string? LastLogoutCookieHeader { get; private set; }

            public Task<StorefrontAuthResult<StorefrontTokenResponse>> LoginAsync(LoginUser user, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.loginResult);
            }

            public Task<StorefrontAuthResult<object>> RegisterAsync(CreateUser user, CancellationToken cancellationToken = default)
            {
                this.RegisterCalls++;
                this.LastRegisteredUser = user;
                return Task.FromResult(this.registerResult);
            }

            public Task<StorefrontAuthResult<object>> LogoutAsync(string? cookieHeader, string? userAgent, CancellationToken cancellationToken = default)
            {
                this.LogoutCalls++;
                this.LastLogoutCookieHeader = cookieHeader;
                return Task.FromResult(this.logoutResult);
            }
        }

        private sealed class StubRobotsService : IStorefrontRobotsService
        {
            private readonly string _content;

            public StubRobotsService(string content)
            {
                _content = content;
            }

            public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_content);
            }
        }

        private sealed class StubSitemapService : IStorefrontSitemapService
        {
            private readonly StorefrontSitemapGenerationResult _result;

            public StubSitemapService(StorefrontSitemapGenerationResult result)
            {
                _result = result;
            }

            public Task<StorefrontSitemapGenerationResult> GenerateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_result);
            }
        }

        private sealed class CurrencyPreferenceHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (request.Method == HttpMethod.Post && path.EndsWith("/currency/preference", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent(new
                        {
                            success = true,
                            message = "OK",
                            data = new
                            {
                                currencyCode = "EUR",
                                requestedCurrencySupported = true,
                                checkoutCurrencyEnabled = true,
                                availableCurrencies = new[] { "USD", "EUR" },
                                checkoutCurrencies = new[] { "USD", "EUR" },
                            },
                        }),
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }
        }

        private sealed class ServiceUnavailableHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
        }

        private sealed class CartApiHandler : HttpMessageHandler
        {
            private static readonly DateTimeOffset ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30);

            private readonly Guid productId;
            private int quantity;

            public CartApiHandler(Guid productId)
            {
                this.productId = productId;
            }

            public string LastAddLineBody { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (request.Method == HttpMethod.Post && path.EndsWith("/cart/session", StringComparison.Ordinal))
                {
                    return JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                            cartToken = "server-token",
                            state = "active",
                            version = 1,
                            expiresAtUtc = ExpiresAtUtc,
                        },
                    });
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/cart/lines", StringComparison.Ordinal))
                {
                    this.LastAddLineBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                    using var document = JsonDocument.Parse(this.LastAddLineBody);
                    this.quantity += document.RootElement.GetProperty("quantity").GetInt32();
                    return JsonResponse(CreateCartEnvelope());
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/cart", StringComparison.Ordinal))
                {
                    return JsonResponse(CreateCartEnvelope());
                }

                if (request.Method == HttpMethod.Delete && path.EndsWith("/cart", StringComparison.Ordinal))
                {
                    this.quantity = 0;
                    return JsonResponse(CreateCartEnvelope());
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            private object CreateCartEnvelope()
            {
                return new
                {
                    success = true,
                    message = "OK",
                    data = new
                    {
                        cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        state = "active",
                        version = 2,
                        lastActivityAtUtc = DateTimeOffset.UtcNow,
                        expiresAtUtc = ExpiresAtUtc,
                        lines = this.quantity <= 0
                            ? []
                            : new[]
                            {
                                new
                                {
                                    lineId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                                    productId = this.productId,
                                    productVariantId = (Guid?)null,
                                    selectedAttributesJson = (string?)null,
                                    personalizationHash = (string?)null,
                                    personalizationJson = (string?)null,
                                    artworkAssetId = (Guid?)null,
                                    artworkVersion = (int?)null,
                                    fulfillmentProviderKey = (string?)null,
                                    quantity = this.quantity,
                                    unitPriceSnapshot = 129.95m,
                                    currencyCodeSnapshot = "EUR",
                                },
                            },
                    },
                };
            }

            private static HttpResponseMessage JsonResponse(object payload)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent(payload),
                };
            }
        }

        private sealed class PaymentAttemptStatusHandler : HttpMessageHandler
        {
            public static readonly Guid AttemptId = Guid.Parse("12121212-1212-1212-1212-121212121212");

            private readonly string state;
            private readonly string? failureMessage;

            public PaymentAttemptStatusHandler(string state, string? failureMessage = null)
            {
                this.state = state;
                this.failureMessage = failureMessage;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (request.Method == HttpMethod.Get && path.EndsWith($"/payments/attempts/{AttemptId:D}", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            id = AttemptId,
                            checkoutSessionId = Guid.Parse("34343434-3434-3434-3434-343434343434"),
                            orderId = string.Equals(this.state, "captured", StringComparison.OrdinalIgnoreCase)
                                ? Guid.Parse("56565656-5656-5656-5656-565656565656")
                                : (Guid?)null,
                            paymentMethodKey = "stripe",
                            providerKey = "stripe",
                            state = this.state,
                            amount = 12.34m,
                            currencyCode = "USD",
                            providerReference = "pi_test",
                            providerSessionId = "cs_test",
                            nextAction = string.Equals(this.state, "requires_action", StringComparison.OrdinalIgnoreCase)
                                ? new
                                {
                                    type = "redirect",
                                    url = "https://checkout.stripe.test/session",
                                }
                                : null,
                            failureCode = this.failureMessage is null ? null : "payment.failed",
                            failureMessage = this.failureMessage,
                            expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                            createdAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                            updatedAtUtc = DateTimeOffset.UtcNow,
                        },
                    }));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static HttpResponseMessage JsonResponse(object payload)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent(payload),
                };
            }
        }

        private sealed class CheckoutPaymentRedirectHandler : HttpMessageHandler
        {
            private static readonly Guid CartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            private static readonly Guid LineId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            private static readonly Guid ProductId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            private static readonly Guid CheckoutSessionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            private static readonly Guid PaymentAttemptId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            private static readonly DateTimeOffset ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30);

            public int PlaceOrderCalls { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (request.Method == HttpMethod.Post && path.EndsWith("/cart/session", StringComparison.Ordinal))
                {
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            cartId = CartId,
                            cartToken = "server-token",
                            state = "active",
                            version = 2,
                            expiresAtUtc = ExpiresAtUtc,
                        },
                    }));
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/cart", StringComparison.Ordinal))
                {
                    return Task.FromResult(JsonResponse(CreateCartEnvelope()));
                }

                if (request.Method == HttpMethod.Get && path.EndsWith($"/catalog/products/{ProductId:D}", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            id = ProductId,
                            name = "Checkout Product",
                            description = "Test product",
                            image = "/media/products/test.webp",
                            price = 12.34m,
                            quantity = 10,
                            categoryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                            createdOn = DateTime.UtcNow,
                            updatedAt = DateTime.UtcNow,
                            variants = Array.Empty<object>(),
                        },
                    }));
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/payments/methods", StringComparison.Ordinal))
                {
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new[]
                        {
                            new
                            {
                                id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                                key = "stripe",
                                name = "Stripe",
                                description = "Pay online",
                            },
                        },
                    }));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/checkout/preview", StringComparison.Ordinal))
                {
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            checkoutSessionId = CheckoutSessionId,
                            cartId = CartId,
                            cartVersion = 2,
                            state = "ready",
                            isValid = true,
                            nextAction = "placeOrder",
                            customerEmail = "customer@example.test",
                            customerName = "Customer One",
                            paymentMethodKey = "stripe",
                            subtotal = 12.34m,
                            shippingTotal = 0m,
                            taxTotal = 0m,
                            discountTotal = 0m,
                            grandTotal = 12.34m,
                            currencyCode = "USD",
                            expiresAtUtc = ExpiresAtUtc,
                            lines = new[]
                            {
                                new
                                {
                                    lineId = LineId,
                                    productId = ProductId,
                                    productVariantId = (Guid?)null,
                                    quantity = 1,
                                    unitPrice = 12.34m,
                                    lineTotal = 12.34m,
                                    selectedAttributesJson = (string?)null,
                                    personalizationHash = (string?)null,
                                    personalizationJson = (string?)null,
                                    artworkAssetId = (Guid?)null,
                                    artworkVersion = (int?)null,
                                    fulfillmentProviderKey = (string?)null,
                                },
                            },
                            issues = Array.Empty<object>(),
                        },
                    }));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/checkout/place-order", StringComparison.Ordinal))
                {
                    this.PlaceOrderCalls++;
                    return Task.FromResult(JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            checkoutSessionId = CheckoutSessionId,
                            paymentAttemptId = PaymentAttemptId,
                            orderId = (Guid?)null,
                            reference = (string?)null,
                            orderStatus = (string?)null,
                            paymentStatus = "requires_action",
                            paymentMethodKey = "stripe",
                            totalAmount = 12.34m,
                            currencyCode = "USD",
                            idempotencyKey = "checkout-online-key",
                            createdOn = DateTime.UtcNow,
                            nextAction = new
                            {
                                type = "redirect",
                                url = "https://checkout.stripe.test/session",
                            },
                        },
                    }));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            private static object CreateCartEnvelope()
            {
                return new
                {
                    success = true,
                    message = "OK",
                    data = new
                    {
                        cartId = CartId,
                        state = "active",
                        version = 2,
                        lastActivityAtUtc = DateTimeOffset.UtcNow,
                        expiresAtUtc = ExpiresAtUtc,
                        lines = new[]
                        {
                            new
                            {
                                lineId = LineId,
                                productId = ProductId,
                                productVariantId = (Guid?)null,
                                selectedAttributesJson = (string?)null,
                                personalizationHash = (string?)null,
                                personalizationJson = (string?)null,
                                artworkAssetId = (Guid?)null,
                                artworkVersion = (int?)null,
                                fulfillmentProviderKey = (string?)null,
                                quantity = 1,
                                unitPriceSnapshot = 12.34m,
                                currencyCodeSnapshot = "USD",
                            },
                        },
                    },
                };
            }

            private static HttpResponseMessage JsonResponse(object payload)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent(payload),
                };
            }
        }
    }
}
