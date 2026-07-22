namespace BlazorShop.Tests.Infrastructure.ControlPlane
{
    using BlazorShop.Application.Common.Results;
    using BlazorShop.Infrastructure.Data.ControlPlane;

    using Xunit;

    public sealed class CommerceNodeAdminGatewayApplicationResultMapperTests
    {
        [Fact]
        public void ToApplicationResult_WhenTransportSucceeds_PreservesPayloadAndMessage()
        {
            var transportResult = new CommerceNodeAdminGatewayResult<string>(
                Success: true,
                Message: "Loaded.",
                Payload: "payload",
                HttpStatusCode: 200);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationResult(transportResult);

            Assert.True(result.Success);
            Assert.Equal("payload", result.Value);
            Assert.Equal("Loaded.", result.Message);
            Assert.Null(result.Error);
        }

        [Fact]
        public void ToApplicationResult_WhenSuccessPayloadIsNull_PreservesNullPayload()
        {
            var transportResult = new CommerceNodeAdminGatewayResult<string>(
                Success: true,
                Message: "No content.",
                Payload: null,
                HttpStatusCode: 204);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationResult(transportResult);

            Assert.True(result.Success);
            Assert.Null(result.Value);
            Assert.Equal("No content.", result.Message);
        }

        [Theory]
        [InlineData(CommerceNodeAdminGatewayFailure.Validation, ApplicationErrorKind.Validation, "commerce_node.validation", 400)]
        [InlineData(CommerceNodeAdminGatewayFailure.NotFound, ApplicationErrorKind.NotFound, "commerce_node.not_found", 404)]
        [InlineData(CommerceNodeAdminGatewayFailure.RemoteFailure, ApplicationErrorKind.RemoteFailure, "commerce_node.remote_failure", 502)]
        public void ToApplicationResult_WhenTransportFails_MapsFailureAndUpstreamStatus(
            CommerceNodeAdminGatewayFailure gatewayFailure,
            ApplicationErrorKind expectedKind,
            string expectedCode,
            int upstreamStatusCode)
        {
            var transportResult = new CommerceNodeAdminGatewayResult<string>(
                Success: false,
                Message: "Gateway failure.",
                Payload: "partial",
                Failure: gatewayFailure,
                HttpStatusCode: upstreamStatusCode);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationResult(transportResult);

            Assert.False(result.Success);
            Assert.Equal("partial", result.Value);
            Assert.NotNull(result.Error);
            Assert.Equal(expectedKind, result.Error.Kind);
            Assert.Equal(expectedCode, result.Error.Code);
            Assert.Equal("Gateway failure.", result.Error.Message);
            Assert.Equal(upstreamStatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture), result.Error.Metadata["upstreamStatusCode"]);
        }

        [Fact]
        public void ToApplicationResult_WhenTransportFailureIsUnknown_DefaultsToRemoteFailure()
        {
            var transportResult = new CommerceNodeAdminGatewayResult<string>(
                Success: false,
                Message: "",
                Failure: null);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationResult(transportResult);

            Assert.False(result.Success);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationErrorKind.RemoteFailure, result.Error.Kind);
            Assert.Equal("commerce_node.remote_failure", result.Error.Code);
            Assert.Equal("Commerce Node gateway request failed.", result.Error.Message);
            Assert.Empty(result.Error.Metadata);
        }

        [Fact]
        public void ToApplicationMediaResult_WhenTransportSucceeds_PreservesBinaryContent()
        {
            var bytes = new byte[] { 1, 2, 3 };
            var transportResult = new CommerceNodeAdminMediaGatewayResult(
                Success: true,
                Message: "Loaded media.",
                Content: bytes,
                ContentType: "image/webp",
                HttpStatusCode: 200);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationMediaResult(
                transportResult,
                "preview.webp");

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Same(bytes, result.Value.Content);
            Assert.Equal("image/webp", result.Value.ContentType);
            Assert.Equal("preview.webp", result.Value.FileName);
            Assert.Equal("200", result.Value.Metadata?["upstreamStatusCode"]);
        }

        [Fact]
        public void ToApplicationMediaResult_WhenContentTypeMissing_UsesOctetStream()
        {
            var transportResult = new CommerceNodeAdminMediaGatewayResult(
                Success: true,
                Message: "Loaded media.",
                Content: null,
                ContentType: null);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationMediaResult(transportResult);

            Assert.True(result.Success);
            Assert.NotNull(result.Value);
            Assert.Empty(result.Value.Content);
            Assert.Equal("application/octet-stream", result.Value.ContentType);
        }

        [Fact]
        public void ToApplicationMediaResult_WhenTransportFails_MapsErrorWithoutUnsafeBinaryPayload()
        {
            var transportResult = new CommerceNodeAdminMediaGatewayResult(
                Success: false,
                Message: "Not found.",
                Content: [9, 9],
                ContentType: "text/plain",
                Failure: CommerceNodeAdminGatewayFailure.NotFound,
                HttpStatusCode: 404);

            var result = CommerceNodeAdminGatewayApplicationResultMapper.ToApplicationMediaResult(transportResult);

            Assert.False(result.Success);
            Assert.Null(result.Value);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationErrorKind.NotFound, result.Error.Kind);
            Assert.Equal("commerce_node.not_found", result.Error.Code);
            Assert.Equal("404", result.Error.Metadata["upstreamStatusCode"]);
        }
    }
}
