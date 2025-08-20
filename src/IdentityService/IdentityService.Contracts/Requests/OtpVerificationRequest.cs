using System.Text.Json.Serialization;

namespace IdentityService.Contracts.Requests
{
    public class OtpVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsValid => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Otp);
    }
}
