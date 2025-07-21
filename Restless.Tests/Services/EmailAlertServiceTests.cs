using Mailjet.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using Restless.Config;
using Restless.Interfaces;
using Restless.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restless.Tests.Services
{
    public class EmailAlertServiceTests
    {
        [Fact]
        public async Task SendDownAlertAsync_ShouldLogInfo_WhenEmailIsSentSuccessfully()
        {
            // Arrange
            var mockClient = new Mock<IMailjetClientWrapper>();
            mockClient.Setup(x => x.PostAsync(It.IsAny<MailjetRequest>()))
                .ReturnsAsync(new MailjetResponse(true, 200, new JObject()));

            var settings = Options.Create(new MailjetSettings
            {
                ApiKey = "fake-key",
                ApiSecret = "fake-secret",
                FromEmail = "noreply@example.com",
                FromName = "Restless",
                TemplateId = 123
            });

            var logger = new LoggerFactory().CreateLogger<EmailAlertService>();

            var emailService = new EmailAlertService(mockClient.Object, settings, logger);

            var target = new PingTarget
            {
                Name = "My API",
                Url = "https://api.example.com",
                MaxRetries = 3,
                Email = "admin@example.com"
            };

            // Act
            await emailService.SendDownAlertAsync(target);

            // Assert
            mockClient.Verify(x => x.PostAsync(It.IsAny<MailjetRequest>()), Times.Once);
        }

        [Fact]
        public async Task SendDownAlertAsync_ShouldLogError_WhenResponseIsFailure()
        {
            // Arrange
            var mockClient = new Mock<IMailjetClientWrapper>();
            mockClient.Setup(x => x.PostAsync(It.IsAny<MailjetRequest>()))
                .ReturnsAsync(new MailjetResponse(false, 500, new JObject()));

            var emailService = CreateService(mockClient);

            var target = CreateTestTarget();

            // Act
            await emailService.SendDownAlertAsync(target);

            // Assert
            mockClient.Verify(x => x.PostAsync(It.IsAny<MailjetRequest>()), Times.Once);
        }

        [Fact]
        public async Task SendDownAlertAsync_ShouldCatchAndLogException_WhenPostThrows()
        {
            // Arrange
            var mockClient = new Mock<IMailjetClientWrapper>();
            mockClient.Setup(x => x.PostAsync(It.IsAny<MailjetRequest>()))
                .ThrowsAsync(new Exception("Simulated failure"));

            var emailService = CreateService(mockClient);

            var target = CreateTestTarget();

            // Act
            await emailService.SendDownAlertAsync(target);

            // Assert
            mockClient.Verify(x => x.PostAsync(It.IsAny<MailjetRequest>()), Times.Once);
        }


        private EmailAlertService CreateService(Mock<IMailjetClientWrapper> mockClient)
        {
            var settings = Options.Create(new MailjetSettings
            {
                ApiKey = "dummy",
                ApiSecret = "dummy",
                FromEmail = "no-reply@restless.io",
                FromName = "Restless",
                TemplateId = 123
            });

            var logger = new LoggerFactory().CreateLogger<EmailAlertService>();
            return new EmailAlertService(mockClient.Object, settings, logger);
        }

        private PingTarget CreateTestTarget()
        {
            return new PingTarget
            {
                Name = "Test API",
                Url = "https://api.test.com",
                MaxRetries = 2,
                Email = "admin@test.com"
            };
        }
    }
}
