namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontPresentationVisualNeutralityTests
    {
        private static readonly string[] TailwindVisualTokens =
        [
            "bg-",
            "rounded-",
            "shadow-",
            "ring-",
            "border-neutral-",
            "border-zinc-",
            "border-slate-",
            "text-neutral-",
            "text-zinc-",
            "text-slate-",
            "text-red-",
            "text-green-",
            "text-emerald-",
            "text-amber-",
            "text-blue-",
            "hover:bg-",
            "focus:ring-",
            "sm:",
            "md:",
            "lg:",
            "xl:",
            "2xl:",
        ];

        private static readonly string[] InlineVisualStyleTokens =
        [
            "style=\"background",
            "style=\"color",
            "style=\"font",
            "style=\"padding",
            "style=\"margin",
            "style=\"box-shadow",
        ];

        private static readonly string[] ForbiddenThemeAssetExtensions =
        [
            ".css",
            ".scss",
            ".sass",
            ".less",
            ".woff",
            ".woff2",
            ".ttf",
            ".otf",
            ".png",
            ".jpg",
            ".jpeg",
            ".svg",
        ];

        [Fact]
        public void PresentationRazorAndCSharpSources_DoNotContainTailwindVisualTokens()
        {
            var violations = EnumeratePresentationSourceFiles()
                .Where(file => file.Extension.Equals(".razor", StringComparison.OrdinalIgnoreCase)
                    || file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                .SelectMany(file => FindTokenViolations(file, TailwindVisualTokens))
                .OrderBy(violation => violation, StringComparer.Ordinal)
                .ToArray();

            AssertNoViolations(violations);
        }

        [Fact]
        public void PresentationRazorSources_DoNotContainInlineVisualStyles()
        {
            var violations = EnumeratePresentationSourceFiles()
                .Where(file => file.Extension.Equals(".razor", StringComparison.OrdinalIgnoreCase))
                .SelectMany(file => FindTokenViolations(file, InlineVisualStyleTokens))
                .OrderBy(violation => violation, StringComparer.Ordinal)
                .ToArray();

            AssertNoViolations(violations);
        }

        [Fact]
        public void PresentationProject_DoesNotOwnThemeCssTailwindConfigFontsOrImages()
        {
            var presentationRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation");
            var violations = Directory
                .EnumerateFiles(presentationRoot, "*", SearchOption.AllDirectories)
                .Select(path => new SourceFile(path, ToRepositoryRelativePath(path), Path.GetExtension(path)))
                .Where(file => !IsExcluded(file.RelativePath))
                .Where(file => IsForbiddenThemeAsset(file))
                .Select(file => file.RelativePath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            AssertNoViolations(violations);
        }

        [Fact]
        public void CuratedPresentationScanner_ExcludesBuildOutputGeneratedOutputDocsAndFixtures()
        {
            var sources = EnumeratePresentationSourceFiles();

            Assert.All(
                sources,
                source =>
                {
                    Assert.False(source.RelativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase), source.RelativePath);
                    Assert.False(source.RelativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase), source.RelativePath);
                    Assert.False(source.RelativePath.Contains("/docs/", StringComparison.OrdinalIgnoreCase), source.RelativePath);
                    Assert.False(source.RelativePath.Contains("/Fixtures/", StringComparison.OrdinalIgnoreCase), source.RelativePath);
                    Assert.False(source.RelativePath.Contains("/generated/", StringComparison.OrdinalIgnoreCase), source.RelativePath);
                });

            Assert.Contains(sources, source => source.RelativePath.EndsWith("BlazorShop.Storefront.Presentation.csproj", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(sources, source => source.RelativePath.EndsWith("AccountRoutePage.razor", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(sources, source => source.RelativePath.EndsWith("storefront.application.js", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ContentPagePresentationResolver_ExposesSemanticPresentationOnly()
        {
            var resolver = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Content/StorefrontPagePresentationResolver.cs");

            Assert.DoesNotContain("ArticleClass", resolver, StringComparison.Ordinal);
            Assert.DoesNotContain("BodyContainerClass", resolver, StringComparison.Ordinal);
            Assert.DoesNotContain("Class,", resolver, StringComparison.Ordinal);
            Assert.DoesNotContain("CssClass", resolver, StringComparison.Ordinal);
            Assert.DoesNotContain("Tailwind", resolver, StringComparison.Ordinal);

            foreach (var token in TailwindVisualTokens)
            {
                Assert.DoesNotContain(token, resolver, StringComparison.Ordinal);
            }

            Assert.Contains("string TemplateKey", resolver, StringComparison.Ordinal);
            Assert.Contains("StorefrontPageLayoutKind LayoutKind", resolver, StringComparison.Ordinal);
            Assert.Contains("StorefrontPageStructuredDataKind StructuredDataKind", resolver, StringComparison.Ordinal);
        }

        [Fact]
        public void PaymentResultPageService_ExposesSemanticOutcomeOnly()
        {
            var service = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageService.cs");
            var context = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Services/Checkout/StorefrontPaymentResultPageContext.cs");

            foreach (var removedProperty in new[]
            {
                "PanelClass",
                "EyebrowClass",
                "HeadingClass",
                "BodyClass",
                "MutedClass",
            })
            {
                Assert.DoesNotContain(removedProperty, service, StringComparison.Ordinal);
                Assert.DoesNotContain(removedProperty, context, StringComparison.Ordinal);
            }

            foreach (var forbiddenToken in new[]
            {
                "rounded-3xl border border-amber",
                "rounded-3xl border border-emerald",
                "rounded-3xl border border-rose",
                "text-amber-700",
                "text-emerald-700",
                "text-rose-700",
            })
            {
                Assert.DoesNotContain(forbiddenToken, service, StringComparison.Ordinal);
            }

            Assert.Contains("StorefrontPaymentResultOutcome Outcome", context, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultOutcome.Success", service, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultOutcome.Pending", service, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultOutcome.Cancelled", service, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultOutcome.Unavailable", service, StringComparison.Ordinal);
        }

        [Fact]
        public void AccountUnauthorizedFallback_IsClasslessAndSemanticOnly()
        {
            var route = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Pages/WasmHost/Account/AccountRoutePage.razor");
            var unauthorizedContent = ExtractBetween(route, "<UnauthorizedContent>", "</UnauthorizedContent>");

            Assert.Contains("data-storefront-account-redirect", unauthorizedContent, StringComparison.Ordinal);
            Assert.DoesNotContain("class=", unauthorizedContent, StringComparison.Ordinal);
            Assert.DoesNotContain("style=", unauthorizedContent, StringComparison.Ordinal);

            foreach (var token in TailwindVisualTokens)
            {
                Assert.DoesNotContain(token, unauthorizedContent, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void PresentationFormClassParameters_DefaultToEmptyStrings()
        {
            var addressFields = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutAddressFields.razor");
            var paymentFields = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation/Components/Checkout/StorefrontCheckoutPaymentFields.razor");

            foreach (var parameterName in new[]
            {
                "ContactSectionClass",
                "AddressSectionClass",
                "AddressGridClass",
                "LabelClass",
                "AddressLineClass",
                "LabelTextClass",
                "InputClass",
                "UppercaseInputClass",
                "HeadingClass",
            })
            {
                Assert.Contains($"public string {parameterName} {{ get; set; }} = string.Empty;", addressFields, StringComparison.Ordinal);
            }

            foreach (var parameterName in new[]
            {
                "SectionClass",
                "HeadingClass",
                "OptionLabelClass",
                "RadioClass",
                "OptionTitleClass",
                "OptionDescriptionClass",
            })
            {
                Assert.Contains($"public string {parameterName} {{ get; set; }} = string.Empty;", paymentFields, StringComparison.Ordinal);
            }
        }

        private static IEnumerable<SourceFile> EnumeratePresentationSourceFiles()
        {
            var presentationRoot = RepositoryPath("BlazorShop.PresentationV2/BlazorShop.Storefront.Presentation");
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".razor",
                ".cs",
                ".js",
                ".css",
                ".csproj",
            };

            return Directory
                .EnumerateFiles(presentationRoot, "*", SearchOption.AllDirectories)
                .Select(path => new SourceFile(path, ToRepositoryRelativePath(path), Path.GetExtension(path)))
                .Where(file => allowedExtensions.Contains(file.Extension))
                .Where(file => !IsExcluded(file.RelativePath))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .ToArray();
        }

        private static IEnumerable<string> FindTokenViolations(SourceFile file, string[] tokens)
        {
            var source = File.ReadAllText(file.AbsolutePath);
            foreach (var token in tokens)
            {
                if (source.Contains(token, StringComparison.Ordinal))
                {
                    yield return $"{file.RelativePath}: contains '{token}'";
                }
            }
        }

        private static bool IsForbiddenThemeAsset(SourceFile file)
        {
            var fileName = Path.GetFileName(file.RelativePath);
            return ForbiddenThemeAssetExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)
                || fileName.StartsWith("tailwind.config.", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("postcss.config.", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExcluded(string relativePath)
        {
            return relativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("/docs/", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("/Fixtures/", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("/generated/", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractBetween(string source, string startMarker, string endMarker)
        {
            var start = source.IndexOf(startMarker, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidOperationException($"Start marker '{startMarker}' was not found.");
            }

            var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException($"End marker '{endMarker}' was not found.");
            }

            return source[start..(end + endMarker.Length)];
        }

        private static void AssertNoViolations(IReadOnlyCollection<string> violations)
        {
            Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(RepositoryPath(relativePath));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ToRepositoryRelativePath(string path)
        {
            return Path.GetRelativePath(FindRepositoryRoot(), path).Replace('\\', '/');
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlazorShop.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        private sealed record SourceFile(string AbsolutePath, string RelativePath, string Extension);
    }
}
