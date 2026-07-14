extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using System.Net;
    using System.Net.Http.Headers;
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

        private HttpClient CreateClient(Action<IServiceCollection> configureServices, bool allowAutoRedirect = true)
        {
            var configuredFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(configureServices);
            });

            return configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect,
            });
        }

        private static async Task<(string Token, string CookieHeader)> ReadAntiforgeryAsync(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
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

        private static string AppendCookie(string cookieHeader, string cookie)
        {
            return string.IsNullOrWhiteSpace(cookieHeader)
                ? cookie
                : $"{cookieHeader}; {cookie}";
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

        private sealed class ServiceUnavailableHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
        }
    }
}
