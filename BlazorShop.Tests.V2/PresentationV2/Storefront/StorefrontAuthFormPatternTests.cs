namespace BlazorShop.Tests.PresentationV2.Storefront;

using Xunit;

public sealed class StorefrontAuthFormPatternTests
{
    [Fact]
    public void AuthForms_PostToPresentationRoutesAndOwnSecurityFields()
    {
        var signIn = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontSignInForm.razor");
        var register = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontRegisterForm.razor");
        var forgot = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontForgotPasswordForm.razor");
        var reset = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontResetPasswordForm.razor");
        var allForms = string.Concat(signIn, register, forgot, reset);

        Assert.Equal(4, Count(allForms, "action=\"@Context.PostAction\""));
        Assert.Equal(4, Count(allForms, "<AntiforgeryToken />"));
        Assert.Contains("data-storefront-captcha-token=\"@CaptchaPurpose\"", signIn);
        Assert.Contains("data-storefront-captcha-token=\"@CaptchaPurpose\"", register);
        Assert.Contains("data-storefront-captcha-token=\"@CaptchaPurpose\"", forgot);
        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.SignIn.ReturnUrl\"", signIn);
        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.Register.ReturnUrl\"", register);
        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.ResetPassword.Email\"", reset);
        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.ResetPassword.Token\"", reset);
    }

    [Fact]
    public void AuthFormFieldNames_MatchEndpointFormDtos()
    {
        var fieldNames = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontAuthFormFieldNames.cs");

        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.Email)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.Password)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.CaptchaToken)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontLoginForm.ReturnUrl)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.FullName)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontRegisterForm.ConfirmPassword)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontForgotPasswordForm.CaptchaToken)", fieldNames);
        Assert.Contains("nameof(global::BlazorShop.Storefront.Services.StorefrontResetPasswordForm.Token)", fieldNames);
    }

    [Fact]
    public void RegisterDisabledPolicy_DoesNotRenderSubmitForm()
    {
        var register = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontRegisterForm.razor");

        Assert.Contains("@if (!Context.RegistrationAllowed)", register);
        Assert.True(
            register.IndexOf("@RegistrationDisabledContent", StringComparison.Ordinal)
            < register.IndexOf("<form method=\"post\"", StringComparison.Ordinal));
        Assert.Contains("data-storefront-register-form", register);
    }

    [Fact]
    public void V2AuthView_DoesNotSelfAuthorAuthPostContracts()
    {
        var v2 = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Auth/V2AuthPageView.razor");
        var starter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Starter/Pages/Ssr/Auth/AuthShellPage.razor");
        var hostViews = string.Concat(v2, starter);

        Assert.DoesNotContain("<form method=\"post\"", hostViews, StringComparison.Ordinal);
        Assert.DoesNotContain("<AntiforgeryToken", hostViews, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"ReturnUrl\"", hostViews, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"CaptchaToken\"", hostViews, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Email\"", hostViews, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Token\"", hostViews, StringComparison.Ordinal);
        Assert.Contains("<StorefrontSignInForm", hostViews, StringComparison.Ordinal);
        Assert.Contains("<StorefrontRegisterForm", hostViews, StringComparison.Ordinal);
        Assert.Contains("<StorefrontForgotPasswordForm", hostViews, StringComparison.Ordinal);
        Assert.Contains("<StorefrontResetPasswordForm", hostViews, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetForm_IncludesTokenAndEmailOnlyThroughPresentationPattern()
    {
        var reset = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Auth/StorefrontResetPasswordForm.razor");
        var v2 = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Theme/Pages/Auth/V2AuthPageView.razor");

        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.ResetPassword.Email\"", reset);
        Assert.Contains("name=\"@StorefrontAuthFormFieldNames.ResetPassword.Token\"", reset);
        Assert.Contains("value=\"@Context.Email\"", reset);
        Assert.Contains("value=\"@Context.Token\"", reset);
        Assert.DoesNotContain("Context.Token", v2, StringComparison.Ordinal);
        Assert.DoesNotContain("Context.Email", v2, StringComparison.Ordinal);
    }

    private static int Count(string value, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private static string ReadRepositoryFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate BlazorShop.sln from the test output directory.");
    }
}
