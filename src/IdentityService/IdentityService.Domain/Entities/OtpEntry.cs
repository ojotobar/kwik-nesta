using EFCore.CrudKit.Library.Models;

namespace IdentityService.Domain.Entities
{
    public class OtpEntry : EntityBase
    {
        public string UserId { get; set; } = string.Empty;
        public string OtpHash { get; set; } = string.Empty;
        public string OtpSalt { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
    }
}
