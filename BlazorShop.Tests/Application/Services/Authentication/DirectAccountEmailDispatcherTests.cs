namespace BlazorShop.Tests.Application.Services.Authentication
{
    using BlazorShop.Application.Services.Authentication;
    using BlazorShop.Application.Services.Contracts.Authentication;
    using BlazorShop.Domain.Contracts;

    using Moq;

    using Xunit;

    public sealed class DirectAccountEmailDispatcherTests
    {
        [Fact]
        public async Task SendActivationAsync_SendsCompatibilityEmail()
        {
            var emailService = new Mock<IEmailService>();
            var dispatcher = new DirectAccountEmailDispatcher(emailService.Object);

            var result = await dispatcher.SendActivationAsync(
                new AccountEmailDispatchRequest(
                    "customer@example.test",
                    "Customer Name",
                    "https://store.example/confirm?token=abc",
                    "user-id"));

            Assert.True(result.Success);
            emailService.Verify(
                email => email.SendEmailAsync(
                    "customer@example.test",
                    "Confirm your email",
                    It.Is<string>(body => body.Contains("https://store.example/confirm?token=abc", StringComparison.Ordinal))),
                Times.Once);
        }

        [Fact]
        public async Task SendPasswordRecoveryAsync_SendsCompatibilityEmail()
        {
            var emailService = new Mock<IEmailService>();
            var dispatcher = new DirectAccountEmailDispatcher(emailService.Object);

            var result = await dispatcher.SendPasswordRecoveryAsync(
                new AccountEmailDispatchRequest(
                    "customer@example.test",
                    "Customer Name",
                    "https://store.example/reset?token=abc",
                    "user-id"));

            Assert.True(result.Success);
            emailService.Verify(
                email => email.SendEmailAsync(
                    "customer@example.test",
                    "Reset your password",
                    It.Is<string>(body => body.Contains("https://store.example/reset?token=abc", StringComparison.Ordinal))),
                Times.Once);
        }
    }
}
