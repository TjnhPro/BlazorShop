extern alias CommerceNodeApi;
extern alias ControlPlaneApi;

namespace BlazorShop.Tests.PresentationV2
{
    using System.Reflection;

    using BlazorShop.Application.Common.Results;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.ControlPlane.Stores;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    using Xunit;

    using CommerceStoresController = CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers.CommerceStoresController;
    using CommerceNodeApplicationResultMapper = CommerceNodeApi::BlazorShop.CommerceNode.API.Responses.CommerceNodeApplicationResultMapper;
    using CommerceNodeApiResponseOfString = CommerceNodeApi::BlazorShop.CommerceNode.API.Responses.CommerceNodeApiResponse<string>;
    using ControlPlaneApplicationResultMapper = ControlPlaneApi::BlazorShop.ControlPlane.API.Responses.ControlPlaneApplicationResultMapper;
    using ControlPlaneApiResponseOfString = ControlPlaneApi::BlazorShop.ControlPlane.API.Responses.ControlPlaneApiResponse<string>;
    using ControlPlaneApiResponseWriter = ControlPlaneApi::BlazorShop.ControlPlane.API.Responses.ControlPlaneApiResponseWriter;
    using ControlPlaneStoresController = ControlPlaneApi::BlazorShop.ControlPlane.API.Controllers.ControlPlaneStoresController;

    public sealed class ApplicationResultStandardizationPhase0Tests
    {
        private static readonly string[] ExpectedOperationResultTypes =
        [
            "ControlPlaneActionOperationResult",
            "ControlPlaneUserOperationResult",
            "ControlPlaneHealthOperationResult",
            "ControlPlaneStoreOperationResult",
            "ControlPlaneStoreDeploymentOperationResult",
            "ControlPlaneCredentialOperationResult",
            "ControlPlaneNodeOperationResult",
            "PaymentProviderOperationResult",
        ];

        private static readonly string[] MigratedMediaResultTypes =
        [
            "ProductMedia" + "OperationResult",
            "CommerceMediaAsset" + "OperationResult",
            "CategoryMedia" + "OperationResult",
            "CommerceStore" + "OperationResult",
            "CommerceTask" + "OperationResult",
        ];

