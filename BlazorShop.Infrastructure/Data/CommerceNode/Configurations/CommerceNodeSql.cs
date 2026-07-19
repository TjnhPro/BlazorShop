namespace BlazorShop.Infrastructure.Data.CommerceNode.Configurations
{
    internal static class CommerceNodeSql
    {
        public static string In(IEnumerable<string> values)
        {
            return string.Join(", ", values.Select(value => $"'{value.Replace("'", "''", StringComparison.Ordinal)}'"));
        }
    }
}
