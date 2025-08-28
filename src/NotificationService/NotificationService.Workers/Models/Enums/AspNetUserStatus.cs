using System.ComponentModel;

namespace NotificationService.Workers.Models.Enums
{
    public enum AspNetUserStatus
    {
        [Description("Pending Verification")]
        PendingVerification,
        [Description("Active")]
        Active,
        [Description("Deactivated")]
        Deactivated,
        [Description("Suspended")]
        Suspended
    }
}
