namespace BlazorShop.ControlPlane.Web.Pages
{
    using System.Globalization;
    using BlazorShop.Application.ControlPlane.Catalog;
    using BlazorShop.Application.CommerceNode.Currencies;
    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Messages;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.ProductImports;
    using BlazorShop.Application.CommerceNode.ProductMedia;
    using BlazorShop.Application.CommerceNode.SecurityPrivacy;
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Application.CommerceNode.Tasks;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs.Admin.Inventory;
    using BlazorShop.Application.DTOs.Admin.Orders;
    using BlazorShop.Application.DTOs.Category;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.DTOs.Product;
    using BlazorShop.Application.DTOs.Product.ProductVariant;
    using BlazorShop.Application.DTOs.Seo;
    using BlazorShop.ControlPlane.Web.Services.Common;
    using BlazorShop.ControlPlane.Web.Services.Nodes;
    using BlazorShop.ControlPlane.Web.Services.Stores;
    using BlazorShop.ControlPlane.Web.Services.Users;
    using BlazorShop.Domain.Contracts;
    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Components.Web;

    public partial class CommerceOrders
    {
        private const int PageSize = 25;

        private readonly List<StoreSummary> stores = [];
        private readonly List<GetOrder> orders = [];
        private Guid? selectedStorePublicId;
        private string? searchTerm;
        private string? statusFilter;
        private string? shippingStatusFilter;
        private int pageNumber = 1;
        private int totalCount;
        private GetOrder? selectedOrder;
        private string? adminNote;
        private string shippingStatus = string.Empty;
        private DateTime shipmentDate = DateTime.Today;
        private UpsertShipmentRequest shipmentForm = new();
        private bool isLoading;
        private bool isSaving;
        private bool isDrawerOpen;
        private string? errorMessage;
        private string? successMessage;

        private bool HasStore => selectedStorePublicId.HasValue && selectedStorePublicId.Value != Guid.Empty;

        private bool CanNextPage => pageNumber * PageSize < totalCount;

        private bool CanCompleteSelectedOrder => selectedOrder is not null
            && string.Equals(selectedOrder.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selectedOrder.OrderStatus, "complete", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selectedOrder.OrderStatus, "cancelled", StringComparison.OrdinalIgnoreCase);

        private bool CanCancelSelectedOrder => selectedOrder is not null
            && !string.Equals(selectedOrder.OrderStatus, "complete", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selectedOrder.OrderStatus, "cancelled", StringComparison.OrdinalIgnoreCase);

        protected override async Task OnInitializedAsync()
        {
            var response = await StoreClient.ListAsync(status: "active", pageSize: 100);
            stores.AddRange(response.Items);
            selectedStorePublicId = stores.FirstOrDefault()?.PublicId;
            await LoadOrdersAsync();
        }

        private async Task SearchAsync()
        {
            pageNumber = 1;
            await LoadOrdersAsync();
        }

