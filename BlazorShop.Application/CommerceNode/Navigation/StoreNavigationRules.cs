namespace BlazorShop.Application.CommerceNode.Navigation
{
    public static class StoreNavigationMenuNames
    {
        public const string Main = "main";
        public const string FooterCompany = "footer_company";
        public const string FooterSupport = "footer_support";
        public const string FooterLegal = "footer_legal";
        public const string Utility = "utility";
        public const string Mobile = "mobile";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Main,
            FooterCompany,
            FooterSupport,
            FooterLegal,
            Utility,
            Mobile,
        };

        public static bool IsKnown(string value)
        {
            return All.Contains(value);
        }
    }

    public static class StoreNavigationTargetTypes
    {
        public const string System = "system";
        public const string Category = "category";
        public const string Page = "page";
        public const string Product = "product";
        public const string ExternalUrl = "external_url";
        public const string Group = "group";
        public const string InternalRoute = "internal_route";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            System,
            Category,
            Page,
            Product,
            ExternalUrl,
            Group,
            InternalRoute,
        };

        public static bool IsKnown(string value)
        {
            return All.Contains(value);
        }
    }

    public static class StoreNavigationSystemTargets
    {
        public const string Home = "home";
        public const string Search = "search";
        public const string Cart = "cart";
        public const string Checkout = "checkout";
        public const string Account = "account";
        public const string Login = "login";
        public const string Register = "register";
        public const string NewReleases = "new_releases";
        public const string TodaysDeals = "todays_deals";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Home,
            Search,
            Cart,
            Checkout,
            Account,
            Login,
            Register,
            NewReleases,
            TodaysDeals,
        };

        public static bool IsKnown(string value)
        {
            return All.Contains(value);
        }
    }

    public static class StoreNavigationInternalRoutes
    {
        public const string Home = "home";
        public const string Search = "search";
        public const string NewReleases = "new_releases";
        public const string TodaysDeals = "todays_deals";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Home,
            Search,
            NewReleases,
            TodaysDeals,
        };

        public static bool IsKnown(string value)
        {
            return All.Contains(value);
        }
    }
}
