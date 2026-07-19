namespace BlazorShop.Application.Common.Results
{
    using System.Collections.ObjectModel;

    public sealed record ApplicationError
    {
        public ApplicationError(
            ApplicationErrorKind kind,
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Application error code is required.", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Application error message is required.", nameof(message));
            }

            Kind = kind;
            Code = code.Trim();
            Message = message.Trim();
            Metadata = metadata is null
                ? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())
                : new ReadOnlyDictionary<string, string>(
                    new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase));
        }

        public ApplicationErrorKind Kind { get; }

        public string Code { get; }

        public string Message { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public static ApplicationError Validation(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.Validation, code, message, metadata);
        }

        public static ApplicationError NotFound(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.NotFound, code, message, metadata);
        }

        public static ApplicationError Conflict(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.Conflict, code, message, metadata);
        }

        public static ApplicationError Unauthorized(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.Unauthorized, code, message, metadata);
        }

        public static ApplicationError Forbidden(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.Forbidden, code, message, metadata);
        }

        public static ApplicationError RemoteFailure(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.RemoteFailure, code, message, metadata);
        }

        public static ApplicationError Failure(
            string code,
            string message,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new ApplicationError(ApplicationErrorKind.Failure, code, message, metadata);
        }
    }
}
