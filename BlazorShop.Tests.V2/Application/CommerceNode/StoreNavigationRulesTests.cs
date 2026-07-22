namespace BlazorShop.Tests.Application.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Navigation;

    using Xunit;

    public sealed class StoreNavigationRulesTests
    {
        [Fact]
        public void MenuNames_AreLimitedToApprovedMvpMenus()
        {
            Assert.Contains(StoreNavigationMenuNames.Main, StoreNavigationMenuNames.All);
            Assert.Contains(StoreNavigationMenuNames.FooterCompany, StoreNavigationMenuNames.All);
            Assert.Contains(StoreNavigationMenuNames.FooterSupport, StoreNavigationMenuNames.All);
            Assert.Contains(StoreNavigationMenuNames.FooterLegal, StoreNavigationMenuNames.All);
            Assert.Contains(StoreNavigationMenuNames.Utility, StoreNavigationMenuNames.All);
            Assert.Contains(StoreNavigationMenuNames.Mobile, StoreNavigationMenuNames.All);
            Assert.False(StoreNavigationMenuNames.IsKnown("mega"));
        }

        [Fact]
        public void TargetTypes_IncludeOnlyApprovedMvpTargetTypes()
        {
            Assert.Contains(StoreNavigationTargetTypes.System, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.Category, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.Page, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.Product, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.ExternalUrl, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.Group, StoreNavigationTargetTypes.All);
            Assert.Contains(StoreNavigationTargetTypes.InternalRoute, StoreNavigationTargetTypes.All);
            Assert.False(StoreNavigationTargetTypes.IsKnown("manufacturer"));
        }

        [Fact]
        public void SystemTargets_ReserveFunctionalStorefrontSlotsWithoutOwningBehavior()
        {
            Assert.Contains(StoreNavigationSystemTargets.Home, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Search, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Cart, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Checkout, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Account, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Login, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.Register, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.NewReleases, StoreNavigationSystemTargets.All);
            Assert.Contains(StoreNavigationSystemTargets.TodaysDeals, StoreNavigationSystemTargets.All);
            Assert.False(StoreNavigationSystemTargets.IsKnown("contact"));
        }
    }
}
