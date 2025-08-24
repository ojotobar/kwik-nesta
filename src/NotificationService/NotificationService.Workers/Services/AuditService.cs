using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.Guid;
using EFCore.CrudKit.Library.Data;
using EFCore.CrudKit.Library.Data.Interfaces;
using KwikNesta.Contracts.Models;
using Microsoft.EntityFrameworkCore;
using NotificationService.Workers.Models;
using NotificationService.Workers.Services.Interfaces;

namespace NotificationService.Workers.Services
{
    public class AuditService : IAuditService
    {
        private readonly IDataForgeRawCrudKit _data;
        private readonly IEFCoreMongoCrudKit _mongo;

        public AuditService(IDataForgeRawCrudKit data, IEFCoreMongoCrudKit mongo)
        {
            _data = data;
            _mongo = mongo;
        }

        public async Task<List<AuditDto>> GetAuditTrails(int page = 1, int size = 10)
        {
            var result = new List<AuditDto>();

            var audits = _mongo.AsQueryable<AuditLog>(l => true)
                .Skip((1 - page) * size).Take(size)
                .OrderByDescending(a => a.Timestamp)
                .ToList();

            if(audits.Count > 0)
            {
                var userIds = audits.Select(a => a.PerformedBy.ToGuid());
                userIds.Union(audits.Select(a => a.DomainId));

                var userQuery = new FluentQuery("SELECT Id, FirstName, LastName")
                    .From("AspNetUsers")
                    .WhereIn("Id", userIds.Distinct().ToList())
                    .ToQuery();

                var users = await _data.FindAsync<UserLeanDto>(userQuery);
                var userDict = users.ToDictionary(u => u.Id, u => u);
                foreach(var audit in audits)
                {
                    var user = userDict.GetValueOrDefault(audit.PerformedBy);
                    var domain = userDict.GetValueOrDefault(audit.DomainId.ToString());
                    result.Add(new AuditDto
                    {
                        PerformedBy = user != null ? $"{user.LastName}, {user.FirstName}" : "Unknown",
                        PerformedOn = domain != null ? $"{domain.LastName}, {domain.FirstName}" : "Unknown",
                        TimeStamp = audit.Timestamp,
                        Action = audit.Action.GetDescription(),
                        Domain = audit.Domain.GetDescription()
                    });
                }
            }

            return result;
        }
    }
}
