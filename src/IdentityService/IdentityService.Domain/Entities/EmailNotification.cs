using IdentityService.Domain.Enums;

namespace IdentityService.Domain.Entities
{
    public class EmailNotification
    {
        public string ReceipientName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public EmailType Type{ get; set; }
        public OtpData? Otp { get; set; }
    }

    public class OtpData
    {
        public string Value { get; set; } = string.Empty;
        public double Span { get; set; }
    }
}
