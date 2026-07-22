namespace BlazorShop.Tests.Application.Common
{
    using BlazorShop.Application.Common.Results;

    using Xunit;

    public sealed class ApplicationResultTests
    {
        [Fact]
        public void Succeeded_ReturnsValueWithoutError()
        {
            var result = ApplicationResult<string>.Succeeded("payload", "done");

            Assert.True(result.Success);
            Assert.Equal("payload", result.Value);
            Assert.Null(result.Error);
            Assert.Equal("done", result.Message);
        }

        [Fact]
        public void Failed_ReturnsErrorAndSafeMessage()
        {
            var error = ApplicationError.NotFound("store.not_found", "Store was not found.");

            var result = ApplicationResult<string>.Failed(error);

            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.Same(error, result.Error);
            Assert.Equal("Store was not found.", result.Message);
        }

        [Fact]
        public void Failed_CanCarryTransitionalPayloadAndFailureAlias()
        {
            var error = ApplicationError.Conflict("task.already_exists", "Task already exists.");

            var result = ApplicationResult<string>.Failed(error, "task-summary");

            Assert.False(result.Success);
            Assert.Equal("task-summary", result.Value);
            Assert.Equal("task-summary", result.Payload);
            Assert.Equal(ApplicationErrorKind.Conflict, result.Failure);
            Assert.True(result.AlreadyExists);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void ApplicationError_WhenCodeIsBlank_Throws(string code)
        {
            Assert.Throws<ArgumentException>(() => ApplicationError.Validation(code, "Message."));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void ApplicationError_WhenMessageIsBlank_Throws(string message)
        {
            Assert.Throws<ArgumentException>(() => ApplicationError.Validation("validation.failed", message));
        }

        [Fact]
        public void ApplicationError_FactoryHelpersSetExpectedKinds()
        {
            Assert.Equal(ApplicationErrorKind.Validation, ApplicationError.Validation("validation", "Validation.").Kind);
            Assert.Equal(ApplicationErrorKind.NotFound, ApplicationError.NotFound("not_found", "Not found.").Kind);
            Assert.Equal(ApplicationErrorKind.Conflict, ApplicationError.Conflict("conflict", "Conflict.").Kind);
            Assert.Equal(ApplicationErrorKind.Unauthorized, ApplicationError.Unauthorized("unauthorized", "Unauthorized.").Kind);
            Assert.Equal(ApplicationErrorKind.Forbidden, ApplicationError.Forbidden("forbidden", "Forbidden.").Kind);
            Assert.Equal(ApplicationErrorKind.RemoteFailure, ApplicationError.RemoteFailure("remote_failure", "Remote failure.").Kind);
            Assert.Equal(ApplicationErrorKind.Failure, ApplicationError.Failure("failure", "Failure.").Kind);
        }

        [Fact]
        public void ApplicationError_MetadataIsOptionalAndCopied()
        {
            var metadata = new Dictionary<string, string>
            {
                ["storeKey"] = "default",
            };

            var error = ApplicationError.Conflict("store.conflict", "Store conflict.", metadata);
            metadata["storeKey"] = "mutated";

            Assert.Equal("default", error.Metadata["storeKey"]);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<string, string>)error.Metadata).Add("newKey", "newValue"));
        }
    }
}
