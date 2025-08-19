using IdentityService.Domain.Enums;

namespace IdentityService.Contracts.Requests
{
    public class OtpResendRequest
    {
        public string Email { get; set; } = string.Empty;
        public OtpType Type { get; set; } = OtpType.AccountVerification;
    }
}
