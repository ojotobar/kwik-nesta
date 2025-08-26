using CSharpTypes.Extensions.Guid;
using EFCore.CrudKit.Library.Data;
using EFCore.CrudKit.Library.Data.Interfaces;
using KwikNesta.Contracts.Extensions;
using KwikNesta.Contracts.Models;
using NotificationService.Workers.Helpers;
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

        public async Task<Paginator<AuditDto>> GetAuditTrails(AuditQuery query)
        {
            var result = new Paginator<AuditDto>();

            var audits = _mongo.AsQueryable<AuditLog>(l => true)
                .Filter(query)
                .OrderByDescending(a => a.Timestamp);

            if(audits.Any())
            {
                var userIds = audits.Select(a => a.PerformedBy);
                userIds.Union(audits.Select(a => a.PerformedOnProfileId));
                var ids = new List<Guid>();
                foreach(var id in userIds)
                {
                    ids.Add(id.ToGuid());
                }

                var userQuery = new FluentQuery("SELECT Id, FirstName, LastName")
                    .From("AspNetUsers")
                    .WhereIn("Id", ids)
                    .ToQuery();

                var users = await _data.FindAsync<UserLeanDto>(userQuery);
                result = Utilities.QueryAuditData(audits, users)
                    .Paginate(query!.Page!.Value, query!.PageSize!.Value);

                return result;
            }

            return result;
        }
    }
}
