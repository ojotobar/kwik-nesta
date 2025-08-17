using NotificationService.Workers.Models;

namespace NotificationService.Workers.Handlers
{
    public interface IMessageHandler
    {
        Task HandleAsync(EmailNotification message);
    }
}
