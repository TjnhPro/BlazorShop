namespace BlazorShop.Storefront.V2.Components.Security;

using BlazorShop.Storefront.Components.Ssr.Security;

public static class StorefrontConsentVisuals
{
    public static StorefrontConsentPanelLabels Labels { get; } = new("Cookie consent", "Privacy preferences", "Essential cookies keep sign-in, cart, checkout, and security working. Optional preferences can improve the Storefront experience.", "Cookie information", "Preferences", "Analytics", "Marketing", "Essential only", "Revoke", "Save choices", "Accept all");

    public static StorefrontConsentPanelClasses Classes { get; } = new(
        Root: "pointer-events-auto fixed inset-x-0 bottom-0 z-[100] border-t border-slate-200 bg-white/95 px-4 py-4 shadow-2xl backdrop-blur sm:px-6",
        Inner: "mx-auto flex max-w-6xl flex-col gap-4 md:flex-row md:items-center md:justify-between",
        Description: "max-w-3xl",
        Heading: "text-sm font-semibold text-slate-950",
        Body: "mt-1 text-sm leading-6 text-slate-700",
        PolicyLink: "mt-2 inline-flex text-sm font-medium text-slate-950 underline underline-offset-4",
        Choices: "flex flex-col gap-3 sm:min-w-80",
        ChoiceLabel: "flex items-center justify-between gap-4 text-sm text-slate-700",
        ChoiceInput: "h-4 w-4 rounded border-slate-300",
        Actions: "flex flex-wrap justify-end gap-2",
        SecondaryButton: "rounded-md border border-slate-300 px-3 py-2 text-sm font-medium text-slate-800 transition hover:bg-slate-100",
        PrimaryButton: "rounded-md bg-slate-950 px-3 py-2 text-sm font-medium text-white transition hover:bg-slate-800");
}
