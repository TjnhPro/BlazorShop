namespace BlazorShop.Storefront.Components.Headless.Account;

public sealed record StorefrontAccountProfileActionDescriptor(
    string FormAction,
    string LoadProfileRoute,
    string SaveProfileRoute)
{
    public static StorefrontAccountProfileActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty,
        string.Empty);
}

public sealed record StorefrontAccountPasswordActionDescriptor(
    string FormAction,
    string ChangePasswordRoute)
{
    public static StorefrontAccountPasswordActionDescriptor Empty { get; } = new(
        string.Empty,
        string.Empty);
}

public sealed record StorefrontAccountFormClasses
{
    public static StorefrontAccountFormClasses Empty { get; } = new();

    public string Root { get; init; } = string.Empty;

    public string StatusAlert { get; init; } = string.Empty;

    public string ErrorAlert { get; init; } = string.Empty;

    public string MissingProfile { get; init; } = string.Empty;

    public string ProfileForm { get; init; } = string.Empty;

    public string PasswordForm { get; init; } = string.Empty;

    public string Field { get; init; } = string.Empty;

    public string WideField { get; init; } = string.Empty;

    public string LabelText { get; init; } = string.Empty;

    public string Input { get; init; } = string.Empty;

    public string CurrencyInput { get; init; } = string.Empty;

    public string ActionRow { get; init; } = string.Empty;

    public string SubmitButton { get; init; } = string.Empty;
}
