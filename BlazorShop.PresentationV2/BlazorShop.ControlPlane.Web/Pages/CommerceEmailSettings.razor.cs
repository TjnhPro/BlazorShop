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

    public partial class CommerceEmailSettings
    {
        private readonly List<StoreSummary> stores = [];
        private Guid? selectedStorePublicId;
        private StoreEmailSettingsResponse? current;
        private EmailSettingsForm form = new();
        private readonly List<MessageTemplateAdminSummary> templates = [];
        private readonly List<QueuedMessageAdminSummary> queuedMessages = [];
        private MessageTemplateAdminDetail? selectedTemplate;
        private TemplateForm templateForm = new();
        private string? rotatePassword;
        private string? testToEmail;
        private string? testSubject;
        private string? previewSubject;
        private string? previewBody;
        private string? queueStatusFilter;
        private string? queueTemplateFilter;
        private string? errorMessage;
        private string? successMessage;
        private bool isLoading;
        private bool isSaving;
        private bool isTesting;
        private bool isLoadingMessages;
        private bool isSavingTemplate;
        private bool isQueueActionRunning;

        private bool HasStore => selectedStorePublicId is not null && selectedStorePublicId != Guid.Empty;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var response = await StoreClient.ListAsync(pageSize: 100);
                stores.Clear();
                stores.AddRange(response.Items.Where(store => !string.Equals(store.Status, "archived", StringComparison.OrdinalIgnoreCase)));
                selectedStorePublicId = stores.FirstOrDefault()?.PublicId;
                await LoadEmailSettingsAsync();
            }
            catch (InvalidOperationException ex)
            {
                errorMessage = ex.Message;
            }
        }

        private async Task LoadEmailSettingsAsync()
        {
            ClearMessages();
            if (!HasStore)
            {
                current = null;
                form = new EmailSettingsForm();
                return;
            }

            isLoading = true;
            try
            {
                var result = await MessageClient.GetEmailSettingsAsync(selectedStorePublicId!.Value);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                current = result.Data;
                form = EmailSettingsForm.From(result.Data);
                await LoadMessageOperationsAsync();
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task SaveAsync()
        {
            if (!HasStore)
            {
                return;
            }

            ClearMessages();
            isSaving = true;
            try
            {
                var result = await MessageClient.UpdateEmailSettingsAsync(selectedStorePublicId!.Value, form.ToRequest());
                ApplyMutationResult(result, "Store email settings saved.");
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task RotatePasswordAsync()
        {
            if (!HasStore || string.IsNullOrWhiteSpace(rotatePassword))
            {
                errorMessage = "New SMTP password is required.";
                return;
            }

            ClearMessages();
            isSaving = true;
            try
            {
                var result = await MessageClient.RotateEmailPasswordAsync(
                    selectedStorePublicId!.Value,
                    new RotateStoreEmailPasswordRequest { Password = rotatePassword });
                rotatePassword = null;
                ApplyMutationResult(result, "SMTP password rotated.");
            }
            finally
            {
                rotatePassword = null;
                isSaving = false;
            }
        }

        private async Task ClearPasswordAsync()
        {
            if (!HasStore)
            {
                return;
            }

            ClearMessages();
            isSaving = true;
            try
            {
                var result = await MessageClient.ClearEmailPasswordAsync(selectedStorePublicId!.Value);
                ApplyMutationResult(result, "SMTP password cleared and store email disabled.");
            }
            finally
            {
                isSaving = false;
            }
        }

        private async Task SendTestAsync()
        {
            if (!HasStore || string.IsNullOrWhiteSpace(testToEmail))
            {
                errorMessage = "Recipient email is required.";
                return;
            }

            ClearMessages();
            isTesting = true;
            try
            {
                var result = await MessageClient.SendEmailTestAsync(
                    selectedStorePublicId!.Value,
                    new SendStoreEmailTestRequest
                    {
                        ToEmail = testToEmail,
                        Subject = testSubject,
                    });
                testToEmail = null;
                testSubject = null;
                successMessage = result.Success
                    ? $"Test email sent at {FormatDate(result.Data?.SentAtUtc)}."
                    : result.Message;
                if (!result.Success)
                {
                    errorMessage = result.Message;
                    successMessage = null;
                }
            }
            finally
            {
                testToEmail = null;
                testSubject = null;
                isTesting = false;
            }
        }

        private void ApplyMutationResult(ControlPlaneClientResult<StoreEmailSettingsResponse> result, string message)
        {
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            current = result.Data;
            form = EmailSettingsForm.From(result.Data);
            successMessage = message;
        }

        private async Task LoadMessageOperationsAsync()
        {
            if (!HasStore)
            {
                return;
            }

            isLoadingMessages = true;
            try
            {
                var templateResult = await MessageClient.ListMessageTemplatesAsync(selectedStorePublicId!.Value);
                if (templateResult.Success && templateResult.Data is not null)
                {
                    templates.Clear();
                    templates.AddRange(templateResult.Data.OrderBy(item => item.SystemName).ThenBy(item => item.LanguageCode));
                }
                else if (!string.IsNullOrWhiteSpace(templateResult.Message))
                {
                    errorMessage = templateResult.Message;
                }

                await LoadQueuedMessagesAsync();
            }
            finally
            {
                isLoadingMessages = false;
            }
        }

        private async Task LoadQueuedMessagesAsync()
        {
            if (!HasStore)
            {
                return;
            }

            var result = await MessageClient.ListQueuedMessagesAsync(
                selectedStorePublicId!.Value,
                queueStatusFilter,
                queueTemplateFilter,
                skip: 0,
                take: 25);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            queuedMessages.Clear();
            queuedMessages.AddRange(result.Data.Items);
        }

        private async Task EditTemplateAsync(Guid templatePublicId)
        {
            if (!HasStore)
            {
                return;
            }

            ClearMessages();
            var result = await MessageClient.GetMessageTemplateAsync(selectedStorePublicId!.Value, templatePublicId);
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            selectedTemplate = result.Data;
            templateForm = TemplateForm.From(result.Data);
            previewSubject = null;
            previewBody = null;
        }

        private async Task SaveTemplateAsync()
        {
            if (!HasStore || selectedTemplate is null)
            {
                return;
            }

            ClearMessages();
            isSavingTemplate = true;
            try
            {
                var result = await MessageClient.UpdateMessageTemplateAsync(
                    selectedStorePublicId!.Value,
                    selectedTemplate.PublicId,
                    templateForm.ToRequest());
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedTemplate = result.Data;
                templateForm = TemplateForm.From(result.Data);
                successMessage = "Message template saved.";
                await LoadMessageOperationsAsync();
            }
            finally
            {
                isSavingTemplate = false;
            }
        }

        private async Task PreviewTemplateAsync()
        {
            if (!HasStore || selectedTemplate is null)
            {
                return;
            }

            ClearMessages();
            var result = await MessageClient.PreviewMessageTemplateAsync(
                selectedStorePublicId!.Value,
                new PreviewMessageTemplateRequest
                {
                    SystemName = selectedTemplate.SystemName,
                    LanguageCode = templateForm.LanguageCode,
                    SubjectTemplate = templateForm.SubjectTemplate,
                    BodyHtmlTemplate = templateForm.BodyHtmlTemplate,
                });
            if (!result.Success || result.Data is null)
            {
                errorMessage = result.Message;
                return;
            }

            previewSubject = result.Data.Subject;
            previewBody = result.Data.BodyHtml;
        }

        private async Task ResetTemplateAsync()
        {
            if (!HasStore || selectedTemplate is null)
            {
                return;
            }

            ClearMessages();
            isSavingTemplate = true;
            try
            {
                var result = await MessageClient.ResetMessageTemplateAsync(selectedStorePublicId!.Value, selectedTemplate.PublicId);
                if (!result.Success || result.Data is null)
                {
                    errorMessage = result.Message;
                    return;
                }

                selectedTemplate = result.Data;
                templateForm = TemplateForm.From(result.Data);
                successMessage = "Message template reset.";
                await LoadMessageOperationsAsync();
            }
            finally
            {
                isSavingTemplate = false;
            }
        }

        private async Task RetryQueuedMessageAsync(Guid publicId)
        {
            if (!HasStore)
            {
                return;
            }

            ClearMessages();
            isQueueActionRunning = true;
            try
            {
                var result = await MessageClient.RetryQueuedMessageAsync(selectedStorePublicId!.Value, publicId);
                successMessage = result.Success ? "Queued message retry requested." : null;
                errorMessage = result.Success ? null : result.Message;
                await LoadQueuedMessagesAsync();
            }
            finally
            {
                isQueueActionRunning = false;
            }
        }

        private async Task CancelQueuedMessageAsync(Guid publicId)
        {
            if (!HasStore)
            {
                return;
            }

            ClearMessages();
            isQueueActionRunning = true;
            try
            {
                var result = await MessageClient.CancelQueuedMessageAsync(selectedStorePublicId!.Value, publicId);
                successMessage = result.Success ? "Queued message canceled." : null;
                errorMessage = result.Success ? null : result.Message;
                await LoadQueuedMessagesAsync();
            }
            finally
            {
                isQueueActionRunning = false;
            }
        }

        private void ClearMessages()
        {
            errorMessage = null;
            successMessage = null;
        }

        private static string FormatDate(DateTimeOffset? value)
        {
            return value is null ? "-" : value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        private static string QueueTone(string? status)
        {
            return status?.ToLowerInvariant() switch
            {
                "sent" => "success",
                "failed" => "danger",
                "waiting_retry" => "warning",
                "canceled" => "neutral",
                _ => "info",
            };
        }

        private sealed class EmailSettingsForm
        {
            public bool Enabled { get; set; }

            public string? SmtpHost { get; set; }

            public int SmtpPort { get; set; } = 587;

            public bool UseSsl { get; set; } = true;

            public string? Username { get; set; }

            public string? FromEmail { get; set; }

            public string? FromDisplayName { get; set; }

            public string? ReplyToEmail { get; set; }

            public string DeliveryMode { get; set; } = "smtp";

            public string? CaptureRedirectToEmail { get; set; }

            public static EmailSettingsForm From(StoreEmailSettingsResponse response)
            {
                return new EmailSettingsForm
                {
                    Enabled = response.Enabled,
                    SmtpHost = response.SmtpHost,
                    SmtpPort = response.SmtpPort,
                    UseSsl = response.UseSsl,
                    Username = response.Username,
                    FromEmail = response.FromEmail,
                    FromDisplayName = response.FromDisplayName,
                    ReplyToEmail = response.ReplyToEmail,
                    DeliveryMode = response.DeliveryMode,
                    CaptureRedirectToEmail = response.CaptureRedirectToEmail,
                };
            }

            public UpdateStoreEmailSettingsRequest ToRequest()
            {
                return new UpdateStoreEmailSettingsRequest
                {
                    Enabled = this.Enabled,
                    SmtpHost = this.SmtpHost,
                    SmtpPort = this.SmtpPort,
                    UseSsl = this.UseSsl,
                    Username = this.Username,
                    UseExistingPassword = true,
                    FromEmail = this.FromEmail,
                    FromDisplayName = this.FromDisplayName,
                    ReplyToEmail = this.ReplyToEmail,
                    DeliveryMode = this.DeliveryMode,
                    CaptureRedirectToEmail = this.CaptureRedirectToEmail,
                };
            }
        }

        private sealed class TemplateForm
        {
            public string SubjectTemplate { get; set; } = string.Empty;

            public string BodyHtmlTemplate { get; set; } = string.Empty;

            public string? Description { get; set; }

            public string? LanguageCode { get; set; }

            public bool IsActive { get; set; } = true;

            public static TemplateForm From(MessageTemplateAdminDetail detail)
            {
                return new TemplateForm
                {
                    SubjectTemplate = detail.SubjectTemplate,
                    BodyHtmlTemplate = detail.BodyHtmlTemplate,
                    Description = detail.Description,
                    LanguageCode = detail.LanguageCode,
                    IsActive = detail.IsActive,
                };
            }

            public UpdateMessageTemplateRequest ToRequest()
            {
                return new UpdateMessageTemplateRequest
                {
                    SubjectTemplate = this.SubjectTemplate,
                    BodyHtmlTemplate = this.BodyHtmlTemplate,
                    Description = this.Description,
                    LanguageCode = this.LanguageCode,
                    IsActive = this.IsActive,
                };
            }
        }
    }
}
