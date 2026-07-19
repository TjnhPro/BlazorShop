namespace BlazorShop.Infrastructure.Data.CommerceNode
{
    using System.Text.Json;

    using BlazorShop.Application.CommerceNode.Media;
    using BlazorShop.Application.CommerceNode.Navigation;
    using BlazorShop.Application.CommerceNode.StorefrontPages;
    using BlazorShop.Domain.Constants;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Domain.Entities.CommerceNode;
    using BlazorShop.Domain.Entities.Identity;
    using BlazorShop.Domain.Entities.Payment;

    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;

    public sealed partial class CommerceNodeDevelopmentSeeder
    {
        private async Task EnsureStorefrontPagesAsync(Guid storeId, CancellationToken cancellationToken)
        {
            await this.EnsureStorefrontPageAsync(
                LegalPageId,
                storeId,
                "qa-legal",
                "QA Legal Page",
                "Published legal page used by release E2E.",
                "<p>This is a synthetic legal page for Storefront release QA.</p>",
                isPublished: true,
                includeInSitemap: true,
                includeInNavigation: true,
                StorefrontPageContentRules.FooterLegal,
                pageKey: "terms_conditions",
                cancellationToken);
            await this.EnsureStorefrontPageAsync(
                CookiePageId,
                storeId,
                "cookies",
                "Cookies",
                "Cookie information for consent QA.",
                "<p>Cookie policy content for development QA.</p>",
                isPublished: true,
                includeInSitemap: true,
                includeInNavigation: true,
                StorefrontPageContentRules.FooterLegal,
                pageKey: "cookie_information",
                cancellationToken);
            await this.EnsureStorefrontPageAsync(
                DraftPageId,
                storeId,
                "qa-unpublished-page",
                "QA Unpublished Page",
                "This page must not be public.",
                "<p>Unpublished content should not leak.</p>",
                isPublished: false,
                includeInSitemap: false,
                includeInNavigation: false,
                navigationLocation: null,
                pageKey: null,
                cancellationToken);
            await this.EnsureStorefrontPageAsync(
                EscapingPageId,
                storeId,
                "qa-escaping-content",
                "QA Escaping <Tag> Page",
                "Published page with encoded script-like text for escaping QA.",
                "<p data-qa=\"escaping-content\">Safe encoded payload: &lt;script&gt;window.__qaXssExecuted=true&lt;/script&gt;</p><p>HTML markup remains controlled while script text stays inert.</p>",
                isPublished: true,
                includeInSitemap: true,
                includeInNavigation: false,
                navigationLocation: null,
                pageKey: "qa_escaping_content",
                cancellationToken);

            await this.EnsureSeoRedirectAsync(
                storeId,
                "/pages/qa-legal-old",
                "/pages/qa-legal",
                "StorefrontPage",
                LegalPageId,
                cancellationToken);
        }

        private async Task EnsureStorefrontPageAsync(
            Guid id,
            Guid storeId,
            string slug,
            string title,
            string intro,
            string bodyHtml,
            bool isPublished,
            bool includeInSitemap,
            bool includeInNavigation,
            string? navigationLocation,
            string? pageKey,
            CancellationToken cancellationToken)
        {
            var page = await this.dbContext.StorefrontPages
                .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (page is null)
            {
                page = new StorefrontPage
                {
                    Id = id,
                    StoreId = storeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StorefrontPages.Add(page);
            }

            page.StoreId = storeId;
            page.PublicId = id switch
            {
                var value when value == LegalPageId => LegalPagePublicId,
                var value when value == CookiePageId => CookiePagePublicId,
                var value when value == DraftPageId => DraftPagePublicId,
                var value when value == EscapingPageId => EscapingPagePublicId,
                _ => page.PublicId,
            };
            page.Slug = slug;
            page.Title = title;
            page.Intro = intro;
            page.BodyHtml = bodyHtml;
            page.IsPublished = isPublished;
            page.IncludeInSitemap = includeInSitemap;
            page.IncludeInNavigation = includeInNavigation;
            page.NavigationLocation = navigationLocation;
            page.PageKey = pageKey;
            page.DisplayOrder = 10;
            page.MetaTitle = title;
            page.MetaDescription = intro;
            page.OgTitle = title;
            page.OgDescription = intro;
            page.RobotsIndex = isPublished;
            page.RobotsFollow = isPublished;
            page.ArchivedAt = null;
            page.UpdatedAt = DateTimeOffset.UtcNow;

            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureSeoRedirectAsync(
            Guid storeId,
            string oldPath,
            string newPath,
            string entityType,
            Guid entityId,
            CancellationToken cancellationToken)
        {
            var redirect = await this.dbContext.SeoRedirects
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.OldPath == oldPath, cancellationToken);
            if (redirect is null)
            {
                this.dbContext.SeoRedirects.Add(new SeoRedirect
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    EntityType = entityType,
                    EntityId = entityId,
                    OldPath = oldPath,
                    NewPath = newPath,
                    StatusCode = SeoConstraints.PermanentRedirectStatusCode,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow,
                });
                await this.dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            redirect.NewPath = newPath;
            redirect.EntityType = entityType;
            redirect.EntityId = entityId;
            redirect.StatusCode = SeoConstraints.PermanentRedirectStatusCode;
            redirect.IsActive = true;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task EnsureNavigationAsync(Guid storeId, CancellationToken cancellationToken)
        {
            var main = await this.EnsureNavigationMenuAsync(
                storeId,
                StoreNavigationMenuNames.Main,
                "Main navigation",
                cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "Home", StoreNavigationTargetTypes.System, StoreNavigationSystemTargets.Home, null, null, 10, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "Apparel", StoreNavigationTargetTypes.Category, null, ApparelCategoryId, null, 20, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "QA Simple", StoreNavigationTargetTypes.Product, null, SimpleProductId, null, 30, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, main.Id, "QA Legal", StoreNavigationTargetTypes.Page, null, LegalPagePublicId, null, 40, cancellationToken);

            var footerLegal = await this.EnsureNavigationMenuAsync(
                storeId,
                StoreNavigationMenuNames.FooterLegal,
                "Footer legal",
                cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, footerLegal.Id, "Terms", StoreNavigationTargetTypes.Page, null, LegalPagePublicId, null, 10, cancellationToken);
            await this.EnsureNavigationItemAsync(storeId, footerLegal.Id, "Cookies", StoreNavigationTargetTypes.Page, null, CookiePagePublicId, null, 20, cancellationToken);
        }

        private async Task<StoreNavigationMenu> EnsureNavigationMenuAsync(
            Guid storeId,
            string systemName,
            string displayName,
            CancellationToken cancellationToken)
        {
            var menu = await this.dbContext.StoreNavigationMenus
                .FirstOrDefaultAsync(item => item.StoreId == storeId && item.SystemName == systemName && item.ArchivedAt == null, cancellationToken);
            if (menu is null)
            {
                menu = new StoreNavigationMenu
                {
                    StoreId = storeId,
                    SystemName = systemName,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreNavigationMenus.Add(menu);
            }

            menu.DisplayName = displayName;
            menu.IsEnabled = true;
            menu.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
            return menu;
        }

        private async Task EnsureNavigationItemAsync(
            Guid storeId,
            Guid menuId,
            string label,
            string targetType,
            string? targetKey,
            Guid? targetEntityPublicId,
            string? url,
            int displayOrder,
            CancellationToken cancellationToken)
        {
            var item = await this.dbContext.StoreNavigationMenuItems.FirstOrDefaultAsync(
                candidate => candidate.StoreId == storeId
                    && candidate.MenuId == menuId
                    && candidate.Label == label
                    && candidate.ArchivedAt == null,
                cancellationToken);
            if (item is null)
            {
                item = new StoreNavigationMenuItem
                {
                    StoreId = storeId,
                    MenuId = menuId,
                    Label = label,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                this.dbContext.StoreNavigationMenuItems.Add(item);
            }

            item.TargetType = targetType;
            item.TargetKey = targetKey;
            item.TargetEntityPublicId = targetEntityPublicId;
            item.Url = url;
            item.IsEnabled = true;
            item.DisplayOrder = displayOrder;
            item.OpensInNewTab = false;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await this.dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
