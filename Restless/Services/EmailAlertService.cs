using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Restless.Config;
using Restless.Interfaces;

namespace Restless.Services
{
    public class EmailAlertService : IEmailAlertService
    {
        private readonly IMailjetClientWrapper client;
        private readonly MailjetSettings settings;
        private readonly ILogger<EmailAlertService> logger;

        public EmailAlertService(IMailjetClientWrapper client,
        IOptions<MailjetSettings> options,
        ILogger<EmailAlertService> logger)
        {
            this.client = client;
            this.settings = options.Value;
            this.logger = logger;
        }

        public async Task SendDownAlertAsync(PingTarget target)
        {
            var request = new MailjetRequest
            {
                Resource = SendV31.Resource
            }
            .Property(Send.Messages, new JArray {
            new JObject {
                {"From", new JObject {
                    {"Email", settings.FromEmail},
                    {"Name", settings.FromName}
                }},
                {"To", new JArray {
                    new JObject {
                        {"Email", target.Email},
                        {"Name", target.Name}
                    }
                }},
                { "TemplateID", settings.TemplateId },
                { "TemplateLanguage", true },
                { "Subject", $"🌀 [Restless] Alert: Your system {target.Name} is unreachable" },
                { "Variables", new JObject {
                    {"maxRetries", target.MaxRetries},
                    {"url", target.Url},
                    {"name", target.Name}
                }}
            }
            });

            try
            {
                var response = await client.PostAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Mailjet send failed: {StatusCode} {Content}", response.StatusCode, response.GetErrorMessage());
                }
                else
                {
                    logger.LogInformation("Alert email sent to {Email}", target.Email);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending alert email for {Name}", target.Name);
            }
        }
    }
}
