namespace BlazorShop.CommerceNode.API.Responses
{
    public sealed record CommerceNodeApiErrorResponse(
        bool Success,
        string Code,
        string Message,
        string TraceId,
        IReadOnlyDictionary<string, string[]>? FieldErrors = null);
}
