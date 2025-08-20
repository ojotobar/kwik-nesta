using IdentityService.Domain.Enums;

namespace IdentityService.Contracts.Requests
{
    public class UserSuspensionRequest
    {
        public string UserId { get; set; } = string.Empty;
        public SuspensionReasons Reason { get; set; }
    }
}
