using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities
{
    public class ActionLogs
    {
        public string EntityId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ActionLogType ActionType { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
