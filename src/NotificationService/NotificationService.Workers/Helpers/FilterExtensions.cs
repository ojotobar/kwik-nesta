using KwikNesta.Contracts.Models;
using NotificationService.Workers.Models;

namespace NotificationService.Workers.Helpers
{
    public static class FilterExtensions
    {
        public static IQueryable<AuditLog> Filter(this IQueryable<AuditLog> auditLogs, AuditQuery pageDto)
        {
            if (pageDto.Action.HasValue)
            {
                auditLogs = auditLogs.Where(au => au.Action == pageDto.Action.Value);
            }
            if (pageDto.Domain.HasValue)
            {
                auditLogs = auditLogs.Where(au => au.Domain == pageDto.Domain.Value);
            }
            return auditLogs.Where(au => au.Timestamp >= pageDto.StartTime && au.Timestamp <= pageDto.EndTime);
        }

        public static IEnumerable<AuditDto> Search(this IEnumerable<AuditDto> auditLogs, string? searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return auditLogs;
            }

            return auditLogs.Where(l => l.PerformedOn.Contains(searchString, StringComparison.OrdinalIgnoreCase) || l.PerformedBy.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }
    }
}
