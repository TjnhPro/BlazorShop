namespace BlazorShop.Tests.Application.Common
{
    using BlazorShop.Application.DTOs;

    using Xunit;

    public sealed class ServiceResponseApplicationResultExtensionsTests
    {
        [Fact]
        public void ToApplicationResult_WhenSuccess_CarriesPayloadAndMessage()
        {
            var response = new ServiceResponse<string>(true, "Loaded.")
            {
                Payload = "payload",
                ResponseType = ServiceResponseType.Success,
            };

            var result = response.ToApplicationResult("catalog");

            Assert.True(result.Success);
            Assert.Equal("payload", result.Value);
            Assert.Equal("Loaded.", result.Message);
            Assert.Null(result.Error);
        }

        [Theory]
        [InlineData(ServiceResponseType.ValidationError, ApplicationErrorKind.Validation, "checkout.validation")]
        [InlineData(ServiceResponseType.NotFound, ApplicationErrorKind.NotFound, "checkout.not_found")]
        [InlineData(ServiceResponseType.Conflict, ApplicationErrorKind.Conflict, "checkout.conflict")]
        [InlineData(ServiceResponseType.Failure, ApplicationErrorKind.Failure, "checkout.failure")]
        [InlineData(ServiceResponseType.None, ApplicationErrorKind.Failure, "checkout.failure")]
        public void ToApplicationResult_WhenFailure_MapsKindAndCode(
            ServiceResponseType responseType,
            ApplicationErrorKind expectedKind,
            string expectedCode)
        {
            var response = new ServiceResponse<string>(false, "Failed.")
            {
                Payload = "context",
                ResponseType = responseType,
            };

            var result = response.ToApplicationResult("checkout");

            Assert.False(result.Success);
            Assert.Equal("context", result.Value);
            Assert.Equal(expectedKind, result.Error!.Kind);
            Assert.Equal(expectedCode, result.Error.Code);
            Assert.Equal("Failed.", result.Message);
        }

        [Theory]
        [InlineData(ApplicationErrorKind.Validation, ServiceResponseType.ValidationError)]
        [InlineData(ApplicationErrorKind.NotFound, ServiceResponseType.NotFound)]
        [InlineData(ApplicationErrorKind.Conflict, ServiceResponseType.Conflict)]
        [InlineData(ApplicationErrorKind.Failure, ServiceResponseType.Failure)]
        [InlineData(ApplicationErrorKind.RemoteFailure, ServiceResponseType.Failure)]
        public void ToServiceResponse_WhenFailure_MapsResponseType(
            ApplicationErrorKind kind,
            ServiceResponseType expectedResponseType)
        {
            var result = new ApplicationResult<string>(
                Success: false,
                Message: "Failed.",
                Payload: "payload",
                Failure: kind);

            var response = result.ToServiceResponse();

            Assert.False(response.Success);
            Assert.Equal("payload", response.Payload);
            Assert.Equal(expectedResponseType, response.ResponseType);
            Assert.Equal("Failed.", response.Message);
        }
    }
}
