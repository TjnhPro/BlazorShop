extern alias ControlPlaneApi;

namespace BlazorShop.Tests.PresentationV2.ControlPlane
{
    using System.Reflection;

    using ControlPlaneApi::BlazorShop.ControlPlane.API.Controllers;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Routing;

    using Xunit;

    public sealed class ControlPlaneCommerceGatewayContractTests
    {
        [Fact]
        public void CommerceGatewayControllers_AreProtected()
        {
            foreach (var controllerType in GetCommerceGatewayControllers())
            {
                Assert.Contains(
                    controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true),
                    attribute => !string.IsNullOrWhiteSpace(attribute.Policy));
                Assert.Empty(controllerType.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
            }
        }

        [Fact]
        public void CommerceGatewayControllers_DoNotUseGetForSideEffects()
        {
            var safePrefixes = new[] { "Get", "List", "Query", "Download", "Preview" };

            foreach (var method in GetCommerceGatewayActions())
            {
                if (!method.GetCustomAttributes<HttpGetAttribute>(inherit: false).Any())
                {
                    continue;
                }

                Assert.Contains(safePrefixes, prefix => method.Name.StartsWith(prefix, StringComparison.Ordinal));
            }
        }

        [Fact]
        public void CommerceGatewayCommandRequestBodies_AreExplicit()
        {
            foreach (var method in GetCommerceGatewayActions())
            {
                var isCommand = method.GetCustomAttributes<HttpPostAttribute>(inherit: false).Any()
                    || method.GetCustomAttributes<HttpPutAttribute>(inherit: false).Any()
                    || method.GetCustomAttributes<HttpPatchAttribute>(inherit: false).Any();

                if (!isCommand)
                {
                    continue;
                }

                foreach (var parameter in method.GetParameters().Where(parameter => parameter.Name == "request"))
                {
                    Assert.NotEmpty(parameter.GetCustomAttributes<FromBodyAttribute>(inherit: false));
                }
            }
        }

        private static IReadOnlyList<Type> GetCommerceGatewayControllers()
        {
            return typeof(ControlPlaneCommerceProductsController)
                .Assembly
                .GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .Where(type => type.Namespace == "BlazorShop.ControlPlane.API.Controllers")
                .Where(type => type.Name.StartsWith("ControlPlaneCommerce", StringComparison.Ordinal))
                .Where(type => type.Name.EndsWith("Controller", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<MethodInfo> GetCommerceGatewayActions()
        {
            return GetCommerceGatewayControllers()
                .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: false).Any());
        }
    }
}
