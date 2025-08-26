using KwikNesta.Contracts.Models;
using NotificationService.Workers.Models;

namespace NotificationService.Workers.Services.Interfaces
{
    public interface IAuditService
    {
        Task<Paginator<AuditDto>> GetAuditTrails(AuditQuery query);
    }
}