        [Fact]
        public void Inventory_OperationResultTypesMatchPhase0Baseline()
        {
            var source = string.Join(
                Environment.NewLine,
                Directory.GetFiles(RepositoryPath("BlazorShop.Application"), "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));

            foreach (var expected in ExpectedOperationResultTypes)
            {
                Assert.Contains(expected, source, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Inventory_MediaOperationResultTypesAreMigratedToApplicationResult()
        {
            var source = string.Join(
                Environment.NewLine,
                Directory.GetFiles(RepositoryPath("BlazorShop.Application"), "*.cs", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));

            foreach (var migrated in MigratedMediaResultTypes)
            {
                Assert.DoesNotContain(migrated, source, StringComparison.Ordinal);
            }
        }

        [Theory]
        [InlineData(ApplicationErrorKind.Validation, StatusCodes.Status400BadRequest)]
        [InlineData(ApplicationErrorKind.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ApplicationErrorKind.Conflict, StatusCodes.Status409Conflict)]
        public void CommerceStoreMapper_PreservesStatusAndEnvelope(
            ApplicationErrorKind failure,
            int expectedStatusCode)
        {
            var result = new ApplicationResult<string>(
                Success: false,
                Message: "store failure",
                Payload: "payload",
                Failure: failure);

            var action = InvokePrivateGenericMapper<ApplicationResult<string>, CommerceStoresController>(
                "ToActionResult",
                result);

            var objectResult = Assert.IsType<ObjectResult>(action);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);
            var response = Assert.IsType<CommerceNodeApiResponseOfString>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal("store failure", response.Message);
            Assert.Equal("payload", response.Data);
        }

        [Theory]
        [InlineData(ControlPlaneStoreDeploymentOperationFailure.Validation, StatusCodes.Status400BadRequest)]
        [InlineData(ControlPlaneStoreDeploymentOperationFailure.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ControlPlaneStoreDeploymentOperationFailure.Conflict, StatusCodes.Status409Conflict)]
        [InlineData(ControlPlaneStoreDeploymentOperationFailure.RemoteFailure, StatusCodes.Status502BadGateway)]
        public void ControlPlaneDeploymentMapper_PreservesStatusAndEnvelope(
            ControlPlaneStoreDeploymentOperationFailure failure,
            int expectedStatusCode)
        {
            var result = new ControlPlaneStoreDeploymentOperationResult<string>(
                Success: false,
                Message: "deployment failure",
                Payload: "payload",
                Failure: failure);

            var action = InvokePrivateGenericMapper<ControlPlaneStoreDeploymentOperationResult<string>, ControlPlaneStoresController>(
                "ToDeploymentActionResult",
                result);

            var objectResult = Assert.IsType<ObjectResult>(action);
            Assert.Equal(expectedStatusCode, objectResult.StatusCode);
            var response = Assert.IsType<ControlPlaneApiResponseOfString>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal("deployment failure", response.Message);
            Assert.Equal("payload", response.Data);
        }

        [Fact]
        public void CommerceStoreMapper_WhenFailureIsUnknown_MapsToInternalServerError()
        {
            var result = new ApplicationResult<string>(
                Success: false,
                Message: "store failure",
                Payload: "payload",
                Failure: null);

            var action = InvokePrivateGenericMapper<ApplicationResult<string>, CommerceStoresController>(
                "ToActionResult",
                result);

            var objectResult = Assert.IsType<ObjectResult>(action);
            Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
            var response = Assert.IsType<CommerceNodeApiResponseOfString>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal("store failure", response.Message);
            Assert.Equal("payload", response.Data);
        }

        [Fact]
        public void ControlPlaneDeploymentMapper_WhenFailureIsUnknown_MapsToBadRequest()
        {
            var result = new ControlPlaneStoreDeploymentOperationResult<string>(
                Success: false,
                Message: "deployment failure",
                Payload: "payload",
                Failure: null);

            var action = InvokePrivateGenericMapper<ControlPlaneStoreDeploymentOperationResult<string>, ControlPlaneStoresController>(
                "ToDeploymentActionResult",
                result);

            var objectResult = Assert.IsType<ObjectResult>(action);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            var response = Assert.IsType<ControlPlaneApiResponseOfString>(objectResult.Value);
            Assert.False(response.Success);
            Assert.Equal("deployment failure", response.Message);
            Assert.Equal("payload", response.Data);
        }

        [Fact]
        public void ControlPlaneResponseWriter_PreservesFailureEnvelopeAndFallbackMessage()
        {
            var result = ControlPlaneApiResponseWriter.Failure<string>(
                StatusCodes.Status400BadRequest,
                message: null,
                data: "payload");

            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            var response = Assert.IsType<ControlPlaneApiResponseOfString>(result.Value);
            Assert.False(response.Success);
            Assert.Equal("The Control Plane request could not be completed.", response.Message);
            Assert.Equal("payload", response.Data);
        }

        [Theory]
        [InlineData(ApplicationErrorKind.Validation, StatusCodes.Status400BadRequest)]
        [InlineData(ApplicationErrorKind.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ApplicationErrorKind.Conflict, StatusCodes.Status409Conflict)]
        [InlineData(ApplicationErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
        [InlineData(ApplicationErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
        [InlineData(ApplicationErrorKind.RemoteFailure, StatusCodes.Status502BadGateway)]
        [InlineData(ApplicationErrorKind.Failure, StatusCodes.Status400BadRequest)]
        public void ControlPlaneApplicationResultMapper_MapsErrorKindsToCurrentStatuses(
            ApplicationErrorKind kind,
            int expectedStatusCode)
        {
            var result = ApplicationResult<string>.Failed(new ApplicationError(kind, "error.code", "Mapped failure."));

            var action = ControlPlaneApplicationResultMapper.ToControlPlaneActionResult(result);

            Assert.Equal(expectedStatusCode, action.StatusCode);
            var response = Assert.IsType<ControlPlaneApiResponseOfString>(action.Value);
            Assert.False(response.Success);
            Assert.Equal("Mapped failure.", response.Message);
            Assert.Null(response.Data);
        }

        [Theory]
        [InlineData(ApplicationErrorKind.Validation, StatusCodes.Status400BadRequest)]
        [InlineData(ApplicationErrorKind.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ApplicationErrorKind.Conflict, StatusCodes.Status409Conflict)]
        [InlineData(ApplicationErrorKind.Unauthorized, StatusCodes.Status401Unauthorized)]
        [InlineData(ApplicationErrorKind.Forbidden, StatusCodes.Status403Forbidden)]
        [InlineData(ApplicationErrorKind.RemoteFailure, StatusCodes.Status502BadGateway)]
        [InlineData(ApplicationErrorKind.Failure, StatusCodes.Status500InternalServerError)]
        public void CommerceNodeApplicationResultMapper_MapsErrorKindsToCurrentStatuses(
            ApplicationErrorKind kind,
            int expectedStatusCode)
        {
            var result = ApplicationResult<string>.Failed(new ApplicationError(kind, "error.code", "Mapped failure."));

            var action = CommerceNodeApplicationResultMapper.ToCommerceNodeActionResult(result);

            Assert.Equal(expectedStatusCode, action.StatusCode);
            var response = Assert.IsType<CommerceNodeApiResponseOfString>(action.Value);
            Assert.False(response.Success);
            Assert.Equal("Mapped failure.", response.Message);
            Assert.Null(response.Data);
        }

        [Fact]
        public void ApplicationResultMappers_PreserveSuccessEnvelope()
        {
            var result = ApplicationResult<string>.Succeeded("payload", "Mapped success.");

            var controlPlane = ControlPlaneApplicationResultMapper.ToControlPlaneActionResult(result, StatusCodes.Status201Created);
            var commerceNode = CommerceNodeApplicationResultMapper.ToCommerceNodeActionResult(result, StatusCodes.Status201Created);

            Assert.Equal(StatusCodes.Status201Created, controlPlane.StatusCode);
            var controlPlaneResponse = Assert.IsType<ControlPlaneApiResponseOfString>(controlPlane.Value);
            Assert.True(controlPlaneResponse.Success);
            Assert.Equal("Mapped success.", controlPlaneResponse.Message);
            Assert.Equal("payload", controlPlaneResponse.Data);

            Assert.Equal(StatusCodes.Status201Created, commerceNode.StatusCode);
            var commerceNodeResponse = Assert.IsType<CommerceNodeApiResponseOfString>(commerceNode.Value);
            Assert.True(commerceNodeResponse.Success);
            Assert.Equal("Mapped success.", commerceNodeResponse.Message);
            Assert.Equal("payload", commerceNodeResponse.Data);
        }

        private static IActionResult InvokePrivateGenericMapper<TResult, TController>(
            string methodName,
            TResult result)
        {
            var method = typeof(TController).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var genericMethod = method!.MakeGenericMethod(typeof(string));
            return Assert.IsAssignableFrom<IActionResult>(genericMethod.Invoke(null, [result]));
        }

        private static string RepositoryPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
