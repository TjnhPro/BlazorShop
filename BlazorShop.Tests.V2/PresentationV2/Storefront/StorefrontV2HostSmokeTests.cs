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
    using Microsoft.AspNetCore.WebUtilities;
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
            Assert.Contains("href=\"/forgot-password\"", content, StringComparison.Ordinal);
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
        [InlineData("/forgot-password")]
        [InlineData("/reset-password")]
        [InlineData("/logout")]
        [InlineData("/account/profile")]
        [InlineData("/account/change-password")]
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
            Assert.Contains("data-storefront-register-form", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Register_WhenRegistrationDisabled_RendersDisabledStateWithoutSubmit()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<IStorefrontAuthClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                    registrationPolicyResult: StorefrontAuthResult<StorefrontRegistrationPolicy>.Succeeded(
                        new StorefrontRegistrationPolicy("disabled", false, "Customer registration is disabled."),
                        "Registration policy returned.",
                        [])));
            });

            using var response = await client.GetAsync(StorefrontRoutes.Register);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Customer registration is disabled.", content, StringComparison.Ordinal);
            Assert.DoesNotContain("data-storefront-register-form", content, StringComparison.Ordinal);
            Assert.DoesNotContain("name=\"FullName\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("data-storefront-captcha-token=\"registration\"", content, StringComparison.Ordinal);
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
        public async Task Register_PostWhenRegistrationDisabled_RedirectsWithoutCallingRegister()
        {
            var authClient = new StubStorefrontAuthClient(
                registerResult: StorefrontAuthResult<object>.Succeeded(new object(), "Created.", []),
                registrationPolicyResult: StorefrontAuthResult<StorefrontRegistrationPolicy>.Succeeded(
                    new StorefrontRegistrationPolicy("disabled", false, "Customer registration is disabled."),
                    "Registration policy returned.",
                    []));
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn);
            using var request = CreateRegisterPost(token, cookieHeader, "/checkout");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/register?returnUrl=%2Fcheckout&error=Customer%20registration%20is%20disabled.", response.Headers.Location?.ToString());
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
        public async Task ForgotPassword_ReturnsRecoveryPage()
        {
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(StorefrontSessionInfo.Anonymous));
            });

            using var response = await client.GetAsync(StorefrontRoutes.ForgotPassword);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Forgot password", content, StringComparison.Ordinal);
            Assert.Contains("method=\"post\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("data-storefront-captcha-token=\"password-recovery\"", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ForgotPassword_PostValidEmail_RedirectsToGenericSentState()
        {
            var authClient = new StubStorefrontAuthClient(
                forgotPasswordResult: StorefrontAuthResult<object>.Succeeded(new object(), "If the email exists, reset instructions will be sent.", []));
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.ForgotPassword);
            using var request = CreateForgotPasswordPost(token, cookieHeader, "customer@example.test");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var query = AssertRedirect(response, StorefrontRoutes.ForgotPassword);
            Assert.Equal("customer@example.test", query["email"]);
            Assert.Equal("1", query["sent"]);
            Assert.Equal(1, authClient.ForgotPasswordCalls);
            Assert.Equal("customer@example.test", authClient.LastForgotPasswordEmail);
        }

        [Fact]
        public async Task ForgotPassword_PostInvalidEmail_RedirectsWithValidationError()
        {
            var authClient = new StubStorefrontAuthClient();
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.ForgotPassword);
            using var request = CreateForgotPasswordPost(token, cookieHeader, "invalid-email");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var query = AssertRedirect(response, StorefrontRoutes.ForgotPassword);
            Assert.Equal("invalid-email", query["email"]);
            Assert.Equal("Enter a valid email address.", query["error"]);
            Assert.Equal(0, authClient.ForgotPasswordCalls);
        }

        [Fact]
        public async Task ResetPassword_ReturnsResetPageWithoutRenderingTokenText()
        {
            using var client = CreateClient(_ => { });

            using var response = await client.GetAsync("/reset-password?email=customer%40example.test&token=secret-reset-token");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Reset password", content, StringComparison.Ordinal);
            Assert.Contains("name=\"Token\"", content, StringComparison.Ordinal);
            Assert.DoesNotContain(">secret-reset-token<", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ResetPassword_PostSuccess_RedirectsToSignInPasswordResetState()
        {
            var authClient = new StubStorefrontAuthClient(
                resetPasswordResult: StorefrontAuthResult<object>.Succeeded(new object(), "Password reset.", []));
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, "/reset-password?email=customer%40example.test&token=reset-token");
            using var request = CreateResetPasswordPost(token, cookieHeader, "customer@example.test", "reset-token");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/signin?passwordReset=1", response.Headers.Location?.ToString());
            Assert.Equal("customer@example.test", authClient.LastResetPasswordEmail);
            Assert.Equal("reset-token", authClient.LastResetPasswordToken);
        }

        [Fact]
        public async Task ResetPassword_PostFailure_RedirectsWithGenericError()
        {
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontAuthClient>(_ => new StubStorefrontAuthClient(
                        resetPasswordResult: StorefrontAuthResult<object>.Failed("Detailed provider failure.")));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, "/reset-password?email=customer%40example.test&token=reset-token");
            using var request = CreateResetPasswordPost(token, cookieHeader, "customer@example.test", "reset-token");
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var query = AssertRedirect(response, StorefrontRoutes.ResetPassword);
            Assert.Equal("customer@example.test", query["email"]);
            Assert.Equal("reset-token", query["token"]);
            Assert.Equal("This reset link is invalid or expired.", query["error"]);
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
        public async Task AccountProfile_WhenAuthenticated_RendersSafeProfileForm()
        {
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new AccountProfileHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.AccountProfile);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("noindex,nofollow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Redirecting to sign in", content, StringComparison.Ordinal);
            Assert.DoesNotContain("name=\"customerId\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name=\"appUserId\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name=\"storeId\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name=\"isActive\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AccountProfile_PostSuccess_UsesBearerAndRedirectsSaved()
        {
            var handler = new AccountProfileHandler();
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.AccountProfile);
            using var request = CreateAccountProfilePost(token, cookieHeader);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/account/profile?saved=1", response.Headers.Location?.ToString());
            Assert.Contains("Bearer access-token", handler.AuthorizationHeaders);
            Assert.Contains("PUT /api/storefront/stores/demo/customer/profile", handler.Requests);
        }

        [Fact]
        public async Task AccountChangePassword_PostSuccess_UsesBearerAndRedirectsSaved()
        {
            var authClient = new StubStorefrontAuthClient(
                changePasswordResult: StorefrontAuthResult<object>.Succeeded(null, "Password changed.", []));
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<IStorefrontAuthClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                    services.AddScoped<IStorefrontAuthClient>(_ => authClient);
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.AccountChangePassword);
            using var request = CreateChangePasswordPost(token, cookieHeader);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/account/change-password?saved=1", response.Headers.Location?.ToString());
            Assert.Equal("access-token", authClient.LastChangePasswordBearerToken);
        }

        [Fact]
        public async Task AccountOrders_WhenAuthenticated_RendersPagedOrders()
        {
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new AccountSelfServiceHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.AccountOrders);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("noindex,nofollow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Redirecting to sign in", content, StringComparison.Ordinal);
            Assert.DoesNotContain("customerId", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("providerReference", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AccountAddresses_WhenAuthenticated_RendersAddressBook()
        {
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new AccountSelfServiceHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync(StorefrontRoutes.AccountAddresses);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("noindex,nofollow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Redirecting to sign in", content, StringComparison.Ordinal);
            Assert.DoesNotContain("name=\"customerId\"", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("name=\"storeId\"", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AccountAddresses_PostCreate_UsesBearerAndRedirectsSaved()
        {
            var handler = new AccountSelfServiceHandler();
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(
                services =>
                {
                    services.RemoveAll<IStorefrontSessionResolver>();
                    services.RemoveAll<StorefrontApiClient>();
                    services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                    services.AddScoped(_ => new StorefrontApiClient(
                        new HttpClient(handler)
                        {
                            BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                        },
                        Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
                },
                allowAutoRedirect: false);

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.AccountAddresses);
            using var request = CreateAccountAddressPost(token, cookieHeader);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/account/addresses?saved=1", response.Headers.Location?.ToString());
            Assert.Contains("Bearer access-token", handler.AuthorizationHeaders);
            Assert.Contains("POST /api/storefront/stores/demo/customer/addresses", handler.Requests);
            Assert.Contains("\"firstName\":\"Customer\"", handler.LastAddressCommandBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("customerId", handler.LastAddressCommandBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("userId", handler.LastAddressCommandBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("storeId", handler.LastAddressCommandBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AccountOrderDetail_WhenAuthenticated_RendersSafeOrderDetail()
        {
            var session = new StorefrontSessionInfo(true, false, "Customer One", "customer@example.test", "access-token");
            using var client = CreateClient(services =>
            {
                services.RemoveAll<IStorefrontSessionResolver>();
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped<IStorefrontSessionResolver>(_ => new StubStorefrontSessionResolver(session));
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(new AccountSelfServiceHandler())
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var response = await client.GetAsync("/account/orders/ORD-1");
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("noindex,nofollow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Redirecting to sign in", content, StringComparison.Ordinal);
            Assert.DoesNotContain("customerId", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("providerReference", content, StringComparison.OrdinalIgnoreCase);
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

        [Fact]
        public async Task CartPage_PrendersInteractiveCartSnapshot()
        {
            var productId = Guid.Parse("31313131-3131-3131-3131-313131313131");
            var handler = new CartApiHandler(productId, initialQuantity: 2);
            using var client = CreateClient(services =>
            {
                services.RemoveAll<StorefrontApiClient>();
                services.AddScoped(_ => new StorefrontApiClient(
                    new HttpClient(handler)
                    {
                        BaseAddress = new Uri("https://commerce-node.example/api/storefront/stores/demo/"),
                    },
                    Microsoft.Extensions.Options.Options.Create(new StorefrontV2::BlazorShop.Storefront.Options.StorefrontApiOptions())));
            });

            using var request = new HttpRequestMessage(HttpMethod.Get, StorefrontRoutes.Cart);
            request.Headers.Add("Cookie", "bs-cart-token=server-token");
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("WASM Cart Product", content, StringComparison.Ordinal);
            Assert.Contains("2 items in cart", content, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-quantity", content, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-remove", content, StringComparison.Ordinal);
            Assert.Contains("data-storefront-cart-clear", content, StringComparison.Ordinal);
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
        public async Task CartApi_PostLine_WhenRateLimitExceeded_ReturnsTypedTooManyRequests()
        {
            var productId = Guid.Parse("12121212-1212-1212-1212-121212121212");
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
                allowAutoRedirect: false,
                configureHost: builder =>
                {
                    builder.UseSetting("Storefront:RateLimiting:Cart:PermitLimit", "1");
                    builder.UseSetting("Storefront:RateLimiting:Cart:WindowSeconds", "60");
                    builder.UseSetting("Storefront:RateLimiting:Cart:QueueLimit", "0");
                });

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.SignIn, "bs-cart-token=server-token");
            using var firstRequest = CreateJsonRequest(
                HttpMethod.Post,
                "/api/cart/lines",
                new { ProductId = productId, Quantity = 1 },
                token,
                AppendCookie(cookieHeader, "bs-cart-token=server-token"));
            using var firstResponse = await client.SendAsync(firstRequest);

            using var secondRequest = CreateJsonRequest(
                HttpMethod.Post,
                "/api/cart/lines",
                new { ProductId = productId, Quantity = 1 },
                token,
                AppendCookie(cookieHeader, "bs-cart-token=server-token"));
            using var secondResponse = await client.SendAsync(secondRequest);
            var content = await secondResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
            Assert.Contains("Too many cart requests", content, StringComparison.Ordinal);
            Assert.Contains("no-store", secondResponse.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(secondResponse.Headers.GetValues("X-Robots-Tag"), value => value.Contains("noindex, nofollow", StringComparison.OrdinalIgnoreCase));
            Assert.True(secondResponse.Headers.RetryAfter?.Delta?.TotalSeconds >= 1);
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
            Assert.False(DeletesCookie(response, "bs-cart-token"));
            Assert.False(DeletesCookie(response, "my-cart"));
            Assert.Equal(1, handler.PlaceOrderCalls);
            Assert.Equal(1, handler.ReviewCalls);
            Assert.Contains("\"expectedCheckoutVersion\":5", handler.LastPlaceOrderBody, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("grandTotal", handler.LastAddressBody + handler.LastPaymentBody + handler.LastPlaceOrderBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Checkout_PostCompletedOrderClearsCartCookies()
        {
            var handler = new CheckoutPaymentRedirectHandler(completedOrder: true);
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

            var (token, cookieHeader) = await ReadAntiforgeryAsync(client, StorefrontRoutes.Checkout, "bs-cart-token=server-token; my-cart=[]");
            using var request = CreateCheckoutPost(token, AppendCookie(cookieHeader, "bs-cart-token=server-token; my-cart=[]"));
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal($"{StorefrontRoutes.Checkout}?orderReference=ORD-TEST-1", response.Headers.Location?.ToString());
            Assert.True(DeletesCookie(response, "bs-cart-token"));
            Assert.True(DeletesCookie(response, "my-cart"));
            Assert.Equal(1, handler.PlaceOrderCalls);
            Assert.Equal(1, handler.ReviewCalls);
        }

        [Fact]
        public async Task Checkout_PostWithStaleCartVersionRedirectsWithoutPlacingOrder()
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
            using var request = CreateCheckoutPost(token, AppendCookie(cookieHeader, "bs-cart-token=server-token"), cartVersion: 1);
            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal($"{StorefrontRoutes.Checkout}?error=Your%20cart%20changed.%20Review%20the%20latest%20cart%20and%20try%20checkout%20again.", response.Headers.Location?.ToString());
            Assert.Equal(0, handler.ReviewCalls);
            Assert.Equal(0, handler.PlaceOrderCalls);
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

        private HttpClient CreateClient(
            Action<IServiceCollection> configureServices,
            bool allowAutoRedirect = true,
            Action<IWebHostBuilder>? configureHost = null)
        {
            var configuredFactory = _factory.WithWebHostBuilder(builder =>
            {
                configureHost?.Invoke(builder);
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

        private static HttpRequestMessage CreateForgotPasswordPost(string antiforgeryToken, string cookieHeader, string email)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.ForgotPassword)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("CaptchaToken", "captcha-token"),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateResetPasswordPost(
            string antiforgeryToken,
            string cookieHeader,
            string email,
            string resetToken,
            string confirmPassword = "NewPassword123!")
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.ResetPassword)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("Email", email),
                    new KeyValuePair<string, string>("Token", resetToken),
                    new KeyValuePair<string, string>("Password", "NewPassword123!"),
                    new KeyValuePair<string, string>("ConfirmPassword", confirmPassword),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static Dictionary<string, Microsoft.Extensions.Primitives.StringValues> AssertRedirect(HttpResponseMessage response, string expectedPath)
        {
            var location = response.Headers.Location?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(location));

            var queryStart = location.IndexOf('?', StringComparison.Ordinal);
            var path = queryStart >= 0 ? location[..queryStart] : location;
            var query = queryStart >= 0 ? location[queryStart..] : string.Empty;

            Assert.Equal(expectedPath, path);
            return QueryHelpers.ParseQuery(query);
        }

        private static HttpRequestMessage CreateCheckoutPost(string antiforgeryToken, string cookieHeader, int cartVersion = 2)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.Checkout)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("CartVersion", cartVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
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

        private static bool DeletesCookie(HttpResponseMessage response, string cookieName)
        {
            return response.Headers.TryGetValues("Set-Cookie", out var values)
                && values.Any(value =>
                    value.Contains(cookieName + "=", StringComparison.Ordinal)
                    && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
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

        private static HttpRequestMessage CreateAccountProfilePost(string antiforgeryToken, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.AccountProfile)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("FullName", "Customer One"),
                    new KeyValuePair<string, string>("Email", "customer@example.test"),
                    new KeyValuePair<string, string>("FirstName", "Customer"),
                    new KeyValuePair<string, string>("LastName", "One"),
                    new KeyValuePair<string, string>("PreferredCurrencyCode", "USD"),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateChangePasswordPost(string antiforgeryToken, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.AccountChangePassword)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("CurrentPassword", "OldPassword123!"),
                    new KeyValuePair<string, string>("NewPassword", "NewPassword123!"),
                    new KeyValuePair<string, string>("ConfirmPassword", "NewPassword123!"),
                ]),
            };

            request.Headers.Add("Cookie", cookieHeader);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            return request;
        }

        private static HttpRequestMessage CreateAccountAddressPost(string antiforgeryToken, string cookieHeader)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, StorefrontRoutes.AccountAddresses)
            {
                Content = new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("__RequestVerificationToken", antiforgeryToken),
                    new KeyValuePair<string, string>("Action", "create"),
                    new KeyValuePair<string, string>("FullName", "Customer One"),
                    new KeyValuePair<string, string>("Email", "customer@example.test"),
                    new KeyValuePair<string, string>("Phone", "5550100"),
                    new KeyValuePair<string, string>("Address1", "1 Test Street"),
                    new KeyValuePair<string, string>("City", "New York"),
                    new KeyValuePair<string, string>("StateProvinceName", "New York"),
                    new KeyValuePair<string, string>("PostalCode", "10000"),
                    new KeyValuePair<string, string>("CountryCode", "US"),
                    new KeyValuePair<string, string>("IsDefaultShipping", "true"),
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
            private readonly StorefrontAuthResult<StorefrontRegistrationPolicy> registrationPolicyResult;
            private readonly StorefrontAuthResult<object> forgotPasswordResult;
            private readonly StorefrontAuthResult<object> resetPasswordResult;
            private readonly StorefrontAuthResult<object> changePasswordResult;
            private readonly StorefrontAuthResult<object> logoutResult;

            public StubStorefrontAuthClient(
                StorefrontAuthResult<StorefrontTokenResponse>? loginResult = null,
                StorefrontAuthResult<object>? registerResult = null,
                StorefrontAuthResult<StorefrontRegistrationPolicy>? registrationPolicyResult = null,
                StorefrontAuthResult<object>? forgotPasswordResult = null,
                StorefrontAuthResult<object>? resetPasswordResult = null,
                StorefrontAuthResult<object>? changePasswordResult = null,
                StorefrontAuthResult<object>? logoutResult = null)
            {
                this.loginResult = loginResult ?? StorefrontAuthResult<StorefrontTokenResponse>.Failed("Login is not used by this test.");
                this.registerResult = registerResult ?? StorefrontAuthResult<object>.Failed("Register is not used by this test.");
                this.registrationPolicyResult = registrationPolicyResult
                    ?? StorefrontAuthResult<StorefrontRegistrationPolicy>.Succeeded(
                        new StorefrontRegistrationPolicy("standard", true, "Customer registration is available."),
                        "Registration policy returned.",
                        []);
                this.forgotPasswordResult = forgotPasswordResult ?? StorefrontAuthResult<object>.Failed("Forgot password is not used by this test.");
                this.resetPasswordResult = resetPasswordResult ?? StorefrontAuthResult<object>.Failed("Reset password is not used by this test.");
                this.changePasswordResult = changePasswordResult ?? StorefrontAuthResult<object>.Failed("Change password is not used by this test.");
                this.logoutResult = logoutResult ?? StorefrontAuthResult<object>.Failed("Logout is not used by this test.");
            }

            public int RegisterCalls { get; private set; }

            public int ForgotPasswordCalls { get; private set; }

            public int LogoutCalls { get; private set; }

            public CreateUser? LastRegisteredUser { get; private set; }

            public string? LastForgotPasswordEmail { get; private set; }

            public string? LastResetPasswordEmail { get; private set; }

            public string? LastResetPasswordToken { get; private set; }

            public string? LastLogoutCookieHeader { get; private set; }

            public string? LastChangePasswordBearerToken { get; private set; }

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

            public Task<StorefrontAuthResult<StorefrontRegistrationPolicy>> GetRegistrationPolicyAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.registrationPolicyResult);
            }

            public Task<StorefrontAuthResult<object>> ForgotPasswordAsync(string email, string? captchaToken, CancellationToken cancellationToken = default)
            {
                this.ForgotPasswordCalls++;
                this.LastForgotPasswordEmail = email;
                return Task.FromResult(this.forgotPasswordResult);
            }

            public Task<StorefrontAuthResult<object>> ResetPasswordAsync(
                string email,
                string token,
                string password,
                string confirmPassword,
                CancellationToken cancellationToken = default)
            {
                this.LastResetPasswordEmail = email;
                this.LastResetPasswordToken = token;
                return Task.FromResult(this.resetPasswordResult);
            }

            public Task<StorefrontAuthResult<object>> ChangePasswordAsync(string bearerToken, ChangePassword changePassword, CancellationToken cancellationToken = default)
            {
                this.LastChangePasswordBearerToken = bearerToken;
                return Task.FromResult(this.changePasswordResult);
            }

            public Task<StorefrontAuthResult<object>> LogoutAsync(string? cookieHeader, string? userAgent, CancellationToken cancellationToken = default)
            {
                this.LogoutCalls++;
                this.LastLogoutCookieHeader = cookieHeader;
                return Task.FromResult(this.logoutResult);
            }
        }

        private sealed class AccountProfileHandler : HttpMessageHandler
        {
            public List<string> Requests { get; } = [];

            public List<string> AuthorizationHeaders { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.Requests.Add($"{request.Method.Method} {request.RequestUri?.AbsolutePath}");
                if (request.Headers.Authorization is not null)
                {
                    this.AuthorizationHeaders.Add($"{request.Headers.Authorization.Scheme} {request.Headers.Authorization.Parameter}");
                }

                if (!string.Equals(
                    request.RequestUri?.AbsolutePath,
                    "/api/storefront/stores/demo/customer/profile",
                    StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "success": true,
                          "message": "Profile returned.",
                          "data": {
                            "customerPublicId": "22222222-2222-2222-2222-222222222222",
                            "email": "customer@example.test",
                            "fullName": "Customer One",
                            "firstName": "Customer",
                            "lastName": "One",
                            "company": null,
                            "phoneNumber": "5550100",
                            "preferredLanguage": "en",
                            "preferredCurrencyCode": "USD",
                            "createdAtUtc": "2026-07-17T00:00:00Z",
                            "lastActivityAtUtc": "2026-07-17T01:00:00Z"
                          }
                        }
                        """,
                        Encoding.UTF8,
                        "application/json"),
                });
            }
        }

        private sealed class AccountSelfServiceHandler : HttpMessageHandler
        {
            public List<string> Requests { get; } = [];

            public List<string> AuthorizationHeaders { get; } = [];

            public string LastAddressCommandBody { get; private set; } = string.Empty;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                this.Requests.Add($"{request.Method.Method} {path}");
                if (request.Headers.Authorization is not null)
                {
                    this.AuthorizationHeaders.Add($"{request.Headers.Authorization.Scheme} {request.Headers.Authorization.Parameter}");
                }

                if (string.Equals(path, "/api/storefront/stores/demo/customer/addresses", StringComparison.Ordinal)
                    && request.Method == HttpMethod.Post)
                {
                    this.LastAddressCommandBody = request.Content is null
                        ? string.Empty
                        : request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                    return Task.FromResult(CreateJsonResponse(
                        """
                        {
                          "success": true,
                          "message": "Address saved.",
                          "data": {
                            "publicId": "11111111-1111-1111-1111-111111111111",
                            "firstName": "Customer",
                            "lastName": "One",
                            "company": null,
                            "address1": "1 Test Street",
                            "address2": null,
                            "city": "New York",
                            "postalCode": "10000",
                            "countryCode": "US",
                            "stateProvinceCode": null,
                            "stateProvinceName": "New York",
                            "phone": "5550100",
                            "email": "customer@example.test",
                            "isDefaultShipping": true,
                            "isDefaultBilling": false,
                            "createdAtUtc": "2026-07-17T00:00:00Z",
                            "updatedAtUtc": "2026-07-17T00:00:00Z"
                          }
                        }
                        """));
                }

                var response = path switch
                {
                    "/api/storefront/stores/demo/orders/current-user" => CreateJsonResponse(
                        """
                        {
                          "success": true,
                          "message": "Orders loaded.",
                          "data": {
                            "items": [
                              {
                                "reference": "ORD-1",
                                "createdOn": "2026-07-17T00:00:00Z",
                                "orderStatus": "processing",
                                "paymentStatus": "paid",
                                "shippingStatus": "not_yet_shipped",
                                "currencyCode": "USD",
                                "totalAmount": 25.00,
                                "itemCount": 2,
                                "trackingSummary": {
                                  "shippingCarrier": null,
                                  "trackingNumber": null,
                                  "trackingUrl": null,
                                  "shippedOn": null,
                                  "deliveredOn": null,
                                  "lastTrackingEventAtUtc": null
                                }
                              }
                            ],
                            "pageNumber": 1,
                            "pageSize": 10,
                            "totalCount": 1,
                            "totalPages": 1
                          }
                        }
                        """),
                    "/api/storefront/stores/demo/customer/addresses" => CreateJsonResponse(
                        """
                        {
                          "success": true,
                          "message": "Addresses loaded.",
                          "data": [
                            {
                              "publicId": "11111111-1111-1111-1111-111111111111",
                              "firstName": "Customer",
                              "lastName": "One",
                              "company": null,
                              "address1": "1 Test Street",
                              "address2": null,
                              "city": "New York",
                              "postalCode": "10000",
                              "countryCode": "US",
                              "stateProvinceCode": "NY",
                              "stateProvinceName": "New York",
                              "phone": "5550100",
                              "email": "customer@example.test",
                              "isDefaultShipping": true,
                              "isDefaultBilling": false,
                              "createdAtUtc": "2026-07-17T00:00:00Z",
                              "updatedAtUtc": "2026-07-17T00:00:00Z"
                            }
                          ]
                        }
                        """),
                    "/api/storefront/stores/demo/orders/current-user/ORD-1" => CreateJsonResponse(CreateOrderDetailJson(receiptMode: false)),
                    "/api/storefront/stores/demo/orders/current-user/ORD-1/receipt" => CreateJsonResponse(CreateOrderDetailJson(receiptMode: true)),
                    _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
                };

                return Task.FromResult(response);
            }

            private static HttpResponseMessage CreateJsonResponse(string json)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }

            private static string CreateOrderDetailJson(bool receiptMode)
            {
                return $$"""
                {
                  "success": true,
                  "message": "Order loaded.",
                  "data": {
                    "reference": "ORD-1",
                    "status": "processing",
                    "orderStatus": "processing",
                    "paymentStatus": "paid",
                    "paymentMethodKey": "cod",
                    "paymentAt": null,
                    "paymentSummary": null,
                    "storeSnapshot": null,
                    "currencyCode": "USD",
                    "totalAmount": 25.00,
                    "totalBreakdown": {
                      "subtotal": 20.00,
                      "shippingTotal": 5.00,
                      "taxTotal": 0,
                      "discountTotal": 0,
                      "grandTotal": 25.00
                    },
                    "baseCurrencyCode": "USD",
                    "baseTotalAmount": 25.00,
                    "baseTotalBreakdown": null,
                    "exchangeRate": null,
                    "exchangeRateProviderKey": null,
                    "exchangeRateSource": null,
                    "exchangeRateEffectiveAtUtc": null,
                    "exchangeRateExpiresAtUtc": null,
                    "createdOn": "2026-07-17T00:00:00Z",
                    "shippingStatus": "not_yet_shipped",
                    "shippingCarrier": null,
                    "trackingNumber": null,
                    "trackingUrl": null,
                    "shippedOn": null,
                    "deliveredOn": null,
                    "customerName": "Customer One",
                    "customerEmail": "customer@example.test",
                    "billingAddress": null,
                    "shippingAddressSnapshot": null,
                    "shippingAddress": {
                      "fullName": "Customer One",
                      "email": "customer@example.test",
                      "phone": null,
                      "address1": "1 Test Street",
                      "address2": null,
                      "city": "New York",
                      "state": "NY",
                      "postalCode": "10000",
                      "countryCode": "US"
                    },
                    "shippingMethod": null,
                    "completedAt": null,
                    "cancelledAt": null,
                    "trackingEvents": [],
                    "historyEntries": [],
                    "lines": [
                      {
                        "productId": "11111111-1111-1111-1111-111111111111",
                        "productName": "Test Product",
                        "sku": "SKU-1",
                        "image": null,
                        "productVariantId": null,
                        "variantAttributes": [],
                        "quantity": 2,
                        "unitPrice": 10.00,
                        "lineTotal": 20.00
                      }
                    ],
                    "actions": {
                      "canRetryPayment": false,
                      "canReorder": false,
                      "canRequestReturn": false,
                      "hasDownloads": false
                    },
                    "receiptMode": {{receiptMode.ToString().ToLowerInvariant()}}
                  }
                }
                """;
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

            public CartApiHandler(Guid productId, int initialQuantity = 0)
            {
                this.productId = productId;
                this.quantity = initialQuantity;
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
                                    displayName = "WASM Cart Product",
                                    productSlug = "wasm-cart-product",
                                    imageUrl = "/media/products/wasm-cart-product.png",
                                    purchasable = true,
                                    quantityMinimum = 1,
                                    quantityMaximum = 9,
                                    quantityStep = 1,
                                    unitPriceSnapshot = 129.95m,
                                    unitPrice = 129.95m,
                                    lineTotal = 129.95m * this.quantity,
                                    lineSubtotal = 129.95m * this.quantity,
                                    currencyCodeSnapshot = "EUR",
                                    selectedAttributes = Array.Empty<object>(),
                                    warnings = Array.Empty<object>(),
                                },
                            },
                        summaryCount = this.quantity,
                        currencyCode = "EUR",
                        subtotal = 129.95m * this.quantity,
                        grandTotal = 129.95m * this.quantity,
                        checkoutAllowed = true,
                        warnings = Array.Empty<object>(),
                        adjustments = Array.Empty<object>(),
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
            private readonly bool completedOrder;

            public CheckoutPaymentRedirectHandler(bool completedOrder = false)
            {
                this.completedOrder = completedOrder;
            }

            public int PlaceOrderCalls { get; private set; }

            public int ReviewCalls { get; private set; }

            public string LastAddressBody { get; private set; } = string.Empty;

            public string LastPaymentBody { get; private set; } = string.Empty;

            public string LastPlaceOrderBody { get; private set; } = string.Empty;

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
                            cartId = CartId,
                            cartToken = "server-token",
                            state = "active",
                            version = 2,
                            expiresAtUtc = ExpiresAtUtc,
                        },
                    });
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/cart", StringComparison.Ordinal))
                {
                    return JsonResponse(CreateCartEnvelope());
                }

                if (request.Method == HttpMethod.Get && path.EndsWith($"/catalog/products/{ProductId:D}", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonResponse(new
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
                    });
                }

                if (request.Method == HttpMethod.Get && path.EndsWith("/payments/methods", StringComparison.Ordinal))
                {
                    return JsonResponse(new
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
                    });
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/checkout/start", StringComparison.Ordinal))
                {
                    return JsonResponse(CreateCheckoutSessionEnvelope(1, "entry", includeShipping: true, includePayment: true, selectedPayment: false));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith($"/checkout/{CheckoutSessionId:D}/addresses", StringComparison.OrdinalIgnoreCase))
                {
                    this.LastAddressBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                    return JsonResponse(CreateCheckoutSessionEnvelope(2, "shipping_method", includeShipping: true, includePayment: false, selectedPayment: false));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith($"/checkout/{CheckoutSessionId:D}/shipping-method", StringComparison.OrdinalIgnoreCase))
                {
                    return JsonResponse(CreateCheckoutSessionEnvelope(3, "payment_method", includeShipping: true, includePayment: true, selectedPayment: false));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith($"/checkout/{CheckoutSessionId:D}/payment-method", StringComparison.OrdinalIgnoreCase))
                {
                    this.LastPaymentBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                    return JsonResponse(CreateCheckoutSessionEnvelope(4, "review", includeShipping: true, includePayment: true, selectedPayment: true));
                }

                if (request.Method == HttpMethod.Post && path.EndsWith($"/checkout/{CheckoutSessionId:D}/review", StringComparison.OrdinalIgnoreCase))
                {
                    this.ReviewCalls++;
                    return JsonResponse(new
                    {
                        success = true,
                        message = "OK",
                        data = new
                        {
                            checkoutSessionId = CheckoutSessionId,
                            cartId = CartId,
                            checkoutVersion = 5,
                            cartVersion = 2,
                            lastValidatedCartVersion = 2,
                            state = "ready",
                            currentStep = "review",
                            completedSteps = new[] { "entry", "shipping_address", "shipping_method", "payment_method", "review" },
                            isActive = true,
                            nextAction = "place_order",
                            customerEmail = "customer@example.test",
                            customerName = "Customer One",
                            billingAddress = CreateAddress(),
                            shippingAddress = CreateAddress(),
                            selectedShippingOption = CreateShippingOption(),
                            selectedPaymentMethod = CreatePaymentOption(selected: true),
                            lines = CreateCheckoutLines(),
                            subtotal = 12.34m,
                            shippingTotal = 0m,
                            taxTotal = 0m,
                            discountTotal = 0m,
                            grandTotal = 12.34m,
                            currencyCode = "USD",
                            termsRequired = false,
                            termsAccepted = false,
                            termsVersion = (string?)null,
                            termsAcceptedAtUtc = (DateTimeOffset?)null,
                            placeOrderAllowed = true,
                            nextRequiredStep = "place_order",
                            issues = Array.Empty<object>(),
                        },
                    });
                }

                if (request.Method == HttpMethod.Post && path.EndsWith("/checkout/place-order", StringComparison.Ordinal))
                {
                    this.PlaceOrderCalls++;
                    this.LastPlaceOrderBody = await request.Content!.ReadAsStringAsync(cancellationToken);
                    if (this.completedOrder)
                    {
                        return JsonResponse(new
                        {
                            success = true,
                            message = "OK",
                            data = new
                            {
                                checkoutSessionId = CheckoutSessionId,
                                paymentAttemptId = PaymentAttemptId,
                                orderId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                                reference = "ORD-TEST-1",
                                orderStatus = "confirmed",
                                paymentStatus = "paid",
                                paymentMethodKey = "cod",
                                totalAmount = 12.34m,
                                currencyCode = "USD",
                                idempotencyKey = "checkout-online-key",
                                createdOn = DateTime.UtcNow,
                                nextAction = (object?)null,
                            },
                        });
                    }

                    return JsonResponse(new
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
                    });
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            private static object CreateCheckoutSessionEnvelope(
                int checkoutVersion,
                string currentStep,
                bool includeShipping,
                bool includePayment,
                bool selectedPayment)
            {
                return new
                {
                    success = true,
                    message = "OK",
                    data = new
                    {
                        checkoutSessionId = CheckoutSessionId,
                        cartId = CartId,
                        checkoutVersion,
                        cartVersion = 2,
                        lastValidatedCartVersion = 2,
                        state = selectedPayment ? "ready" : "draft",
                        currentStep,
                        completedSteps = Array.Empty<string>(),
                        isActive = true,
                        nextAction = selectedPayment ? "review" : currentStep,
                        customerEmail = "customer@example.test",
                        customerName = "Customer One",
                        paymentMethodKey = selectedPayment ? "stripe" : string.Empty,
                        subtotal = 12.34m,
                        shippingTotal = 0m,
                        taxTotal = 0m,
                        discountTotal = 0m,
                        grandTotal = 12.34m,
                        currencyCode = "USD",
                        expiresAtUtc = ExpiresAtUtc,
                        shippingRequired = true,
                        selectedShippingOption = includeShipping ? CreateShippingOption() : null,
                        shippingOptions = includeShipping ? new[] { CreateShippingOption() } : Array.Empty<object>(),
                        selectedPaymentMethod = selectedPayment ? CreatePaymentOption(selected: true) : null,
                        paymentMethods = includePayment ? new[] { CreatePaymentOption(selected: selectedPayment) } : Array.Empty<object>(),
                        lines = CreateCheckoutLines(),
                        issues = Array.Empty<object>(),
                    },
                };
            }

            private static object CreateAddress()
            {
                return new
                {
                    fullName = "Customer One",
                    email = "customer@example.test",
                    phone = "5550100",
                    address1 = "1 Test Street",
                    address2 = (string?)null,
                    city = "Test City",
                    state = (string?)null,
                    postalCode = "10000",
                    countryCode = "US",
                };
            }

            private static object CreateShippingOption()
            {
                return new
                {
                    key = "free_standard",
                    displayName = "Standard shipping",
                    description = "Standard delivery",
                    price = 0m,
                    currencyCode = "USD",
                    deliveryEstimateText = "3-5 business days",
                    selected = true,
                };
            }

            private static object CreatePaymentOption(bool selected)
            {
                return new
                {
                    key = "stripe",
                    displayName = "Stripe",
                    description = "Pay online",
                    shortDisplayText = "Card",
                    iconUrl = (string?)null,
                    providerKey = "stripe",
                    nextActionKind = "redirect",
                    selected,
                };
            }

            private static object[] CreateCheckoutLines()
            {
                return
                [
                    new
                    {
                        lineId = LineId,
                        productId = ProductId,
                        productVariantId = (Guid?)null,
                        quantity = 1,
                        unitPrice = 12.34m,
                        lineTotal = 12.34m,
                        currencyCode = "USD",
                    },
                ];
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
