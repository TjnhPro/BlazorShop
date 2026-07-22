extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using System.Reflection;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;

    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;

    public sealed class CommerceNodeRouteBoundaryTests
    {
        [Fact]
        public void CommerceNodeApi_DoesNotExposeLegacyInternalControllerRoutes()
        {
            var routeTemplates = GetControllerRouteTemplates(typeof(StorefrontScopedCatalogController).Assembly);

            Assert.DoesNotContain(
                routeTemplates,
                route => route.StartsWith("api/internal", StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetControllerRouteTemplates(Assembly assembly)
        {
            return assembly
                .GetTypes()
                .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
                .SelectMany(GetRouteTemplates);
        }

        private static IEnumerable<string> GetRouteTemplates(Type controllerType)
        {
            foreach (var routeAttribute in controllerType.GetCustomAttributes<RouteAttribute>(inherit: true))
            {
                yield return Normalize(routeAttribute.Template);
            }

            foreach (var method in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var routeAttribute in method.GetCustomAttributes<RouteAttribute>(inherit: true))
                {
                    yield return Normalize(routeAttribute.Template);
                }

                foreach (var httpMethodAttribute in method.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
                {
                    yield return Normalize(httpMethodAttribute.Template);
                }
            }
        }

        private static string Normalize(string? routeTemplate)
        {
            return (routeTemplate ?? string.Empty).TrimStart('~').TrimStart('/').Trim();
        }
    }
}
