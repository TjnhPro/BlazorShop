namespace BlazorShop.Storefront.Components.Contracts.System;

public sealed record StorefrontToastRegionClasses(
    string Region = "",
    string Toast = "",
    string Content = "",
    string Accent = "",
    string Icon = "",
    string Text = "",
    string Heading = "",
    string Message = "",
    string CloseButton = "",
    string CloseIcon = "");