        private async Task LoadOrdersAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isLoading = true;
            errorMessage = null;
            try
            {
                var result = await OrderClient.QueryOrdersAsync(
                    selectedStorePublicId!.Value,
                    new AdminOrderQueryDto
                    {
                        PageNumber = pageNumber,
                        PageSize = PageSize,
                        SearchTerm = searchTerm,
                        Status = statusFilter,
                        ShippingStatus = shippingStatusFilter,
                    });

                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                orders.Clear();
                orders.AddRange(result.Data.Items);
                totalCount = result.Data.TotalCount;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task OpenOrderAsync(Guid orderId)
        {
            if (!HasStore)
            {
                return;
            }

            errorMessage = null;
            var result = await OrderClient.GetOrderAsync(selectedStorePublicId!.Value, orderId);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedOrder = result.Data;
            adminNote = selectedOrder.AdminNote;
            shippingStatus = selectedOrder.ShippingStatus;
            shipmentForm = new UpsertShipmentRequest
            {
                ShipDate = selectedOrder.ShippedOn ?? DateTime.Today,
                CarrierName = selectedOrder.ShippingCarrier ?? string.Empty,
                TrackingNumber = selectedOrder.TrackingNumber ?? string.Empty,
                TrackingUrl = selectedOrder.TrackingUrl,
            };
            shipmentDate = shipmentForm.ShipDate.Date;

            if (HasExistingShipmentSnapshot(selectedOrder))
            {
                var shipmentResult = await OrderClient.GetShipmentAsync(selectedStorePublicId.Value, orderId);
                if (shipmentResult.Success && shipmentResult.Data is not null)
                {
                    ApplyShipment(shipmentResult.Data);
                }
            }

            isDrawerOpen = true;
        }

        private async Task SaveAdminNoteAsync()
        {
            if (!HasStore || selectedOrder is null)
            {
                return;
            }

            isSaving = true;
            try
            {
                var result = await OrderClient.UpdateOrderAdminNoteAsync(
                    selectedStorePublicId!.Value,
                    selectedOrder.Id,
                    new UpdateOrderAdminNoteRequest { AdminNote = adminNote });

                await ApplyOrderResultAsync(result);
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task SaveShippingStatusAsync()
        {
            if (!HasStore || selectedOrder is null)
            {
                return;
            }

            isSaving = true;
            try
            {
                var result = await OrderClient.UpdateOrderShippingStatusAsync(
                    selectedStorePublicId!.Value,
                    selectedOrder.Id,
                    new UpdateShippingStatusRequest
                    {
                        ShippingStatus = shippingStatus,
                        ShippedOn = shipmentDate,
                        DeliveredOn = selectedOrder.DeliveredOn,
                    });

                await ApplyOrderResultAsync(result);
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task SaveShipmentAsync()
        {
            if (!HasStore || selectedOrder is null)
            {
                return;
            }

            isSaving = true;
            try
            {
                shipmentForm.ShipDate = shipmentDate;
                var result = await OrderClient.UpsertShipmentAsync(selectedStorePublicId!.Value, selectedOrder.Id, shipmentForm);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                ApplyShipment(result.Data);
                successMessage = result.Message;
                await OpenOrderAsync(selectedOrder.Id);
                await LoadOrdersAsync();
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task CompleteOrderAsync()
        {
            if (!HasStore || selectedOrder is null)
            {
                return;
            }

            isSaving = true;
            try
            {
                var result = await OrderClient.CompleteOrderAsync(selectedStorePublicId!.Value, selectedOrder.Id);
                await ApplyOrderResultAsync(result);
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task CancelOrderAsync()
        {
            if (!HasStore || selectedOrder is null)
            {
                return;
            }

            isSaving = true;
            try
            {
                var result = await OrderClient.CancelOrderAsync(selectedStorePublicId!.Value, selectedOrder.Id);
                await ApplyOrderResultAsync(result);
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task ApplyOrderResultAsync(ControlPlaneClientResult<GetOrder> result)
        {
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedOrder = result.Data;
            adminNote = selectedOrder.AdminNote;
            shippingStatus = selectedOrder.ShippingStatus;
            successMessage = result.Message;
            await LoadOrdersAsync();
        }

        private void ApplyShipment(GetShipment shipment)
        {
            shipmentForm = new UpsertShipmentRequest
            {
                ShipDate = shipment.ShipDate,
                CarrierName = shipment.CarrierName,
                CarrierService = shipment.CarrierService,
                TrackingNumber = shipment.TrackingNumber,
                TrackingUrl = shipment.TrackingUrl,
                Note = shipment.Note,
            };
            shipmentDate = shipment.ShipDate.Date;
        }

        private static bool HasExistingShipmentSnapshot(GetOrder order)
        {
            return !string.IsNullOrWhiteSpace(order.ShippingCarrier)
                || !string.IsNullOrWhiteSpace(order.TrackingNumber)
                || !string.IsNullOrWhiteSpace(order.TrackingUrl)
                || order.ShippedOn.HasValue
                || string.Equals(order.ShippingStatus, "Shipped", StringComparison.OrdinalIgnoreCase)
                || string.Equals(order.ShippingStatus, "Delivered", StringComparison.OrdinalIgnoreCase);
        }

        private async Task PreviousPageAsync()
        {
            pageNumber = Math.Max(1, pageNumber - 1);
            await LoadOrdersAsync();
        }

        private async Task NextPageAsync()
        {
            if (CanNextPage)
            {
                pageNumber++;
                await LoadOrdersAsync();
            }
        }

        private async Task OnSearchKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await SearchAsync();
            }
        }

        private Task OnDrawerChanged(bool value)
        {
            isDrawerOpen = value;
            return Task.CompletedTask;
        }

        private static string StatusTone(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "paid" or "complete" or "completed" or "succeeded" => "success",
                "cancelled" or "failed" => "danger",
                "pending" or "processing" => "warning",
                _ => "neutral",
            };
        }

        private static string PaymentTone(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "paid" or "authorized" => "success",
                "refunded" or "voided" => "danger",
                "pending" or "partially_refunded" => "warning",
                _ => "neutral",
            };
        }

        private static string ShippingTone(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "shipped" or "delivered" => "success",
                "cancelled" or "failed" => "danger",
                "pendingshipment" or "pending" or "processing" => "warning",
                _ => "neutral",
            };
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "-";
        }
    }
}
