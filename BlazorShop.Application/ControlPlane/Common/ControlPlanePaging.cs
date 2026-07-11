namespace BlazorShop.Application.ControlPlane.Common
{
    public sealed record ControlPlanePageQuery(int PageNumber = 1, int PageSize = 25);

    public sealed record ControlPlanePagedResponse<TItem>(
        IReadOnlyList<TItem> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

    public static class ControlPlanePaging
    {
        public const int DefaultPageSize = 25;

        public const int MaxPageSize = 100;

        public static ControlPlanePage Normalize(int pageNumber, int pageSize, int defaultPageSize = DefaultPageSize, int maxPageSize = MaxPageSize)
        {
            var normalizedDefault = Math.Clamp(defaultPageSize <= 0 ? DefaultPageSize : defaultPageSize, 1, maxPageSize);
            var normalizedPageSize = Math.Clamp(pageSize <= 0 ? normalizedDefault : pageSize, 1, maxPageSize);
            var normalizedPageNumber = Math.Max(1, pageNumber);
            return new ControlPlanePage(normalizedPageNumber, normalizedPageSize);
        }

        public static int GetTotalPages(int totalCount, int pageSize)
        {
            return totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)Math.Max(1, pageSize));
        }

        public static ControlPlanePagedResponse<TItem> ToResponse<TItem>(
            IReadOnlyList<TItem> items,
            int totalCount,
            ControlPlanePage page)
        {
            return new ControlPlanePagedResponse<TItem>(
                items,
                totalCount,
                page.PageNumber,
                page.PageSize,
                GetTotalPages(totalCount, page.PageSize));
        }
    }

    public readonly record struct ControlPlanePage(int PageNumber, int PageSize)
    {
        public int Skip => (this.PageNumber - 1) * this.PageSize;
    }
}
