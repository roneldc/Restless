using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Restless.Config;
using Restless.Interfaces;
using Restless.Services;
using System.Net;
namespace Restless.Tests.Services
{
    public class HealthCheckServiceTests
    {
        [Fact]
        public async Task PingAsync_ShouldNotSendEmail_WhenTargetIsUp()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(handlerMock.Object);

            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var emailService = new Mock<IEmailAlertService>(); // Mock the interface
            var logger = Mock.Of<ILogger<HealthCheckService>>();

            var target = new PingTarget
            {
                Name = "Test API",
                Url = "https://api.test.com",
                MaxRetries = 3
            };

            var options = Options.Create(new List<PingTarget> { target });

            var service = new HealthCheckService(httpClientFactory.Object, options, emailService.Object, logger);

            // Act
            await service.PingAsync(target, CancellationToken.None);

            // Assert
            emailService.Verify(x => x.SendDownAlertAsync(It.IsAny<PingTarget>()), Times.Never);
        }

        [Fact]
        public async Task PingAsync_ShouldSendEmail_WhenTargetIsDown()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var emailService = new Mock<IEmailAlertService>();
            var logger = Mock.Of<ILogger<HealthCheckService>>();

            var target = new PingTarget
            {
                Name = "Test API2",
                Url = "https://api.test2.com",
                MaxRetries = 1
            };

            var options = Options.Create(new List<PingTarget> { target });

            var service = new HealthCheckService(httpClientFactory.Object, options, emailService.Object, logger);

            // Act
            await service.PingAsync(target, CancellationToken.None);

            // Assert
            emailService.Verify(x => x.SendDownAlertAsync(It.Is<PingTarget>(t => t.Url == "https://api.test2.com")), Times.Once);
        }

        [Fact]
        public async Task PingAsync_ShouldNotSendDuplicateEmails_IfServiceRemainsDown()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)); // Always down

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var emailService = new Mock<IEmailAlertService>();
            var logger = Mock.Of<ILogger<HealthCheckService>>();

            var target = new PingTarget
            {
                Name = "Test API",
                Url = "https://api.test.com",
                MaxRetries = 1
            };

            var options = Options.Create(new List<PingTarget> { target });

            var service = new HealthCheckService(httpClientFactory.Object, options, emailService.Object, logger);

            // Act – simulate multiple checks during same downtime
            await service.PingAsync(target, CancellationToken.None);
            await service.PingAsync(target, CancellationToken.None);
            await service.PingAsync(target, CancellationToken.None);

            // Assert – only one email sent
            emailService.Verify(x => x.SendDownAlertAsync(It.IsAny<PingTarget>()), Times.Once);
        }

        [Fact]
        public async Task PingAsync_ShouldResendEmail_IfServiceRecoversThenFailsAgain()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            var sequence = new Queue<HttpResponseMessage>(new[]
            {
        new HttpResponseMessage(HttpStatusCode.InternalServerError), // Down
        new HttpResponseMessage(HttpStatusCode.OK),                  // Recovered
        new HttpResponseMessage(HttpStatusCode.InternalServerError) // Down again
    });

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => sequence.Dequeue());

            var httpClient = new HttpClient(handlerMock.Object);
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var emailService = new Mock<IEmailAlertService>();
            var logger = Mock.Of<ILogger<HealthCheckService>>();

            var target = new PingTarget
            {
                Name = "Test API3",
                Url = "https://api.test3.com",
                MaxRetries = 1
            };

            var options = Options.Create(new List<PingTarget> { target });

            var service = new HealthCheckService(httpClientFactory.Object, options, emailService.Object, logger);

            // Act – 1st down -> 1st alert
            await service.PingAsync(target, CancellationToken.None);

            // Act – recovery
            await service.PingAsync(target, CancellationToken.None);

            // Act – down again -> should trigger new alert
            await service.PingAsync(target, CancellationToken.None);

            // Assert
            emailService.Verify(x => x.SendDownAlertAsync(It.IsAny<PingTarget>()), Times.Exactly(2));
        }
    }
}