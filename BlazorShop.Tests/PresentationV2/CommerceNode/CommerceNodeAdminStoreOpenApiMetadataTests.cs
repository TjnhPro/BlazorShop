namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using Xunit;

    public sealed class CommerceNodeAdminStoreOpenApiMetadataTests
    {
        [Fact]
        public void CommerceStoreAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceStores_List",
                "CommerceStores_Get",
                "CommerceStores_Create",
                "CommerceStores_Update",
                "CommerceStores_Activate",
                "CommerceStores_Deactivate",
                "CommerceStores_Archive",
                "CommerceStores_AddDomain",
                "CommerceStores_VerifyDomain",
                "CommerceStores_DisableDomain",
                "CommerceStores_SetPrimaryDomain",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("typeof(CommerceNodeApiResponse<CommerceStoreDetail>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<CommerceStoreListResponse>)", source);
            Assert.Contains("requestBody.Required = true", source);
            Assert.Contains("CreateJsonResponse(context, metadata.ResponseType, \"Error.\")", source);
        }

        [Fact]
        public void CommerceStoreAdminSwagger_DocumentsNodeCredentialHeaders()
        {
            var source = ReadCommerceNodeSwaggerSource();

            Assert.Contains("CommerceNodeAdminCredentialHeaderOperationFilter", source);
            Assert.Contains("CommerceNodeCredentialMiddleware.NodeKeyHeaderName", source);
            Assert.Contains("CommerceNodeCredentialMiddleware.NodeSecretHeaderName", source);
        }

        [Fact]
        public void CommerceCurrencyAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceCurrencies_List",
                "CommerceCurrencies_Update",
                "CommerceCurrencies_ListExchangeRates",
                "CommerceCurrencies_ListExchangeRateProviders",
                "CommerceCurrencies_FetchExchangeRates",
                "CommerceCurrencies_QueueExchangeRateUpdateTask",
                "CommerceCurrencies_UpsertExchangeRate",
                "CommerceCurrencies_DisableExchangeRate",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("typeof(CommerceNodeApiResponse<IReadOnlyList<StoreCurrencyExchangeRateDto>>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<IReadOnlyList<StoreCurrencyExchangeRateProviderDto>>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<StoreCurrencyExchangeRateProviderFetchResult>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<CommerceTaskSummary>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<StoreCurrencyExchangeRateDto>)", source);
        }

        [Fact]
        public void CommerceSeoSlugAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceSeoSlugs_Generate",
                "CommerceSeoSlugs_Validate",
                "CommerceSeoSlugs_ListHistory",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("CommerceSeoSlugAdminOperationMetadataFilter", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<StoreSeoSlugPolicyResult>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<IReadOnlyList<StoreSeoSlugHistoryDto>>)", source);
            Assert.Contains("requestBody.Required = true", source);
        }

        [Fact]
        public void CommerceCategoryMediaAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceCategoryMedia_GetPrimary",
                "CommerceCategoryMedia_SetPrimary",
                "CommerceCategoryMedia_ClearPrimary",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("CommerceCategoryMediaAdminOperationMetadataFilter", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<CategoryMediaAssignmentDto>)", source);
            Assert.Contains("requestBody.Required = true", source);
        }

        [Fact]
        public void CommerceShippingSettingsAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceShippingSettings_Get",
                "CommerceShippingSettings_Update",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("CommerceShippingAdminOperationMetadataFilter", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<StoreShippingSettingsDto>)", source);
            Assert.Contains("api/commerce/admin/shipping/settings", source);
            Assert.Contains("requestBody.Required = true", source);
        }

        [Fact]
        public void CommerceTransactionalMessageAdminSwaggerFilter_DefinesStableOperationMetadata()
        {
            var source = ReadCommerceNodeSwaggerSource();

            var operationIds = new[]
            {
                "CommerceMessageTemplates_List",
                "CommerceMessageTemplates_Get",
                "CommerceMessageTemplates_Update",
                "CommerceMessageTemplates_Reset",
                "CommerceMessageTemplates_Preview",
                "CommerceQueuedMessages_List",
                "CommerceQueuedMessages_Get",
                "CommerceQueuedMessages_Retry",
                "CommerceQueuedMessages_Cancel",
            };

            foreach (var operationId in operationIds)
            {
                Assert.Contains(operationId, source);
            }

            Assert.Contains("CommerceTransactionalMessageAdminOperationMetadataFilter", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<MessageTemplateAdminDetail>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<QueuedMessageAdminDetail>)", source);
            Assert.Contains("typeof(CommerceNodeApiResponse<QueuedMessageAdminListResponse>)", source);
            Assert.Contains("requestBody.Required = true", source);
        }

        private static string ReadCommerceNodeSwaggerSource()
        {
            var swaggerDirectory = Path.Combine(
                FindRepositoryRoot(),
                "BlazorShop.PresentationV2",
                "BlazorShop.CommerceNode.API",
                "Swagger");

            return string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(swaggerDirectory, "*.cs")
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .Select(File.ReadAllText));
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
}
