using Microsoft.Extensions.Options;
using Restless.Config;
using Restless.Interfaces;

namespace Restless.Services
{
    public class HealthCheckService : BackgroundService
    {
        private static readonly TimeSpan PingInterval = TimeSpan.FromMinutes(5);
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOptions<List<PingTarget>> targets;
        private readonly IEmailAlertService emailService;
        private readonly ILogger<HealthCheckService> logger;

        public HealthCheckService(
        IHttpClientFactory httpClientFactory,
        IOptions<List<PingTarget>> targets,
        IEmailAlertService emailService,
        ILogger<HealthCheckService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.targets = targets;
            this.emailService = emailService;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var tasks = targets.Value.Select(target => PingAsync(target, stoppingToken));
                await Task.WhenAll(tasks);


                // Main loop delay (adjust if needed)
                await Task.Delay(PingInterval, stoppingToken);
            }
        }

        public async Task PingAsync(PingTarget target, CancellationToken cancellationToken)
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
