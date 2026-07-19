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

    public partial class Stores
    {
        private readonly List<NodeSummary> nodes = [];
        private readonly List<StoreSummary> stores = [];
        private StoreDetail? selectedStore;
        private RuntimeStoreDetail? runtimeStore;
        private string? search;
        private string? status;
        private string? errorMessage;
        private string? successMessage;
        private bool isLoading;
        private bool isSaving;
        private bool showCreateForm;
        private string createStoreKey = string.Empty;
        private string createName = string.Empty;
        private string createNodePublicId = string.Empty;
        private string createMetadataJson = "{}";
        private string editName = string.Empty;
        private string editNodePublicId = string.Empty;
        private string editMetadataJson = "{}";
        private string newDomain = string.Empty;
        private string deployPrimaryDomain = string.Empty;
        private string deployStorefrontImage = "storefront-v2";
        private string deployCurrency = "USD";
        private string deployCulture = "en-US";
        private string deployBaseUrl = string.Empty;
        private string deployNetworkName = string.Empty;
        private StoreDeploymentTaskSummary? lastDeploymentTask;
        private bool runtimeMaintenanceEnabled;
        private int runtimeDisplayOrder;
        private string runtimeName = string.Empty;
        private string runtimeMaintenanceMessage = string.Empty;
        private string runtimeBaseUrl = string.Empty;
        private string runtimeCdnHost = string.Empty;
        private string runtimeLogoUrl = string.Empty;
        private string runtimeFaviconUrl = string.Empty;
        private string runtimePngIconUrl = string.Empty;
        private string runtimeAppleTouchIconUrl = string.Empty;
        private string runtimeMsTileImageUrl = string.Empty;
        private string runtimeMsTileColor = string.Empty;
        private string runtimeDefaultCurrencyCode = "USD";
        private string runtimeDefaultCulture = "en-US";
        private string runtimeCompanyName = string.Empty;
        private string runtimeCompanyEmail = string.Empty;
        private string runtimeCompanyPhone = string.Empty;
        private string runtimeCompanyAddress = string.Empty;
        private string runtimeSupportEmail = string.Empty;
        private string runtimeSupportPhone = string.Empty;
        private const int StorePageSize = 25;
        private int storePageNumber = 1;
        private int storeTotalCount;
        private int storeTotalPages;

        private bool CanLoadNextStoresPage => storePageNumber < storeTotalPages;

        protected override async Task OnInitializedAsync()
        {
            await LoadNodesAsync();
            await LoadStoresAsync();
        }

        private async Task LoadNodesAsync()
        {
            try
            {
                var response = await NodeClient.ListAsync(pageSize: 100);
                nodes.Clear();
                nodes.AddRange(response.Items.Where(node => node.Status != "disabled"));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        private async Task LoadStoresAsync()
        {
            isLoading = true;
            errorMessage = null;

            try
            {
                var response = await StoreClient.ListAsync(search, status, pageNumber: storePageNumber, pageSize: StorePageSize);
                stores.Clear();
                stores.AddRange(response.Items);
                storeTotalCount = response.TotalCount;
                storeTotalPages = response.TotalPages;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SearchStoresAsync()
        {
            storePageNumber = 1;
            await LoadStoresAsync();
        }

        private void OpenCreateForm()
        {
            showCreateForm = true;
            createNodePublicId = nodes.FirstOrDefault()?.PublicId.ToString() ?? string.Empty;
        }

        private void CloseCreateForm()
        {
            showCreateForm = false;
        }

        private async Task CreateStoreAsync()
        {
            if (!Guid.TryParse(createNodePublicId, out var nodePublicId))
            {
                errorMessage = "Select a node.";
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.CreateAsync(new StoreCreateRequest(createStoreKey, createName, nodePublicId, createMetadataJson));
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedStore = result.Store;
                SyncEditFields();
                successMessage = "Store created.";
                showCreateForm = false;
                createStoreKey = string.Empty;
                createName = string.Empty;
                createMetadataJson = "{}";
                storePageNumber = 1;
            });
        }

        private async Task LoadDetailAsync(Guid publicId)
        {
            errorMessage = null;
            selectedStore = await StoreClient.GetAsync(publicId);
            runtimeStore = selectedStore is null
                ? null
                : await StoreClient.GetRuntimeStoreAsync(publicId);
            SyncEditFields();
        }

        private async Task UpdateSelectedAsync()
        {
            if (selectedStore is null)
            {
                return;
            }

            if (!Guid.TryParse(editNodePublicId, out var nodePublicId))
            {
                errorMessage = "Select a node.";
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.UpdateAsync(selectedStore.PublicId, new StoreUpdateRequest(editName, nodePublicId, editMetadataJson));
                selectedStore = result.Store ?? selectedStore;
                SyncEditFields();
                successMessage = result.Success ? "Store updated." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task AddDomainAsync()
        {
            if (selectedStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.AddDomainAsync(selectedStore.PublicId, new StoreDomainCreateRequest(newDomain));
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedStore = result.Store;
                SyncEditFields();
                newDomain = string.Empty;
                successMessage = "Domain added.";
            });
        }

        private async Task VerifyDomainAsync(long domainId)
        {
            if (selectedStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.VerifyDomainAsync(selectedStore.PublicId, domainId);
                selectedStore = result.Store ?? selectedStore;
                SyncEditFields();
                successMessage = result.Success ? "Domain verified." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task DisableDomainAsync(long domainId)
        {
            if (selectedStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.DisableDomainAsync(selectedStore.PublicId, domainId);
                selectedStore = result.Store ?? selectedStore;
                SyncEditFields();
                successMessage = result.Success ? "Domain disabled." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task ArchiveSelectedAsync()
        {
            if (selectedStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.ArchiveAsync(selectedStore.PublicId);
                selectedStore = result.Store ?? selectedStore;
                SyncEditFields();
                successMessage = result.Success ? "Store archived." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task SubmitDeploymentAsync()
        {
            if (selectedStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.DeployAsync(
                    selectedStore.PublicId,
                    new StoreDeploymentRequest(
                        deployStorefrontImage,
                        EmptyToNull(deployPrimaryDomain),
                        EmptyToNull(deployBaseUrl),
                        string.IsNullOrWhiteSpace(deployCurrency) ? "USD" : deployCurrency.Trim(),
                        string.IsNullOrWhiteSpace(deployCulture) ? "en-US" : deployCulture.Trim(),
                        EmptyToNull(deployNetworkName)));

                if (!result.Success)
                {
                    errorMessage = result.Message;
                    return;
                }

                lastDeploymentTask = result.Task;
                successMessage = result.Task is null
                    ? "Deployment task submitted."
                    : $"Deployment task submitted: {result.Task.Status}.";
            });
        }

        private async Task SaveRuntimeStoreAsync()
        {
            if (selectedStore is null || runtimeStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.UpdateRuntimeStoreAsync(
                    selectedStore.PublicId,
                    runtimeStore.PublicId,
                    new RuntimeStoreUpdateRequest(
                        string.IsNullOrWhiteSpace(runtimeName) ? runtimeStore.Name : runtimeName.Trim(),
                        EmptyToNull(runtimeBaseUrl),
                        runtimeStore.ForceHttps,
                        runtimeStore.SslEnabled,
                        runtimeStore.SslPort,
                        runtimeDisplayOrder,
                        runtimeStore.HtmlBodyId,
                        EmptyToNull(runtimeCdnHost),
                        EmptyToNull(runtimeLogoUrl),
                        EmptyToNull(runtimeCompanyName),
                        EmptyToNull(runtimeCompanyEmail),
                        EmptyToNull(runtimeCompanyPhone),
                        EmptyToNull(runtimeCompanyAddress),
                        EmptyToNull(runtimeFaviconUrl),
                        EmptyToNull(runtimePngIconUrl),
                        EmptyToNull(runtimeAppleTouchIconUrl),
                        EmptyToNull(runtimeMsTileImageUrl),
                        EmptyToNull(runtimeMsTileColor),
                        string.IsNullOrWhiteSpace(runtimeDefaultCurrencyCode) ? runtimeStore.DefaultCurrencyCode : runtimeDefaultCurrencyCode.Trim().ToUpperInvariant(),
                        string.IsNullOrWhiteSpace(runtimeDefaultCulture) ? runtimeStore.DefaultCulture : runtimeDefaultCulture.Trim(),
                        EmptyToNull(runtimeSupportEmail),
                        EmptyToNull(runtimeSupportPhone),
                        runtimeMaintenanceEnabled,
                        EmptyToNull(runtimeMaintenanceMessage),
                        runtimeStore.MetadataJson,
                        runtimeStore.Status));

                runtimeStore = result.Store ?? runtimeStore;
                SyncRuntimeFields();
                successMessage = result.Success ? "Runtime store updated." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task ActivateRuntimeStoreAsync()
        {
            if (selectedStore is null || runtimeStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.ActivateRuntimeStoreAsync(selectedStore.PublicId, runtimeStore.PublicId);
                runtimeStore = result.Store ?? runtimeStore;
                SyncRuntimeFields();
                successMessage = result.Success ? "Runtime store activated." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task DeactivateRuntimeStoreAsync()
        {
            if (selectedStore is null || runtimeStore is null)
            {
                return;
            }

            await SaveStoreOperationAsync(async () =>
            {
                var result = await StoreClient.DeactivateRuntimeStoreAsync(selectedStore.PublicId, runtimeStore.PublicId);
                runtimeStore = result.Store ?? runtimeStore;
                SyncRuntimeFields();
                successMessage = result.Success ? "Runtime store deactivated." : null;
                errorMessage = result.Success ? null : result.Message;
            });
        }

        private async Task SaveStoreOperationAsync(Func<Task> operation)
        {
            isSaving = true;
            errorMessage = null;
            successMessage = null;

            try
            {
                await operation();
                await LoadStoresAsync();
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task PreviousStoresPageAsync()
        {
            if (storePageNumber <= 1)
            {
                return;
            }

            storePageNumber--;
            await LoadStoresAsync();
        }

        private async Task NextStoresPageAsync()
        {
            if (!CanLoadNextStoresPage)
            {
                return;
            }

            storePageNumber++;
            await LoadStoresAsync();
        }

        private static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "active" => "rounded-full bg-green-50 px-2 py-1 text-xs font-semibold text-green-700",
                "verified" => "rounded-full bg-green-50 px-2 py-1 text-xs font-semibold text-green-700",
                "provisioning" => "rounded-full bg-blue-50 px-2 py-1 text-xs font-semibold text-blue-700",
                "pending" => "rounded-full bg-amber-50 px-2 py-1 text-xs font-semibold text-amber-700",
                "disabled" => "rounded-full bg-ink-100 px-2 py-1 text-xs font-semibold text-ink-600",
                "archived" => "rounded-full bg-ink-100 px-2 py-1 text-xs font-semibold text-ink-600",
                _ => "rounded-full bg-blue-50 px-2 py-1 text-xs font-semibold text-blue-700"
            };
        }

        private void SyncEditFields()
        {
            if (selectedStore is null)
            {
                editName = string.Empty;
                editNodePublicId = string.Empty;
                editMetadataJson = "{}";
                deployPrimaryDomain = string.Empty;
                runtimeStore = null;
                SyncRuntimeFields();
                lastDeploymentTask = null;
                return;
            }

            editName = selectedStore.Name;
            editNodePublicId = selectedStore.NodePublicId.ToString();
            editMetadataJson = selectedStore.MetadataJson ?? "{}";
            deployPrimaryDomain = selectedStore.Domains.FirstOrDefault(domain => domain.Status == "verified")?.Domain
                ?? selectedStore.Domains.FirstOrDefault()?.Domain
                ?? $"{selectedStore.StoreKey}.local";
            lastDeploymentTask = null;
            SyncRuntimeFields();
        }

        private void SyncRuntimeFields()
        {
            runtimeMaintenanceEnabled = runtimeStore?.MaintenanceModeEnabled ?? false;
            runtimeDisplayOrder = runtimeStore?.DisplayOrder ?? 0;
            runtimeName = runtimeStore?.Name ?? string.Empty;
            runtimeMaintenanceMessage = runtimeStore?.MaintenanceMessage ?? string.Empty;
            runtimeBaseUrl = runtimeStore?.BaseUrl ?? string.Empty;
            runtimeCdnHost = runtimeStore?.CdnHost ?? string.Empty;
            runtimeLogoUrl = runtimeStore?.LogoUrl ?? string.Empty;
            runtimeFaviconUrl = runtimeStore?.FaviconUrl ?? string.Empty;
            runtimePngIconUrl = runtimeStore?.PngIconUrl ?? string.Empty;
            runtimeAppleTouchIconUrl = runtimeStore?.AppleTouchIconUrl ?? string.Empty;
            runtimeMsTileImageUrl = runtimeStore?.MsTileImageUrl ?? string.Empty;
            runtimeMsTileColor = runtimeStore?.MsTileColor ?? string.Empty;
            runtimeDefaultCurrencyCode = runtimeStore?.DefaultCurrencyCode ?? "USD";
            runtimeDefaultCulture = runtimeStore?.DefaultCulture ?? "en-US";
            runtimeCompanyName = runtimeStore?.CompanyName ?? string.Empty;
            runtimeCompanyEmail = runtimeStore?.CompanyEmail ?? string.Empty;
            runtimeCompanyPhone = runtimeStore?.CompanyPhone ?? string.Empty;
            runtimeCompanyAddress = runtimeStore?.CompanyAddress ?? string.Empty;
            runtimeSupportEmail = runtimeStore?.SupportEmail ?? string.Empty;
            runtimeSupportPhone = runtimeStore?.SupportPhone ?? string.Empty;
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static bool CanPreviewImage(string? value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out var uri)
                && uri.Scheme is "http" or "https";
        }

        private static bool CanPreviewTileColor(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var candidate = value.Trim();
            return System.Text.RegularExpressions.Regex.IsMatch(candidate, "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
                || candidate.Equals("transparent", StringComparison.OrdinalIgnoreCase)
                || candidate.Equals("black", StringComparison.OrdinalIgnoreCase)
                || candidate.Equals("white", StringComparison.OrdinalIgnoreCase);
        }

        private static int DisplayTotalPages(int totalPages)
        {
            return Math.Max(1, totalPages);
        }
    }
}
