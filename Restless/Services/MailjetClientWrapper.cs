using Mailjet.Client;
using Restless.Interfaces;

namespace Restless.Services
{
    public class MailjetClientWrapper : IMailjetClientWrapper
    {
        private readonly MailjetClient client;

        public MailjetClientWrapper(string apiKey, string apiSecret)
        {
            client = new MailjetClient(apiKey, apiSecret);
        }
        public Task<MailjetResponse> PostAsync(MailjetRequest request)
        {
            return client.PostAsync(request);
        }
    }
}
