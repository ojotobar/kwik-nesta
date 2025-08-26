using CSharpTypes.Extensions.Enumeration;
using KwikNesta.Contracts.Enums;
using KwikNesta.Contracts.Models;
using NotificationService.Workers.Models;

namespace NotificationService.Workers.Helpers
{
    internal class Utilities
    {
        public static IEnumerable<AuditDto> GetAuditData(IEnumerable<AuditLog> audits, List<UserLeanDto> users)
        {
            var query = audits
                    .Join(users, audit => audit.PerformedBy, user => user.Id,
                         (audit, performer) => new { audit, performer })
                    .Join(users, ap => ap.audit.PerformedOnProfileId, user => user.Id,
                         (ap, profile) =>
                    new AuditDto
                    {
                        PerformedBy = ap.performer != null ? $"{ap.performer.FirstName}, {ap.performer.LastName}" : "Unknown",
                        PerformedOn = profile != null ? $"{profile.FirstName}, {profile.LastName}" : "Unknown",
                        TimeStamp = ap.audit.Timestamp,
                        Action = ap.audit.Action.GetDescription(),
                        Domain = ap.audit.Domain.GetDescription()
                    });

            return query;
        }

        public static IEnumerable<AuditDto> QueryAuditData(IEnumerable<AuditLog> audits, List<UserLeanDto> users)
        {
            var query = from audit in audits
                        join performer in users on audit.PerformedBy equals performer.Id into performerGroup
                        from performer in performerGroup.DefaultIfEmpty()
                        join profile in users on audit.PerformedOnProfileId equals profile.Id into profileGroup
                        from profile in profileGroup.DefaultIfEmpty()
                        select new AuditDto
                        {
                            PerformedBy = performer != null ? string.Format("{0} {1}", performer.FirstName, performer.LastName) : "Unknown",
                            PerformedOn = profile != null && audit.Domain == AuditDomain.User && profile.Id == audit.DomainId.ToString() ? "Self" : 
                                profile != null ? string.Format("{0} {1}", profile.FirstName, profile.LastName) : "Unknown",
                            TimeStamp = audit.Timestamp,
                            Action = audit.Action.GetDescription(),
                            Domain = audit.Domain.GetDescription()
                        };

            return query;
        }
    }
}
