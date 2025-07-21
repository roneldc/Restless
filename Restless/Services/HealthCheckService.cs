using Microsoft.Extensions.Options;
using Restless.Config;
using Restless.Interfaces;
using System.Collections.Concurrent;

namespace Restless.Services
{
    public class HealthCheckService : BackgroundService
    {
        // Track which targets have already triggered a down alert
        private static readonly ConcurrentDictionary<string, bool> alertSentMap = new();
        private static readonly TimeSpan pingInterval = TimeSpan.FromMinutes(5);
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
                await Task.Delay(pingInterval, stoppingToken);
            }
        }

        public async Task PingAsync(PingTarget target, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();
            HttpResponseMessage? response = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= target.MaxRetries; attempt++)
            {
                try
                {
                    response = await client.GetAsync(target.Url, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        alertSentMap[target.Url] = false;
                        logger.LogInformation("{Name} responded OK ({StatusCode})", target.Name, response.StatusCode);
                        return;
                    }

                    logger.LogWarning("{Name} responded with status {StatusCode}", target.Name, response.StatusCode);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    logger.LogWarning(ex, "Attempt {Attempt} failed for {Target}", attempt, target.Name);
                }

                await Task.Delay(1000, cancellationToken);
            }

            if (!alertSentMap.GetOrAdd(target.Url, false))
            {
                await emailService.SendDownAlertAsync(target);
                alertSentMap[target.Url] = true;
                logger.LogError("Down alert sent for {Target}", target.Name);
            }
            else
            {
                logger.LogError("Down alert already sent for {Target}, skipping duplicate alert.", target.Name);
            }
        }
    }
}
