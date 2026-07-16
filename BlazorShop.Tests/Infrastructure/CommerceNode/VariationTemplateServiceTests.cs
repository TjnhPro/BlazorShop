namespace BlazorShop.Tests.Infrastructure.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.CommerceNode.VariationTemplates;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Admin.Audit;
    using BlazorShop.Application.Services;
    using BlazorShop.Application.Services.Contracts.Admin;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Contracts;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.EntityFrameworkCore;

    using Xunit;

    public sealed class VariationTemplateServiceTests
    {
        [Fact]
        public async Task CreateOptionAsync_RejectsUnknownControlType()
        {
            await using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var template = await SeedTemplateAsync(context, storeId);
            var service = CreateService(context, storeId);

            var result = await service.CreateOptionAsync(
                template.Id,
                new CreateVariationTemplateOptionRequest("Color", ControlType: "swatch"));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal("Variation option control type is invalid.", result.Message);
        }

        [Fact]
        public async Task CreateValueAsync_NormalizesColorHex_ForColorOption()
        {
            await using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var template = await SeedTemplateAsync(context, storeId, new VariationTemplateOption
            {
                Name = "Color",
                ControlType = VariationControlTypes.Color,
                IsRequired = true,
            });
            var option = template.Options.Single();
            var service = CreateService(context, storeId);

            var result = await service.CreateValueAsync(
                template.Id,
                option.Id,
                new CreateVariationTemplateValueRequest("Red", ColorHex: "ff0000"));

            Assert.True(result.Success);
            var value = Assert.Single(result.Payload!.Options.Single().Values);
            Assert.Equal("#FF0000", value.ColorHex);
        }

        [Theory]
        [InlineData(VariationControlTypes.Dropdown, "#FF0000", "Color hex is only allowed for color variation options.")]
        [InlineData(VariationControlTypes.Color, "#FFF", "Color hex must be a 6-digit hex color.")]
        public async Task CreateValueAsync_RejectsInvalidColorHex(string controlType, string colorHex, string expectedMessage)
        {
            await using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var template = await SeedTemplateAsync(context, storeId, new VariationTemplateOption
            {
                Name = "Color",
                ControlType = controlType,
                IsRequired = true,
            });
            var option = template.Options.Single();
            var service = CreateService(context, storeId);

            var result = await service.CreateValueAsync(
                template.Id,
                option.Id,
                new CreateVariationTemplateValueRequest("Red", ColorHex: colorHex));

            Assert.False(result.Success);
            Assert.Equal(ServiceResponseType.ValidationError, result.ResponseType);
            Assert.Equal(expectedMessage, result.Message);
        }

        [Fact]
        public async Task UpdateOptionAsync_ClearsColorHex_WhenControlTypeChangesAwayFromColor()
        {
            await using var context = CreateContext();
            var storeId = Guid.NewGuid();
            var value = new VariationTemplateValue { Value = "Red", ColorHex = "#FF0000" };
            var template = await SeedTemplateAsync(context, storeId, new VariationTemplateOption
            {
                Name = "Color",
                ControlType = VariationControlTypes.Color,
                IsRequired = true,
                Values = { value },
            });
            var option = template.Options.Single();
            var service = CreateService(context, storeId);

            var result = await service.UpdateOptionAsync(
                template.Id,
                option.Id,
                new UpdateVariationTemplateOptionRequest(
                    "Color",
                    ControlType: VariationControlTypes.Dropdown,
                    IsRequired: false));

            Assert.True(result.Success);
            var updatedOption = Assert.Single(result.Payload!.Options);
            Assert.Equal(VariationControlTypes.Dropdown, updatedOption.ControlType);
            Assert.False(updatedOption.IsRequired);
            Assert.Null(Assert.Single(updatedOption.Values).ColorHex);
        }

        private static VariationTemplateService CreateService(CommerceNodeDbContext context, Guid storeId)
        {
            return new VariationTemplateService(
                context,
                new StubCommerceStoreContext(storeId),
                new SlugService(),
                new NoopAdminAuditService());
        }

        private static async Task<VariationTemplate> SeedTemplateAsync(
            CommerceNodeDbContext context,
            Guid storeId,
            VariationTemplateOption? option = null)
        {
            var template = new VariationTemplate
            {
                StoreId = storeId,
                Name = "Shirt options",
                Slug = "shirt-options",
                IsActive = true,
            };

            if (option is not null)
            {
                template.Options.Add(option);
            }

            context.VariationTemplates.Add(template);
            await context.SaveChangesAsync();
            return template;
        }

        private static CommerceNodeDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<CommerceNodeDbContext>()
                .UseInMemoryDatabase($"variation-template-service-{Guid.NewGuid():N}")
                .Options;

            return new CommerceNodeDbContext(options);
        }

        private sealed class StubCommerceStoreContext : ICommerceStoreContext
        {
            private readonly Guid storeId;

            public StubCommerceStoreContext(Guid storeId)
            {
                this.storeId = storeId;
            }

            public Task<CommerceStoreOperationResult<CommerceCurrentStore>> GetCurrentStoreAsync(
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<CommerceStoreOperationResult<Guid>> GetCurrentStoreIdAsync(
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", this.storeId));
            }
        }

        private sealed class NoopAdminAuditService : IAdminAuditService
        {
            public Task<PagedResult<AdminAuditLogDto>> GetAsync(AdminAuditQueryDto query)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> GetByIdAsync(Guid id)
            {
                throw new NotSupportedException();
            }

            public Task<ServiceResponse<AdminAuditLogDto>> LogAsync(CreateAdminAuditLogDto request)
            {
                return Task.FromResult(new ServiceResponse<AdminAuditLogDto>(true, "Audit logged."));
            }
        }
    }
}
