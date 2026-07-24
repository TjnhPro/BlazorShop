namespace BlazorShop.Storefront.Services
{
    using System.Globalization;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Web.SharedV2.Models;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Storefront.Options;

    using Microsoft.Extensions.Options;


    public sealed record StorefrontSubmitResult<TData>(bool Success, string Message, TData? Data)
    {
        public static StorefrontSubmitResult<TData> Succeeded(TData? data, string? message)
        {
            return new(true, string.IsNullOrWhiteSpace(message) ? "Request completed." : message, data);
        }

        public static StorefrontSubmitResult<TData> Failed(string? message)
        {
            return new(false, string.IsNullOrWhiteSpace(message) ? "The request could not be completed." : message, default);
        }
    }
}
