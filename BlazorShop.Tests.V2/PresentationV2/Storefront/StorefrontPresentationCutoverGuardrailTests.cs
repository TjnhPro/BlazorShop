namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontPresentationCutoverGuardrailTests
{
    private const string CutoverTodo = "SPF16 guardrail placeholder; enable after the matching cutover phase implements the final state.";

    [Fact(Skip = CutoverTodo)]
    public void StorefrontPresentation_DIGraph_IsHostIndependent()
    {
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontVisualViews_DoNotOwnRoutesOrSeoHead()
    {
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontStarter_ViewsRenderPresentationContextsOnly()
    {
    }

    [Fact(Skip = CutoverTodo)]
    public void StorefrontRoutes_ArePresentationAssemblyOnly()
    {
    }
}
