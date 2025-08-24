using KwikNesta.Contracts.Models;

namespace NotificationService.Workers.Handlers
{
    public interface IMessageHandler
    {
        Task HandleAsync(NotificationMessage message);
        Task HandleAsync(AuditLog message);
    }
}
