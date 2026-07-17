extern alias CommerceNodeApi;

namespace BlazorShop.Tests.PresentationV2.CommerceNode
{
    using BlazorShop.Application.CommerceNode.Payments;
    using BlazorShop.Application.CommerceNode.Stores;
    using BlazorShop.Application.DTOs;
    using BlazorShop.Application.DTOs.Payment;
    using BlazorShop.Application.Options;
    using BlazorShop.Application.Services.Contracts.Payment;
    using BlazorShop.Domain.Entities;
    using BlazorShop.Infrastructure.Data.CommerceNode.Services;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using Moq;
    using Xunit;

    using CommerceNodeApi::BlazorShop.CommerceNode.API.Contracts.Storefront;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Controllers;
    using CommerceNodeApi::BlazorShop.CommerceNode.API.Responses;

    public sealed class StorefrontScopedPaymentWebhookHardeningTests
    {
        [Fact]
        public async Task HandleWebhook_WhenSignatureVerifierRejects_ReturnsFailureBeforeRecordingEvent()
        {
            var storeId = Guid.NewGuid();
            var paymentAttempts = new Mock<IPaymentAttemptService>();
            var verifier = new Mock<IPaymentWebhookSignatureVerifier>();
            verifier
                .Setup(service => service.VerifyAsync("stripe", "{}", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<object?>(false, "Payment provider signature is required.")
                {
                    Payload = null,
                    ResponseType = ServiceResponseType.ValidationError,
                });
            var controller = CreateController(storeId, paymentAttempts, verifier);

            var result = await controller.HandleWebhook(
                "stripe",
                providerSignature: null,
                new StorefrontPaymentWebhookRequest
                {
                    EventId = "evt_1",
                    EventType = "checkout.session.completed",
                    State = "captured",
                    PayloadJson = "{}",
                },
                CancellationToken.None);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
            Assert.IsType<CommerceNodeApiErrorResponse>(objectResult.Value);
            paymentAttempts.Verify(
                service => service.RecordProviderEventAsync(It.IsAny<RecordPaymentProviderEventRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleWebhook_WhenBodyRequestsState_DoesNotTransitionAttempt()
        {
            var storeId = Guid.NewGuid();
            var paymentAttempts = new Mock<IPaymentAttemptService>();
            var verifier = new Mock<IPaymentWebhookSignatureVerifier>();
            verifier
                .Setup(service => service.VerifyAsync("cod", "{}", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<object?>(true, "Signature not required.")
                {
                    Payload = null,
                    ResponseType = ServiceResponseType.Success,
                });
            paymentAttempts
                .Setup(service => service.RecordProviderEventAsync(It.IsAny<RecordPaymentProviderEventRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentProviderEventDto>(true, "Payment provider event recorded.")
                {
                    Payload = new PaymentProviderEventDto(
                        Guid.NewGuid(),
                        storeId,
                        null,
                        "cod",
                        "evt_1",
                        "provider.test",
                        "payload-hash",
                        false,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    ResponseType = ServiceResponseType.Success,
                });
            var controller = CreateController(storeId, paymentAttempts, verifier);

            var result = await controller.HandleWebhook(
                "cod",
                providerSignature: null,
                new StorefrontPaymentWebhookRequest
                {
                    PaymentAttemptId = Guid.NewGuid(),
                    EventId = "evt_1",
                    EventType = "provider.test",
                    ProviderReference = "pi_test_123",
                    ProviderSessionId = "cs_test_123",
                    State = "captured",
                    PayloadJson = "{}",
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            paymentAttempts.Verify(
                service => service.TransitionAsync(It.IsAny<TransitionPaymentAttemptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
            paymentAttempts.Verify(
                service => service.RecordProviderEventAsync(
                    It.Is<RecordPaymentProviderEventRequest>(request =>
                        request.ProviderReference == "pi_test_123"
                        && request.ProviderSessionId == "cs_test_123"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleWebhook_WhenProviderConfirmsState_TransitionsAttempt()
        {
            var storeId = Guid.NewGuid();
            var attemptId = Guid.NewGuid();
            var paymentAttempts = new Mock<IPaymentAttemptService>();
            var verifier = new Mock<IPaymentWebhookSignatureVerifier>();
            verifier
                .Setup(service => service.VerifyAsync("cod", "{}", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<object?>(true, "Signature not required.")
                {
                    Payload = null,
                    ResponseType = ServiceResponseType.Success,
                });
            paymentAttempts
                .Setup(service => service.RecordProviderEventAsync(It.IsAny<RecordPaymentProviderEventRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentProviderEventDto>(true, "Payment provider event recorded.")
                {
                    Payload = new PaymentProviderEventDto(
                        Guid.NewGuid(),
                        storeId,
                        null,
                        "cod",
                        "evt_1",
                        "provider.test",
                        "payload-hash",
                        false,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    ResponseType = ServiceResponseType.Success,
                });
            paymentAttempts
                .Setup(service => service.TransitionAsync(It.IsAny<TransitionPaymentAttemptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentAttemptDto>(true, "Payment attempt transitioned.")
                {
                    Payload = CreateAttemptDto(storeId, attemptId),
                    ResponseType = ServiceResponseType.Success,
                });
            var controller = CreateController(
                storeId,
                paymentAttempts,
                verifier,
                new StorefrontPaymentProviderResolver([new ConfirmingPaymentProvider("cod")]));

            var result = await controller.HandleWebhook(
                "cod",
                providerSignature: null,
                new StorefrontPaymentWebhookRequest
                {
                    PaymentAttemptId = attemptId,
                    EventId = "evt_1",
                    EventType = "provider.test",
                    State = "failed",
                    PayloadJson = "{}",
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            paymentAttempts.Verify(
                service => service.TransitionAsync(
                    It.Is<TransitionPaymentAttemptRequest>(request =>
                        request.PaymentAttemptId == attemptId
                        && request.NewState == "captured"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleProviderCallback_WhenBodyRequestsState_DoesNotTransitionAttempt()
        {
            var storeId = Guid.NewGuid();
            var paymentAttempts = new Mock<IPaymentAttemptService>();
            paymentAttempts
                .Setup(service => service.RecordProviderEventAsync(It.IsAny<RecordPaymentProviderEventRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentProviderEventDto>(true, "Payment provider event recorded.")
                {
                    Payload = new PaymentProviderEventDto(
                        Guid.NewGuid(),
                        storeId,
                        null,
                        "stripe",
                        "evt_1",
                        "provider.callback",
                        "payload-hash",
                        false,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    ResponseType = ServiceResponseType.Success,
                });
            var controller = CreateController(storeId, paymentAttempts, new Mock<IPaymentWebhookSignatureVerifier>());

            var result = await controller.HandleProviderCallback(
                "stripe",
                new StorefrontPaymentCallbackRequest
                {
                    PaymentAttemptId = Guid.NewGuid(),
                    ProviderEventId = "evt_1",
                    EventType = "provider.callback",
                    State = "captured",
                    PayloadJson = "{}",
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            paymentAttempts.Verify(
                service => service.TransitionAsync(It.IsAny<TransitionPaymentAttemptRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleProviderCallback_WhenProviderConfirmsCancel_TransitionsCancelled()
        {
            var storeId = Guid.NewGuid();
            var attemptId = Guid.NewGuid();
            var paymentAttempts = new Mock<IPaymentAttemptService>();
            paymentAttempts
                .Setup(service => service.RecordProviderEventAsync(It.IsAny<RecordPaymentProviderEventRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentProviderEventDto>(true, "Payment provider event recorded.")
                {
                    Payload = new PaymentProviderEventDto(
                        Guid.NewGuid(),
                        storeId,
                        null,
                        "cod",
                        "evt_cancel",
                        "provider.cancel",
                        "payload-hash",
                        false,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow),
                    ResponseType = ServiceResponseType.Success,
                });
            paymentAttempts
                .Setup(service => service.TransitionAsync(It.IsAny<TransitionPaymentAttemptRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ServiceResponse<PaymentAttemptDto>(true, "Payment attempt transitioned.")
                {
                    Payload = CreateAttemptDto(storeId, attemptId, "cancelled"),
                    ResponseType = ServiceResponseType.Success,
                });
            var controller = CreateController(
                storeId,
                paymentAttempts,
                new Mock<IPaymentWebhookSignatureVerifier>(),
                new StorefrontPaymentProviderResolver([new ConfirmingPaymentProvider("cod")]));

            var result = await controller.HandleProviderCallback(
                "cod",
                new StorefrontPaymentCallbackRequest
                {
                    PaymentAttemptId = attemptId,
                    ProviderEventId = "evt_cancel",
                    EventType = "provider.cancel",
                    State = "captured",
                    PayloadJson = "{}",
                },
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);
            paymentAttempts.Verify(
                service => service.TransitionAsync(
                    It.Is<TransitionPaymentAttemptRequest>(request =>
                        request.PaymentAttemptId == attemptId
                        && request.NewState == "cancelled"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static StorefrontScopedPaymentsController CreateController(
            Guid storeId,
            Mock<IPaymentAttemptService> paymentAttempts,
            Mock<IPaymentWebhookSignatureVerifier> verifier,
            IStorefrontPaymentProviderResolver? providerResolver = null)
        {
            var storeContext = new Mock<ICommerceStoreContext>();
            storeContext
                .Setup(context => context.GetCurrentStoreIdAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommerceStoreOperationResult<Guid>(true, "Store resolved.", storeId));

            return new StorefrontScopedPaymentsController(
                storeContext.Object,
                paymentAttempts.Object,
                verifier.Object,
                providerResolver ?? CreateProviderResolver(),
                new Mock<IPaymentMethodService>().Object,
                new StubPayPalPaymentService(),
                Options.Create(new ClientAppOptions()))
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
        }

        private static IStorefrontPaymentProviderResolver CreateProviderResolver()
        {
            var resolver = new Mock<IStorefrontPaymentProviderResolver>();
            resolver
                .Setup(service => service.Resolve(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Provider adapter is not configured."));
            return resolver.Object;
        }

        private static PaymentAttemptDto CreateAttemptDto(Guid storeId, Guid attemptId, string state = "captured")
        {
            return new PaymentAttemptDto(
                attemptId,
                storeId,
                Guid.NewGuid(),
                null,
                "cod",
                "cod",
                state,
                12.34m,
                "USD",
                "webhook-transition",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                DateTimeOffset.UtcNow.AddMinutes(30),
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }

        private sealed class ConfirmingPaymentProvider : IStorefrontPaymentProvider
        {
            public ConfirmingPaymentProvider(string providerKey)
            {
                this.ProviderKey = providerKey;
            }

            public string ProviderKey { get; }

            public Task<ServiceResponse<PaymentProviderOperationResult>> HandleCancelAsync(
                PaymentProviderOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(PaymentProviderOperationResult.Succeeded(
                    "Payment cancelled.",
                    recommendedState: "cancelled"));
            }

            public Task<ServiceResponse<PaymentProviderOperationResult>> HandleWebhookAsync(
                PaymentProviderOperationRequest request,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(PaymentProviderOperationResult.Succeeded(
                    "Payment captured.",
                    recommendedState: "captured"));
            }

            public Task<ServiceResponse<PaymentProviderSessionResult>> CreateHostedSessionAsync(
                CreatePaymentProviderSessionRequest request,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class StubPayPalPaymentService : IPayPalPaymentService
        {
            public Task<ServiceResponse> Pay(
                decimal totalAmount,
                IEnumerable<Product> cartProducts,
                IEnumerable<ProcessCart> carts)
            {
                return Task.FromResult(new ServiceResponse(false, "PayPal test stub does not implement payment creation."));
            }

            public Task<bool> CaptureAsync(string orderId)
            {
                return Task.FromResult(false);
            }
        }
    }
}
