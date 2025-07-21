using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.X509;
using Restless.Config;
using System.Threading;

namespace Restless.Services
{
    public class HealthCheckService : BackgroundService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<HealthCheckService> logger;
        private readonly List<PingTarget> targets;
        private readonly EmailAlertService emailService;
        private static readonly TimeSpan PingInterval = TimeSpan.FromMinutes(5);

        public HealthCheckService(
            IHttpClientFactory httpClientFactory,
            IOptions<List<PingTarget>> options,
            EmailAlertService emailService,
            ILogger<HealthCheckService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.targets = options.Value;
            this.emailService = emailService;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = targets.Select(target => PingAsync(target, stoppingToken));
                await Task.WhenAll(tasks);


                // Main loop delay (adjust if needed)
                await Task.Delay(PingInterval, stoppingToken);
            }
        }

        private async Task PingAsync(PingTarget target, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();

            int attempts = 0;
            bool isUp = false;

            while (attempts < target.MaxRetries && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await client.GetAsync(target.Url);
                    if (response.IsSuccessStatusCode)
                    {
                        logger.LogInformation("{Name} responded OK ({StatusCode})", target.Name, response.StatusCode);
                        isUp = true;
                        break;
                    }

                    logger.LogWarning("{Name} responded with status {StatusCode}", target.Name, response.StatusCode);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception while pinging {Name}", target.Name);
                }

                attempts++;
                await Task.Delay(1000, cancellationToken);
            }

            if (!isUp)
            {
                logger.LogError("{Name} is DOWN after {Attempts} attempts", target.Name, target.MaxRetries);
                await emailService.SendDownAlertAsync(target);
            }
        }
    }
}
