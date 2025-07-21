using Mailjet.Client;

namespace Restless.Interfaces
{
    public interface IMailjetClientWrapper
    {
        Task<MailjetResponse> PostAsync(MailjetRequest request);
    }
}
