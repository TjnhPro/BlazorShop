namespace BlazorShop.Tests.PresentationV2.Storefront
{
    using Xunit;

    public sealed class StorefrontVisualSourceOwnershipTests
    {
        private static readonly string[] ActiveV2RazorSourceFiles =
        [
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/StorefrontHeader.razor",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Catalog/SearchPage.razor",
            "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Product/StorefrontProductPurchasePanel.razor",
        ];

        [Fact]
        public void StorefrontCommerceScript_DoesNotOwnToastVisualValuesOrFeedbackColorClasses()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");

            foreach (var forbiddenToken in new[]
            {
                "resolveToastTheme",
                "resolveToastIcon",
                "style.backgroundColor",
                "style.color",
                "style.opacity",
                "style.transform",
                "innerHTML",
                "text-emerald-700",
                "text-red-700",
            })
            {
                Assert.DoesNotContain(forbiddenToken, script, StringComparison.Ordinal);
            }

            Assert.Contains("function normalizeToastLevel(level)", script, StringComparison.Ordinal);
            Assert.Contains("toast.dataset.level = normalizeToastLevel(level)", script, StringComparison.Ordinal);
            Assert.Contains("toast.dataset.state = \"entering\"", script, StringComparison.Ordinal);
            Assert.Contains("toast.dataset.state = \"open\"", script, StringComparison.Ordinal);
            Assert.Contains("toast.dataset.state = \"closing\"", script, StringComparison.Ordinal);
            Assert.Contains("feedbackElement.dataset.level = isError ? \"error\" : \"success\"", script, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCommerceScript_KeepsDataImageOnlyForGalleryFallback()
        {
            var script = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/js/storefrontCommerce.js");
            var occurrences = CountOccurrences(script, "data:image/svg+xml");

            Assert.Equal(1, occurrences);

            var galleryFallback = ExtractFunction(script, "showGalleryImageFallback");
            Assert.Contains("image.src = \"data:image/svg+xml", galleryFallback, StringComparison.Ordinal);
            Assert.DoesNotContain("showToast", galleryFallback, StringComparison.Ordinal);
        }

        [Fact]
        public void ActiveStorefrontV2RazorSources_DoNotUseFontAwesomeClassBasedIcons()
        {
            var forbiddenTokens = new[]
            {
                "fa-solid",
                "fa-regular",
                "fa-brands",
                "fa-magnifying-glass",
                "fa-check",
            };

            foreach (var sourceFile in ActiveV2RazorSourceFiles)
            {
                var source = ReadRepositoryFile(sourceFile);
                foreach (var forbiddenToken in forbiddenTokens)
                {
                    Assert.DoesNotContain(forbiddenToken, source, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void ActiveStorefrontV2Source_DoesNotExposeSubmitIconCssClass()
        {
            foreach (var sourceFile in ActiveV2RazorSourceFiles)
            {
                Assert.DoesNotContain("SubmitIconCssClass", ReadRepositoryFile(sourceFile), StringComparison.Ordinal);
            }

            var catalogFilter = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.Components.Ssr/Catalog/StorefrontCatalogFilterPanel.razor");
            Assert.Contains("public RenderFragment? SubmitIcon { get; set; }", catalogFilter, StringComparison.Ordinal);
            Assert.Contains("@SubmitIcon", catalogFilter, StringComparison.Ordinal);
        }

        [Fact]
        public void MainLayout_OwnsToastIconMarkupAndNoInlineToastVisualState()
        {
            var layout = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Components/Layout/MainLayout.razor");
            var toastTemplate = ExtractToastTemplate(layout);

            foreach (var expectedIcon in new[]
            {
                "data-storefront-toast-icon=\"info\"",
                "data-storefront-toast-icon=\"success\"",
                "data-storefront-toast-icon=\"warning\"",
                "data-storefront-toast-icon=\"error\"",
            })
            {
                Assert.Contains(expectedIcon, toastTemplate, StringComparison.Ordinal);
            }

            Assert.Contains("data-level=\"info\"", toastTemplate, StringComparison.Ordinal);
            Assert.Contains("data-state=\"entering\"", toastTemplate, StringComparison.Ordinal);
            Assert.Contains("aria-live=\"polite\"", layout, StringComparison.Ordinal);
            Assert.Contains("aria-label=\"Dismiss notification\"", toastTemplate, StringComparison.Ordinal);
            Assert.DoesNotContain("style=\"opacity", toastTemplate, StringComparison.Ordinal);
            Assert.DoesNotContain("transform: translateY", toastTemplate, StringComparison.Ordinal);
            Assert.DoesNotContain("transition: opacity", toastTemplate, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontCssOwnsToastAndPurchaseFeedbackVisualSelectors()
        {
            var css = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/storefront.css");

            foreach (var expectedSelector in new[]
            {
                ".bs-storefront-toast",
                ".bs-storefront-toast[data-state=\"entering\"]",
                ".bs-storefront-toast[data-state=\"open\"]",
                ".bs-storefront-toast[data-state=\"closing\"]",
                ".bs-storefront-toast[data-level=\"info\"]",
                ".bs-storefront-toast[data-level=\"success\"]",
                ".bs-storefront-toast[data-level=\"warning\"]",
                ".bs-storefront-toast[data-level=\"error\"]",
                "[data-storefront-toast-icon]",
                "[data-storefront-purchase-feedback][data-level=\"success\"]",
                "[data-storefront-purchase-feedback][data-level=\"error\"]",
            })
            {
                Assert.Contains(expectedSelector, css, StringComparison.Ordinal);
            }

            Assert.Contains("--bs-toast-background", css, StringComparison.Ordinal);
            Assert.Contains("--bs-toast-accent-background", css, StringComparison.Ordinal);
            Assert.Contains("--bs-toast-accent-color", css, StringComparison.Ordinal);
        }

        [Fact]
        public void CuratedOwnershipScan_ExcludesGeneratedCssDocsFixturesAndBuildOutput()
        {
            var curatedSources = EnumerateCuratedOwnershipSources();

            Assert.DoesNotContain(curatedSources, file => file.RelativePath.EndsWith("wwwroot/css/site.css", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.EndsWith("wwwroot/css/wasm-site.css", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.StartsWith("docs/", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(curatedSources, file => file.RelativePath.Contains("/Fixtures/", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StorefrontV2ContentPage_OwnsSemanticLayoutToVisualClassMapping()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Ssr/Content/StorefrontPage.razor");

            Assert.Contains("@GetArticleClass(Context.Presentation.LayoutKind)", page, StringComparison.Ordinal);
            Assert.Contains("@GetBodyContainerClass(Context.Presentation.LayoutKind)", page, StringComparison.Ordinal);
            Assert.Contains("private static string GetArticleClass(StorefrontPageLayoutKind layoutKind)", page, StringComparison.Ordinal);
            Assert.Contains("private static string GetBodyContainerClass(StorefrontPageLayoutKind layoutKind)", page, StringComparison.Ordinal);

            foreach (var expectedVisualVariant in new[]
            {
                "bs-storefront-content-page bs-storefront-content-page--standard",
                "bs-storefront-content-page bs-storefront-content-page--policy",
                "bs-storefront-content-page bs-storefront-content-page--faq",
                "bs-storefront-content-page bs-storefront-content-page--support",
                "rounded-3xl border border-neutral-200/70 bg-white/90 p-6 shadow-lg sm:p-8",
                "rounded-2xl border border-neutral-200 bg-white p-6 shadow-sm sm:p-8",
            })
            {
                Assert.Contains(expectedVisualVariant, page, StringComparison.Ordinal);
            }

            Assert.Contains("StorefrontPageLayoutKind.Policy", page, StringComparison.Ordinal);
            Assert.Contains("StorefrontPageLayoutKind.Faq", page, StringComparison.Ordinal);
            Assert.Contains("StorefrontPageLayoutKind.Support", page, StringComparison.Ordinal);
            Assert.DoesNotContain("Context.Presentation.ArticleClass", page, StringComparison.Ordinal);
            Assert.DoesNotContain("Context.Presentation.BodyContainerClass", page, StringComparison.Ordinal);
        }

        [Fact]
        public void StorefrontV2PaymentResultPage_OwnsOutcomeToVisualToneMapping()
        {
            var page = ReadRepositoryFile("BlazorShop.PresentationV2/BlazorShop.Storefront.V2/Pages/Hybrid/Commerce/PaymentResultPage.razor");

            Assert.Contains("@GetPanelClass(Context.Outcome)", page, StringComparison.Ordinal);
            Assert.Contains("@GetEyebrowClass(Context.Outcome)", page, StringComparison.Ordinal);
            Assert.Contains("@GetHeadingClass(Context.Outcome)", page, StringComparison.Ordinal);
            Assert.Contains("@GetBodyClass(Context.Outcome)", page, StringComparison.Ordinal);
            Assert.Contains("@GetMutedClass(Context.Outcome)", page, StringComparison.Ordinal);

            foreach (var expectedVisualTone in new[]
            {
                "rounded-3xl border border-emerald-200 bg-emerald-50 px-6 py-10 text-center shadow-sm",
                "rounded-3xl border border-amber-200 bg-amber-50 px-6 py-10 text-center shadow-sm",
                "rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center shadow-sm",
                "text-emerald-700",
                "text-emerald-950",
                "text-emerald-900",
                "text-amber-700",
                "text-amber-950",
                "text-amber-900",
                "text-rose-700",
                "text-rose-950",
                "text-rose-900",
            })
            {
                Assert.Contains(expectedVisualTone, page, StringComparison.Ordinal);
            }

            Assert.Contains("StorefrontPaymentResultOutcome.Success", page, StringComparison.Ordinal);
            Assert.Contains("StorefrontPaymentResultOutcome.Pending or StorefrontPaymentResultOutcome.Cancelled", page, StringComparison.Ordinal);
            Assert.Contains("_ => \"rounded-3xl border border-rose-200 bg-rose-50 px-6 py-10 text-center shadow-sm\"", page, StringComparison.Ordinal);

            foreach (var removedContextField in new[]
            {
                "Context.PanelClass",
                "Context.EyebrowClass",
                "Context.HeadingClass",
                "Context.BodyClass",
                "Context.MutedClass",
            })
            {
                Assert.DoesNotContain(removedContextField, page, StringComparison.Ordinal);
            }
        }

        private static IReadOnlyList<SourceFile> EnumerateCuratedOwnershipSources()
        {
            var repositoryRoot = FindRepositoryRoot();
            var sourceRoots = new[]
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM",
            };
            var sourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".razor",
                ".cs",
                ".js",
                ".css",
                ".json",
            };
            var excludedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2/wwwroot/css/site.css",
                "BlazorShop.PresentationV2/BlazorShop.Storefront.V2.WASM/wwwroot/css/wasm-site.css",
            };
            var files = new List<SourceFile>();

            foreach (var sourceRoot in sourceRoots)
            {
                var absoluteRoot = Path.Combine(repositoryRoot, sourceRoot);
                foreach (var absolutePath in Directory.EnumerateFiles(absoluteRoot, "*.*", SearchOption.AllDirectories))
                {
                    var relativePath = NormalizePath(Path.GetRelativePath(repositoryRoot, absolutePath));
                    if (!sourceExtensions.Contains(Path.GetExtension(absolutePath))
                        || excludedRelativePaths.Contains(relativePath)
                        || relativePath.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
                        || relativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
                        || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    files.Add(new SourceFile(absolutePath, relativePath));
                }
            }

            return files;
        }

        private static string ExtractToastTemplate(string layout)
        {
            const string startMarker = "<template data-storefront-toast-template>";
            const string endMarker = "</template>";
            var start = layout.IndexOf(startMarker, StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidOperationException("Toast template start marker was not found.");
            }

            var end = layout.IndexOf(endMarker, start, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new InvalidOperationException("Toast template end marker was not found.");
            }

            return layout[start..(end + endMarker.Length)];
        }

        private static string ExtractFunction(string source, string functionName)
        {
            var start = source.IndexOf($"function {functionName}", StringComparison.Ordinal);
            if (start < 0)
            {
                throw new InvalidOperationException($"Function '{functionName}' was not found.");
            }

            var nextFunction = source.IndexOf("\n  function ", start + 1, StringComparison.Ordinal);
            return nextFunction < 0 ? source[start..] : source[start..nextFunction];
        }

        private static int CountOccurrences(string source, string value)
        {
            var count = 0;
            var index = 0;

            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string ReadRepositoryFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath));
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

        private static string NormalizePath(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }

        private sealed record SourceFile(string AbsolutePath, string RelativePath);
    }
}
