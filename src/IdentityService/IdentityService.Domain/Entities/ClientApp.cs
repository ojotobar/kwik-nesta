using EFCore.CrudKit.Library.Models;

namespace IdentityService.Domain.Entities
{
    public class ClientApp : EntityBase
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecretHash { get; set; } = string.Empty;
        public List<string> AllowedAudiences { get; set; } = [];
        public List<string> AllowedScopes { get; set; } = [];
        public List<string> RedirectUris { get; set; } = [];
    }
}
