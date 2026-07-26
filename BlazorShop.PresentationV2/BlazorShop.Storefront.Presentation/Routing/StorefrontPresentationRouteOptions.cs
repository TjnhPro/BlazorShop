namespace BlazorShop.Storefront.Presentation.Routing;

using System.Reflection;

public sealed class StorefrontPresentationRouteOptions
{
    public IList<Assembly> AdditionalAssemblies { get; } = new List<Assembly>();
}
