using Restless.Config;

namespace Restless.Interfaces
{
    public interface IEmailAlertService
    {
        Task SendDownAlertAsync(PingTarget target);
    }
}
