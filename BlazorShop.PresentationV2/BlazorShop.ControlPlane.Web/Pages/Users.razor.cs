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

    public partial class Users
    {
        private readonly List<UserSummary> users = [];
        private IReadOnlyList<RoleCatalogItem> roleCatalog = [];
        private IReadOnlyList<PermissionCatalogItem> permissionCatalog = [];
        private UserDetail? selectedUser;
        private string? search;
        private string? status;
        private string? roleKey;
        private string? permissionKey;
        private string? successMessage;
        private string? errorMessage;
        private string? createdTemporaryPassword;
        private string? createEmail;
        private string? createDisplayName;
        private string createIdentityRole = "User";
        private string? createRoleKey;
        private string? createPermissionKey;
        private string? createTemporaryPassword;
        private string? editDisplayName;
        private string? assignRoleKey;
        private string? assignPermissionKey;
        private bool isLoading;
        private bool isSaving;
        private bool showCreateForm;
        private const int UserPageSize = 25;
        private int userPageNumber = 1;
        private int userTotalCount;
        private int userTotalPages;

        private bool CanLoadNextUsersPage => userPageNumber < userTotalPages;

        protected override async Task OnInitializedAsync()
        {
            await LoadInitialAsync();
        }

        private async Task LoadInitialAsync()
        {
            try
            {
                isLoading = true;
                errorMessage = null;
                var rolesTask = UserClient.GetRoleCatalogAsync();
                var permissionsTask = UserClient.GetPermissionCatalogAsync();
                var usersTask = UserClient.ListAsync(search, status, roleKey, permissionKey, userPageNumber, UserPageSize);

                await Task.WhenAll(rolesTask, permissionsTask, usersTask);

                roleCatalog = rolesTask.Result.Items;
                permissionCatalog = permissionsTask.Result.Items;
                users.Clear();
                users.AddRange(usersTask.Result.Items);
                userTotalCount = usersTask.Result.TotalCount;
                userTotalPages = usersTask.Result.TotalPages;
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

        private async Task ApplyFiltersAsync()
        {
            selectedUser = null;
            userPageNumber = 1;
            await LoadInitialAsync();
        }

        private async Task PreviousUsersPageAsync()
        {
            if (userPageNumber <= 1)
            {
                return;
            }

            userPageNumber--;
            await LoadInitialAsync();
        }

        private async Task NextUsersPageAsync()
        {
            if (!CanLoadNextUsersPage)
            {
                return;
            }

            userPageNumber++;
            await LoadInitialAsync();
        }

        private async Task LoadDetailAsync(Guid publicId)
        {
            try
            {
                errorMessage = null;
                selectedUser = await UserClient.GetAsync(publicId);
                editDisplayName = selectedUser?.DisplayName;
                assignRoleKey = null;
                assignPermissionKey = null;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        private void OpenCreateForm()
        {
            showCreateForm = true;
            successMessage = null;
            errorMessage = null;
            createdTemporaryPassword = null;
        }

        private void CloseCreateForm()
        {
            showCreateForm = false;
        }

        private async Task CreateUserAsync()
        {
            try
            {
                isSaving = true;
                errorMessage = null;
                successMessage = null;
                createdTemporaryPassword = null;

                var request = new CreateUserRequest(
                    createEmail ?? string.Empty,
                    createDisplayName ?? string.Empty,
                    createIdentityRole,
                    string.IsNullOrWhiteSpace(createRoleKey) ? [] : [createRoleKey],
                    string.IsNullOrWhiteSpace(createPermissionKey) ? [] : [createPermissionKey],
                    string.IsNullOrWhiteSpace(createTemporaryPassword) ? null : createTemporaryPassword);

                var result = await UserClient.CreateAsync(request);
                if (!result.Success || result.Payload is null)
                {
                    errorMessage = result.Message ?? "Unable to create user.";
                    return;
                }

                createdTemporaryPassword = result.Payload.TemporaryPassword;
                successMessage = "User created.";
                showCreateForm = false;
                createEmail = null;
                createDisplayName = null;
                createIdentityRole = "User";
                createRoleKey = null;
                createPermissionKey = null;
                createTemporaryPassword = null;
                await LoadInitialAsync();
                await LoadDetailAsync(result.Payload.User.PublicId);
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

        private async Task UpdateSelectedAsync()
        {
            if (selectedUser is null)
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.UpdateAsync(selectedUser.PublicId, new UpdateUserRequest(editDisplayName ?? string.Empty)),
                "User updated.");
        }

        private async Task DisableSelectedAsync()
        {
            if (selectedUser is null)
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.DisableAsync(selectedUser.PublicId, new ChangeUserStatusRequest("Disabled from User Management UI.")),
                "User disabled.");
        }

        private async Task EnableSelectedAsync()
        {
            if (selectedUser is null)
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.EnableAsync(selectedUser.PublicId, new ChangeUserStatusRequest("Enabled from User Management UI.")),
                "User enabled.");
        }

        private async Task AssignRoleAsync()
        {
            if (selectedUser is null || string.IsNullOrWhiteSpace(assignRoleKey))
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.AssignRoleAsync(selectedUser.PublicId, new AssignRoleRequest(assignRoleKey)),
                "Role assigned.");
            assignRoleKey = null;
        }

        private async Task RemoveRoleAsync(string key)
        {
            if (selectedUser is null)
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.RemoveRoleAsync(selectedUser.PublicId, key),
                "Role removed.");
        }

        private async Task AssignPermissionAsync()
        {
            if (selectedUser is null || string.IsNullOrWhiteSpace(assignPermissionKey))
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.AssignPermissionAsync(selectedUser.PublicId, new AssignPermissionRequest(assignPermissionKey)),
                "Permission assigned.");
            assignPermissionKey = null;
        }

        private async Task RemovePermissionAsync(string key)
        {
            if (selectedUser is null)
            {
                return;
            }

            await RunSelectedMutationAsync(
                () => UserClient.RemovePermissionAsync(selectedUser.PublicId, key),
                "Permission removed.");
        }

        private async Task RunSelectedMutationAsync(Func<Task<UserMutationResult>> mutation, string message)
        {
            if (selectedUser is null)
            {
                return;
            }

            var publicId = selectedUser.PublicId;

            try
            {
                isSaving = true;
                errorMessage = null;
                successMessage = null;
                var result = await mutation();

                if (!result.Success || result.User is null)
                {
                    errorMessage = result.Message ?? "Unable to update user.";
                    return;
                }

                successMessage = message;
                await LoadInitialAsync();
                selectedUser = result.User;
                editDisplayName = selectedUser.DisplayName;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                selectedUser = await UserClient.GetAsync(publicId);
            }
            finally
            {
                isSaving = false;
            }
        }

        private static string FormatCount(int count, string label)
        {
            return count == 1 ? $"1 {label}" : $"{count} {label}";
        }

        private static string FormatDate(DateTimeOffset? value)
        {
            return value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "Never";
        }

        private static string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "active" => "rounded-full bg-green-50 px-2 py-1 text-xs font-semibold text-green-700",
                "disabled" => "rounded-full bg-ink-100 px-2 py-1 text-xs font-semibold text-ink-700",
                "invited" => "rounded-full bg-blue-50 px-2 py-1 text-xs font-semibold text-blue-700",
                _ => "rounded-full bg-ink-100 px-2 py-1 text-xs font-semibold text-ink-700"
            };
        }

        private static int DisplayTotalPages(int totalPages)
        {
            return Math.Max(1, totalPages);
        }
    }
}
