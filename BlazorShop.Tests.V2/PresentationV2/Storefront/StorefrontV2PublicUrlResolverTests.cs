extern alias StorefrontV2;

namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;
    using Xunit;

    using StorefrontV2::BlazorShop.Storefront.Options;
    using StorefrontV2::BlazorShop.Storefront.Services;

    public sealed class StorefrontV2PublicUrlResolverTests
    {
        [Fact]
        public void ResolveBaseUrl_PrefersPublicUrlOptionAndNormalizesIt()
        {
            var resolver = CreateResolver(
                requestContext: null,
                configuredOptionBaseUrl: "https://public-store.example/shop?utm=ignored#section");

            var result = resolver.ResolveBaseUrl("https://seo.example/catalog");

            Assert.Equal("https://public-store.example/shop/", result);
        }

        [Fact]
        public void ResolveBaseUrl_UsesSeoConfiguredBaseBeforeRequestFallback()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("request.example");

            var resolver = CreateResolver(httpContext, configuredOptionBaseUrl: null);

            var result = resolver.ResolveBaseUrl("https://seo.example/storefront?ignored=true#fragment");

            Assert.Equal("https://seo.example/storefront/", result);
        }

        [Fact]
        public void ResolveAbsoluteUrl_FallsBackToCurrentRequestSchemeHostAndPathBase()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("store.example", 8443);
            httpContext.Request.PathBase = "/shop";

            var resolver = CreateResolver(httpContext, configuredOptionBaseUrl: null);

            var result = resolver.ResolveAbsoluteUrl("/category/shoes");

            Assert.Equal("https://store.example:8443/shop/category/shoes", result);
        }

        [Fact]
        public void ResolveBaseUrl_DoesNotUseUnsupportedRequestScheme()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "ftp";
            httpContext.Request.Host = new HostString("store.example");

            var resolver = CreateResolver(httpContext, configuredOptionBaseUrl: null);

            Assert.Null(resolver.ResolveBaseUrl());
        }

        private static StorefrontPublicUrlResolver CreateResolver(HttpContext? requestContext, string? configuredOptionBaseUrl)
        {
            return new StorefrontPublicUrlResolver(
                new HttpContextAccessor { HttpContext = requestContext },
                Options.Create(new StorefrontPublicUrlOptions { BaseUrl = configuredOptionBaseUrl }));
        }
    }
}
