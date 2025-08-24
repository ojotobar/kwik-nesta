using NotificationService.Workers.Models;

namespace NotificationService.Workers.Services.Interfaces
{
    public interface IAuditService
    {
        Task<List<AuditDto>> GetAuditTrails(int page = 1, int size = 10);
    }
}
