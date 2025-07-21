

using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Restless.Config;

namespace Restless.Services
{
    public class EmailAlertService
    {
        private readonly MailjetClient client;
        private readonly MailjetSettings settings;
        private readonly ILogger<EmailAlertService> logger;

        public EmailAlertService(IOptions<MailjetSettings> options, ILogger<EmailAlertService> logger)
        {
            settings = options.Value;
            client = new MailjetClient(settings.ApiKey, settings.ApiSecret);
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
                  {"Email", "johnroneldc@gmail.com"},
                  {"Name", "Restless Monitoring"}
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
